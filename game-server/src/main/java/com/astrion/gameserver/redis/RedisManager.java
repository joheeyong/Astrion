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
        this.client = RedisClient.create("redis://" + host + ":" + port);
        this.connection = client.connect();
        this.commands = connection.sync();
        log.info("Connected to Redis at {}:{}", host, port);
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

    // Generic key-value
    public void set(String key, String value) {
        commands.set(key, value);
    }

    public String get(String key) {
        return commands.get(key);
    }

    public void shutdown() {
        connection.close();
        client.shutdown();
        log.info("Redis connection closed");
    }
}
