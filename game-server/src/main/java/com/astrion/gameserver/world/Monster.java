package com.astrion.gameserver.world;

public class Monster {
    public final String id;
    public final String type;
    public final String zoneId;
    public final float originX, originY;
    public final float patrolRange;
    public final float speed;
    public final int maxHp;

    public float x, y;
    public int hp;
    public int direction = 1; // +1 or -1
    public boolean dead = false;
    public long respawnAt = 0L;
    public int expReward = 30; // EXP given to last hitter on death
    public int goldReward = 8; // gold given to last hitter on death
    public String lastHitterId = "";

    // Drop table — populated by MonsterManager.spawnFresh
    public java.util.List<MonsterManager.DropEntry> dropTable;
    public float dropChance = 0.5f;

    // Broadcast throttling
    public long lastBroadcastAt = 0L;
    public float lastBroadcastX, lastBroadcastY;
    public int lastBroadcastDir = 1;

    public Monster(String id, String type, String zoneId,
                   float x, float y, int maxHp, float patrolRange, float speed) {
        this.id = id;
        this.type = type;
        this.zoneId = zoneId;
        this.x = x; this.y = y;
        this.originX = x; this.originY = y;
        this.maxHp = maxHp; this.hp = maxHp;
        this.patrolRange = patrolRange;
        this.speed = speed;
        this.lastBroadcastX = x;
        this.lastBroadcastY = y;
    }
}
