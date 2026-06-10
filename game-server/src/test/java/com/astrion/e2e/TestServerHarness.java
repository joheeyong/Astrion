package com.astrion.e2e;

import com.astrion.gameserver.network.ConnectionRateLimitHandler;
import com.astrion.gameserver.network.ConnectionRateLimiter;
import com.astrion.gameserver.network.GameServerInitializer;
import com.astrion.gameserver.redis.RedisManager;
import com.astrion.gameserver.world.AchievementManager;
import com.astrion.gameserver.world.AuctionManager;
import com.astrion.gameserver.world.MonsterManager;
import com.astrion.gameserver.world.PlayerStateLocks;
import com.astrion.gameserver.world.TradeManager;
import com.astrion.gameserver.world.WorldManager;
import io.netty.bootstrap.ServerBootstrap;
import io.netty.channel.Channel;
import io.netty.channel.ChannelOption;
import io.netty.channel.EventLoopGroup;
import io.netty.channel.nio.NioEventLoopGroup;
import io.netty.channel.socket.nio.NioServerSocketChannel;
import io.netty.util.concurrent.DefaultEventExecutorGroup;

import java.net.InetSocketAddress;

/// Boots the full game server in-JVM for E2E tests: the same manager
/// wiring as GameServerMain, the real pipeline (rate gate → idle →
/// codec → business-executor-pinned GamePacketHandler), a real Redis
/// (localhost:6379 by default, ASTRION_REDIS_HOST to override — the CI
/// job provides a redis service container), and an ephemeral port so
/// parallel CI jobs can't collide.
///
/// Deliberately omitted vs production: TLS (no certs → bots speak
/// plaintext), the HTTP health listeners, and the JVM shutdown hook
/// (stop() handles teardown explicitly).
final class TestServerHarness {

    private final EventLoopGroup boss;
    private final EventLoopGroup worker;
    private final DefaultEventExecutorGroup business;
    private final RedisManager redis;
    private final MonsterManager monsters;
    private final AuctionManager auctions;
    private final Channel serverChannel;
    private final int port;

    private TestServerHarness(EventLoopGroup boss, EventLoopGroup worker,
                              DefaultEventExecutorGroup business, RedisManager redis,
                              MonsterManager monsters, AuctionManager auctions,
                              Channel serverChannel, int port) {
        this.boss = boss;
        this.worker = worker;
        this.business = business;
        this.redis = redis;
        this.monsters = monsters;
        this.auctions = auctions;
        this.serverChannel = serverChannel;
        this.port = port;
    }

    static TestServerHarness start() throws InterruptedException {
        String redisHost = System.getenv().getOrDefault("ASTRION_REDIS_HOST", "localhost");
        RedisManager redis = new RedisManager(redisHost, 6379);
        WorldManager world = new WorldManager();
        MonsterManager monsters = new MonsterManager(world);
        monsters.setRedisManager(redis);
        PlayerStateLocks locks = new PlayerStateLocks();
        TradeManager trade = new TradeManager(world, redis, locks);
        AchievementManager achievements = new AchievementManager(world, redis, locks);
        monsters.setAchievementManager(achievements);
        trade.setAchievementManager(achievements);
        AuctionManager auctions = new AuctionManager(world, redis, locks);

        EventLoopGroup boss = new NioEventLoopGroup(1);
        EventLoopGroup worker = new NioEventLoopGroup(2);
        DefaultEventExecutorGroup business = new DefaultEventExecutorGroup(4);
        ConnectionRateLimitHandler gate = new ConnectionRateLimitHandler(new ConnectionRateLimiter());

        ServerBootstrap bootstrap = new ServerBootstrap()
            .group(boss, worker)
            .channel(NioServerSocketChannel.class)
            .childHandler(new GameServerInitializer(world, redis, monsters, trade,
                achievements, auctions, locks, business, /* sslCtx */ null, gate))
            .childOption(ChannelOption.TCP_NODELAY, true);

        Channel ch = bootstrap.bind(0).sync().channel(); // 0 → ephemeral port
        int port = ((InetSocketAddress) ch.localAddress()).getPort();
        return new TestServerHarness(boss, worker, business, redis, monsters, auctions, ch, port);
    }

    int port() { return port; }
    RedisManager redis() { return redis; }

    void stop() {
        try { serverChannel.close().sync(); } catch (InterruptedException ignored) {}
        worker.shutdownGracefully();
        boss.shutdownGracefully();
        business.shutdownGracefully();
        auctions.shutdown();
        monsters.shutdown();
        redis.shutdown();
    }
}
