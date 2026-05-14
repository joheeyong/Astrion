package com.astrion.gameserver;

import com.astrion.gameserver.network.GameServerInitializer;
import com.astrion.gameserver.redis.RedisManager;
import com.astrion.gameserver.world.MonsterManager;
import com.astrion.gameserver.world.WorldManager;
import io.netty.bootstrap.ServerBootstrap;
import io.netty.channel.ChannelFuture;
import io.netty.channel.ChannelOption;
import io.netty.channel.EventLoopGroup;
import io.netty.channel.nio.NioEventLoopGroup;
import io.netty.channel.socket.nio.NioServerSocketChannel;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class GameServerMain {

    private static final Logger log = LoggerFactory.getLogger(GameServerMain.class);

    private static final int PORT = 9000;
    private static final String REDIS_HOST = "localhost";
    private static final int REDIS_PORT = 6379;

    public static void main(String[] args) throws Exception {
        log.info("=== Astrion Game Server ===");

        RedisManager redisManager = new RedisManager(REDIS_HOST, REDIS_PORT);
        WorldManager worldManager = new WorldManager();
        MonsterManager monsterManager = new MonsterManager(worldManager);

        EventLoopGroup bossGroup = new NioEventLoopGroup(1);
        EventLoopGroup workerGroup = new NioEventLoopGroup();

        try {
            ServerBootstrap bootstrap = new ServerBootstrap();
            bootstrap.group(bossGroup, workerGroup)
                    .channel(NioServerSocketChannel.class)
                    .childHandler(new GameServerInitializer(worldManager, redisManager, monsterManager))
                    .option(ChannelOption.SO_BACKLOG, 128)
                    .childOption(ChannelOption.SO_KEEPALIVE, true)
                    .childOption(ChannelOption.TCP_NODELAY, true);

            ChannelFuture future = bootstrap.bind(PORT).sync();
            log.info("Game server started on port {}", PORT);
            log.info("Redis connected at {}:{}", REDIS_HOST, REDIS_PORT);

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
