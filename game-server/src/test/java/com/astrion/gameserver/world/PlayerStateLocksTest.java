package com.astrion.gameserver.world;

import org.junit.jupiter.api.Test;

import java.util.concurrent.CountDownLatch;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;

import static org.junit.jupiter.api.Assertions.*;

/// Regression tests for the per-player JVM lock used to serialise reads
/// and writes of the persistent player state JSON. These exercises mirror
/// the real race patterns: trade + auction on the same player, two-player
/// trade with mirror invocation, etc.
class PlayerStateLocksTest {

    @Test
    void singlePlayerLockSerialisesIncrement() throws Exception {
        // Without a lock the unsynchronised counter races and never reaches
        // the target — this exact pattern is the read-modify-write bug we
        // fixed in TradeManager / AuctionManager / AchievementManager.
        PlayerStateLocks locks = new PlayerStateLocks();
        int[] counter = new int[1];
        int threads = 32;
        int loops = 1_000;
        ExecutorService pool = Executors.newFixedThreadPool(threads);
        CountDownLatch done = new CountDownLatch(threads);
        for (int i = 0; i < threads; i++) {
            pool.submit(() -> {
                for (int k = 0; k < loops; k++) {
                    locks.withLock("alice", () -> {
                        int v = counter[0];
                        // Simulate the modify window — without a lock this
                        // would let another thread observe v and clobber.
                        Thread.yield();
                        counter[0] = v + 1;
                    });
                }
                done.countDown();
            });
        }
        assertTrue(done.await(10, TimeUnit.SECONDS), "increments timed out");
        pool.shutdownNow();
        assertEquals(threads * loops, counter[0]);
    }

    @Test
    void differentPlayersDoNotBlockEachOther() throws Exception {
        // The lock is per-player, not global — concurrent ops on disjoint
        // players must run in parallel or the throughput tanks.
        PlayerStateLocks locks = new PlayerStateLocks();
        CountDownLatch bobInside = new CountDownLatch(1);
        CountDownLatch aliceMayGo = new CountDownLatch(1);
        AtomicInteger order = new AtomicInteger(0);
        int[] aliceFinishOrder = new int[1];
        int[] bobFinishOrder = new int[1];

        Thread bob = new Thread(() -> locks.withLock("bob", () -> {
            bobInside.countDown();
            try { aliceMayGo.await(2, TimeUnit.SECONDS); } catch (InterruptedException ignored) {}
            bobFinishOrder[0] = order.incrementAndGet();
        }));
        bob.start();

        assertTrue(bobInside.await(2, TimeUnit.SECONDS));
        // bob still holds his lock, but alice's lock is independent.
        Thread alice = new Thread(() -> locks.withLock("alice", () -> {
            aliceFinishOrder[0] = order.incrementAndGet();
        }));
        alice.start();
        alice.join(2_000);
        aliceMayGo.countDown();
        bob.join(2_000);

        // alice finished first even though bob was still inside his lock.
        assertEquals(1, aliceFinishOrder[0]);
        assertEquals(2, bobFinishOrder[0]);
    }

    @Test
    void reentrantSamePlayerLockDoesNotDeadlock() {
        // TradeManager.execute holds locks for both sides, then calls
        // AchievementManager.grantReward which re-acquires the lock for
        // each side. The lock must be reentrant or we self-deadlock.
        PlayerStateLocks locks = new PlayerStateLocks();
        int[] depth = new int[1];
        locks.withLock("alice", () -> {
            depth[0]++;
            locks.withLock("alice", () -> {
                depth[0]++;
                locks.withLock("alice", () -> depth[0]++);
            });
        });
        assertEquals(3, depth[0]);
    }

    @Test
    void twoPlayerLockOrderingPreventsAbbaDeadlock() throws Exception {
        // Trade execute on (A,B) on one thread, on (B,A) on another. If
        // withLocks didn't sort the acquisition order we'd hit ABBA
        // deadlock immediately.
        PlayerStateLocks locks = new PlayerStateLocks();
        int rounds = 200;
        ExecutorService pool = Executors.newFixedThreadPool(2);
        CountDownLatch done = new CountDownLatch(2);
        int[] counter = new int[1];

        pool.submit(() -> {
            for (int i = 0; i < rounds; i++) {
                locks.withLocks("alice", "bob", () -> counter[0]++);
            }
            done.countDown();
        });
        pool.submit(() -> {
            for (int i = 0; i < rounds; i++) {
                locks.withLocks("bob", "alice", () -> counter[0]++);
            }
            done.countDown();
        });
        assertTrue(done.await(5, TimeUnit.SECONDS),
            "withLocks deadlocked — ordering bug");
        pool.shutdownNow();
        assertEquals(2 * rounds, counter[0]);
    }

    @Test
    void twoPlayerLockOnSamePlayerCollapsesToSingleLock() {
        // withLocks("alice","alice") must not try to lock twice on a
        // non-reentrant path — and the inner op still runs.
        PlayerStateLocks locks = new PlayerStateLocks();
        boolean[] ran = new boolean[1];
        locks.withLocks("alice", "alice", () -> ran[0] = true);
        assertTrue(ran[0]);
    }

    @Test
    void nullPlayerIdIsHandledGracefully() {
        PlayerStateLocks locks = new PlayerStateLocks();
        boolean[] ran = new boolean[2];
        locks.withLock(null, () -> ran[0] = true);
        locks.withLocks(null, null, () -> ran[1] = true);
        assertTrue(ran[0]);
        assertTrue(ran[1]);
    }
}
