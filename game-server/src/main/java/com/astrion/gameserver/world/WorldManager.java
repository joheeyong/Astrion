package com.astrion.gameserver.world;

import com.astrion.common.model.Position;
import com.astrion.common.packet.GamePacket;
import com.astrion.common.packet.PacketType;
import io.netty.channel.Channel;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Collection;
import java.util.EnumSet;
import java.util.Objects;
import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

public class WorldManager {

    private static final Logger log = LoggerFactory.getLogger(WorldManager.class);

    // ── Backpressure ──────────────────────────────────────────────────────
    // A client that stops draining its socket (suspended laptop, dead NAT
    // entry, deliberately slow reader) makes Netty queue every broadcast
    // we write to it. Without a gate the outbound buffer grows without
    // bound — at ~30 broadcasts/s per busy zone that's an OOM fuse.
    //
    // Policy, applied per recipient inside every broadcast loop:
    //   channel writable      → write normally (fast path, the 99.9% case)
    //   unwritable + droppable → drop the packet, bump the counter. These
    //                            types are high-frequency and self-healing:
    //                            the next update supersedes the lost one.
    //   unwritable + critical  → still write (login results, trade results,
    //                            spawn/despawn must arrive). They're low-
    //                            frequency so they can't be the flood source.
    //   pending > kill limit   → close the channel. The client is beyond
    //                            saving; freeing the buffer protects the
    //                            server. ReconnectSystem walks them back
    //                            in when their network recovers.
    private static final EnumSet<PacketType> DROPPABLE = EnumSet.of(
        PacketType.PLAYER_MOVED,
        PacketType.MONSTER_MOVE,
        PacketType.MONSTER_HP,
        PacketType.PLAYER_STATUS,
        PacketType.SKILL_BROADCAST);

    /// Bytes the channel must drain before becoming writable again — i.e.
    /// how far past the high watermark the queue has grown. Above this the
    /// connection is considered dead weight and gets closed.
    private static final long FORCE_CLOSE_BYTES = 1_500_000L;

    private static final AtomicLong droppedPackets = new AtomicLong();
    private static final AtomicLong forcedCloses = new AtomicLong();
    public static long getDroppedPacketCount() { return droppedPackets.get(); }
    public static long getForcedCloseCount() { return forcedCloses.get(); }

    /// Test seam — keep the classification logic pure so JUnit can cover it
    /// without spinning up Netty channels.
    static boolean isDroppable(PacketType type) { return DROPPABLE.contains(type); }
    static boolean shouldForceClose(long bytesBeforeWritable) { return bytesBeforeWritable > FORCE_CLOSE_BYTES; }

    /// Single choke point for every broadcast write. Direct 1:1 pushes
    /// (login result, trade state, etc.) intentionally bypass this — they
    /// are low-frequency and must never be dropped.
    private void send(PlayerSession session, GamePacket packet) {
        Channel ch = session.getChannel();
        if (!ch.isActive()) return;
        if (!ch.isWritable()) {
            // bytesBeforeWritable() is how much must drain before isWritable
            // flips back — a direct measure of queue overgrowth. Thread-safe
            // (volatile read), unlike poking the outbound buffer directly.
            long backlog = ch.bytesBeforeWritable();
            if (shouldForceClose(backlog)) {
                forcedCloses.incrementAndGet();
                log.warn("[backpressure] closing {} — {} bytes stuck past high watermark",
                    session.getPlayerId(), backlog);
                ch.close();
                return;
            }
            if (isDroppable(packet.getType())) {
                droppedPackets.incrementAndGet();
                return;
            }
        }
        ch.writeAndFlush(packet);
    }

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
                send(session, packet);
            }
        }
    }

    /**
     * Broadcast to all connected players.
     */
    public void broadcastAll(GamePacket packet, String excludePlayerId) {
        for (PlayerSession session : sessions.values()) {
            if (excludePlayerId != null && session.getPlayerId().equals(excludePlayerId)) continue;
            send(session, packet);
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
            send(session, packet);
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
            send(session, packet);
        }
    }
}
