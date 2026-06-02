package com.astrion.gameserver.world;

import com.astrion.common.model.Position;
import com.astrion.common.packet.GamePacket;
import com.astrion.common.packet.PacketType;
import io.netty.channel.Channel;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Collection;
import java.util.Objects;
import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;

public class WorldManager {

    private static final Logger log = LoggerFactory.getLogger(WorldManager.class);

    // channelId -> PlayerSession
    private final ConcurrentHashMap<String, PlayerSession> sessions = new ConcurrentHashMap<>();
    // playerId -> PlayerSession
    private final ConcurrentHashMap<String, PlayerSession> playerIndex = new ConcurrentHashMap<>();
    // zoneId -> Set<PlayerSession>. Maintained alongside sessions/playerIndex
    // by addPlayer / removePlayer / setZoneId. Zone-scoped broadcasts use this
    // instead of scanning the full sessions map, which collapses MOVE/MONSTER
    // broadcasts from O(total players) to O(players in this zone).
    private final ConcurrentHashMap<String, Set<PlayerSession>> sessionsByZone = new ConcurrentHashMap<>();

    public PlayerSession addPlayer(String playerId, Channel channel) {
        PlayerSession session = new PlayerSession(playerId, channel);
        sessions.put(channel.id().asShortText(), session);
        playerIndex.put(playerId, session);
        // Zone index is updated when setZoneId fires (LOGIN → ZONE_ENTER).
        log.info("Player {} joined the world. Online: {}", playerId, sessions.size());
        return session;
    }

    public PlayerSession removePlayer(Channel channel) {
        PlayerSession session = sessions.remove(channel.id().asShortText());
        if (session != null) {
            playerIndex.remove(session.getPlayerId());
            removeFromZoneIndex(session);
            log.info("Player {} left the world. Online: {}", session.getPlayerId(), sessions.size());
        }
        return session;
    }

    /** Move a session between zones, keeping the zone index consistent.
     *  Callers should use this instead of PlayerSession.setZoneId directly. */
    public void setZoneId(PlayerSession session, String newZone) {
        if (session == null) return;
        String old = session.getZoneId();
        String next = newZone == null ? "" : newZone;
        if (Objects.equals(old, next)) return;
        if (old != null && !old.isEmpty()) {
            Set<PlayerSession> s = sessionsByZone.get(old);
            if (s != null) s.remove(session);
        }
        session.setZoneId(next);
        if (!next.isEmpty()) {
            sessionsByZone.computeIfAbsent(next, k -> ConcurrentHashMap.newKeySet()).add(session);
        }
    }

    private void removeFromZoneIndex(PlayerSession session) {
        String z = session.getZoneId();
        if (z == null || z.isEmpty()) return;
        Set<PlayerSession> s = sessionsByZone.get(z);
        if (s != null) s.remove(session);
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
     * Broadcast to players in {@code zoneId} within {@code range} of {@code origin}.
     * Iterates only the zone's bucket and avoids sqrt by comparing squared
     * distances. Pre-optimisation this scanned every session every move.
     */
    public void broadcastNearby(String zoneId, Position origin, float range, GamePacket packet, String excludePlayerId) {
        if (zoneId == null || zoneId.isEmpty()) return;
        Set<PlayerSession> bucket = sessionsByZone.get(zoneId);
        if (bucket == null) return;
        float r2 = range * range;
        for (PlayerSession session : bucket) {
            if (excludePlayerId != null && session.getPlayerId().equals(excludePlayerId)) continue;
            Position p = session.getPosition();
            float dx = p.getX() - origin.getX();
            float dy = p.getY() - origin.getY();
            float dz = p.getZ() - origin.getZ();
            if (dx * dx + dy * dy + dz * dz <= r2) {
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
     * Broadcast to all players currently in the given zone. O(zone size).
     */
    public void broadcastToZone(String zoneId, GamePacket packet) {
        if (zoneId == null) return;
        Set<PlayerSession> bucket = sessionsByZone.get(zoneId);
        if (bucket == null) return;
        for (PlayerSession session : bucket) {
            session.getChannel().writeAndFlush(packet);
        }
    }

    /// Read-only view of the zone's session set — used by per-recipient
    /// filtered broadcasts (e.g. chat with block lists). Returns an empty
    /// list when the zone is unknown so callers can iterate unconditionally.
    public Iterable<PlayerSession> sessionsInZone(String zoneId) {
        if (zoneId == null) return java.util.Collections.emptyList();
        Set<PlayerSession> bucket = sessionsByZone.get(zoneId);
        return bucket == null ? java.util.Collections.emptyList() : bucket;
    }

    public void broadcastToZoneExcept(String zoneId, GamePacket packet, String excludePlayerId) {
        if (zoneId == null) return;
        Set<PlayerSession> bucket = sessionsByZone.get(zoneId);
        if (bucket == null) return;
        for (PlayerSession session : bucket) {
            if (excludePlayerId != null && session.getPlayerId().equals(excludePlayerId)) continue;
            session.getChannel().writeAndFlush(packet);
        }
    }
}
