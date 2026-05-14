package com.astrion.gameserver.world;

import com.astrion.common.packet.GamePacket;
import com.astrion.common.packet.PacketType;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ObjectNode;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;

public class MonsterManager {

    private static final Logger log = LoggerFactory.getLogger(MonsterManager.class);
    private static final long RESPAWN_DELAY_MS = 15_000L;
    private static final long TICK_INTERVAL_MS = 100L;
    private static final long BROADCAST_INTERVAL_MS = 200L;

    private final WorldManager worldManager;
    private final ObjectMapper mapper = new ObjectMapper();
    private final ConcurrentHashMap<String, Monster> monsters = new ConcurrentHashMap<>();
    private final ScheduledExecutorService executor;

    public MonsterManager(WorldManager worldManager) {
        this.worldManager = worldManager;
        this.executor = Executors.newSingleThreadScheduledExecutor(r -> {
            Thread t = new Thread(r, "monster-tick");
            t.setDaemon(true);
            return t;
        });
        spawnInitial();
        executor.scheduleAtFixedRate(this::tick, TICK_INTERVAL_MS, TICK_INTERVAL_MS, TimeUnit.MILLISECONDS);
        log.info("MonsterManager started with {} monsters", monsters.size());
    }

    private void spawnInitial() {
        // 3 slimes in forgotten_woods (HP 50)
        spawnFresh("slime", "forgotten_woods", -4f, -2.8f, 50, 3.5f, 1.6f);
        spawnFresh("slime", "forgotten_woods", 8f, -2.8f, 50, 3.5f, 1.6f);
        spawnFresh("slime", "forgotten_woods", -2f, 5.7f, 50, 3.5f, 1.6f);
    }

    private Monster spawnFresh(String type, String zoneId, float x, float y, int hp, float range, float speed) {
        String id = UUID.randomUUID().toString();
        Monster m = new Monster(id, type, zoneId, x, y, hp, range, speed);
        monsters.put(id, m);
        return m;
    }

    private void tick() {
        try {
            long now = System.currentTimeMillis();
            float dt = TICK_INTERVAL_MS / 1000f;

            for (Monster m : monsters.values()) {
                if (m.dead) {
                    if (now >= m.respawnAt) {
                        // Respawn
                        m.dead = false;
                        m.hp = m.maxHp;
                        m.x = m.originX;
                        m.y = m.originY;
                        m.direction = 1;
                        m.lastBroadcastX = m.x;
                        m.lastBroadcastY = m.y;
                        m.lastBroadcastDir = m.direction;
                        m.lastBroadcastAt = now;
                        broadcastSpawn(m);
                    }
                    continue;
                }
                // Patrol AI
                m.x += m.direction * m.speed * dt;
                if (Math.abs(m.x - m.originX) > m.patrolRange) {
                    m.direction = -m.direction;
                    m.x = m.originX + m.direction * m.patrolRange;
                }
                // Throttled broadcast
                if (now - m.lastBroadcastAt >= BROADCAST_INTERVAL_MS
                    || m.direction != m.lastBroadcastDir) {
                    broadcastMove(m);
                    m.lastBroadcastAt = now;
                    m.lastBroadcastX = m.x;
                    m.lastBroadcastY = m.y;
                    m.lastBroadcastDir = m.direction;
                }
            }
        } catch (Exception e) {
            log.error("Monster tick error: {}", e.getMessage(), e);
        }
    }

    /**
     * Called when a player enters a zone — send them all alive monsters in that zone.
     */
    public void onPlayerEnteredZone(PlayerSession session) {
        String zoneId = session.getZoneId();
        if (zoneId == null || zoneId.isEmpty()) return;
        for (Monster m : monsters.values()) {
            if (m.dead) continue;
            if (!zoneId.equals(m.zoneId)) continue;
            try {
                ObjectNode n = mapper.createObjectNode();
                n.put("id", m.id);
                n.put("type", m.type);
                n.put("x", m.x);
                n.put("y", m.y);
                n.put("hp", m.hp);
                n.put("maxHp", m.maxHp);
                n.put("direction", m.direction);
                session.getChannel().writeAndFlush(new GamePacket(PacketType.MONSTER_SPAWN, mapper.writeValueAsString(n)));
            } catch (Exception e) { /* ignore */ }
        }
    }

    public void onMonsterHit(PlayerSession attacker, String monsterId, int damage) {
        Monster m = monsters.get(monsterId);
        if (m == null || m.dead) return;
        int applied = Math.min(m.hp, Math.max(1, damage));
        m.hp = Math.max(0, m.hp - applied);
        m.lastHitterId = attacker.getPlayerId();
        if (m.hp <= 0) {
            m.dead = true;
            m.respawnAt = System.currentTimeMillis() + RESPAWN_DELAY_MS;
            broadcastDie(m, applied);
            // Award EXP to the killing blower
            try {
                String json = "{\"exp\":" + m.expReward + "}";
                attacker.getChannel().writeAndFlush(new GamePacket(PacketType.EXP_GAINED, json));
            } catch (Exception e) { /* ignore */ }
            log.info("Monster {} killed by {} for {} dmg (+{} exp; respawn in {}s)",
                m.id, attacker.getPlayerId(), applied, m.expReward, RESPAWN_DELAY_MS / 1000);
        } else {
            broadcastHp(m, applied);
        }
    }

    private void broadcastSpawn(Monster m) {
        try {
            ObjectNode n = mapper.createObjectNode();
            n.put("id", m.id);
            n.put("type", m.type);
            n.put("x", m.x);
            n.put("y", m.y);
            n.put("hp", m.hp);
            n.put("maxHp", m.maxHp);
            n.put("direction", m.direction);
            worldManager.broadcastToZone(m.zoneId, new GamePacket(PacketType.MONSTER_SPAWN, mapper.writeValueAsString(n)));
        } catch (Exception e) { /* ignore */ }
    }

    private void broadcastMove(Monster m) {
        try {
            ObjectNode n = mapper.createObjectNode();
            n.put("id", m.id);
            n.put("x", m.x);
            n.put("y", m.y);
            n.put("direction", m.direction);
            worldManager.broadcastToZone(m.zoneId, new GamePacket(PacketType.MONSTER_MOVE, mapper.writeValueAsString(n)));
        } catch (Exception e) { /* ignore */ }
    }

    private void broadcastDie(Monster m, int damage) {
        try {
            ObjectNode n = mapper.createObjectNode();
            n.put("id", m.id);
            n.put("damage", damage);
            worldManager.broadcastToZone(m.zoneId, new GamePacket(PacketType.MONSTER_DIE, mapper.writeValueAsString(n)));
        } catch (Exception e) { /* ignore */ }
    }

    private void broadcastHp(Monster m, int damage) {
        try {
            ObjectNode n = mapper.createObjectNode();
            n.put("id", m.id);
            n.put("hp", m.hp);
            n.put("damage", damage);
            worldManager.broadcastToZone(m.zoneId, new GamePacket(PacketType.MONSTER_HP, mapper.writeValueAsString(n)));
        } catch (Exception e) { /* ignore */ }
    }

    public void shutdown() {
        executor.shutdownNow();
    }
}
