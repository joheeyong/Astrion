package com.astrion.gameserver.redis;

import org.junit.jupiter.api.Test;

import java.util.concurrent.CompletableFuture;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;

import static org.junit.jupiter.api.Assertions.*;

/// Pure-Java regression tests for the async migration's pieces that don't
/// need a live Redis. Real Redis-talking integration tests would need an
/// embedded server (Testcontainers redis) — out of scope for this PR.
class RedisManagerAsyncTest {

    @Test
    void bothCompletesWhenBothFuturesDo() {
        var a = CompletableFuture.completedFuture("alpha");
        var b = CompletableFuture.completedFuture(42);
        var both = RedisManager.both(a, b).join();
        assertEquals("alpha", both.first());
        assertEquals(42, both.second());
    }

    @Test
    void bothPropagatesFailures() {
        var a = CompletableFuture.completedFuture("ok");
        var b = CompletableFuture.<Integer>failedFuture(new RuntimeException("boom"));
        var combined = RedisManager.both(a, b);
        var ex = assertThrows(java.util.concurrent.CompletionException.class, combined::join);
        assertTrue(ex.getCause() instanceof RuntimeException);
        assertEquals("boom", ex.getCause().getMessage());
    }

    @Test
    void bothDoesNotCompleteUntilBothFuturesDo() throws Exception {
        // Hand-controlled futures — verifies the structural property
        // (both must complete before the combined future does) without
        // relying on wall-clock timing. The 'does it actually overlap?'
        // question we leave to prod observation via the slow-call metric;
        // in JUnit we can't make wall-clock claims reliable.
        var a = new CompletableFuture<String>();
        var b = new CompletableFuture<String>();
        var combined = RedisManager.both(a, b);
        assertFalse(combined.isDone(), "should still be pending with neither complete");
        a.complete("A");
        assertFalse(combined.isDone(), "should still be pending with only one complete");
        b.complete("B");
        var pair = combined.get(1, TimeUnit.SECONDS);
        assertEquals("A", pair.first());
        assertEquals("B", pair.second());
    }
}
