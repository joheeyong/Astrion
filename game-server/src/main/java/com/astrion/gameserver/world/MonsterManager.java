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
    private com.astrion.gameserver.redis.RedisManager redisManager; // injected post-construction
    private final ObjectMapper mapper = new ObjectMapper();

    /** Optional redis handle — when present, monster kills check the killer's
     *  party and share EXP/gold with same-zone party members at SHARE_RATE. */
    public void setRedisManager(com.astrion.gameserver.redis.RedisManager r) { this.redisManager = r; }
    private static final float PARTY_EXP_SHARE = 0.50f; // each non-killer party member in same zone
    private final ConcurrentHashMap<String, Monster> monsters = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<String, ItemDrop> activeDrops = new ConcurrentHashMap<>();
    private final Random rng = new Random();
    private final ScheduledExecutorService executor;

    public int getMonsterCount() { return monsters.size(); }
    public int getActiveDropCount() { return activeDrops.size(); }

    /** Snapshot of monster count per zoneId. Iterates once over the live map;
     *  safe to call from any thread because ConcurrentHashMap.values() returns
     *  a weakly consistent iterator. */
    public java.util.Map<String, Integer> getMonsterCountByZone() {
        java.util.TreeMap<String, Integer> out = new java.util.TreeMap<>();
        for (Monster m : monsters.values()) {
            String z = m.zoneId == null || m.zoneId.isEmpty() ? "(none)" : m.zoneId;
            out.merge(z, 1, Integer::sum);
        }
        return out;
    }

    public java.util.Map<String, Integer> getActiveDropCountByZone() {
        java.util.TreeMap<String, Integer> out = new java.util.TreeMap<>();
        for (ItemDrop d : activeDrops.values()) {
            String z = d.zoneId == null || d.zoneId.isEmpty() ? "(none)" : d.zoneId;
            out.merge(z, 1, Integer::sum);
        }
        return out;
    }

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
        // ───── forgotten_woods (existing — kept) ─────────────────────────
        // 3 slimes in forgotten_woods (HP 50)
        spawnFresh("slime", "forgotten_woods", -4f, -2.8f, 50, 3.5f, 1.6f);
        spawnFresh("slime", "forgotten_woods", 8f, -2.8f, 50, 3.5f, 1.6f);
        spawnFresh("slime", "forgotten_woods", -2f, 5.7f, 50, 3.5f, 1.6f);
        // 3 bats — airborne, faster, lower HP
        spawnBat("forgotten_woods", -10f, 1.8f);
        spawnBat("forgotten_woods",  3f,  3.5f);
        spawnBat("forgotten_woods", 14f,  2.6f);
        // Act I boss: Shadow Hulk
        spawnShadowHulk();

        // ───── New v1 worldmap (see docs/WORLDMAP.md) ───────────────────
        // Each spawnLine populates one hunting zone. Counts are intentionally
        // small (2–4 mobs/zone) so monsters_by_zone in /metrics stays readable
        // until the real density work happens in a later pass. HP/EXP follow
        // the Lv table in the design doc (HP ≈ Lv*8, EXP ≈ Lv*2).

        // Solaria tree
        spawnLine("snail",    "solaria_outskirts",  -5f, 3f, -2.8f,  30,  8, 2.5f, 0.8f);
        spawnLine("slime",    "sunlit_plains",      -4f, 5f, -2.8f,  60, 14, 3.5f, 1.6f);
        spawnLine("mushroom", "wheat_fields",       -3f, 4f, -2.8f, 100, 22, 3.0f, 1.4f);
        spawnLine("wolf",     "pinewood_trail",     -3f, 5f, -2.8f, 140, 32, 4.0f, 2.2f);

        // Pyresummit tree
        spawnLine("fire_imp",     "cinder_ridge",   -4f, 4f, -2.8f, 180, 42, 3.8f, 2.0f);
        spawnLine("gargoyle",     "ashfall_cliffs", -3f, 5f, -2.8f, 280, 60, 3.5f, 1.8f);
        spawnLine("lava_slime",   "magma_hollow",   -3f, 6f, -2.8f, 420, 95, 3.0f, 1.5f);

        // Verdaglen tree (forgotten_woods is the last node, already populated above)
        spawnLine("sprite", "mossglade",          -4f, 4f, -2.8f,  80, 20, 4.0f, 1.8f);
        spawnLine("faerie", "whispering_boughs",  -3f, 5f, -2.8f, 160, 38, 4.2f, 2.0f);
        spawnLine("ent",    "old_roots",          -3f, 5f, -2.8f, 260, 58, 2.8f, 1.2f);

        // Nightport tree
        spawnLine("alley_cat", "backalleys",          -4f, 4f, -2.8f, 220, 50, 4.5f, 2.2f);
        spawnLine("rat_king",  "sewer_tunnels",       -3f, 5f, -2.8f, 320, 72, 4.0f, 2.0f);
        spawnLine("golem",     "underground_vault",   -3f, 6f, -2.8f, 480,110, 2.5f, 1.0f);

        // Tidehaven tree
        spawnLine("jellyfish",    "tide_docks",       -4f, 4f, -2.8f, 280, 65, 3.5f, 1.6f);
        spawnLine("crab",         "driftwood_beach",  -3f, 5f, -2.8f, 360, 80, 3.8f, 1.8f);
        spawnLine("kraken_spawn", "sunken_reef",      -3f, 5f, -2.8f, 520,120, 4.2f, 2.0f);
    }

    /** Spawn a horizontal row of monsters between (x1, y) and (x2, y). Used to
     *  populate the v1 hunting zones uniformly without a 50-line wall of
     *  spawnFresh calls. step = (x2 - x1) split into 3 evenly-spaced positions. */
    private void spawnLine(String type, String zoneId, float x1, float x2, float y,
                           int hp, int exp, float range, float speed) {
        float[] xs = { x1, (x1 + x2) * 0.5f, x2 };
        for (float x : xs) {
            Monster m = spawnFresh(type, zoneId, x, y, hp, range, speed);
            m.expReward = exp;
        }
    }

    private Monster spawnBat(String zoneId, float x, float y) {
        String id = UUID.randomUUID().toString();
        Monster m = new Monster(id, "bat", zoneId, x, y, 25, 4.0f, 2.4f);
        m.expReward = 15;
        m.goldReward = 4;
        m.dropChance = 0.35f;
        m.dropTable = makeBatDropTable();
        m.contactDamage = 4;
        m.aggroSpeedMul = 1.8f;
        m.attackRange = 0.7f;
        m.attackCooldownMs = 700L;
        m.aggroDurationMs = 8_000L;
        monsters.put(id, m);
        return m;
    }

    private List<DropEntry> makeBatDropTable() {
        List<DropEntry> t = new ArrayList<>();
        t.add(new DropEntry("bread",          1, 1, 35f));
        t.add(new DropEntry("stardust",       1, 2, 50f));
        t.add(new DropEntry("elixir",         1, 1, 12f));
        t.add(new DropEntry("bronze_dagger",  1, 1,  3f));
        return t;
    }

    private Monster spawnShadowHulk() {
        String id = UUID.randomUUID().toString();
        Monster m = new Monster(id, "shadow_hulk", "forgotten_woods", 20f, -2.5f, 200, 2.5f, 0.8f);
        m.expReward = 250;
        m.goldReward = 120;
        m.dropChance = 1.0f; // boss always drops
        m.dropTable = makeShadowHulkDropTable();
        // Boss combat profile — hits harder, longer chase
        m.contactDamage = 30;
        m.aggroSpeedMul = 1.8f;
        m.attackRange = 1.3f;
        m.attackCooldownMs = 1400L;
        m.aggroDurationMs = 20_000L;
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
        // Slime combat profile
        m.contactDamage = 7;
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
                boolean aggro = !m.targetPlayerId.isEmpty() && now < m.aggroUntil;
                if (aggro) {
                    PlayerSession target = worldManager.getSessionByPlayerId(m.targetPlayerId);
                    if (target == null || !m.zoneId.equals(target.getZoneId())) {
                        // Lost the target — drop aggro
                        m.targetPlayerId = "";
                        m.aggroUntil = 0L;
                    } else {
                        var p = target.getPosition();
                        float dx = p.getX() - m.x;
                        float dy = p.getY() - m.y;
                        float dist = (float) Math.sqrt(dx * dx + dy * dy);

                        if (dist <= m.attackRange) {
                            // In range — try to attack on cooldown
                            if (now - m.lastAttackAt >= m.attackCooldownMs) {
                                m.lastAttackAt = now;
                                try {
                                    ObjectNode atk = mapper.createObjectNode();
                                    atk.put("id", m.id);
                                    atk.put("targetPlayerId", m.targetPlayerId);
                                    atk.put("damage", m.contactDamage);
                                    worldManager.broadcastToZone(m.zoneId,
                                        new GamePacket(PacketType.MONSTER_ATTACK,
                                            mapper.writeValueAsString(atk)));
                                } catch (Exception e) { /* ignore */ }
                            }
                            // Face the player while attacking
                            m.direction = dx >= 0 ? 1 : -1;
                        } else {
                            // Chase — move horizontally toward target
                            float chaseSpeed = m.speed * m.aggroSpeedMul;
                            float moveAmt = chaseSpeed * dt;
                            if (Math.abs(dx) <= moveAmt) m.x = p.getX();
                            else m.x += Math.signum(dx) * moveAmt;
                            m.direction = dx >= 0 ? 1 : -1;
                        }
                    }
                }
                if (!aggro) {
                    // Patrol AI (default)
                    m.x += m.direction * m.speed * dt;
                    if (Math.abs(m.x - m.originX) > m.patrolRange) {
                        m.direction = -m.direction;
                        m.x = m.originX + m.direction * m.patrolRange;
                    }
                }
                // Throttled broadcast — also fires when chasing so client sees movement
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
        onMonsterHit(attacker, monsterId, damage, false);
    }

    /// Splits a fraction of a kill's reward to the killer's same-zone party
    /// members. No-op when no redis handle is wired or when the killer isn't
    /// in a party; safe to call on every kill.
    private void sharePartyExp(PlayerSession killer, Monster monster) {
        if (redisManager == null) return;
        String partyId = redisManager.getPartyOf(killer.getPlayerId());
        if (partyId == null || partyId.isEmpty()) return;
        java.util.Set<String> members = redisManager.getPartyMembers(partyId);
        if (members == null || members.isEmpty()) return;

        int sharedExp  = Math.max(1, Math.round(monster.expReward  * PARTY_EXP_SHARE));
        int sharedGold = Math.max(0, Math.round(monster.goldReward * PARTY_EXP_SHARE));
        String json = "{\"exp\":" + sharedExp + ",\"gold\":" + sharedGold + "}";
        String zone = killer.getZoneId();
        for (String name : members) {
            if (name.equals(killer.getPlayerId())) continue;
            PlayerSession s = worldManager.getSessionByPlayerId(name);
            if (s == null) continue;
            if (!zone.equals(s.getZoneId())) continue;
            try {
                s.getChannel().writeAndFlush(new GamePacket(PacketType.EXP_GAINED, json));
            } catch (Exception ignored) { /* best effort */ }
        }
    }

    public void onMonsterHit(PlayerSession attacker, String monsterId, int damage, boolean isCritical) {
        Monster m = monsters.get(monsterId);
        if (m == null || m.dead) return;
        int applied = Math.min(m.hp, Math.max(1, damage));
        m.hp = Math.max(0, m.hp - applied);
        m.lastHitterId = attacker.getPlayerId();
        // Remember crit so broadcast can include it
        m.lastHitCritical = isCritical;
        // Aggro on first / each hit — hostile until aggroDuration after last hit
        m.targetPlayerId = attacker.getPlayerId();
        m.aggroUntil = System.currentTimeMillis() + m.aggroDurationMs;
        if (m.hp <= 0) {
            m.dead = true;
            m.respawnAt = System.currentTimeMillis() + RESPAWN_DELAY_MS;
            broadcastDie(m, applied);
            // Award EXP + gold to the killing blow.
            try {
                String json = "{\"exp\":" + m.expReward + ",\"gold\":" + m.goldReward + "}";
                attacker.getChannel().writeAndFlush(new GamePacket(PacketType.EXP_GAINED, json));
            } catch (Exception e) { /* ignore */ }
            // Party share — each online party member in the same zone gets a
            // fixed fraction. Solo grinders keep the full reward; partying
            // pays out more total but rewards group play directly.
            sharePartyExp(attacker, m);
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
            n.put("crit", m.lastHitCritical);
            worldManager.broadcastToZone(m.zoneId, new GamePacket(PacketType.MONSTER_DIE, mapper.writeValueAsString(n)));
        } catch (Exception e) { /* ignore */ }
    }

    private void broadcastHp(Monster m, int damage) {
        try {
            ObjectNode n = mapper.createObjectNode();
            n.put("id", m.id);
            n.put("hp", m.hp);
            n.put("damage", damage);
            n.put("crit", m.lastHitCritical);
            worldManager.broadcastToZone(m.zoneId, new GamePacket(PacketType.MONSTER_HP, mapper.writeValueAsString(n)));
        } catch (Exception e) { /* ignore */ }
    }

    public void shutdown() {
        executor.shutdownNow();
    }
}
