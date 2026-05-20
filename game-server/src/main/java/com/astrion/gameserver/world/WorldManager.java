package com.astrion.gameserver.world;

import com.astrion.common.model.Position;
import com.astrion.common.packet.GamePacket;
import com.astrion.common.packet.PacketType;
import io.netty.channel.Channel;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Collection;
import java.util.concurrent.ConcurrentHashMap;

public class WorldManager {

    private static final Logger log = LoggerFactory.getLogger(WorldManager.class);

    // channelId -> PlayerSession
    private final ConcurrentHashMap<String, PlayerSession> sessions = new ConcurrentHashMap<>();
    // playerId -> PlayerSession
    private final ConcurrentHashMap<String, PlayerSession> playerIndex = new ConcurrentHashMap<>();

    public PlayerSession addPlayer(String playerId, Channel channel) {
        PlayerSession session = new PlayerSession(playerId, channel);
        sessions.put(channel.id().asShortText(), session);
        playerIndex.put(playerId, session);
        log.info("Player {} joined the world. Online: {}", playerId, sessions.size());
        return session;
    }

    public PlayerSession removePlayer(Channel channel) {
        PlayerSession session = sessions.remove(channel.id().asShortText());
        if (session != null) {
            playerIndex.remove(session.getPlayerId());
            log.info("Player {} left the world. Online: {}", session.getPlayerId(), sessions.size());
        }
        return session;
    }

    public PlayerSession getSession(Channel channel) {
        return sessions.get(channel.id().asShortText());
    }

    public PlayerSession getSessionByPlayerId(String playerId) {
        return playerIndex.get(playerId);
    }

    public Collection<PlayerSession> getAllSessions() {
        return sessions.values();
    }

    /** Close every active client channel. channelInactive will fire on each
     *  one synchronously inside the Netty loop, so by the time the worker
     *  group shuts down each player is already marked offline in Redis and
     *  a DESPAWN_PLAYER has been broadcast. Used during server shutdown. */
    public void disconnectAll() {
        for (PlayerSession s : sessions.values()) {
            try { s.getChannel().close(); } catch (Exception ignored) { /* best-effort */ }
        }
    }

    /**
     * Broadcast a packet to all players within a certain range of the given position.
     */
    public void broadcastNearby(Position origin, float range, GamePacket packet, String excludePlayerId) {
        for (PlayerSession session : sessions.values()) {
            if (session.getPlayerId().equals(excludePlayerId)) continue;
            if (session.getPosition().distanceTo(origin) <= range) {
                session.getChannel().writeAndFlush(packet);
            }
        }
    }

    /**
     * Broadcast to all connected players.
     */
    public void broadcastAll(GamePacket packet, String excludePlayerId) {
        for (PlayerSession session : sessions.values()) {
            if (excludePlayerId != null && session.getPlayerId().equals(excludePlayerId)) continue;
            session.getChannel().writeAndFlush(packet);
        }
    }

    /**
     * Broadcast to all players currently in the given zone.
     */
    public void broadcastToZone(String zoneId, GamePacket packet) {
        if (zoneId == null) return;
        for (PlayerSession session : sessions.values()) {
            if (zoneId.equals(session.getZoneId()))
                session.getChannel().writeAndFlush(packet);
        }
    }

    public void broadcastToZoneExcept(String zoneId, GamePacket packet, String excludePlayerId) {
        if (zoneId == null) return;
        for (PlayerSession session : sessions.values()) {
            if (excludePlayerId != null && session.getPlayerId().equals(excludePlayerId)) continue;
            if (zoneId.equals(session.getZoneId()))
                session.getChannel().writeAndFlush(packet);
        }
    }
}
