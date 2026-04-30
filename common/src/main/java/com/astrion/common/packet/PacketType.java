package com.astrion.common.packet;

public enum PacketType {
    // Client -> Server
    LOGIN(0x01),
    MOVE(0x02),
    CHAT(0x03),
    ATTACK(0x04),
    CHARACTER_LIST(0x05),
    CHARACTER_CREATE(0x06),
    CHARACTER_DELETE(0x07),

    // Server -> Client
    LOGIN_RESULT(0x81),
    SPAWN_PLAYER(0x82),
    DESPAWN_PLAYER(0x83),
    PLAYER_MOVED(0x84),
    CHAT_MESSAGE(0x85),
    WORLD_STATE(0x86),
    CHARACTER_LIST_RESULT(0x87),
    CHARACTER_CREATE_RESULT(0x88),
    CHARACTER_DELETE_RESULT(0x89);

    private final int code;

    PacketType(int code) {
        this.code = code;
    }

    public int getCode() {
        return code;
    }

    public static PacketType fromCode(int code) {
        for (PacketType type : values()) {
            if (type.code == code) {
                return type;
            }
        }
        throw new IllegalArgumentException("Unknown packet code: " + code);
    }
}
