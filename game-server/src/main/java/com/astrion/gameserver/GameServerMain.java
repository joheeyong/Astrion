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

public class GameServerMain {

    private static final Logger log = LoggerFactory.getLogger(GameServerMain.class);

    private static final int PORT = 9000;
    private static final int HTTP_PORT = 9001;
    private static final String REDIS_HOST = "localhost";
    private static final int REDIS_PORT = 6379;

    public static void main(String[] args) throws Exception {
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

            // Sibling HTTP server for /health and /metrics, shares the same worker
            // loop — probe traffic is tiny so this stays cheap.
            ServerBootstrap httpBootstrap = new ServerBootstrap();
            httpBootstrap.group(bossGroup, workerGroup)
                    .channel(NioServerSocketChannel.class)
                    .childHandler(new ChannelInitializer<SocketChannel>() {
                        @Override
                        protected void initChannel(SocketChannel ch) {
                            ch.pipeline()
                                    .addLast(new HttpServerCodec())
                                    .addLast(new HttpObjectAggregator(8 * 1024))
                                    .addLast(new HealthHttpHandler(worldManager, monsterManager, startTimeMs));
                        }
                    })
                    .childOption(ChannelOption.SO_KEEPALIVE, false);
            httpBootstrap.bind(HTTP_PORT).sync();
            log.info("HTTP metrics endpoint on port {} (GET /health, /metrics)", HTTP_PORT);

            future.channel().closeFuture().sync();
        } finally {
            workerGroup.shutdownGracefully();
            bossGroup.shutdownGracefully();
            monsterManager.shutdown();
            redisManager.shutdown();
            log.info("Game server shut down");
        }
    }
}
