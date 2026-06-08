package com.astrion.gameserver.world;

import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.locks.ReentrantLock;

/// JVM-level per-player locks for serialising read-modify-write paths
/// that operate on the Redis-persisted player state JSON. Trade, auction,
/// achievement grant, and STATE_SAVE all hit the same JSON; without a
/// shared mutex two of them on the same player can race and either lose
/// a write or duplicate items.
///
/// Example race we're closing: player A confirms a trade and simultaneously
/// hits 'buy' on an auction listing.
///   T1: TradeManager.execute reads A.state {gold:100, items:[sword]}
///   T2: AuctionManager.buy    reads A.state {gold:100, items:[sword]}
///   T1: writes A.state {gold:100, items:[]}              (sword given away)
///   T2: writes A.state {gold:50,  items:[sword, bought]} (clobbers T1)
///   → sword duplicated on A's side (still in inventory) AND given to B
///
/// ReentrantLock so a critical section can call another that re-acquires
/// the same player's lock (e.g. TradeManager.execute finishes the swap,
/// then triggers AchievementManager.grantReward for both sides — already
/// holding both locks, the grant re-enters them).
///
/// Single JVM is enough — the server runs as one process. If we ever
/// horizontally scale, replace with Redis distributed locks (SET NX EX
/// + token release) — the call sites stay the same.
///
/// Memory: one ReentrantLock per ever-seen playerId. ~64 bytes each.
/// At our scale (thousands of unique players over a session) negligible;
/// if it ever isn't, add an eviction policy keyed by 'no active session
/// AND lock unheld'.
public class PlayerStateLocks {

    private final ConcurrentHashMap<String, ReentrantLock> locks = new ConcurrentHashMap<>();

    public ReentrantLock lockFor(String playerId) {
        return locks.computeIfAbsent(playerId, k -> new ReentrantLock());
    }

    public void withLock(String playerId, Runnable op) {
        if (playerId == null) { op.run(); return; }
        ReentrantLock lock = lockFor(playerId);
        lock.lock();
        try { op.run(); } finally { lock.unlock(); }
    }

    /// Atomic two-player critical section. Order the locks by player id
    /// so two trades trade(A,B) and trade(B,A) always acquire in the
    /// same order — no ABBA deadlock.
    public void withLocks(String a, String b, Runnable op) {
        if (a == null && b == null) { op.run(); return; }
        if (a == null) { withLock(b, op); return; }
        if (b == null || a.equals(b)) { withLock(a, op); return; }
        boolean aFirst = a.compareTo(b) < 0;
        String first  = aFirst ? a : b;
        String second = aFirst ? b : a;
        ReentrantLock l1 = lockFor(first);
        ReentrantLock l2 = lockFor(second);
        l1.lock();
        try {
            l2.lock();
            try { op.run(); } finally { l2.unlock(); }
        } finally {
            l1.unlock();
        }
    }
}
