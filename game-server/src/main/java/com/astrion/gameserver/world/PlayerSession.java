package com.astrion.gameserver.world;

import com.astrion.common.model.Position;
import io.netty.channel.Channel;

public class PlayerSession {
    private final String playerId;
    private final Channel channel;
    private Position position;
    private String zoneId = "";

    public PlayerSession(String playerId, Channel channel) {
        this.playerId = playerId;
        this.channel = channel;
        this.position = new Position(0, 0, 0);
    }

    public String getPlayerId() { return playerId; }
    public Channel getChannel() { return channel; }
    public Position getPosition() { return position; }
    public void setPosition(Position position) { this.position = position; }
    public String getZoneId() { return zoneId; }
    public void setZoneId(String zoneId) { this.zoneId = zoneId == null ? "" : zoneId; }
}
