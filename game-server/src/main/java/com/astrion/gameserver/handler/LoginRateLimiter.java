package com.astrion.gameserver.handler;

import java.util.Iterator;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Per-IP login throttle.
 *
 * Counts both LOGIN and REGISTER attempts. A successful login clears the
 * counter for that IP so legitimate users who fat-fingered a few times don't
 * stay penalised. Memory is cleaned lazily on each {@link #check} call.
 *
 * Single instance shared across all handlers (static field on GamePacketHandler).
 */
final class LoginRateLimiter {
    /** Attempts allowed inside one window before the IP is blocked. */
    private static final int MAX_ATTEMPTS = 5;
    /** Rolling-window length. Failed attempts older than this don't count. */
    private static final long WINDOW_MS = 60_000L;
    /** Penalty applied when MAX_ATTEMPTS is exceeded. */
    private static final long BLOCK_MS = 5L * 60_000L;
    /** Lazy cleanup cadence — keeps the map from growing under a wide scan. */
    private static final long CLEANUP_INTERVAL_MS = 10L * 60_000L;

    private final Map<String, Record> records = new ConcurrentHashMap<>();
    private volatile long lastCleanup = System.currentTimeMillis();

    /** Returns {@code true} and 0 if the attempt is allowed, otherwise
     *  {@code false} and the remaining cooldown seconds. */
    Result check(String ip) {
        long now = System.currentTimeMillis();
        maybeCleanup(now);

        Record r = records.computeIfAbsent(ip, k -> new Record());
        synchronized (r) {
            if (r.blockedUntil > now) {
                return new Result(false, (r.blockedUntil - now + 999) / 1000);
            }
            if (now - r.windowStart > WINDOW_MS) {
                r.windowStart = now;
                r.count = 0;
            }
            r.count++;
            if (r.count > MAX_ATTEMPTS) {
                r.blockedUntil = now + BLOCK_MS;
                return new Result(false, BLOCK_MS / 1000);
            }
            return new Result(true, 0);
        }
    }

    /** Called after a successful auth — the IP is clearly legitimate. */
    void onSuccess(String ip) {
        records.remove(ip);
    }

    private void maybeCleanup(long now) {
        if (now - lastCleanup < CLEANUP_INTERVAL_MS) return;
        lastCleanup = now;
        // Drop records whose window has expired AND aren't currently blocked.
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

    static final class Result {
        final boolean allowed;
        final long secondsLeft;

        Result(boolean allowed, long secondsLeft) {
            this.allowed = allowed;
            this.secondsLeft = secondsLeft;
        }
    }
}
