package com.astrion.common.packet;

/**
 * Wire format: [length(4 bytes)][packetType(1 byte)][json payload(N bytes)]
 */
public class GamePacket {
    private PacketType type;
    private String payload;

    public GamePacket() {}

    public GamePacket(PacketType type, String payload) {
        this.type = type;
        this.payload = payload;
    }

    public PacketType getType() { return type; }
    public void setType(PacketType type) { this.type = type; }
    public String getPayload() { return payload; }
    public void setPayload(String payload) { this.payload = payload; }
}
