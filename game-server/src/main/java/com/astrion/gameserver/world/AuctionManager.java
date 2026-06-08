package com.astrion.gameserver.world;

import com.astrion.common.packet.GamePacket;
import com.astrion.common.packet.PacketType;
import com.astrion.gameserver.redis.RedisManager;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ArrayNode;
import com.fasterxml.jackson.databind.node.ObjectNode;
import io.netty.channel.Channel;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;

/// Global asynchronous marketplace. One queue across every city — the
/// Auctioneer NPC in any town opens the same listings. Item is held by
/// the auction (removed from seller's stored state) until either bought
/// or returned by cancel/expiry.
///
/// Pricing: buyer pays {price} in full. Seller receives 95% of {price}
/// (a 5% house fee absorbs the silver sink that asynchronous markets
/// otherwise create). v1 has no bidding — fixed-price buyout only.
///
/// Storage layout in Redis (see RedisManager) — id is a global increment
/// counter, indexed in a single ZSET sorted by createdAt for fast 'latest
/// listings' fetch, plus per-seller SET for cancel/reclaim lookups.
public class AuctionManager {

    private static final Logger log = LoggerFactory.getLogger(AuctionManager.class);
    private static final ObjectMapper mapper = new ObjectMapper();

    private static final int MAX_LISTINGS_PER_SELLER = 10;
    private static final int MAX_RESULTS = 50;
    private static final long DEFAULT_DURATION_MS = 24 * 3600_000L; // 24h
    private static final long MAX_DURATION_MS = 7 * 24 * 3600_000L; // 1 week
    private static final long MAX_PRICE = 1_000_000_000L;
    private static final long MAX_QTY = 999L;
    private static final float HOUSE_FEE = 0.05f;

    private final WorldManager world;
    private final RedisManager redis;
    private final PlayerStateLocks locks;
    private final ScheduledExecutorService sweeper;

    public AuctionManager(WorldManager world, RedisManager redis, PlayerStateLocks locks) {
        this.world = world;
        this.redis = redis;
        this.locks = locks;
        this.sweeper = Executors.newSingleThreadScheduledExecutor(r -> {
            Thread t = new Thread(r, "auction-sweep");
            t.setDaemon(true);
            return t;
        });
        // Background expiry sweep — unsold listings return their items to
        // the seller. Every minute is plenty; expiry granularity isn't
        // visible to users and bounding sweep cost matters more.
        sweeper.scheduleAtFixedRate(this::sweepExpired, 30, 60, TimeUnit.SECONDS);
    }

    public void shutdown() { sweeper.shutdownNow(); }

    // ──────────────────────── REGISTER ────────────────────────

    public void register(String seller, String itemId, int qty, long price, long durationHours) {
        locks.withLock(seller, () -> registerLocked(seller, itemId, qty, price, durationHours));
    }

    private void registerLocked(String seller, String itemId, int qty, long price, long durationHours) {
        if (itemId == null || itemId.isEmpty()) { error(seller, "잘못된 아이템입니다.", "register"); return; }
        if (qty <= 0 || qty > MAX_QTY) { error(seller, "수량이 잘못되었습니다.", "register"); return; }
        if (price <= 0 || price > MAX_PRICE) { error(seller, "가격이 잘못되었습니다.", "register"); return; }
        long duration = durationHours > 0
            ? Math.min(durationHours * 3600_000L, MAX_DURATION_MS)
            : DEFAULT_DURATION_MS;

        if (redis.sellerAuctions(seller).size() >= MAX_LISTINGS_PER_SELLER) {
            error(seller, "매물이 너무 많습니다 (최대 " + MAX_LISTINGS_PER_SELLER + ").", "register");
            return;
        }

        // Pull the seller's stored inventory, verify they hold the item,
        // remove it. Same JSON-mutation approach TradeManager.execute uses.
        try {
            String json = redis.getPlayerState(seller);
            if (json == null) json = "{}";
            ObjectNode state = (ObjectNode) mapper.readTree(json);
            if (!inventoryContains(state, itemId, qty)) {
                error(seller, "인벤토리에 아이템이 부족합니다.", "register");
                return;
            }
            removeFromInventory(state, itemId, qty);
            redis.savePlayerState(seller, mapper.writeValueAsString(state));
        } catch (Exception e) {
            log.warn("auction register: read/modify inventory failed for {}: {}", seller, e.getMessage());
            error(seller, "내부 오류로 등록할 수 없습니다.", "register");
            return;
        }

        long now = System.currentTimeMillis();
        long expiresAt = now + duration;
        String id = String.valueOf(redis.nextAuctionId());

        Map<String, String> fields = new HashMap<>();
        fields.put("id", id);
        fields.put("seller", seller);
        fields.put("itemId", itemId);
        fields.put("qty", String.valueOf(qty));
        fields.put("price", String.valueOf(price));
        fields.put("createdAt", String.valueOf(now));
        fields.put("expiresAt", String.valueOf(expiresAt));
        redis.saveAuction(id, fields);
        redis.addActiveAuction(id, now);
        redis.addSellerAuction(seller, id);

        ok(seller, "매물 등록 완료", "register");
        pushList(seller); // refresh the seller's view
        log.info("Auction registered [{}]: {} {}x{} @ {}g (exp {}ms)", id, seller, itemId, qty, price, duration);
    }

    // ──────────────────────── BUY ────────────────────────

    public void buy(String buyer, String auctionId) {
        // Peek to learn the seller without touching state; the real read +
        // mutation happens under the two-player lock to guarantee no other
        // op on either side races us.
        Map<String, String> peek = redis.getAuction(auctionId);
        if (peek == null || peek.isEmpty()) { error(buyer, "이미 판매되었거나 만료되었습니다.", "buy"); return; }
        String peekSeller = peek.get("seller");
        if (peekSeller == null) { error(buyer, "잘못된 매물입니다.", "buy"); return; }
        if (peekSeller.equals(buyer)) { error(buyer, "본인의 매물은 살 수 없습니다.", "buy"); return; }
        locks.withLocks(buyer, peekSeller, () -> buyLocked(buyer, auctionId));
    }

    private void buyLocked(String buyer, String auctionId) {
        Map<String, String> a = redis.getAuction(auctionId);
        if (a == null || a.isEmpty()) { error(buyer, "이미 판매되었거나 만료되었습니다.", "buy"); return; }
        String seller = a.get("seller");
        if (seller == null) { error(buyer, "잘못된 매물입니다.", "buy"); return; }
        if (seller.equals(buyer)) { error(buyer, "본인의 매물은 살 수 없습니다.", "buy"); return; }

        String itemId = a.get("itemId");
        int qty = parseInt(a.get("qty"), 0);
        long price = parseLong(a.get("price"), 0);
        long expiresAt = parseLong(a.get("expiresAt"), 0);
        if (qty <= 0 || price <= 0) { error(buyer, "잘못된 매물입니다.", "buy"); return; }
        if (expiresAt > 0 && expiresAt < System.currentTimeMillis()) {
            // Expired but not yet swept — sweep now and refuse.
            returnItemToSeller(seller, itemId, qty);
            cleanupAuction(auctionId, seller);
            error(buyer, "이미 만료된 매물입니다.", "buy");
            return;
        }

        try {
            // Parallel reads — buyer state and seller state are independent,
            // round-trips overlap inside Lettuce. Halves the read latency
            // on the buy-path; the locks above still serialise concurrent
            // ops on either side.
            var bothStates = com.astrion.gameserver.redis.RedisManager.both(
                redis.getPlayerStateAsync(buyer),
                redis.getPlayerStateAsync(seller)).join();
            String buyerJson  = bothStates.first()  == null ? "{}" : bothStates.first();
            String sellerJson = bothStates.second() == null ? "{}" : bothStates.second();
            ObjectNode buyerState  = (ObjectNode) mapper.readTree(buyerJson);
            ObjectNode sellerState = (ObjectNode) mapper.readTree(sellerJson);
            long buyerGold = goldOf(buyerState);
            if (buyerGold < price) { error(buyer, "골드가 부족합니다.", "buy"); return; }

            // Buyer: pay gold, gain item.
            setGold(buyerState, buyerGold - price);
            addToInventory(buyerState, itemId, qty);

            // Seller: receive 95% of price. Updates their stored gold so
            // it lands whether they're online or not; if online, push them
            // a fresh STATE_DATA so the HUD updates without log-out.
            long sellerGain = Math.round(price * (1f - HOUSE_FEE));
            setGold(sellerState, goldOf(sellerState) + sellerGain);

            // Parallel writes — both saves go out together; we only resume
            // once both ACKs land.
            com.astrion.gameserver.redis.RedisManager.both(
                redis.savePlayerStateAsync(buyer,  mapper.writeValueAsString(buyerState)),
                redis.savePlayerStateAsync(seller, mapper.writeValueAsString(sellerState))).join();

            cleanupAuction(auctionId, seller);
            ok(buyer, "구매 성공", "buy");
            pushStateRefresh(buyer);
            // Notify seller asynchronously — they can be elsewhere or offline.
            PlayerSession sellerSess = world.getSessionByPlayerId(seller);
            if (sellerSess != null) {
                ok(seller, "[경매] 판매되었습니다 (+" + sellerGain + " G)", "sold");
                pushStateRefresh(seller);
                pushList(seller);
            }
            log.info("Auction sold [{}]: {} -> {} @ {}g (seller +{}g)",
                auctionId, buyer, seller, price, sellerGain);
        } catch (Exception e) {
            log.warn("auction buy failed for {}: {}", buyer, e.getMessage());
            error(buyer, "내부 오류로 구매할 수 없습니다.", "buy");
        }
    }

    // ──────────────────────── CANCEL ────────────────────────

    public void cancel(String user, String auctionId) {
        locks.withLock(user, () -> cancelLocked(user, auctionId));
    }

    private void cancelLocked(String user, String auctionId) {
        Map<String, String> a = redis.getAuction(auctionId);
        if (a == null || a.isEmpty()) { error(user, "이미 판매되었거나 만료되었습니다.", "cancel"); return; }
        String seller = a.get("seller");
        if (seller == null || !seller.equals(user)) {
            error(user, "본인의 매물만 취소할 수 있습니다.", "cancel");
            return;
        }
        String itemId = a.get("itemId");
        int qty = parseInt(a.get("qty"), 0);
        returnItemToSeller(seller, itemId, qty);
        cleanupAuction(auctionId, seller);
        ok(user, "매물 취소", "cancel");
        pushList(user);
        pushStateRefresh(user);
        log.info("Auction cancelled [{}] by {}", auctionId, seller);
    }

    // ──────────────────────── LIST ────────────────────────

    public void sendList(Channel ch, String requester) {
        try {
            List<String> ids = redis.recentAuctions(MAX_RESULTS);
            ObjectNode root = mapper.createObjectNode();
            ArrayNode arr = root.putArray("entries");
            long now = System.currentTimeMillis();
            for (String id : ids) {
                Map<String, String> a = redis.getAuction(id);
                if (a == null || a.isEmpty()) continue;
                long exp = parseLong(a.get("expiresAt"), 0);
                if (exp > 0 && exp < now) continue; // hide stale, sweeper will clean
                ObjectNode e = arr.addObject();
                e.put("id", a.getOrDefault("id", id));
                e.put("seller", a.getOrDefault("seller", ""));
                e.put("itemId", a.getOrDefault("itemId", ""));
                e.put("qty", parseInt(a.get("qty"), 0));
                e.put("price", parseLong(a.get("price"), 0));
                e.put("expiresAt", exp);
                e.put("mine", a.getOrDefault("seller", "").equals(requester));
            }
            ch.writeAndFlush(new GamePacket(PacketType.AUCTION_LIST_DATA, mapper.writeValueAsString(root)));
        } catch (Exception e) {
            log.warn("sendList failed: {}", e.getMessage());
        }
    }

    private void pushList(String user) {
        PlayerSession s = world.getSessionByPlayerId(user);
        if (s != null) sendList(s.getChannel(), user);
    }

    // ──────────────────────── SWEEP ────────────────────────

    private void sweepExpired() {
        try {
            long now = System.currentTimeMillis();
            List<String> ids = redis.recentAuctions(MAX_RESULTS * 4); // wider window
            for (String id : ids) {
                Map<String, String> peek = redis.getAuction(id);
                if (peek == null || peek.isEmpty()) { redis.removeActiveAuction(id); continue; }
                long exp = parseLong(peek.get("expiresAt"), 0);
                if (exp <= 0 || exp > now) continue;
                String seller = peek.get("seller");
                // Each expiry refund runs under the seller's lock so a
                // concurrent register/buy/cancel can't see a half-applied
                // sweep state.
                locks.withLock(seller, () -> sweepOne(id, seller));
            }
        } catch (Exception e) {
            log.warn("auction sweep failed: {}", e.getMessage());
        }
    }

    private void sweepOne(String id, String seller) {
        Map<String, String> a = redis.getAuction(id);
        if (a == null || a.isEmpty()) return;
        long exp = parseLong(a.get("expiresAt"), 0);
        if (exp <= 0 || exp > System.currentTimeMillis()) return;
        String itemId = a.get("itemId");
        int qty = parseInt(a.get("qty"), 0);
        returnItemToSeller(seller, itemId, qty);
        cleanupAuction(id, seller);
        log.info("Auction expired [{}] returned to {} ({} x{})", id, seller, itemId, qty);
        PlayerSession s = world.getSessionByPlayerId(seller);
        if (s != null) {
            ok(seller, "[경매] 만료 — 아이템 반환됨", "expired");
            pushStateRefresh(seller);
            pushList(seller);
        }
    }

    // ──────────────────── helpers ────────────────────

    private void cleanupAuction(String id, String seller) {
        redis.removeActiveAuction(id);
        redis.deleteAuction(id);
        if (seller != null) redis.removeSellerAuction(seller, id);
    }

    private void returnItemToSeller(String seller, String itemId, int qty) {
        if (seller == null || itemId == null || qty <= 0) return;
        try {
            String json = redis.getPlayerState(seller);
            if (json == null) json = "{}";
            ObjectNode state = (ObjectNode) mapper.readTree(json);
            addToInventory(state, itemId, qty);
            redis.savePlayerState(seller, mapper.writeValueAsString(state));
        } catch (Exception e) {
            log.warn("returnItem to {} failed: {}", seller, e.getMessage());
        }
    }

    private void ok(String user, String message, String action) {
        PlayerSession s = world.getSessionByPlayerId(user);
        if (s == null) return;
        try {
            ObjectNode n = mapper.createObjectNode();
            n.put("success", true);
            n.put("message", message);
            n.put("action", action);
            s.getChannel().writeAndFlush(new GamePacket(PacketType.AUCTION_RESULT, mapper.writeValueAsString(n)));
        } catch (Exception ignored) {}
    }

    private void error(String user, String message, String action) {
        PlayerSession s = world.getSessionByPlayerId(user);
        if (s == null) return;
        try {
            ObjectNode n = mapper.createObjectNode();
            n.put("success", false);
            n.put("message", message);
            n.put("action", action);
            s.getChannel().writeAndFlush(new GamePacket(PacketType.AUCTION_RESULT, mapper.writeValueAsString(n)));
        } catch (Exception ignored) {}
    }

    /// Push the latest persisted state back to a client so their UI reflects
    /// inventory / gold changes the auction just made server-side.
    private void pushStateRefresh(String user) {
        PlayerSession s = world.getSessionByPlayerId(user);
        if (s == null) return;
        String json = redis.getPlayerState(user);
        if (json == null) json = "{}";
        try {
            s.getChannel().writeAndFlush(new GamePacket(PacketType.STATE_DATA, json));
        } catch (Exception ignored) {}
    }

    // ──────────────────── JSON inventory helpers ────────────────────

    private static long goldOf(ObjectNode state) {
        return state.has("gold") ? state.get("gold").asLong() : 0L;
    }
    private static void setGold(ObjectNode state, long gold) { state.put("gold", gold); }

    private static boolean inventoryContains(ObjectNode state, String itemId, int qty) {
        JsonNode ids = state.get("inventoryItemIds");
        JsonNode qts = state.get("inventoryQuantities");
        if (ids == null || !ids.isArray() || qts == null || !qts.isArray()) return false;
        int n = Math.min(ids.size(), qts.size());
        int have = 0;
        for (int i = 0; i < n; i++) {
            if (itemId.equals(ids.get(i).asText(""))) have += qts.get(i).asInt(0);
            if (have >= qty) return true;
        }
        return false;
    }

    private static void removeFromInventory(ObjectNode state, String itemId, int qty) {
        ArrayNode ids = (ArrayNode) state.get("inventoryItemIds");
        ArrayNode qts = (ArrayNode) state.get("inventoryQuantities");
        if (ids == null || qts == null) return;
        int need = qty;
        int n = Math.min(ids.size(), qts.size());
        for (int i = 0; i < n && need > 0; i++) {
            if (!itemId.equals(ids.get(i).asText(""))) continue;
            int have = qts.get(i).asInt(0);
            int take = Math.min(have, need);
            int left = have - take;
            ids.set(i, mapper.getNodeFactory().textNode(left > 0 ? itemId : ""));
            qts.set(i, mapper.getNodeFactory().numberNode(left));
            need -= take;
        }
    }

    private static void addToInventory(ObjectNode state, String itemId, int qty) {
        ArrayNode ids = state.has("inventoryItemIds") && state.get("inventoryItemIds").isArray()
            ? (ArrayNode) state.get("inventoryItemIds")
            : state.putArray("inventoryItemIds");
        ArrayNode qts = state.has("inventoryQuantities") && state.get("inventoryQuantities").isArray()
            ? (ArrayNode) state.get("inventoryQuantities")
            : state.putArray("inventoryQuantities");
        ids.add(itemId);
        qts.add(qty);
    }

    private static int parseInt(String s, int fallback) {
        try { return s == null ? fallback : Integer.parseInt(s); } catch (Exception e) { return fallback; }
    }
    private static long parseLong(String s, long fallback) {
        try { return s == null ? fallback : Long.parseLong(s); } catch (Exception e) { return fallback; }
    }
}
