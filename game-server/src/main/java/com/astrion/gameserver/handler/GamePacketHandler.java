package com.astrion.gameserver.handler;

import com.astrion.common.model.Position;
import com.astrion.common.packet.GamePacket;
import com.astrion.common.packet.PacketType;
import com.astrion.gameserver.redis.RedisManager;
import com.astrion.gameserver.world.MonsterManager;
import com.astrion.gameserver.world.PlayerSession;
import com.astrion.gameserver.world.WorldManager;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import io.netty.channel.ChannelHandlerContext;
import io.netty.channel.SimpleChannelInboundHandler;
import io.netty.handler.timeout.IdleStateEvent;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;

public class GamePacketHandler extends SimpleChannelInboundHandler<GamePacket> {

    private static final Logger log = LoggerFactory.getLogger(GamePacketHandler.class);
    private static final ObjectMapper mapper = new ObjectMapper();
    private static final float BROADCAST_RANGE = 100f;

    private final WorldManager worldManager;
    private final RedisManager redisManager;
    private final MonsterManager monsterManager;

    public GamePacketHandler(WorldManager worldManager, RedisManager redisManager, MonsterManager monsterManager) {
        this.worldManager = worldManager;
        this.redisManager = redisManager;
        this.monsterManager = monsterManager;
    }

    @Override
    protected void channelRead0(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        switch (packet.getType()) {
            case LOGIN -> handleLogin(ctx, packet);
            case MOVE -> handleMove(ctx, packet);
            case CHAT -> handleChat(ctx, packet);
            case ATTACK -> handleAttack(ctx, packet);
            case CHARACTER_LIST -> handleCharacterList(ctx, packet);
            case CHARACTER_CREATE -> handleCharacterCreate(ctx, packet);
            case CHARACTER_DELETE -> handleCharacterDelete(ctx, packet);
            case STATE_REQUEST -> handleStateRequest(ctx, packet);
            case STATE_SAVE -> handleStateSave(ctx, packet);
            case ZONE_ENTER -> handleZoneEnter(ctx, packet);
            case MONSTER_HIT -> handleMonsterHit(ctx, packet);
            case SKILL_CAST -> handleSkillCast(ctx, packet);
            default -> log.warn("Unhandled packet type: {}", packet.getType());
        }
    }

    private void handleSkillCast(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        JsonNode node = mapper.readTree(packet.getPayload());
        float x = (float) node.get("x").asDouble();
        float y = (float) node.get("y").asDouble();
        int dir = node.has("dir") ? node.get("dir").asInt() : 1;
        String skillType = node.has("type") ? node.get("type").asText() : "starbolt";
        String broadcastPayload = mapper.writeValueAsString(
            new SkillCastBroadcast(session.getPlayerId(), x, y, dir, skillType));
        worldManager.broadcastToZone(session.getZoneId(),
            new GamePacket(PacketType.SKILL_BROADCAST, broadcastPayload));
    }

    record SkillCastBroadcast(String playerId, float x, float y, int dir, String type) {}

    private void handleZoneEnter(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        JsonNode node = mapper.readTree(packet.getPayload());
        String newZone = node.has("zoneId") ? node.get("zoneId").asText() : "";
        String oldZone = session.getZoneId();

        if (!java.util.Objects.equals(oldZone, newZone)) {
            // Tell players in old zone this one is gone
            if (oldZone != null && !oldZone.isEmpty()) {
                String despawnData = "{\"playerId\":\"" + session.getPlayerId() + "\"}";
                worldManager.broadcastToZone(oldZone,
                    new GamePacket(PacketType.DESPAWN_PLAYER, despawnData));
            }
            session.setZoneId(newZone);
            // Announce this player to the new zone
            String spawnData = mapper.writeValueAsString(new SpawnData(session.getPlayerId(), session.getPosition()));
            worldManager.broadcastToZone(newZone,
                new GamePacket(PacketType.SPAWN_PLAYER, spawnData));
            // Send back a snapshot of existing players in this zone (so the new arrival sees them)
            sendPlayerSnapshot(session);
        }

        log.info("Player {} entered zone: {}", session.getPlayerId(), newZone);
        monsterManager.onPlayerEnteredZone(session);
    }

    private void sendPlayerSnapshot(PlayerSession self) throws Exception {
        String selfId = self.getPlayerId();
        String zoneId = self.getZoneId();
        if (zoneId == null || zoneId.isEmpty()) return;
        for (PlayerSession other : worldManager.getAllSessions()) {
            if (other.getPlayerId().equals(selfId)) continue;
            if (!zoneId.equals(other.getZoneId())) continue;
            String spawnData = mapper.writeValueAsString(new SpawnData(other.getPlayerId(), other.getPosition()));
            self.getChannel().writeAndFlush(new GamePacket(PacketType.SPAWN_PLAYER, spawnData));
        }
    }

    private void handleMonsterHit(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        JsonNode node = mapper.readTree(packet.getPayload());
        String monsterId = node.get("id").asText();
        int damage = node.has("damage") ? node.get("damage").asInt() : 1;
        monsterManager.onMonsterHit(session, monsterId, damage);
    }

    private void handleStateRequest(ChannelHandlerContext ctx, GamePacket packet) {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        String json = redisManager.getPlayerState(session.getPlayerId());
        if (json == null) json = "{}";
        ctx.writeAndFlush(new GamePacket(PacketType.STATE_DATA, json));
    }

    private void handleStateSave(ChannelHandlerContext ctx, GamePacket packet) {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        redisManager.savePlayerState(session.getPlayerId(), packet.getPayload());
    }

    private void handleLogin(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        JsonNode node = mapper.readTree(packet.getPayload());
        String username = node.get("username").asText();
        String password = node.get("password").asText();
        boolean isRegister = node.has("isRegister") && node.get("isRegister").asBoolean();

        String hashedPassword = hashPassword(password);
        String accountKey = "account:" + username;

        if (isRegister) {
            // Register
            String existing = redisManager.get(accountKey);
            if (existing != null) {
                String result = mapper.writeValueAsString(new LoginResult(false, null, "Username already exists."));
                ctx.writeAndFlush(new GamePacket(PacketType.LOGIN_RESULT, result));
                return;
            }
            redisManager.set(accountKey, hashedPassword);
            log.info("New account registered: {}", username);
        } else {
            // Login
            String storedPassword = redisManager.get(accountKey);
            if (storedPassword == null) {
                String result = mapper.writeValueAsString(new LoginResult(false, null, "Account not found."));
                ctx.writeAndFlush(new GamePacket(PacketType.LOGIN_RESULT, result));
                return;
            }
            if (!storedPassword.equals(hashedPassword)) {
                String result = mapper.writeValueAsString(new LoginResult(false, null, "Wrong password."));
                ctx.writeAndFlush(new GamePacket(PacketType.LOGIN_RESULT, result));
                return;
            }
        }

        // Check if already logged in
        if (worldManager.getSessionByPlayerId(username) != null) {
            String result = mapper.writeValueAsString(new LoginResult(false, null, "Already logged in."));
            ctx.writeAndFlush(new GamePacket(PacketType.LOGIN_RESULT, result));
            return;
        }

        // Login success — actual SPAWN_PLAYER broadcast happens on ZONE_ENTER
        PlayerSession session = worldManager.addPlayer(username, ctx.channel());
        redisManager.setPlayerOnline(username);

        String result = mapper.writeValueAsString(new LoginResult(true, username, "OK"));
        ctx.writeAndFlush(new GamePacket(PacketType.LOGIN_RESULT, result));

        log.info("Player {} logged in", username);
    }

    private String hashPassword(String password) {
        try {
            MessageDigest md = MessageDigest.getInstance("SHA-256");
            byte[] hash = md.digest(password.getBytes(StandardCharsets.UTF_8));
            StringBuilder sb = new StringBuilder();
            for (byte b : hash) {
                sb.append(String.format("%02x", b));
            }
            return sb.toString();
        } catch (Exception e) {
            throw new RuntimeException("Failed to hash password", e);
        }
    }

    private void handleMove(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;

        JsonNode node = mapper.readTree(packet.getPayload());
        Position newPos = new Position(
                (float) node.get("x").asDouble(),
                (float) node.get("y").asDouble(),
                (float) node.get("z").asDouble()
        );
        int facing = node.has("facing") ? node.get("facing").asInt() : 1;

        session.setPosition(newPos);
        redisManager.updatePlayerPosition(session.getPlayerId(), newPos);

        String moveData = mapper.writeValueAsString(new MoveData(session.getPlayerId(), newPos, facing));
        worldManager.broadcastNearby(newPos, BROADCAST_RANGE,
                new GamePacket(PacketType.PLAYER_MOVED, moveData), session.getPlayerId());
    }

    private void handleChat(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;

        JsonNode node = mapper.readTree(packet.getPayload());
        String message = node.get("message").asText();

        String chatData = mapper.writeValueAsString(new ChatData(session.getPlayerId(), message));
        GamePacket out = new GamePacket(PacketType.CHAT_MESSAGE, chatData);

        String zoneId = session.getZoneId();
        if (zoneId == null || zoneId.isEmpty()) {
            // Sender hasn't picked a zone yet — echo only to themselves.
            ctx.writeAndFlush(out);
        } else {
            worldManager.broadcastToZone(zoneId, out);
        }
    }

    private void handleAttack(ChannelHandlerContext ctx, GamePacket packet) {
        log.info("Attack packet received from {}", ctx.channel().id().asShortText());
    }

    private void handleCharacterList(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;

        var chars = redisManager.getCharacters(session.getPlayerId());
        StringBuilder sb = new StringBuilder("{\"characters\":[");
        for (int i = 0; i < chars.size(); i++) {
            if (i > 0) sb.append(",");
            sb.append(chars.get(i));
        }
        sb.append("]}");

        ctx.writeAndFlush(new GamePacket(PacketType.CHARACTER_LIST_RESULT, sb.toString()));
        log.info("Character list sent to {}: {} characters", session.getPlayerId(), chars.size());
    }

    private void handleCharacterCreate(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;

        JsonNode node = mapper.readTree(packet.getPayload());
        String charName = node.get("name").asText();
        String charClass = node.get("className").asText();

        // Validate name
        if (charName.length() < 2 || charName.length() > 16) {
            String result = mapper.writeValueAsString(new CharacterCreateResult(false, "Name must be 2-16 characters."));
            ctx.writeAndFlush(new GamePacket(PacketType.CHARACTER_CREATE_RESULT, result));
            return;
        }

        // Check max characters (4)
        var existing = redisManager.getCharacters(session.getPlayerId());
        if (existing.size() >= 4) {
            String result = mapper.writeValueAsString(new CharacterCreateResult(false, "Maximum 4 characters allowed."));
            ctx.writeAndFlush(new GamePacket(PacketType.CHARACTER_CREATE_RESULT, result));
            return;
        }

        // Check duplicate name
        if (redisManager.characterExists(session.getPlayerId(), charName)) {
            String result = mapper.writeValueAsString(new CharacterCreateResult(false, "Character name already exists."));
            ctx.writeAndFlush(new GamePacket(PacketType.CHARACTER_CREATE_RESULT, result));
            return;
        }

        // Save character
        redisManager.saveCharacter(session.getPlayerId(), charName, charClass, 1);

        String result = mapper.writeValueAsString(new CharacterCreateResult(true, "Character created!"));
        ctx.writeAndFlush(new GamePacket(PacketType.CHARACTER_CREATE_RESULT, result));
        log.info("Character created for {}: {} ({})", session.getPlayerId(), charName, charClass);
    }

    private void handleCharacterDelete(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;

        JsonNode node = mapper.readTree(packet.getPayload());
        String charName = node.get("name").asText();

        boolean deleted = redisManager.deleteCharacter(session.getPlayerId(), charName);
        String msg = deleted ? "Character deleted." : "Character not found.";
        ctx.writeAndFlush(new GamePacket(PacketType.CHARACTER_DELETE_RESULT,
                "{\"success\":" + deleted + ",\"message\":\"" + msg + "\"}"));
        log.info("Character delete for {}: {} ({})", session.getPlayerId(), charName, deleted);
    }

    @Override
    public void channelInactive(ChannelHandlerContext ctx) {
        PlayerSession session = worldManager.removePlayer(ctx.channel());
        if (session != null) {
            redisManager.setPlayerOffline(session.getPlayerId());
            String despawnData = "{\"playerId\":\"" + session.getPlayerId() + "\"}";
            worldManager.broadcastAll(new GamePacket(PacketType.DESPAWN_PLAYER, despawnData), session.getPlayerId());
            log.info("Player {} disconnected", session.getPlayerId());
        }
    }

    @Override
    public void userEventTriggered(ChannelHandlerContext ctx, Object evt) {
        if (evt instanceof IdleStateEvent) {
            log.info("Idle connection detected, closing: {}", ctx.channel().id().asShortText());
            ctx.close();
        }
    }

    @Override
    public void exceptionCaught(ChannelHandlerContext ctx, Throwable cause) {
        log.error("Error in channel {}: {}", ctx.channel().id().asShortText(), cause.getMessage());
        ctx.close();
    }

    // DTO records
    record LoginResult(boolean success, String playerId, String message) {}
    record SpawnData(String playerId, Position position) {}
    record MoveData(String playerId, Position position, int facing) {}
    record ChatData(String playerId, String message) {}
    record CharacterCreateResult(boolean success, String message) {}
}
