package com.astrion.gameserver.world;

import com.astrion.common.packet.GamePacket;
import com.astrion.common.packet.PacketType;
import com.astrion.gameserver.redis.RedisManager;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ArrayNode;
import com.fasterxml.jackson.databind.node.ObjectNode;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/// Server-authoritative achievements. Hooks at the existing event points
/// (level/gold change, kill count change, friend list change, party join,
/// trade execute, whisper send, city enter) and unlocks any def whose
/// threshold is reached. Unlock pushes ACHIEVEMENT_UNLOCK + grants the
/// reward by direct Redis state mutation (same approach TradeManager.execute
/// uses to swap inventories).
///
/// Def list is hardcoded and *must* mirror the client's AchievementDatabase.
/// Bump the version suffix on a def id if you change its target or reward
/// so re-checks don't think a player has already claimed the older one.
public class AchievementManager {

    private static final Logger log = LoggerFactory.getLogger(AchievementManager.class);
    private static final ObjectMapper mapper = new ObjectMapper();

    public enum Kind { LEVEL, KILLS, GOLD, FRIENDS, PARTY, TRADE, WHISPER, CITIES }

    public record Def(String id, String displayName, String description,
                      Kind kind, long target,
                      String rewardItemId, int rewardQty) {}

    public static final Def[] ALL = new Def[] {
        // Progression — character level
        new Def("LV_5",   "첫 발걸음",     "캐릭터 Lv 5 도달",          Kind.LEVEL, 5,    "stardust", 50),
        new Def("LV_10",  "모험가",       "캐릭터 Lv 10 도달",         Kind.LEVEL, 10,   "stardust", 100),
        new Def("LV_30",  "베테랑",       "캐릭터 Lv 30 도달",         Kind.LEVEL, 30,   "stardust", 500),
        new Def("LV_50",  "챔피언",       "캐릭터 Lv 50 도달",         Kind.LEVEL, 50,   "stardust", 1000),

        // Combat — kill count (ranking:kills mirror)
        new Def("KILL_100",   "사냥꾼",     "몬스터 100마리 처치",     Kind.KILLS, 100,   "stardust", 50),
        new Def("KILL_1000",  "숙련 사냥꾼", "몬스터 1,000마리 처치",   Kind.KILLS, 1000,  "stardust", 200),
        new Def("KILL_10000", "전설의 사냥꾼", "몬스터 10,000마리 처치", Kind.KILLS, 10000, "stardust", 1000),

        // Wealth — gold balance
        new Def("GOLD_10K",  "부자",   "10,000 골드 보유",   Kind.GOLD, 10_000,  "stardust", 50),
        new Def("GOLD_100K", "부호",   "100,000 골드 보유",  Kind.GOLD, 100_000, "stardust", 200),

        // Social — relationships built up via the systems we shipped
        new Def("FRIEND_1",   "사교가",    "친구 1명 만들기",        Kind.FRIENDS, 1,  "stardust", 50),
        new Def("FRIEND_10",  "인기인",    "친구 10명 만들기",       Kind.FRIENDS, 10, "stardust", 200),
        new Def("PARTY_FIRST", "동행",     "첫 파티 합류",            Kind.PARTY,   1,  "stardust", 50),
        new Def("TRADE_FIRST", "첫 거래",  "거래 1회 성사",           Kind.TRADE,   1,  "stardust", 50),
        new Def("WHISPER_FIRST","속삭임",  "귓속말 1회 보내기",       Kind.WHISPER, 1,  "stardust", 20),

        // Exploration
        new Def("CITIES_ALL", "세계여행자", "5개 도시 모두 방문",     Kind.CITIES,  5,  "stardust", 500),
    };

    private final WorldManager world;
    private final RedisManager redis;
    private final PlayerStateLocks locks;

    public AchievementManager(WorldManager world, RedisManager redis, PlayerStateLocks locks) {
        this.world = world;
        this.redis = redis;
        this.locks = locks;
    }

    public Def[] defs() { return ALL; }

    public void onLevelChanged(String user, long level)   { checkNumeric(user, Kind.LEVEL, level); }
    public void onKillsChanged(String user, long kills)   { checkNumeric(user, Kind.KILLS, kills); }
    public void onGoldChanged(String user, long gold)     { checkNumeric(user, Kind.GOLD, gold); }
    public void onFriendCount(String user, long count)    { checkNumeric(user, Kind.FRIENDS, count); }
    public void onPartyJoined(String user)                { checkNumeric(user, Kind.PARTY, 1L); }
    public void onTradeCompleted(String user)             { checkNumeric(user, Kind.TRADE, 1L); }
    public void onWhisperSent(String user)                { checkNumeric(user, Kind.WHISPER, 1L); }

    /// Returns true if the city was a new visit (caller can re-check the
    /// CITIES_ALL achievement). Called from handleZoneEnter; only city
    /// zones should be passed in.
    public void onCityEntered(String user, String zoneId) {
        boolean fresh = redis.recordCityVisit(user, zoneId);
        if (!fresh) return;
        long count = redis.cityVisitCount(user);
        checkNumeric(user, Kind.CITIES, count);
    }

    private void checkNumeric(String user, Kind kind, long current) {
        for (Def d : ALL) {
            if (d.kind != kind) continue;
            if (current < d.target) continue;
            if (!redis.unlockAchievement(user, d.id)) continue; // already had it
            grantReward(user, d);
            push(user, d);
            log.info("Achievement [{}] unlocked by {} (reward: {}x {})",
                d.id, user, d.rewardQty, d.rewardItemId);
        }
    }

    /// Append rewardItemId × rewardQty to the player's stored inventory.
    /// Same Redis-JSON manipulation TradeManager uses on execute — no
    /// merging into existing stacks, just a new entry appended; the
    /// client's RestoreFromState compacts on next load.
    /// Wrapped in the user's lock — reentrant when called from inside an
    /// already-locked path (TradeManager.execute, AuctionManager.buy etc.).
    private void grantReward(String user, Def d) {
        if (d.rewardItemId == null || d.rewardItemId.isEmpty() || d.rewardQty <= 0) return;
        locks.withLock(user, () -> grantRewardLocked(user, d));
    }

    private void grantRewardLocked(String user, Def d) {
        try {
            String json = redis.getPlayerState(user);
            if (json == null) json = "{}";
            ObjectNode state = (ObjectNode) mapper.readTree(json);
            PlayerStateJson.addItem(state, d.rewardItemId, d.rewardQty);
            redis.savePlayerState(user, mapper.writeValueAsString(state));
        } catch (Exception e) {
            log.warn("grantReward failed for {} {}: {}", user, d.id, e.getMessage());
        }
    }

    private void push(String user, Def d) {
        PlayerSession s = world.getSessionByPlayerId(user);
        if (s == null) return;
        try {
            ObjectNode n = mapper.createObjectNode();
            n.put("id", d.id);
            n.put("displayName", d.displayName);
            n.put("description", d.description);
            n.put("rewardItemId", d.rewardItemId);
            n.put("rewardQty", d.rewardQty);
            s.getChannel().writeAndFlush(new GamePacket(PacketType.ACHIEVEMENT_UNLOCK, mapper.writeValueAsString(n)));
        } catch (Exception ignored) {}
    }

    /// Snapshot — list of unlocked ids + the current progress counters so
    /// the client UI can render progress bars on still-locked achievements.
    public void sendList(io.netty.channel.Channel ch, String user, long currentKills, long currentLevel, long currentGold, long currentFriends) {
        try {
            ObjectNode n = mapper.createObjectNode();
            ArrayNode arr = n.putArray("unlocked");
            for (String id : redis.getUnlockedAchievements(user)) arr.add(id);
            ObjectNode prog = n.putObject("progress");
            prog.put("level",  currentLevel);
            prog.put("kills",  currentKills);
            prog.put("gold",   currentGold);
            prog.put("friends", currentFriends);
            prog.put("cities", redis.cityVisitCount(user));
            ch.writeAndFlush(new GamePacket(PacketType.ACHIEVEMENT_LIST_DATA, mapper.writeValueAsString(n)));
        } catch (Exception e) {
            log.warn("sendList for {} failed: {}", user, e.getMessage());
        }
    }
}
