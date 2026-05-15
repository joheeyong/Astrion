package com.astrion.gameserver.world;

import com.astrion.common.packet.GamePacket;
import com.astrion.common.packet.PacketType;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ObjectNode;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.ArrayList;
import java.util.List;
import java.util.Random;
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

    private static final long DROP_LIFETIME_MS = 30_000L;

    public static class DropEntry {
        public final String itemId;
        public final int minQty, maxQty;
        public final float weight;
        public DropEntry(String itemId, int minQty, int maxQty, float weight) {
            this.itemId = itemId; this.minQty = minQty; this.maxQty = maxQty; this.weight = weight;
        }
    }

    public static class ItemDrop {
        public String dropId;
        public String zoneId;
        public String itemId;
        public int quantity;
        public float x, y;
        public long expiresAt;
    }

    private final WorldManager worldManager;
    private final ObjectMapper mapper = new ObjectMapper();
    private final ConcurrentHashMap<String, Monster> monsters = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<String, ItemDrop> activeDrops = new ConcurrentHashMap<>();
    private final Random rng = new Random();
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
        // Act I boss: Shadow Hulk
        spawnShadowHulk();
    }

    private Monster spawnShadowHulk() {
        String id = UUID.randomUUID().toString();
        Monster m = new Monster(id, "shadow_hulk", "forgotten_woods", 20f, -2.5f, 200, 2.5f, 0.8f);
        m.expReward = 250;
        m.goldReward = 120;
        m.dropChance = 1.0f; // boss always drops
        m.dropTable = makeShadowHulkDropTable();
        monsters.put(id, m);
        return m;
    }

    private List<DropEntry> makeShadowHulkDropTable() {
        List<DropEntry> t = new ArrayList<>();
        t.add(new DropEntry("dawn_dagger",    1, 1, 25f)); // 25% — legendary
        t.add(new DropEntry("iron_dagger",    1, 1, 35f));
        t.add(new DropEntry("stardust_ring",  1, 1, 15f));
        t.add(new DropEntry("stardust",       5, 10, 25f));
        return t;
    }

    private Monster spawnFresh(String type, String zoneId, float x, float y, int hp, float range, float speed) {
        String id = UUID.randomUUID().toString();
        Monster m = new Monster(id, type, zoneId, x, y, hp, range, speed);
        m.dropChance = 0.5f;
        m.goldReward = 8;
        m.dropTable = makeSlimeDropTable();
        monsters.put(id, m);
        return m;
    }

    private List<DropEntry> makeSlimeDropTable() {
        List<DropEntry> t = new ArrayList<>();
        t.add(new DropEntry("bread",          1, 1, 40f));
        t.add(new DropEntry("stardust",       1, 3, 25f));
        t.add(new DropEntry("bronze_dagger",  1, 1, 15f));
        t.add(new DropEntry("leather_helmet", 1, 1, 10f));
        t.add(new DropEntry("iron_dagger",    1, 1,  7f));
        t.add(new DropEntry("dawn_dagger",    1, 1,  3f));
        return t;
    }

    private void rollAndSpawnDrop(Monster m) {
        if (m.dropTable == null || m.dropTable.isEmpty()) return;
        if (rng.nextFloat() > m.dropChance) return; // no drop this time

        float total = 0f;
        for (DropEntry e : m.dropTable) total += e.weight;
        float roll = rng.nextFloat() * total;
        float cum = 0f;
        DropEntry chosen = null;
        for (DropEntry e : m.dropTable) {
            cum += e.weight;
            if (roll <= cum) { chosen = e; break; }
        }
        if (chosen == null) return;
        int qty = chosen.minQty + (chosen.maxQty > chosen.minQty ? rng.nextInt(chosen.maxQty - chosen.minQty + 1) : 0);

        ItemDrop drop = new ItemDrop();
        drop.dropId = UUID.randomUUID().toString();
        drop.zoneId = m.zoneId;
        drop.itemId = chosen.itemId;
        drop.quantity = qty;
        drop.x = m.x;
        drop.y = m.y;
        drop.expiresAt = System.currentTimeMillis() + DROP_LIFETIME_MS;
        activeDrops.put(drop.dropId, drop);
        broadcastDropSpawn(drop);
    }

    public void onDropClaim(PlayerSession claimer, String dropId) {
        ItemDrop drop = activeDrops.remove(dropId);
        if (drop == null) return;
        try {
            ObjectNode grant = mapper.createObjectNode();
            grant.put("dropId", drop.dropId);
            grant.put("itemId", drop.itemId);
            grant.put("quantity", drop.quantity);
            claimer.getChannel().writeAndFlush(new GamePacket(PacketType.DROP_GRANTED, mapper.writeValueAsString(grant)));

            ObjectNode rem = mapper.createObjectNode();
            rem.put("dropId", drop.dropId);
            worldManager.broadcastToZone(drop.zoneId, new GamePacket(PacketType.DROP_REMOVED, mapper.writeValueAsString(rem)));
            log.info("Drop {} claimed by {} ({} x{})", drop.dropId, claimer.getPlayerId(), drop.itemId, drop.quantity);
        } catch (Exception e) { /* ignore */ }
    }

    private void broadcastDropSpawn(ItemDrop d) {
        try {
            ObjectNode n = mapper.createObjectNode();
            n.put("dropId", d.dropId);
            n.put("itemId", d.itemId);
            n.put("quantity", d.quantity);
            n.put("x", d.x);
            n.put("y", d.y);
            worldManager.broadcastToZone(d.zoneId, new GamePacket(PacketType.DROP_SPAWN, mapper.writeValueAsString(n)));
        } catch (Exception e) { /* ignore */ }
    }

    private void sweepExpiredDrops(long now) {
        for (var entry : activeDrops.entrySet()) {
            ItemDrop d = entry.getValue();
            if (d.expiresAt < now) {
                activeDrops.remove(entry.getKey());
                try {
                    ObjectNode rem = mapper.createObjectNode();
                    rem.put("dropId", d.dropId);
                    worldManager.broadcastToZone(d.zoneId, new GamePacket(PacketType.DROP_REMOVED, mapper.writeValueAsString(rem)));
                } catch (Exception e) { /* ignore */ }
            }
        }
    }

    private void tick() {
        try {
            long now = System.currentTimeMillis();
            float dt = TICK_INTERVAL_MS / 1000f;
            sweepExpiredDrops(now);

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
        // Send active drops in this zone too
        for (ItemDrop d : activeDrops.values()) {
            if (!zoneId.equals(d.zoneId)) continue;
            try {
                ObjectNode n = mapper.createObjectNode();
                n.put("dropId", d.dropId);
                n.put("itemId", d.itemId);
                n.put("quantity", d.quantity);
                n.put("x", d.x);
                n.put("y", d.y);
                session.getChannel().writeAndFlush(new GamePacket(PacketType.DROP_SPAWN, mapper.writeValueAsString(n)));
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
            // Award EXP + gold to the killing blower
            try {
                String json = "{\"exp\":" + m.expReward + ",\"gold\":" + m.goldReward + "}";
                attacker.getChannel().writeAndFlush(new GamePacket(PacketType.EXP_GAINED, json));
            } catch (Exception e) { /* ignore */ }
            // Roll for an item drop (zone-wide, first-claim wins)
            rollAndSpawnDrop(m);
            log.info("Monster {} killed by {} for {} dmg (+{} exp, +{} gold; respawn in {}s)",
                m.id, attacker.getPlayerId(), applied, m.expReward, m.goldReward, RESPAWN_DELAY_MS / 1000);
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
