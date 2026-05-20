package com.astrion.gameserver.handler;

import com.astrion.gameserver.redis.RedisManager;

/**
 * Persistent account lockout. Differs from LoginRateLimiter in two ways:
 *  - Counts only WRONG-PASSWORD attempts (account-not-found never causes
 *    a real account to get locked — username spray would otherwise become
 *    a DoS-the-victim attack).
 *  - State lives in Redis, so a server restart does NOT reset the counter
 *    and a slow/distributed attacker can't reset by timing the JVM bounce.
 *
 * Policy: 10 wrong-password attempts in a rolling 24h window locks the
 * account for 1 hour. The successful login that follows clears both
 * counters. 24h is long enough to catch the 'one attempt per hour' style
 * stealth attacks that in-memory limiters miss.
 */
public final class AccountLockout {

    private static final int  MAX_FAILS      = 10;
    private static final long LOCK_SECONDS   = 3600L;        // 1h hard lock
    private static final long COUNTER_TTL_S  = 24 * 3600L;   // failure counter horizon

    private final RedisManager redis;

    public AccountLockout(RedisManager redis) { this.redis = redis; }

    /** @return seconds left in the lockout, or 0 if the account is free. */
    public long lockSecondsLeft(String username) {
        long ttl = redis.ttl(lockedKey(username));
        return ttl > 0 ? ttl : 0;
    }

    /** Record a wrong-password attempt. Locks the account when MAX_FAILS hits. */
    public void recordFailure(String username) {
        String counter = failsKey(username);
        long n = redis.incr(counter);
        // Refresh the counter horizon on every failure — the 24h window is
        // 'rolling' in the sense that it resets every time we observe activity.
        redis.expire(counter, COUNTER_TTL_S);
        if (n >= MAX_FAILS) {
            redis.setex(lockedKey(username), LOCK_SECONDS, "1");
        }
    }

    /** Successful login — wipe both keys so the user starts fresh. */
    public void clear(String username) {
        redis.del(failsKey(username));
        redis.del(lockedKey(username));
    }

    private static String failsKey(String username)  { return "account:fails:"  + username; }
    private static String lockedKey(String username) { return "account:locked:" + username; }
}
