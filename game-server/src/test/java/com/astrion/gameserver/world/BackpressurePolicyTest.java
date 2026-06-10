package com.astrion.gameserver.world;

import com.astrion.common.packet.PacketType;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

/// Covers the backpressure classification in WorldManager: which packet
/// types may be dropped when a recipient's socket is congested, and when
/// a congested connection crosses into force-close territory. The Netty
/// channel mechanics (isWritable flipping at the watermarks) are Netty's
/// own contract — what's ours, and what a refactor could silently break,
/// is this classification.
class BackpressurePolicyTest {

    @Test
    void selfHealingBroadcastsAreDroppable() {
        // High-frequency updates where the next packet supersedes the lost
        // one. Dropping these to a congested client degrades smoothness,
        // not correctness.
        assertTrue(WorldManager.isDroppable(PacketType.PLAYER_MOVED));
        assertTrue(WorldManager.isDroppable(PacketType.MONSTER_MOVE));
        assertTrue(WorldManager.isDroppable(PacketType.MONSTER_HP));
        assertTrue(WorldManager.isDroppable(PacketType.PLAYER_STATUS));
        assertTrue(WorldManager.isDroppable(PacketType.SKILL_BROADCAST));
    }

    @Test
    void stateChangingPacketsAreNeverDroppable() {
        // Spawn/despawn pairs must both arrive or the client's world view
        // desyncs permanently (ghost players, invisible monsters). Kill +
        // loot events carry rewards. Chat that silently vanishes is a
        // trust problem. None of these may be dropped.
        assertFalse(WorldManager.isDroppable(PacketType.SPAWN_PLAYER));
        assertFalse(WorldManager.isDroppable(PacketType.DESPAWN_PLAYER));
        assertFalse(WorldManager.isDroppable(PacketType.MONSTER_SPAWN));
        assertFalse(WorldManager.isDroppable(PacketType.MONSTER_DIE));
        assertFalse(WorldManager.isDroppable(PacketType.CHAT_MESSAGE));
        assertFalse(WorldManager.isDroppable(PacketType.EXP_GAINED));
        assertFalse(WorldManager.isDroppable(PacketType.DROP_SPAWN));
        assertFalse(WorldManager.isDroppable(PacketType.WHISPER_RESULT));
        assertFalse(WorldManager.isDroppable(PacketType.TRADE_STATE));
        assertFalse(WorldManager.isDroppable(PacketType.PARTY_UPDATE));
        assertFalse(WorldManager.isDroppable(PacketType.SESSION_KICKED));
        assertFalse(WorldManager.isDroppable(PacketType.STATE_DATA));
        assertFalse(WorldManager.isDroppable(PacketType.LOGIN_RESULT));
    }

    @Test
    void forceCloseTriggersOnlyBeyondThreshold() {
        // Below/at threshold: keep the connection, queue critical packets.
        assertFalse(WorldManager.shouldForceClose(0));
        assertFalse(WorldManager.shouldForceClose(128 * 1024));   // just unwritable
        assertFalse(WorldManager.shouldForceClose(1_500_000));    // exactly at limit
        // Past it: the client stopped draining long ago — cut them loose.
        assertTrue(WorldManager.shouldForceClose(1_500_001));
        assertTrue(WorldManager.shouldForceClose(10_000_000));
    }
}
