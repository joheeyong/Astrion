package com.astrion.gameserver.network;

import com.astrion.gameserver.handler.GamePacketHandler;
import com.astrion.gameserver.redis.RedisManager;
import com.astrion.gameserver.world.MonsterManager;
import com.astrion.gameserver.world.WorldManager;
import io.netty.channel.ChannelInitializer;
import io.netty.channel.ChannelPipeline;
import io.netty.channel.socket.SocketChannel;
import io.netty.handler.ssl.SslContext;
import io.netty.handler.timeout.IdleStateHandler;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.concurrent.TimeUnit;

public class GameServerInitializer extends ChannelInitializer<SocketChannel> {

    private static final Logger log = LoggerFactory.getLogger(GameServerInitializer.class);

    private final WorldManager worldManager;
    private final RedisManager redisManager;
    private final MonsterManager monsterManager;
    private final SslContext sslCtx; // null when TLS is disabled

    public GameServerInitializer(WorldManager worldManager, RedisManager redisManager,
                                 MonsterManager monsterManager, SslContext sslCtx) {
        this.worldManager = worldManager;
        this.redisManager = redisManager;
        this.monsterManager = monsterManager;
        this.sslCtx = sslCtx;
    }

    @Override
    protected void initChannel(SocketChannel ch) {
        ChannelPipeline pipeline = ch.pipeline();

        // TLS first — every other handler downstream sees decrypted bytes.
        // newHandler creates a fresh SslHandler per connection (required).
        if (sslCtx != null) {
            pipeline.addLast(sslCtx.newHandler(ch.alloc()));
        }

        // Idle detection: 60s read timeout, 30s write timeout
        pipeline.addLast(new IdleStateHandler(60, 30, 0, TimeUnit.SECONDS));

        // Codec
        pipeline.addLast(new PacketDecoder());
        pipeline.addLast(new PacketEncoder());

        // Game logic handler
        pipeline.addLast(new GamePacketHandler(worldManager, redisManager, monsterManager));
    }
}
