package com.astrion.gameserver.world;

import com.astrion.common.model.Position;
import io.netty.channel.Channel;

public class PlayerSession {
    private final String playerId;
    private final Channel channel;
    private Position position;
    private String zoneId = "";
    private String nickname = ""; // character name; falls back to playerId

    public PlayerSession(String playerId, Channel channel) {
        this.playerId = playerId;
        this.channel = channel;
        this.position = new Position(0, 0, 0);
        this.nickname = playerId;
    }

    public String getPlayerId() { return playerId; }
    public Channel getChannel() { return channel; }
    public Position getPosition() { return position; }
    public void setPosition(Position position) { this.position = position; }
    public String getZoneId() { return zoneId; }
    public void setZoneId(String zoneId) { this.zoneId = zoneId == null ? "" : zoneId; }
    public String getNickname() { return nickname == null || nickname.isEmpty() ? playerId : nickname; }
    public void setNickname(String nickname) { this.nickname = nickname == null ? "" : nickname; }

    // Combat stats reported by the client via STATUS_UPDATE — used to cap incoming damage
    public int level = 1;
    public int intStat = 5;
    public int weaponDmg = 0;
    public int starboltLv = 1;

    // Movement validation state
    public long lastMoveAt = 0L; // 0 = skip next validation (fresh session or zone change)
    public Position lastValidPos = new Position(0, 0, 0);
}
