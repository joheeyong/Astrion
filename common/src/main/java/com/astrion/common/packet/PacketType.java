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
    STATE_REQUEST(0x09),
    STATE_SAVE(0x0A),
    ZONE_ENTER(0x0B),
    MONSTER_HIT(0x0C),
    SKILL_CAST(0x0D),
    DROP_CLAIM(0x0E),
    STATUS_UPDATE(0x0F),

    // Server -> Client
    LOGIN_RESULT(0x81),
    SPAWN_PLAYER(0x82),
    DESPAWN_PLAYER(0x83),
    PLAYER_MOVED(0x84),
    CHAT_MESSAGE(0x85),
    WORLD_STATE(0x86),
    CHARACTER_LIST_RESULT(0x87),
    CHARACTER_CREATE_RESULT(0x88),
    CHARACTER_DELETE_RESULT(0x89),
    STATE_DATA(0x8A),
    MONSTER_SPAWN(0x90),
    MONSTER_MOVE(0x91),
    MONSTER_DIE(0x92),
    MONSTER_HP(0x93),
    SKILL_BROADCAST(0x94),
    EXP_GAINED(0x95),
    DROP_SPAWN(0x96),
    DROP_GRANTED(0x97),
    DROP_REMOVED(0x98),
    PLAYER_STATUS(0x99),
    STATE_ACK(0x9A);

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
