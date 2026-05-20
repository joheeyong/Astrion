package com.astrion.gameserver.handler;

import java.util.Iterator;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Sliding-window login throttle. Keyed generically by an arbitrary string so
 * the same class can drive both the per-IP limiter (key=clientIp) and the
 * per-username limiter (key=username) — different keys, different policies,
 * but identical bookkeeping.
 *
 * A successful login clears the key's counter so a user who fat-fingered
 * their password a few times isn't penalised after they finally get in.
 * Memory is cleaned lazily on each {@link #check} call.
 */
final class LoginRateLimiter {
    /** Attempts allowed inside one window before the key is blocked. */
    private final int maxAttempts;
    /** Rolling-window length. Failed attempts older than this don't count. */
    private final long windowMs;
    /** Penalty applied when maxAttempts is exceeded. */
    private final long blockMs;
    /** Lazy cleanup cadence — keeps the map from growing under a wide scan. */
    private static final long CLEANUP_INTERVAL_MS = 10L * 60_000L;

    private final Map<String, Record> records = new ConcurrentHashMap<>();
    private volatile long lastCleanup = System.currentTimeMillis();

    LoginRateLimiter(int maxAttempts, long windowMs, long blockMs) {
        this.maxAttempts = maxAttempts;
        this.windowMs = windowMs;
        this.blockMs = blockMs;
    }

    /** Returns {@code true} and 0 if the attempt is allowed, otherwise
     *  {@code false} and the remaining cooldown seconds. */
    Result check(String key) {
        long now = System.currentTimeMillis();
        maybeCleanup(now);

        Record r = records.computeIfAbsent(key, k -> new Record());
        synchronized (r) {
            if (r.blockedUntil > now) {
                return new Result(false, (r.blockedUntil - now + 999) / 1000);
            }
            if (now - r.windowStart > windowMs) {
                r.windowStart = now;
                r.count = 0;
            }
            r.count++;
            if (r.count > maxAttempts) {
                r.blockedUntil = now + blockMs;
                return new Result(false, blockMs / 1000);
            }
            return new Result(true, 0);
        }
    }

    /** Called after a successful auth — the key is clearly legitimate. */
    void onSuccess(String key) {
        records.remove(key);
    }

    private void maybeCleanup(long now) {
        if (now - lastCleanup < CLEANUP_INTERVAL_MS) return;
        lastCleanup = now;
        // Drop records whose window has expired AND aren't currently blocked.
        Iterator<Map.Entry<String, Record>> it = records.entrySet().iterator();
        while (it.hasNext()) {
            Record r = it.next().getValue();
            synchronized (r) {
                if (r.blockedUntil <= now && now - r.windowStart > windowMs) {
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
