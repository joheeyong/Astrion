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

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/// In-memory P2P trade sessions. One Session per ongoing trade; both
/// participants share the same Session object indexed by username so any
/// packet handler can resolve the live state from a channel.
///
/// State transitions:
///   request    → push TRADE_REQUEST_FROM to target
///   accept     → both get TRADE_OPEN + initial TRADE_STATE
///   offer/gold → mutate the offerer's side, both sides unlock, broadcast state
///   lock       → mark the locker side locked; when both locked, the
///                client can show 'press 확정 to finalise'
///   confirm    → first confirm waits; both confirmed → execute (read both
///                inventories from Redis, validate, mutate, save) → push
///                TRADE_RESULT to both, kill the session
///   cancel     → push TRADE_RESULT(success=false) to both, kill session
///   disconnect → identical to cancel
public class TradeManager {

    private static final Logger log = LoggerFactory.getLogger(TradeManager.class);
    private static final ObjectMapper mapper = new ObjectMapper();
    private static final int SLOTS_PER_SIDE = 4;

    public static class Slot {
        public String itemId = "";
        public int qty = 0;
        public boolean isEmpty() { return itemId == null || itemId.isEmpty() || qty <= 0; }
    }

    public static class Session {
        public final String a;
        public final String b;
        public final Slot[] aOffer = new Slot[SLOTS_PER_SIDE];
        public final Slot[] bOffer = new Slot[SLOTS_PER_SIDE];
        public long aGold = 0L, bGold = 0L;
        public boolean aLocked, bLocked;
        public boolean aConfirmed, bConfirmed;
        public Session(String a, String b) {
            this.a = a; this.b = b;
            for (int i = 0; i < SLOTS_PER_SIDE; i++) { aOffer[i] = new Slot(); bOffer[i] = new Slot(); }
        }
        public boolean isParticipant(String u) { return a.equals(u) || b.equals(u); }
        public Slot[] sideOf(String u) { return a.equals(u) ? aOffer : bOffer; }
    }

    private final WorldManager world;
    private final RedisManager redis;
    private final PlayerStateLocks locks;
    private AchievementManager achievements; // injected post-construction
    private final Map<String, Session> sessionsByUser = new ConcurrentHashMap<>();
    // Pending request invites: target → inviter. Only the latest is kept;
    // a fresh invite overrides any prior one. Cleared on accept/reject/timeout.
    private final Map<String, String> pendingInvites = new ConcurrentHashMap<>();

    public TradeManager(WorldManager world, RedisManager redis, PlayerStateLocks locks) {
        this.world = world;
        this.redis = redis;
        this.locks = locks;
    }
    public void setAchievementManager(AchievementManager a) { this.achievements = a; }

    public void requestTrade(String inviter, String target) {
        if (target == null || target.isEmpty() || target.equals(inviter)) {
            sendError(inviter, "잘못된 대상입니다.");
            return;
        }
        if (sessionsByUser.containsKey(inviter)) { sendError(inviter, "이미 거래 중입니다."); return; }
        if (sessionsByUser.containsKey(target))  { sendError(inviter, target + " 님은 이미 다른 거래 중입니다."); return; }
        PlayerSession ts = world.getSessionByPlayerId(target);
        if (ts == null) { sendError(inviter, target + " 님은 접속 중이 아닙니다."); return; }
        if (redis.get("account:" + target) == null) { sendError(inviter, "그런 모험가는 없습니다."); return; }
        if (redis.isBlocked(inviter, target) || redis.isBlocked(target, inviter)) {
            sendError(inviter, "해당 사용자에게 거래를 신청할 수 없습니다.");
            return;
        }

        pendingInvites.put(target, inviter);
        push(ts.getChannel(), PacketType.TRADE_REQUEST_FROM,
            "{\"from\":\"" + esc(inviter) + "\"}");
    }

    public void acceptTrade(String accepter, String inviter) {
        String pending = pendingInvites.get(accepter);
        if (pending == null || !pending.equals(inviter)) {
            sendError(accepter, "유효한 거래 신청이 없습니다.");
            return;
        }
        pendingInvites.remove(accepter);
        PlayerSession aSess = world.getSessionByPlayerId(inviter);
        PlayerSession bSess = world.getSessionByPlayerId(accepter);
        if (aSess == null || bSess == null) { sendError(accepter, "상대방이 접속 중이 아닙니다."); return; }
        if (sessionsByUser.containsKey(inviter) || sessionsByUser.containsKey(accepter)) {
            sendError(accepter, "이미 거래 중입니다.");
            return;
        }

        Session s = new Session(inviter, accepter);
        sessionsByUser.put(inviter, s);
        sessionsByUser.put(accepter, s);
        push(aSess.getChannel(), PacketType.TRADE_OPEN, "{\"partner\":\"" + esc(accepter) + "\"}");
        push(bSess.getChannel(), PacketType.TRADE_OPEN, "{\"partner\":\"" + esc(inviter) + "\"}");
        broadcastState(s);
    }

    public void rejectTrade(String rejecter, String inviter) {
        pendingInvites.remove(rejecter);
        // Quiet rejection — no return notification by design.
    }

    public void setOffer(String user, int slot, String itemId, int qty) {
        Session s = sessionsByUser.get(user);
        if (s == null) return;
        if (slot < 0 || slot >= SLOTS_PER_SIDE) return;
        Slot[] side = s.sideOf(user);
        side[slot].itemId = (qty <= 0 || itemId == null) ? "" : itemId;
        side[slot].qty    = (qty <= 0 || itemId == null) ? 0 : qty;
        // Any offer change unlocks both — the partner can't be staring at a
        // stale snapshot when they hit 확정.
        s.aLocked = false; s.bLocked = false;
        s.aConfirmed = false; s.bConfirmed = false;
        broadcastState(s);
    }

    public void setGold(String user, long gold) {
        Session s = sessionsByUser.get(user);
        if (s == null) return;
        gold = Math.max(0L, gold);
        if (s.a.equals(user)) s.aGold = gold; else s.bGold = gold;
        s.aLocked = false; s.bLocked = false;
        s.aConfirmed = false; s.bConfirmed = false;
        broadcastState(s);
    }

    public void lock(String user) {
        Session s = sessionsByUser.get(user);
        if (s == null) return;
        if (s.a.equals(user)) s.aLocked = true; else s.bLocked = true;
        broadcastState(s);
    }

    public void unlock(String user) {
        Session s = sessionsByUser.get(user);
        if (s == null) return;
        if (s.a.equals(user)) { s.aLocked = false; s.aConfirmed = false; }
        else                  { s.bLocked = false; s.bConfirmed = false; }
        broadcastState(s);
    }

    public void confirm(String user) {
        Session s = sessionsByUser.get(user);
        if (s == null) return;
        if (!s.aLocked || !s.bLocked) {
            sendError(user, "양쪽 모두 잠금해야 확정할 수 있습니다.");
            return;
        }
        if (s.a.equals(user)) s.aConfirmed = true; else s.bConfirmed = true;
        if (s.aConfirmed && s.bConfirmed) {
            execute(s);
        } else {
            broadcastState(s);
        }
    }

    public void cancel(String user, String reason) {
        Session s = sessionsByUser.remove(user);
        if (s == null) return;
        sessionsByUser.remove(s.a);
        sessionsByUser.remove(s.b);
        notifyResult(s, false, reason, null, null, 0, 0);
    }

    /// Hooked by GamePacketHandler.channelInactive — a disconnect mid-trade
    /// has to look like a cancel to the other side.
    public void onDisconnect(String user) {
        if (sessionsByUser.containsKey(user)) cancel(user, "상대방의 연결이 끊어졌습니다.");
        pendingInvites.entrySet().removeIf(e -> e.getValue().equals(user) || e.getKey().equals(user));
    }

    /// Both sides have confirmed — atomically swap inventories via the
    /// Redis-persisted state JSON. Either side's inventory missing the
    /// offered items aborts cleanly with TRADE_RESULT(success=false).
    /// Wrapped in a two-player lock so a parallel AuctionManager.buy
    /// (or another trade involving either side) can't race the JSON
    /// read-modify-write.
    private void execute(Session s) {
        locks.withLocks(s.a, s.b, () -> executeLocked(s));
    }

    private void executeLocked(Session s) {
        try {
            // Parallel reads — both player state blobs are independent so
            // overlapping their round-trips halves the read latency from
            // 2×RTT to 1×RTT. Lock is still held; only the *waits* run
            // in parallel inside Lettuce, not the JSON mutations below.
            var both = com.astrion.gameserver.redis.RedisManager.both(
                redis.getPlayerStateAsync(s.a),
                redis.getPlayerStateAsync(s.b)).join();
            String aJson = both.first()  == null ? "{}" : both.first();
            String bJson = both.second() == null ? "{}" : both.second();
            ObjectNode aNode = (ObjectNode) mapper.readTree(aJson);
            ObjectNode bNode = (ObjectNode) mapper.readTree(bJson);

            List<Slot> aGiving = nonEmpty(s.aOffer);
            List<Slot> bGiving = nonEmpty(s.bOffer);

            long aHave = goldOf(aNode);
            long bHave = goldOf(bNode);

            // Validate both sides hold what they're offering.
            if (s.aGold > aHave) { abort(s, s.a + " 의 골드가 부족합니다."); return; }
            if (s.bGold > bHave) { abort(s, s.b + " 의 골드가 부족합니다."); return; }
            if (!inventoryContains(aNode, aGiving)) { abort(s, s.a + " 의 인벤토리가 부족합니다."); return; }
            if (!inventoryContains(bNode, bGiving)) { abort(s, s.b + " 의 인벤토리가 부족합니다."); return; }

            // Apply: remove giving, add receiving, swap gold.
            removeItems(aNode, aGiving);
            removeItems(bNode, bGiving);
            addItems(aNode, bGiving);
            addItems(bNode, aGiving);
            setGold(aNode, aHave - s.aGold + s.bGold);
            setGold(bNode, bHave - s.bGold + s.aGold);

            // Parallel writes — same logic as the parallel reads above.
            // join() blocks until both SETs ACK so the rest of the function
            // (notifyResult, achievements) only fires after persistence.
            com.astrion.gameserver.redis.RedisManager.both(
                redis.savePlayerStateAsync(s.a, mapper.writeValueAsString(aNode)),
                redis.savePlayerStateAsync(s.b, mapper.writeValueAsString(bNode))).join();

            notifyResult(s, true, "거래 성공", bGiving, aGiving, s.bGold, s.aGold);
            if (achievements != null) {
                achievements.onTradeCompleted(s.a);
                achievements.onTradeCompleted(s.b);
            }
            sessionsByUser.remove(s.a);
            sessionsByUser.remove(s.b);
            log.info("Trade {} <-> {} executed: {} items / {} gold each way",
                s.a, s.b, aGiving.size() + bGiving.size(), s.aGold + s.bGold);
        } catch (Exception e) {
            log.warn("Trade execute failed for {}<->{}: {}", s.a, s.b, e.getMessage());
            abort(s, "내부 오류로 거래가 취소되었습니다.");
        }
    }

    private void abort(Session s, String reason) {
        sessionsByUser.remove(s.a);
        sessionsByUser.remove(s.b);
        notifyResult(s, false, reason, null, null, 0, 0);
    }

    private void notifyResult(Session s, boolean success, String msg,
                               List<Slot> aGains, List<Slot> bGains,
                               long aGainGold, long bGainGold) {
        PlayerSession aSess = world.getSessionByPlayerId(s.a);
        PlayerSession bSess = world.getSessionByPlayerId(s.b);
        if (aSess != null) push(aSess.getChannel(), PacketType.TRADE_RESULT, resultJson(success, msg, aGains, aGainGold));
        if (bSess != null) push(bSess.getChannel(), PacketType.TRADE_RESULT, resultJson(success, msg, bGains, bGainGold));
    }

    private String resultJson(boolean success, String msg, List<Slot> gains, long gainGold) {
        ObjectNode n = mapper.createObjectNode();
        n.put("success", success);
        n.put("message", msg);
        n.put("gainedGold", gainGold);
        ArrayNode arr = n.putArray("gainedItems");
        if (gains != null) for (Slot g : gains) {
            ObjectNode item = mapper.createObjectNode();
            item.put("id", g.itemId);
            item.put("qty", g.qty);
            arr.add(item);
        }
        try { return mapper.writeValueAsString(n); }
        catch (Exception e) { return "{\"success\":false,\"message\":\"\"}"; }
    }

    private void broadcastState(Session s) {
        String json = stateJson(s);
        PlayerSession aSess = world.getSessionByPlayerId(s.a);
        PlayerSession bSess = world.getSessionByPlayerId(s.b);
        if (aSess != null) push(aSess.getChannel(), PacketType.TRADE_STATE, json);
        if (bSess != null) push(bSess.getChannel(), PacketType.TRADE_STATE, json);
    }

    private String stateJson(Session s) {
        ObjectNode n = mapper.createObjectNode();
        n.put("a", s.a);
        n.put("b", s.b);
        n.put("aGold", s.aGold);
        n.put("bGold", s.bGold);
        n.put("aLocked", s.aLocked);
        n.put("bLocked", s.bLocked);
        n.put("aConfirmed", s.aConfirmed);
        n.put("bConfirmed", s.bConfirmed);
        ArrayNode aArr = n.putArray("aOffer");
        for (Slot sl : s.aOffer) {
            ObjectNode o = mapper.createObjectNode();
            o.put("itemId", sl.itemId == null ? "" : sl.itemId);
            o.put("qty", sl.qty);
            aArr.add(o);
        }
        ArrayNode bArr = n.putArray("bOffer");
        for (Slot sl : s.bOffer) {
            ObjectNode o = mapper.createObjectNode();
            o.put("itemId", sl.itemId == null ? "" : sl.itemId);
            o.put("qty", sl.qty);
            bArr.add(o);
        }
        try { return mapper.writeValueAsString(n); }
        catch (Exception e) { return "{}"; }
    }

    // ── inventory helpers ─────────────────────────────────────────────────
    // Thin List<Slot> adapters over the shared PlayerStateJson utility —
    // the actual array arithmetic lives there (tested in PlayerStateJsonTest).

    private static long goldOf(ObjectNode state) { return PlayerStateJson.goldOf(state); }
    private static void setGold(ObjectNode state, long gold) { PlayerStateJson.setGold(state, gold); }

    private static Map<String, Integer> needOf(List<Slot> slots) {
        Map<String, Integer> need = new HashMap<>();
        for (Slot g : slots) need.merge(g.itemId, g.qty, Integer::sum);
        return need;
    }

    private static boolean inventoryContains(ObjectNode state, List<Slot> giving) {
        return PlayerStateJson.contains(state, needOf(giving));
    }

    private static void removeItems(ObjectNode state, List<Slot> giving) {
        PlayerStateJson.removeItems(state, needOf(giving));
    }

    private static void addItems(ObjectNode state, List<Slot> gained) {
        for (Slot g : gained) PlayerStateJson.addItem(state, g.itemId, g.qty);
    }

    private static List<Slot> nonEmpty(Slot[] arr) {
        List<Slot> out = new ArrayList<>(arr.length);
        for (Slot s : arr) if (!s.isEmpty()) out.add(s);
        return out;
    }

    private void sendError(String user, String msg) {
        PlayerSession s = world.getSessionByPlayerId(user);
        if (s == null) return;
        push(s.getChannel(), PacketType.TRADE_ERROR,
            "{\"message\":\"" + esc(msg) + "\"}");
    }

    private static void push(Channel ch, PacketType type, String payload) {
        try { ch.writeAndFlush(new GamePacket(type, payload)); }
        catch (Exception ignored) {}
    }

    private static String esc(String s) {
        if (s == null) return "";
        return s.replace("\\", "\\\\").replace("\"", "\\\"");
    }
}
