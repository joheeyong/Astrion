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
    private final com.astrion.gameserver.world.TradeManager tradeManager;
    private final com.astrion.gameserver.world.AchievementManager achievements;
    private final com.astrion.gameserver.world.AuctionManager auctions;
    private final com.astrion.gameserver.world.PlayerStateLocks playerLocks;
    private final SslContext sslCtx; // null when TLS is disabled
    private final ConnectionRateLimitHandler connectionRateLimit;

    public GameServerInitializer(WorldManager worldManager, RedisManager redisManager,
                                 MonsterManager monsterManager,
                                 com.astrion.gameserver.world.TradeManager tradeManager,
                                 com.astrion.gameserver.world.AchievementManager achievements,
                                 com.astrion.gameserver.world.AuctionManager auctions,
                                 com.astrion.gameserver.world.PlayerStateLocks playerLocks,
                                 SslContext sslCtx,
                                 ConnectionRateLimitHandler connectionRateLimit) {
        this.worldManager = worldManager;
        this.redisManager = redisManager;
        this.monsterManager = monsterManager;
        this.tradeManager = tradeManager;
        this.achievements = achievements;
        this.auctions = auctions;
        this.playerLocks = playerLocks;
        this.sslCtx = sslCtx;
        this.connectionRateLimit = connectionRateLimit;
    }

    @Override
    protected void initChannel(SocketChannel ch) {
        ChannelPipeline pipeline = ch.pipeline();

        // Connection-rate gate FIRST — before TLS even gets the chance to
        // burn cycles on a handshake from a flooding IP. @Sharable means the
        // same handler instance lives on every channel and consults shared
        // per-IP state.
        pipeline.addLast(connectionRateLimit);

        // TLS next — every other handler downstream sees decrypted bytes.
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
        pipeline.addLast(new GamePacketHandler(worldManager, redisManager, monsterManager, tradeManager, achievements, auctions, playerLocks));
    }
}
