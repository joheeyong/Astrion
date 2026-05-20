package com.astrion.gameserver.network;

import io.netty.channel.ChannelHandler;
import io.netty.channel.ChannelHandlerContext;
import io.netty.channel.ChannelInboundHandlerAdapter;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.net.InetSocketAddress;
import java.net.SocketAddress;

/**
 * Pipeline-first handler that rejects new connections from over-frequent IPs
 * before TLS even starts negotiating. @Sharable because one instance services
 * every channel — the rate limiter is the shared state.
 */
@ChannelHandler.Sharable
public class ConnectionRateLimitHandler extends ChannelInboundHandlerAdapter {

    private static final Logger log = LoggerFactory.getLogger(ConnectionRateLimitHandler.class);
    private final ConnectionRateLimiter limiter;

    public ConnectionRateLimitHandler(ConnectionRateLimiter limiter) {
        this.limiter = limiter;
    }

    @Override
    public void channelActive(ChannelHandlerContext ctx) {
        String ip = clientIpOf(ctx);
        // Same loopback bypass logic as LoginRateLimiter — load tests run
        // from inside the box and need to skip the gate that protects
        // the public interface.
        if (!"127.0.0.1".equals(ip) && !"0:0:0:0:0:0:0:1".equals(ip)) {
            if (!limiter.allow(ip)) {
                // Don't even propagate channelActive; downstream handlers
                // (SslHandler, codec, GamePacketHandler) never see this socket.
                log.warn("Rejecting connection from {}: connection rate limit", ip);
                ctx.close();
                return;
            }
        }
        ctx.fireChannelActive();
    }

    private static String clientIpOf(ChannelHandlerContext ctx) {
        SocketAddress addr = ctx.channel().remoteAddress();
        if (addr instanceof InetSocketAddress) {
            return ((InetSocketAddress) addr).getAddress().getHostAddress();
        }
        return addr == null ? "unknown" : addr.toString();
    }
}
