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
import com.fasterxml.jackson.databind.node.ObjectNode;
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
            case DROP_CLAIM -> handleDropClaim(ctx, packet);
            case STATUS_UPDATE -> handleStatusUpdate(ctx, packet);
            default -> log.warn("Unhandled packet type: {}", packet.getType());
        }
    }

    private void handleStatusUpdate(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        JsonNode node = mapper.readTree(packet.getPayload());
        int hp = node.has("hp") ? node.get("hp").asInt() : 0;
        int maxHp = node.has("maxHp") ? node.get("maxHp").asInt() : 0;

        // Combat stats — used later to cap damage. Clamp to sane ranges so a forged
        // STATUS_UPDATE can't enable arbitrary damage either.
        if (node.has("level"))      session.level      = Math.min(100, Math.max(1, node.get("level").asInt()));
        if (node.has("intStat"))    session.intStat    = Math.min(999, Math.max(1, node.get("intStat").asInt()));
        if (node.has("weaponDmg"))  session.weaponDmg  = Math.min(500, Math.max(0, node.get("weaponDmg").asInt()));
        if (node.has("starboltLv")) session.starboltLv = Math.min(10,  Math.max(1, node.get("starboltLv").asInt()));

        String zoneId = session.getZoneId();
        if (zoneId == null || zoneId.isEmpty()) return;
        String payload = mapper.writeValueAsString(new PlayerStatus(session.getPlayerId(), hp, maxHp));
        worldManager.broadcastToZoneExcept(zoneId,
            new GamePacket(PacketType.PLAYER_STATUS, payload), session.getPlayerId());
    }

    // Authoritative max damage the client is allowed to claim. Formula mirrors
    // PlayerStats.ComputeBoltDamage with a 1.3× safety margin to absorb variance + skill bonus.
    private static int maxAllowedDamage(PlayerSession s) {
        // Base formula: 5 + INT*2 + LV*3 + weaponDmg + (starboltLv-1)*5
        int base = 5 + s.intStat * 2 + s.level * 3 + s.weaponDmg + (s.starboltLv - 1) * 5;
        // Variance up to ±20%, plus small headroom for legitimate skills (e.g. meteor)
        return Math.max(10, (int) Math.ceil(base * 1.6));
    }

    private void handleDropClaim(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        JsonNode node = mapper.readTree(packet.getPayload());
        String dropId = node.get("dropId").asText();
        monsterManager.onDropClaim(session, dropId);
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
        if (node.has("nickname")) {
            String nick = node.get("nickname").asText();
            if (nick != null && !nick.isEmpty()) session.setNickname(nick);
        }
        String oldZone = session.getZoneId();

        if (!java.util.Objects.equals(oldZone, newZone)) {
            // Tell players in old zone this one is gone
            if (oldZone != null && !oldZone.isEmpty()) {
                String despawnData = "{\"playerId\":\"" + session.getPlayerId() + "\"}";
                worldManager.broadcastToZone(oldZone,
                    new GamePacket(PacketType.DESPAWN_PLAYER, despawnData));
            }
            session.setZoneId(newZone);
            // Zone change is a legitimate "teleport" — skip the next move validation
            session.lastMoveAt = 0L;
            // Announce this player to the new zone
            String spawnData = mapper.writeValueAsString(new SpawnData(session.getPlayerId(), session.getNickname(), session.getPosition()));
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
            String spawnData = mapper.writeValueAsString(new SpawnData(other.getPlayerId(), other.getNickname(), other.getPosition()));
            self.getChannel().writeAndFlush(new GamePacket(PacketType.SPAWN_PLAYER, spawnData));
        }
    }

    private void handleMonsterHit(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        JsonNode node = mapper.readTree(packet.getPayload());
        String monsterId = node.get("id").asText();
        int claimed = node.has("damage") ? node.get("damage").asInt() : 1;
        int cap = maxAllowedDamage(session);
        int applied = Math.max(1, Math.min(claimed, cap));
        if (claimed > cap) {
            log.warn("[anti-cheat] {} claimed dmg {} -> capped to {} (LV{} INT{} WPN{} BoltLv{})",
                session.getPlayerId(), claimed, cap,
                session.level, session.intStat, session.weaponDmg, session.starboltLv);
        }
        monsterManager.onMonsterHit(session, monsterId, applied);
    }

    private void handleStateRequest(ChannelHandlerContext ctx, GamePacket packet) {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        String json = redisManager.getPlayerState(session.getPlayerId());
        if (json == null) {
            // Brand-new player — hand out starter kit:
            //   bread x3, 50 G, and four sealed boxes (weapon / helmet / armor / ring)
            //   that the client unpacks into class-appropriate gear on use.
            json = "{"
                + "\"inventoryItemIds\":[\"bread\",\"weapon_box\",\"helmet_box\",\"armor_box\",\"ring_box\"],"
                + "\"inventoryQuantities\":[3,1,1,1,1],"
                + "\"gold\":50"
                + "}";
            redisManager.savePlayerState(session.getPlayerId(), json);
            log.info("Starter kit granted to new player {}", session.getPlayerId());
        }
        ctx.writeAndFlush(new GamePacket(PacketType.STATE_DATA, json));
    }

    private void handleStateSave(ChannelHandlerContext ctx, GamePacket packet) {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        String json = packet.getPayload();
        String saveId = null;

        // If the client tagged this save with a saveId, ACK it. Strip the field
        // before persisting so Redis stores clean state (saveId is per-attempt).
        try {
            JsonNode node = mapper.readTree(json);
            if (node != null && node.has("saveId")) {
                String s = node.get("saveId").asText();
                if (s != null && !s.isEmpty()) {
                    saveId = s;
                    if (node instanceof ObjectNode) {
                        ((ObjectNode) node).remove("saveId");
                        json = mapper.writeValueAsString(node);
                    }
                }
            }
        } catch (Exception e) { /* fall through; persist raw payload */ }

        redisManager.savePlayerState(session.getPlayerId(), json);

        if (saveId != null) {
            try {
                String ack = "{\"saveId\":\"" + saveId + "\"}";
                ctx.writeAndFlush(new GamePacket(PacketType.STATE_ACK, ack));
            } catch (Exception e) { /* ignore */ }
        }
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

    // Movement anti-cheat thresholds — wide enough to absorb 1s lag spikes and
    // the player's worst-case dash + jump combo, tight enough to reject teleports.
    private static final double MAX_MOVE_SPEED = 12.0;   // world units per second
    private static final double MOVE_GRACE_SECONDS = 0.5; // soft margin (lag spike absorber)
    private static final double MOVE_FIXED_TOLERANCE = 1.0; // extra constant pad in units

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

        long now = System.currentTimeMillis();

        // Validate against last accepted move (skipped on the very first move and right after a zone change).
        if (session.lastMoveAt > 0) {
            double dt = (now - session.lastMoveAt) / 1000.0;
            double dx = newPos.getX() - session.lastValidPos.getX();
            double dy = newPos.getY() - session.lastValidPos.getY();
            double dist = Math.sqrt(dx * dx + dy * dy);
            double allowed = MAX_MOVE_SPEED * (dt + MOVE_GRACE_SECONDS) + MOVE_FIXED_TOLERANCE;
            if (dist > allowed)
            {
                log.warn("[anti-cheat] {} teleport rejected dist={} dt={}s allowed={} ({},{}) -> ({},{})",
                    session.getPlayerId(),
                    String.format("%.2f", dist),
                    String.format("%.3f", dt),
                    String.format("%.2f", allowed),
                    session.lastValidPos.getX(), session.lastValidPos.getY(),
                    newPos.getX(), newPos.getY());
                // Drop the move: server keeps the last valid position; nothing is
                // broadcast. The cheating client will look frozen to other players
                // until they fall back into the allowed envelope.
                return;
            }
        }

        session.lastMoveAt = now;
        session.lastValidPos = newPos;
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
    record SpawnData(String playerId, String nickname, Position position) {}
    record MoveData(String playerId, Position position, int facing) {}
    record ChatData(String playerId, String message) {}
    record CharacterCreateResult(boolean success, String message) {}
    record PlayerStatus(String playerId, int hp, int maxHp) {}
}
