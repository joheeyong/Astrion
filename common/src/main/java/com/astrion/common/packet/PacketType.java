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
    CLIENT_LOG(0x10),  // client-side Exception / Error forwarded to server
    FRIEND_ADD(0x11),       // semantics: send a request (not auto-mutual anymore)
    FRIEND_REMOVE(0x12),
    FRIEND_LIST_REQUEST(0x13),
    FRIEND_ACCEPT(0x14),    // accept an incoming request → both become friends
    FRIEND_REJECT(0x15),    // reject an incoming request → drop it
    FRIEND_CANCEL(0x16),    // cancel an outgoing request
    WHISPER(0x17),          // 1:1 chat — payload {target, message}
    PARTY_INVITE(0x18),     // C2S: invite username to your party (creates one if needed)
    PARTY_ACCEPT(0x19),     // C2S: accept a pending invite from username
    PARTY_REJECT(0x1A),     // C2S: reject a pending invite from username
    PARTY_LEAVE(0x1B),      // C2S: leave your current party
    PARTY_KICK(0x1C),       // C2S: leader-only kick of a member by username
    PARTY_REQUEST(0x1D),    // C2S: request current party state (on login / scene swap)
    RANKING_REQUEST(0x1E),  // C2S: payload {category: "level"|"gold"|"kills"}
    TRADE_REQUEST(0x1F),    // C2S: {target} — invite a trade
    TRADE_ACCEPT(0x20),     // C2S: {from}
    TRADE_REJECT(0x21),     // C2S: {from}
    TRADE_OFFER(0x22),      // C2S: {slot, itemId, qty} — set your slot's offered item (qty 0 clears)
    TRADE_GOLD(0x23),       // C2S: {gold} — set the gold you're offering
    TRADE_LOCK(0x24),       // C2S: {} — lock my side
    TRADE_UNLOCK(0x25),     // C2S: {} — unlock (also auto-fires when other side changes)
    TRADE_CONFIRM(0x26),    // C2S: {} — only valid after both sides locked
    TRADE_CANCEL(0x27),     // C2S: {} — abort the trade
    BLOCK_ADD(0x28),        // C2S: {target}
    BLOCK_REMOVE(0x29),     // C2S: {target}
    BLOCK_LIST_REQUEST(0x2A), // C2S: {} — pull my current block list
    ACHIEVEMENT_LIST_REQUEST(0x2B), // C2S: {} — pull every unlocked id
    AUCTION_LIST_REQUEST(0x2C), // C2S: {} — fetch active auctions (latest first)
    AUCTION_REGISTER(0x2D),     // C2S: {itemId, qty, pricePerUnit, durationHours}
    AUCTION_BUY(0x2E),          // C2S: {auctionId}
    AUCTION_CANCEL(0x2F),       // C2S: {auctionId} — only seller can cancel

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
    STATE_ACK(0x9A),
    MONSTER_ATTACK(0x9B),
    FRIEND_LIST_DATA(0xA0),    // includes friends + incoming + outgoing in one shot
    FRIEND_ERROR(0xA1),
    FRIEND_ADDED_BY(0xA2),     // someone accepted YOUR request, or vice-versa
    FRIEND_REQUEST_FROM(0xA3), // someone sent you a request (toast hint)
    WHISPER_RESULT(0xA4),      // whisper delivery — kind=incoming|echo|error
    PARTY_UPDATE(0xA5),        // full party snapshot pushed to every member
    PARTY_INVITE_FROM(0xA6),   // someone invited YOU to their party
    PARTY_ERROR(0xA7),         // user-facing reason a party action failed
    RANKING_DATA(0xA8),        // {category, entries:[{rank,name,score}], selfRank, selfScore}
    TRADE_REQUEST_FROM(0xA9),  // {from}
    TRADE_OPEN(0xAA),          // {partner} — trade window should open
    TRADE_STATE(0xAB),         // full bilateral state snapshot
    TRADE_RESULT(0xAC),        // {success, message, gainedItems:[{id,qty}], gainedGold}
    TRADE_ERROR(0xAD),         // {message}
    BLOCK_LIST_DATA(0xAE),     // {blocked:[name,...]}
    ACHIEVEMENT_UNLOCK(0xAF),  // {id, displayName, rewardItemId, rewardQty}
    ACHIEVEMENT_LIST_DATA(0xB0), // {unlocked:[id,...], progress:{kind:value,...}}
    AUCTION_LIST_DATA(0xB1),     // {entries:[{id, seller, itemId, qty, price, expiresAt}]}
    AUCTION_RESULT(0xB2),        // {success, message, action} — register/buy/cancel
    SESSION_KICKED(0xB3);        // {reason} — server is closing this connection;
                                 // the client must NOT auto-reconnect.

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
