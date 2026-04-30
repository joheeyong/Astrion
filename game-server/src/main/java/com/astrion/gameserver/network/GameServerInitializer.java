package com.astrion.gameserver.network;

import com.astrion.gameserver.handler.GamePacketHandler;
import com.astrion.gameserver.redis.RedisManager;
import com.astrion.gameserver.world.WorldManager;
import io.netty.channel.ChannelInitializer;
import io.netty.channel.ChannelPipeline;
import io.netty.channel.socket.SocketChannel;
import io.netty.handler.timeout.IdleStateHandler;

import java.util.concurrent.TimeUnit;

public class GameServerInitializer extends ChannelInitializer<SocketChannel> {

    private final WorldManager worldManager;
    private final RedisManager redisManager;

    public GameServerInitializer(WorldManager worldManager, RedisManager redisManager) {
        this.worldManager = worldManager;
        this.redisManager = redisManager;
    }

    @Override
    protected void initChannel(SocketChannel ch) {
        ChannelPipeline pipeline = ch.pipeline();

        // Idle detection: 60s read timeout, 30s write timeout
        pipeline.addLast(new IdleStateHandler(60, 30, 0, TimeUnit.SECONDS));

        // Codec
        pipeline.addLast(new PacketDecoder());
        pipeline.addLast(new PacketEncoder());

        // Game logic handler
        pipeline.addLast(new GamePacketHandler(worldManager, redisManager));
    }
}
