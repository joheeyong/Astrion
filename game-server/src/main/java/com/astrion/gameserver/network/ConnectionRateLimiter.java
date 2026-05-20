package com.astrion.gameserver.network;

import java.util.Iterator;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Per-IP TCP-connect throttle. Sits in front of TLS, so a flood of new
 * connections from one IP is rejected before the JVM spends RSA/AES work
 * on a handshake.
 *
 * Different layer from LoginRateLimiter:
 *   LoginRateLimiter  — same connection, multiple LOGIN packets
 *   this              — many new connections at once
 *
 * Each protects a distinct DOS shape.
 */
public final class ConnectionRateLimiter {
    /** Connections allowed inside one window before the IP is blocked. A
     *  legitimate game client uses exactly 1 long-lived connection; 30/min
     *  leaves plenty of room for reconnect storms on bad Wi-Fi without
     *  catching real attacks. */
    private static final int MAX_CONNECTS = 30;
    private static final long WINDOW_MS = 60_000L;
    private static final long BLOCK_MS = 60_000L;
    private static final long CLEANUP_INTERVAL_MS = 10L * 60_000L;

    private final Map<String, Record> records = new ConcurrentHashMap<>();
    private volatile long lastCleanup = System.currentTimeMillis();

    public ConnectionRateLimiter() { /* default */ }

    /** @return true if the connection should be accepted. */
    public boolean allow(String ip) {
        long now = System.currentTimeMillis();
        maybeCleanup(now);
        Record r = records.computeIfAbsent(ip, k -> new Record());
        synchronized (r) {
            if (r.blockedUntil > now) return false;
            if (now - r.windowStart > WINDOW_MS) {
                r.windowStart = now;
                r.count = 0;
            }
            r.count++;
            if (r.count > MAX_CONNECTS) {
                r.blockedUntil = now + BLOCK_MS;
                return false;
            }
            return true;
        }
    }

    private void maybeCleanup(long now) {
        if (now - lastCleanup < CLEANUP_INTERVAL_MS) return;
        lastCleanup = now;
        Iterator<Map.Entry<String, Record>> it = records.entrySet().iterator();
        while (it.hasNext()) {
            Record r = it.next().getValue();
            synchronized (r) {
                if (r.blockedUntil <= now && now - r.windowStart > WINDOW_MS) {
                    it.remove();
                }
            }
        }
    }

    private static final class Record {
        long windowStart;
        int count;
        long blockedUntil;
    }
}
