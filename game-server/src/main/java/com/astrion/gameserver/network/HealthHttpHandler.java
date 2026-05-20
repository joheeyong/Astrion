package com.astrion.gameserver.network;

import com.astrion.gameserver.world.MonsterManager;
import com.astrion.gameserver.world.PlayerSession;
import com.astrion.gameserver.world.WorldManager;
import io.netty.buffer.Unpooled;
import io.netty.channel.ChannelFutureListener;
import io.netty.channel.ChannelHandlerContext;
import io.netty.channel.SimpleChannelInboundHandler;
import io.netty.handler.codec.http.DefaultFullHttpResponse;
import io.netty.handler.codec.http.FullHttpRequest;
import io.netty.handler.codec.http.FullHttpResponse;
import io.netty.handler.codec.http.HttpHeaderNames;
import io.netty.handler.codec.http.HttpHeaderValues;
import io.netty.handler.codec.http.HttpMethod;
import io.netty.handler.codec.http.HttpResponseStatus;
import io.netty.handler.codec.http.HttpUtil;
import io.netty.handler.codec.http.HttpVersion;

import java.nio.charset.StandardCharsets;
import java.util.Map;
import java.util.TreeMap;

/**
 * Minimal HTTP introspection endpoint for operations / uptime probes.
 *
 * GET /health     — liveness probe, returns {status:"ok"} only
 * GET /metrics    — full snapshot: uptime, online players, monsters, drops
 *
 * Lives on a separate port from the game protocol (see GameServerMain).
 * Lock down at the firewall — anything past liveness can leak operational
 * data to an attacker.
 */
public class HealthHttpHandler extends SimpleChannelInboundHandler<FullHttpRequest> {

    private final WorldManager worldManager;
    private final MonsterManager monsterManager;
    private final long startTimeMs;

    public HealthHttpHandler(WorldManager worldManager, MonsterManager monsterManager, long startTimeMs) {
        this.worldManager = worldManager;
        this.monsterManager = monsterManager;
        this.startTimeMs = startTimeMs;
    }

    @Override
    protected void channelRead0(ChannelHandlerContext ctx, FullHttpRequest req) {
        if (!HttpMethod.GET.equals(req.method())) {
            send(ctx, req, HttpResponseStatus.METHOD_NOT_ALLOWED, "{\"error\":\"GET only\"}");
            return;
        }

        // Strip query string before matching
        String path = req.uri();
        int q = path.indexOf('?');
        if (q >= 0) path = path.substring(0, q);

        switch (path) {
            case "/health":
                send(ctx, req, HttpResponseStatus.OK, "{\"status\":\"ok\"}");
                return;
            case "/metrics":
                send(ctx, req, HttpResponseStatus.OK, buildMetricsJson());
                return;
            default:
                send(ctx, req, HttpResponseStatus.NOT_FOUND, "{\"error\":\"not found\"}");
        }
    }

    private String buildMetricsJson() {
        long uptimeSec = (System.currentTimeMillis() - startTimeMs) / 1000L;
        int players = worldManager.getAllSessions().size();
        int monsters = monsterManager.getMonsterCount();
        int drops = monsterManager.getActiveDropCount();
        Runtime rt = Runtime.getRuntime();
        long heapUsedMb = (rt.totalMemory() - rt.freeMemory()) / (1024L * 1024L);
        long heapMaxMb = rt.maxMemory() / (1024L * 1024L);

        // Single pass over the session collection. TreeMap so the JSON has a
        // stable, alphabetised key order — easier to diff between polls.
        TreeMap<String, Integer> byZone = new TreeMap<>();
        for (PlayerSession s : worldManager.getAllSessions()) {
            String z = s.getZoneId();
            if (z == null || z.isEmpty()) z = "(none)"; // logged in, not yet ZONE_ENTER
            byZone.merge(z, 1, Integer::sum);
        }

        StringBuilder sb = new StringBuilder(384);
        sb.append('{')
          .append("\"version\":\"").append(com.astrion.common.Version.CURRENT).append("\",")
          .append("\"uptime_seconds\":").append(uptimeSec).append(',')
          .append("\"players_online\":").append(players).append(',')
          .append("\"monsters\":").append(monsters).append(',')
          .append("\"active_drops\":").append(drops).append(',')
          .append("\"heap_used_mb\":").append(heapUsedMb).append(',')
          .append("\"heap_max_mb\":").append(heapMaxMb).append(',')
          .append("\"players_by_zone\":{");
        boolean first = true;
        for (Map.Entry<String, Integer> e : byZone.entrySet()) {
            if (!first) sb.append(',');
            first = false;
            sb.append('"').append(escapeJson(e.getKey())).append("\":").append(e.getValue());
        }
        sb.append("}}");
        return sb.toString();
    }

    /** Minimal JSON-string escape — zone IDs are internal constants, but a
     *  defensive escape keeps the response well-formed if one ever changes. */
    private static String escapeJson(String s) {
        if (s.indexOf('"') < 0 && s.indexOf('\\') < 0) return s;
        return s.replace("\\", "\\\\").replace("\"", "\\\"");
    }

    private static void send(ChannelHandlerContext ctx, FullHttpRequest req, HttpResponseStatus status, String body) {
        FullHttpResponse resp = new DefaultFullHttpResponse(
            HttpVersion.HTTP_1_1, status,
            Unpooled.copiedBuffer(body, StandardCharsets.UTF_8));
        resp.headers().set(HttpHeaderNames.CONTENT_TYPE, "application/json; charset=utf-8");
        resp.headers().setInt(HttpHeaderNames.CONTENT_LENGTH, resp.content().readableBytes());
        // No keep-alive needed for probes — close after each response.
        if (HttpUtil.isKeepAlive(req)) {
            resp.headers().set(HttpHeaderNames.CONNECTION, HttpHeaderValues.KEEP_ALIVE);
            ctx.writeAndFlush(resp);
        } else {
            ctx.writeAndFlush(resp).addListener(ChannelFutureListener.CLOSE);
        }
    }

    @Override
    public void exceptionCaught(ChannelHandlerContext ctx, Throwable cause) {
        ctx.close();
    }
}
