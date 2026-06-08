package com.astrion.gameserver.redis;

import com.astrion.common.model.Position;
import io.lettuce.core.RedisClient;
import io.lettuce.core.api.StatefulRedisConnection;
import io.lettuce.core.api.async.RedisAsyncCommands;
import io.lettuce.core.api.sync.RedisCommands;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.concurrent.CompletableFuture;
import java.util.concurrent.atomic.AtomicLong;

public class RedisManager {

    private static final Logger log = LoggerFactory.getLogger(RedisManager.class);

    private final RedisClient client;
    private final StatefulRedisConnection<String, String> connection;
    private final RedisCommands<String, String> commands;
    private final RedisAsyncCommands<String, String> asyncCommands;

    /// Slow-call threshold. A single Redis round-trip should land in single-
    /// digit ms; anything above this is worth flagging. Picked low enough to
    /// surface real outliers, high enough that a busy GC pause + network jitter
    /// doesn't produce noise. Tunable via ASTRION_REDIS_SLOW_MS env var if
    /// the prod baseline shifts.
    private static final long SLOW_REDIS_THRESHOLD_MS;
    static {
        long v = 100L;
        String env = System.getenv("ASTRION_REDIS_SLOW_MS");
        if (env != null) try { v = Long.parseLong(env); } catch (Exception ignored) {}
        SLOW_REDIS_THRESHOLD_MS = v;
    }
    private static final AtomicLong slowCallCount = new AtomicLong();
    public static long getSlowCallCount() { return slowCallCount.get(); }

    public RedisManager(String host, int port) {
        // Lettuce URL accepts ':password@' in the userinfo segment for AUTH.
        // We pick it up from ASTRION_REDIS_PASSWORD; an empty/absent value
        // gives the unauthenticated URL, which is the same shape that worked
        // before requirepass was added on the Redis side. Lets local dev
        // boxes without a password keep running.
        String pw = System.getenv("ASTRION_REDIS_PASSWORD");
        String url = (pw == null || pw.isEmpty())
            ? "redis://" + host + ":" + port
            : "redis://:" + pw + "@" + host + ":" + port;
        this.client = RedisClient.create(url);
        this.connection = client.connect();
        // Both views share the same multiplexed connection — Lettuce pipelines
        // async commands over the wire automatically. Sync calls block the
        // caller waiting for the reply; async returns RedisFuture that
        // completes on Lettuce's event loop.
        this.commands = connection.sync();
        this.asyncCommands = connection.async();
        log.info("Connected to Redis at {}:{} ({}) — slow-call threshold {} ms",
            host, port, (pw == null || pw.isEmpty()) ? "no auth" : "AUTH set",
            SLOW_REDIS_THRESHOLD_MS);
    }

    /// Wraps an async command with elapsed-time tracking. Anything over the
    /// threshold gets logged once and bumps the slow-call counter exposed on
    /// /metrics. Lambdas only allocate when needed; sub-threshold calls add
    /// just one whenComplete callback to Lettuce's normal completion path.
    private <T> CompletableFuture<T> tracked(String op, CompletableFuture<T> future) {
        long startNs = System.nanoTime();
        return future.whenComplete((value, err) -> {
            long ms = (System.nanoTime() - startNs) / 1_000_000L;
            if (err != null) {
                log.warn("[redis] {} failed after {}ms: {}", op, ms, err.getMessage());
            } else if (ms >= SLOW_REDIS_THRESHOLD_MS) {
                slowCallCount.incrementAndGet();
                log.warn("[slow-redis] {} took {}ms", op, ms);
            }
        });
    }

    /// Convenience to drive multiple parallel async commands and continue
    /// once they all complete. Equivalent to CompletableFuture.allOf but
    /// types-through the result tuple — easier to read at call sites.
    public static <A, B> CompletableFuture<Pair<A, B>> both(
            CompletableFuture<A> a, CompletableFuture<B> b) {
        return CompletableFuture.allOf(a, b)
            .thenApply(ignored -> new Pair<>(a.join(), b.join()));
    }

    public record Pair<A, B>(A first, B second) {}

    // Player position cache
    public void updatePlayerPosition(String playerId, Position pos) {
        String key = "player:pos:" + playerId;
        commands.hset(key, "x", String.valueOf(pos.getX()));
        commands.hset(key, "y", String.valueOf(pos.getY()));
        commands.hset(key, "z", String.valueOf(pos.getZ()));
    }

    public Position getPlayerPosition(String playerId) {
        String key = "player:pos:" + playerId;
        String x = commands.hget(key, "x");
        String y = commands.hget(key, "y");
        String z = commands.hget(key, "z");

        if (x == null) return null;

        return new Position(Float.parseFloat(x), Float.parseFloat(y), Float.parseFloat(z));
    }

    // Player online status
    public void setPlayerOnline(String playerId) {
        commands.sadd("players:online", playerId);
    }

    public void setPlayerOffline(String playerId) {
        commands.srem("players:online", playerId);
        commands.del("player:pos:" + playerId);
    }

    public java.util.Set<String> getOnlinePlayers() {
        return commands.smembers("players:online");
    }

    // Character storage
    public void saveCharacter(String accountId, String charName, String charClass, int level) {
        String key = "characters:" + accountId;
        // Store as JSON in a list
        String charJson = "{\"name\":\"" + charName + "\",\"className\":\"" + charClass + "\",\"level\":" + level + "}";
        commands.rpush(key, charJson);
    }

    public java.util.List<String> getCharacters(String accountId) {
        String key = "characters:" + accountId;
        return commands.lrange(key, 0, -1);
    }

    public boolean deleteCharacter(String accountId, String charName) {
        String key = "characters:" + accountId;
        var chars = commands.lrange(key, 0, -1);
        for (String charJson : chars) {
            if (charJson.contains("\"name\":\"" + charName + "\"")) {
                commands.lrem(key, 1, charJson);
                return true;
            }
        }
        return false;
    }

    public boolean characterExists(String accountId, String charName) {
        var chars = getCharacters(accountId);
        for (String charJson : chars) {
            if (charJson.contains("\"name\":\"" + charName + "\"")) {
                return true;
            }
        }
        return false;
    }

    // Per-player game state (quest progress, collected items, etc.) — stored as JSON blob
    // ──── Player state JSON ──────────────────────────────────────────────
    // The blob behind STATE_SAVE / STATE_DATA. Trade/auction/achievement
    // read-modify-write it under PlayerStateLocks. The async variants are
    // the primary API now — sync wrappers stay for cold paths that don't
    // benefit from overlap.

    public CompletableFuture<String> getPlayerStateAsync(String playerId) {
        return tracked("getPlayerState",
            asyncCommands.get("player:state:" + playerId).toCompletableFuture());
    }

    public CompletableFuture<String> savePlayerStateAsync(String playerId, String json) {
        return tracked("savePlayerState",
            asyncCommands.set("player:state:" + playerId, json).toCompletableFuture());
    }

    public void savePlayerState(String playerId, String json) {
        commands.set("player:state:" + playerId, json);
    }

    public String getPlayerState(String playerId) {
        return commands.get("player:state:" + playerId);
    }

    // Generic key-value
    public void set(String key, String value) {
        commands.set(key, value);
    }

    public String get(String key) {
        return commands.get(key);
    }

    // Friends — mutual relationship stored as two parallel Redis sets so a
    // lookup of 'who are A's friends' is O(N) without scanning anyone else.
    // friends:{user} is the source of truth; the addFriend/removeFriend pair
    // keeps both sides consistent.
    public void addFriendBoth(String a, String b) {
        commands.sadd("friends:" + a, b);
        commands.sadd("friends:" + b, a);
    }
    public void removeFriendBoth(String a, String b) {
        commands.srem("friends:" + a, b);
        commands.srem("friends:" + b, a);
    }
    public java.util.Set<String> getFriends(String username) {
        return commands.smembers("friends:" + username);
    }
    public int friendCount(String username) {
        return commands.scard("friends:" + username).intValue();
    }
    public boolean areFriends(String a, String b) {
        return commands.sismember("friends:" + a, b);
    }

    // Friend requests — pending invites. Two parallel sets so each side
    // can see what's pending without scanning the other. A sends to B:
    //   friend_req_out:A <- B    (A's perspective: 'I asked B')
    //   friend_req_in :B <- A    (B's perspective: 'A asked me')
    public void addFriendRequest(String from, String to) {
        commands.sadd("friend_req_out:" + from, to);
        commands.sadd("friend_req_in:"  + to,   from);
    }
    public void removeFriendRequest(String from, String to) {
        commands.srem("friend_req_out:" + from, to);
        commands.srem("friend_req_in:"  + to,   from);
    }
    public java.util.Set<String> incomingRequests(String username) {
        return commands.smembers("friend_req_in:" + username);
    }
    public java.util.Set<String> outgoingRequests(String username) {
        return commands.smembers("friend_req_out:" + username);
    }
    public boolean hasIncomingRequest(String user, String from) {
        return commands.sismember("friend_req_in:" + user, from);
    }
    public boolean hasOutgoingRequest(String user, String to) {
        return commands.sismember("friend_req_out:" + user, to);
    }

    // Party storage. A party has a stable id (UUID-ish, generated on first
    // accept) and a member set; each member also stores their partyId
    // back-pointer so 'what party am I in?' is one read. The leader is
    // tracked separately so promotion on leader-leave is a single SREM
    // + SET pair. Invites are kept per-recipient with a 60s TTL so stale
    // ones don't linger forever if the inviter disappears.
    private static final long PARTY_INVITE_TTL_SEC = 60L;

    public String getPartyOf(String username) {
        return commands.get("party_of:" + username);
    }
    public void setPartyOf(String username, String partyId) {
        commands.set("party_of:" + username, partyId);
    }
    public void clearPartyOf(String username) {
        commands.del("party_of:" + username);
    }
    public java.util.Set<String> getPartyMembers(String partyId) {
        return commands.smembers("party_members:" + partyId);
    }
    public long partyMemberCount(String partyId) {
        return commands.scard("party_members:" + partyId);
    }
    public void addPartyMember(String partyId, String username) {
        commands.sadd("party_members:" + partyId, username);
    }
    public void removePartyMember(String partyId, String username) {
        commands.srem("party_members:" + partyId, username);
    }
    public String getPartyLeader(String partyId) {
        return commands.get("party_leader:" + partyId);
    }
    public void setPartyLeader(String partyId, String username) {
        commands.set("party_leader:" + partyId, username);
    }
    public void deleteParty(String partyId) {
        commands.del("party_members:" + partyId);
        commands.del("party_leader:" + partyId);
    }
    // Invites: party_inv:{recipient} → set of inviter usernames. We also
    // store the partyId alongside via party_inv_to:{recipient}:{inviter}
    // so an accept knows which party to drop into.
    public void addPartyInvite(String recipient, String inviter, String partyId) {
        commands.sadd("party_inv:" + recipient, inviter);
        commands.setex("party_inv_to:" + recipient + ":" + inviter, PARTY_INVITE_TTL_SEC, partyId);
        commands.expire("party_inv:" + recipient, PARTY_INVITE_TTL_SEC);
    }
    public boolean hasPartyInvite(String recipient, String inviter) {
        return commands.sismember("party_inv:" + recipient, inviter);
    }
    public String getInvitedPartyId(String recipient, String inviter) {
        return commands.get("party_inv_to:" + recipient + ":" + inviter);
    }
    public void removePartyInvite(String recipient, String inviter) {
        commands.srem("party_inv:" + recipient, inviter);
        commands.del("party_inv_to:" + recipient + ":" + inviter);
    }

    // ──── Auction house ──────────────────────────────────────────────────
    // auction:{id}         → HASH (id, seller, itemId, qty, price, createdAt, expiresAt)
    // auction:active       → ZSET scored by createdAt (most recent first when
    //                         we ZREVRANGE) — single global ordering
    // auction:seller:{u}   → SET of auction ids the user owns
    // auction:next_id      → INCR counter for unique numeric ids
    public long nextAuctionId() { return commands.incr("auction:next_id"); }
    public void saveAuction(String id, java.util.Map<String, String> fields) {
        commands.hmset("auction:" + id, fields);
    }
    public java.util.Map<String, String> getAuction(String id) {
        return commands.hgetall("auction:" + id);
    }
    public void deleteAuction(String id) {
        commands.del("auction:" + id);
    }
    public void addActiveAuction(String id, long createdAtMs) {
        commands.zadd("auction:active", (double) createdAtMs, id);
    }
    public void removeActiveAuction(String id) {
        commands.zrem("auction:active", id);
    }
    public java.util.List<String> recentAuctions(int n) {
        // Latest first — ZREVRANGE gives high-score → low-score
        return commands.zrevrange("auction:active", 0, n - 1);
    }
    public void addSellerAuction(String seller, String id) {
        commands.sadd("auction:seller:" + seller, id);
    }
    public void removeSellerAuction(String seller, String id) {
        commands.srem("auction:seller:" + seller, id);
    }
    public java.util.Set<String> sellerAuctions(String seller) {
        return commands.smembers("auction:seller:" + seller);
    }

    // ──── Achievements ──────────────────────────────────────────────────
    // ach:{user}       → SET<id> of unlocked achievement ids
    // ach_zones:{user} → SET<zoneId> of city zones the user has entered
    //                    (only cities counted; field zones are ignored)
    public boolean unlockAchievement(String user, String id) {
        return commands.sadd("ach:" + user, id) == 1L;
    }
    public boolean isAchievementUnlocked(String user, String id) {
        return commands.sismember("ach:" + user, id);
    }
    public java.util.Set<String> getUnlockedAchievements(String user) {
        return commands.smembers("ach:" + user);
    }
    /// True iff this was a first-time visit (added to the set).
    public boolean recordCityVisit(String user, String zoneId) {
        return commands.sadd("ach_zones:" + user, zoneId) == 1L;
    }
    public long cityVisitCount(String user) {
        return commands.scard("ach_zones:" + user);
    }

    // ──── Blocklist (per-user set) ───────────────────────────────────────
    // blocks:{user} → set of usernames the user has muted/blocked. Block is
    // one-directional: 'A blocks B' means B can't whisper/party/trade A,
    // and B's zone chat is hidden from A's screen. A can still talk to B —
    // hiding goes both ways only if both sides block each other.
    public void addBlock(String user, String target) {
        commands.sadd("blocks:" + user, target);
    }
    public void removeBlock(String user, String target) {
        commands.srem("blocks:" + user, target);
    }
    public boolean isBlocked(String user, String target) {
        return commands.sismember("blocks:" + user, target);
    }
    public java.util.Set<String> getBlocks(String user) {
        return commands.smembers("blocks:" + user);
    }
    public int blockCount(String user) {
        return commands.scard("blocks:" + user).intValue();
    }

    // ──── Rankings (sorted sets) ────────────────────────────────────────
    // Three leaderboards, each scored by a single int. ZADD is idempotent
    // so re-sending the same level/gold is cheap; kill count uses ZINCRBY
    // since the server only knows the delta per kill, not the total.
    //
    //   ranking:level  → score = character level
    //   ranking:gold   → score = current gold balance
    //   ranking:kills  → score = total monster kills (lifetime)
    private static String rankingKey(String category) {
        return "ranking:" + category;
    }
    public void updateRankingScore(String category, String username, long score) {
        if (username == null || username.isEmpty()) return;
        commands.zadd(rankingKey(category), score, username);
    }
    public long incrementRankingScore(String category, String username, long delta) {
        if (username == null || username.isEmpty()) return 0L;
        return commands.zincrby(rankingKey(category), delta, username).longValue();
    }
    /// Top N descending — index 0 is the leader. Each tuple is (name, score).
    public java.util.List<io.lettuce.core.ScoredValue<String>> getRankingTop(String category, int n) {
        if (n <= 0) return java.util.Collections.emptyList();
        return commands.zrevrangeWithScores(rankingKey(category), 0, n - 1);
    }
    /// 0-based rank, -1 when the user is unranked.
    public long getRankingRank(String category, String username) {
        Long r = commands.zrevrank(rankingKey(category), username);
        return r == null ? -1L : r;
    }
    public Double getRankingScore(String category, String username) {
        return commands.zscore(rankingKey(category), username);
    }

    // Low-level passthroughs. Used by AccountLockout (and any future feature
    // that needs counters / TTL keys outside the dedicated DTO accessors above).
    public long incr(String key)                       { return commands.incr(key); }
    public void expire(String key, long seconds)       { commands.expire(key, seconds); }
    public long ttl(String key)                        { return commands.ttl(key); }
    public boolean exists(String key)                  { return commands.exists(key) > 0; }
    public void setex(String key, long sec, String v)  { commands.setex(key, sec, v); }
    public void del(String key)                        { commands.del(key); }

    public void shutdown() {
        connection.close();
        client.shutdown();
        log.info("Redis connection closed");
    }
}
