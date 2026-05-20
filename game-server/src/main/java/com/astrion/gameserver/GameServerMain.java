package com.astrion.gameserver;

import com.astrion.gameserver.network.GameServerInitializer;
import com.astrion.gameserver.network.HealthHttpHandler;
import com.astrion.gameserver.redis.RedisManager;
import com.astrion.gameserver.world.MonsterManager;
import com.astrion.gameserver.world.WorldManager;
import io.netty.bootstrap.ServerBootstrap;
import io.netty.channel.ChannelFuture;
import io.netty.channel.ChannelInitializer;
import io.netty.channel.ChannelOption;
import io.netty.channel.EventLoopGroup;
import io.netty.channel.nio.NioEventLoopGroup;
import io.netty.channel.socket.SocketChannel;
import io.netty.channel.socket.nio.NioServerSocketChannel;
import io.netty.handler.codec.http.HttpObjectAggregator;
import io.netty.handler.codec.http.HttpServerCodec;
import io.netty.handler.ssl.SslContext;
import io.netty.handler.ssl.SslContextBuilder;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.File;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

public class GameServerMain {

    private static final Logger log = LoggerFactory.getLogger(GameServerMain.class);

    private static final int PORT = 9000;
    private static final int HTTP_PORT = 9001;       // ops port: /health + /metrics. Lock down in SG.
    private static final int LIVENESS_PORT = 9002;   // public liveness: /health only. Safe to open to the world.
    private static final String REDIS_HOST = "localhost";
    private static final int REDIS_PORT = 6379;

    public static void main(String[] args) throws Exception {
        // Catch anything thrown out of a thread that didn't install its own
        // handler (background timers, monster-tick, Netty boss thread under
        // some failure modes). Without this the JVM prints to stderr and
        // logback's rolling sinks never see it. Routing through log.error
        // means it lands in BOTH server.log and the dedicated errors.log.
        Thread.setDefaultUncaughtExceptionHandler((t, ex) ->
            log.error("Uncaught exception on thread {}: {}", t.getName(), ex.getMessage(), ex));

        log.info("=== Astrion Game Server ===");
        long startTimeMs = System.currentTimeMillis();

        RedisManager redisManager = new RedisManager(REDIS_HOST, REDIS_PORT);
        WorldManager worldManager = new WorldManager();
        MonsterManager monsterManager = new MonsterManager(worldManager);

        // TLS: load cert + key from env-overridable paths. The key is a
        // PKCS#8 PEM ('BEGIN PRIVATE KEY'); convert legacy 'BEGIN RSA
        // PRIVATE KEY' files with `openssl pkcs8 -topk8 -nocrypt`.
        // Falls back to plaintext if either file is missing — useful for
        // local dev, but ops should never see this branch in prod.
        SslContext sslCtx = null;
        String certPath = System.getenv().getOrDefault("ASTRION_TLS_CERT", "/home/ubuntu/game-server-cert/server.crt");
        String keyPath  = System.getenv().getOrDefault("ASTRION_TLS_KEY",  "/home/ubuntu/game-server-cert/server.key");
        File certFile = new File(certPath);
        File keyFile = new File(keyPath);
        if (certFile.isFile() && keyFile.isFile()) {
            sslCtx = SslContextBuilder.forServer(certFile, keyFile).build();
            log.info("TLS enabled (cert={})", certPath);
        } else {
            log.warn("TLS DISABLED — cert/key not found at {} / {}. Wire is plaintext.", certPath, keyPath);
        }

        EventLoopGroup bossGroup = new NioEventLoopGroup(1);
        EventLoopGroup workerGroup = new NioEventLoopGroup();

        try {
            final SslContext gameSslCtx = sslCtx;
            ServerBootstrap bootstrap = new ServerBootstrap();
            bootstrap.group(bossGroup, workerGroup)
                    .channel(NioServerSocketChannel.class)
                    .childHandler(new GameServerInitializer(worldManager, redisManager, monsterManager, gameSslCtx))
                    .option(ChannelOption.SO_BACKLOG, 128)
                    .childOption(ChannelOption.SO_KEEPALIVE, true)
                    .childOption(ChannelOption.TCP_NODELAY, true);

            ChannelFuture future = bootstrap.bind(PORT).sync();
            log.info("Game server started on port {}", PORT);
            log.info("Redis connected at {}:{}", REDIS_HOST, REDIS_PORT);

            // SIGTERM handler. systemd sends SIGTERM and waits TimeoutStopSec
            // (30s, set in the unit file) before escalating to SIGKILL. Inside
            // that window we want every active player to land in Redis as
            // 'offline' and every pending write to flush.
            AtomicBoolean shutdownStarted = new AtomicBoolean(false);
            Runtime.getRuntime().addShutdownHook(new Thread(() -> {
                if (!shutdownStarted.compareAndSet(false, true)) return;
                log.info("Graceful shutdown initiated");
                try {
                    // 1. Close the listening socket so no new connections sneak in.
                    future.channel().close().sync();
                    // 2. Close all active client channels. channelInactive runs
                    //    for each, doing setPlayerOffline + DESPAWN_PLAYER on
                    //    the Netty thread before we drain.
                    worldManager.disconnectAll();
                    // 3. Drain in-flight writes. quietPeriod=1s, timeout=10s —
                    //    gives Netty time to flush the despawn broadcasts.
                    workerGroup.shutdownGracefully(1, 10, TimeUnit.SECONDS).sync();
                    bossGroup.shutdownGracefully(1, 5, TimeUnit.SECONDS).sync();
                    monsterManager.shutdown();
                    redisManager.shutdown();
                } catch (InterruptedException e) {
                    Thread.currentThread().interrupt();
                }
                log.info("Graceful shutdown complete");
            }, "astrion-shutdown"));

            // Two HTTP listeners share the same worker loop — probe traffic
            // is tiny so a dedicated EventLoopGroup would just be wasted threads.
            //
            // HTTP_PORT (9001): full /metrics — operator-only. Restrict in
            //                   the AWS security group to monitoring IPs.
            // LIVENESS_PORT (9002): /health only — safe to open to the world.
            //                       External uptime probes / Route 53 health
            //                       checks should hit this.
            bindHttp(bossGroup, workerGroup, HTTP_PORT,
                () -> new HealthHttpHandler(worldManager, monsterManager, startTimeMs, true));
            log.info("HTTP ops endpoint on port {} (GET /health, /metrics)", HTTP_PORT);

            bindHttp(bossGroup, workerGroup, LIVENESS_PORT,
                () -> new HealthHttpHandler(worldManager, monsterManager, startTimeMs, false));
            log.info("HTTP liveness endpoint on port {} (GET /health)", LIVENESS_PORT);

            future.channel().closeFuture().sync();
            // Reaching here means the listening channel closed cleanly — the
            // shutdown hook already drained everything else. No explicit
            // cleanup here on purpose; the hook owns the lifecycle now.
        } catch (Throwable t) {
            // If something explodes BEFORE the shutdown hook would normally
            // run (e.g. bind fails), make sure we still release the loops so
            // the JVM doesn't hang at exit.
            log.error("Fatal startup error, tearing down event loops", t);
            workerGroup.shutdownGracefully();
            bossGroup.shutdownGracefully();
            monsterManager.shutdown();
            redisManager.shutdown();
            throw t;
        }
    }

    private static void bindHttp(EventLoopGroup boss, EventLoopGroup worker, int port,
                                 java.util.function.Supplier<io.netty.channel.ChannelHandler> handlerFactory)
                                 throws InterruptedException {
        ServerBootstrap b = new ServerBootstrap();
        b.group(boss, worker)
                .channel(NioServerSocketChannel.class)
                .childHandler(new ChannelInitializer<SocketChannel>() {
                    @Override protected void initChannel(SocketChannel ch) {
                        ch.pipeline()
                                .addLast(new HttpServerCodec())
                                .addLast(new HttpObjectAggregator(8 * 1024))
                                .addLast(handlerFactory.get());
                    }
                })
                .childOption(ChannelOption.SO_KEEPALIVE, false);
        b.bind(port).sync();
    }
}
