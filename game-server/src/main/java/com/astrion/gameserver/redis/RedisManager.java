package com.astrion.gameserver.redis;

import com.astrion.common.model.Position;
import io.lettuce.core.RedisClient;
import io.lettuce.core.api.StatefulRedisConnection;
import io.lettuce.core.api.sync.RedisCommands;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class RedisManager {

    private static final Logger log = LoggerFactory.getLogger(RedisManager.class);

    private final RedisClient client;
    private final StatefulRedisConnection<String, String> connection;
    private final RedisCommands<String, String> commands;

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
        this.commands = connection.sync();
        log.info("Connected to Redis at {}:{} ({})",
            host, port, (pw == null || pw.isEmpty()) ? "no auth" : "AUTH set");
    }

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
