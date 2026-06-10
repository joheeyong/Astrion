package com.astrion.gameserver.world;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ObjectNode;
import org.junit.jupiter.api.Test;

import java.util.Map;

import static org.junit.jupiter.api.Assertions.*;

/// Regression suite for the shared player-state JSON manipulation used by
/// trade execution, auction register/buy/cancel/expiry, and achievement
/// rewards. These array-arithmetic helpers move real items and gold — an
/// off-by-one here IS an item-duplication or item-loss bug, which is why
/// every edge that bit us in design review gets a pinned test.
class PlayerStateJsonTest {

    private static final ObjectMapper mapper = new ObjectMapper();

    private static ObjectNode state(String json) {
        try { return (ObjectNode) mapper.readTree(json); }
        catch (Exception e) { throw new RuntimeException(e); }
    }

    // ── gold ─────────────────────────────────────────────────────────────

    @Test
    void goldDefaultsToZeroWhenAbsent() {
        assertEquals(0L, PlayerStateJson.goldOf(state("{}")));
    }

    @Test
    void goldRoundTrips() {
        ObjectNode s = state("{}");
        PlayerStateJson.setGold(s, 12_345L);
        assertEquals(12_345L, PlayerStateJson.goldOf(s));
        PlayerStateJson.setGold(s, 0L);
        assertEquals(0L, PlayerStateJson.goldOf(s));
    }

    // ── totals / contains ────────────────────────────────────────────────

    @Test
    void totalsMergeSplitStacks() {
        // Stardust split across two slots (common after several pickups,
        // since adds append rather than merge).
        ObjectNode s = state("""
            {"inventoryItemIds":["stardust","sword","stardust"],
             "inventoryQuantities":[30,1,25]}""");
        Map<String, Integer> totals = PlayerStateJson.inventoryTotals(s);
        assertEquals(55, totals.get("stardust"));
        assertEquals(1, totals.get("sword"));
        assertEquals(2, totals.size());
    }

    @Test
    void totalsSkipBlankAndZeroedSlots() {
        // ("",0) is the shape removeItems leaves behind — must not count.
        ObjectNode s = state("""
            {"inventoryItemIds":["","sword","ghost"],
             "inventoryQuantities":[5,2,0]}""");
        Map<String, Integer> totals = PlayerStateJson.inventoryTotals(s);
        assertNull(totals.get(""));
        assertNull(totals.get("ghost")); // qty 0 → not owned
        assertEquals(2, totals.get("sword"));
    }

    @Test
    void totalsToleratesMismatchedArrayLengths() {
        // Defensive: a hand-edited or partially-written state must not
        // crash the server. Excess tail is ignored (min of both lengths).
        ObjectNode s = state("""
            {"inventoryItemIds":["a","b","c"],
             "inventoryQuantities":[1,2]}""");
        Map<String, Integer> totals = PlayerStateJson.inventoryTotals(s);
        assertEquals(1, totals.get("a"));
        assertEquals(2, totals.get("b"));
        assertNull(totals.get("c"));
    }

    @Test
    void totalsEmptyWhenArraysMissingOrMalformed() {
        assertTrue(PlayerStateJson.inventoryTotals(state("{}")).isEmpty());
        assertTrue(PlayerStateJson.inventoryTotals(
            state("{\"inventoryItemIds\":\"not-an-array\"}")).isEmpty());
    }

    @Test
    void containsAcrossSplitStacks() {
        ObjectNode s = state("""
            {"inventoryItemIds":["stardust","stardust"],
             "inventoryQuantities":[30,25]}""");
        assertTrue(PlayerStateJson.containsItem(s, "stardust", 55));  // exact
        assertFalse(PlayerStateJson.containsItem(s, "stardust", 56)); // one over
    }

    @Test
    void containsMultiItemNeed() {
        // Trade-shaped check: both offered items must be present.
        ObjectNode s = state("""
            {"inventoryItemIds":["sword","bread"],
             "inventoryQuantities":[1,10]}""");
        assertTrue(PlayerStateJson.contains(s, Map.of("sword", 1, "bread", 5)));
        assertFalse(PlayerStateJson.contains(s, Map.of("sword", 1, "elixir", 1)));
        assertTrue(PlayerStateJson.contains(s, Map.of())); // empty need — trivially satisfied
    }

    // ── removeItems ──────────────────────────────────────────────────────

    @Test
    void removeDrainsAcrossMultipleStacks() {
        // The auction-register scenario that motivated the shared util:
        // listing 40 stardust held as 30 + 25 must drain the first stack
        // fully (slot zeroed) and the second partially.
        ObjectNode s = state("""
            {"inventoryItemIds":["stardust","sword","stardust"],
             "inventoryQuantities":[30,1,25]}""");
        PlayerStateJson.removeItem(s, "stardust", 40);

        assertEquals("", s.get("inventoryItemIds").get(0).asText());
        assertEquals(0, s.get("inventoryQuantities").get(0).asInt());
        assertEquals("sword", s.get("inventoryItemIds").get(1).asText()); // untouched
        assertEquals("stardust", s.get("inventoryItemIds").get(2).asText());
        assertEquals(15, s.get("inventoryQuantities").get(2).asInt());
        assertEquals(15, PlayerStateJson.inventoryTotals(s).get("stardust"));
    }

    @Test
    void removeExactStackZeroesSlotInPlace() {
        // Array length must not change — the client's slot indices map to
        // positions; we zero in place and let RestoreFromState compact.
        ObjectNode s = state("""
            {"inventoryItemIds":["sword"],"inventoryQuantities":[1]}""");
        PlayerStateJson.removeItem(s, "sword", 1);
        assertEquals(1, s.get("inventoryItemIds").size());
        assertEquals("", s.get("inventoryItemIds").get(0).asText());
        assertEquals(0, s.get("inventoryQuantities").get(0).asInt());
    }

    @Test
    void removePartialKeepsItemIdWithRemainder() {
        ObjectNode s = state("""
            {"inventoryItemIds":["bread"],"inventoryQuantities":[10]}""");
        PlayerStateJson.removeItem(s, "bread", 3);
        assertEquals("bread", s.get("inventoryItemIds").get(0).asText());
        assertEquals(7, s.get("inventoryQuantities").get(0).asInt());
    }

    @Test
    void removeNeverGoesNegativeOnInsufficientInventory() {
        // Callers must check contains() first, but a race or bug upstream
        // must degrade to 'removed what was there', never negative qty
        // (negative quantities would mint items on the next compaction).
        ObjectNode s = state("""
            {"inventoryItemIds":["bread"],"inventoryQuantities":[2]}""");
        PlayerStateJson.removeItem(s, "bread", 99);
        assertEquals("", s.get("inventoryItemIds").get(0).asText());
        assertEquals(0, s.get("inventoryQuantities").get(0).asInt());
        assertTrue(PlayerStateJson.inventoryTotals(s).isEmpty());
    }

    @Test
    void removeStopsExactlyAtRequestedQuantity() {
        // Three stacks of 10; removing 10 must drain only the first —
        // touching the later stacks would be silent item loss.
        ObjectNode s = state("""
            {"inventoryItemIds":["bread","bread","bread"],
             "inventoryQuantities":[10,10,10]}""");
        PlayerStateJson.removeItem(s, "bread", 10);
        assertEquals(20, PlayerStateJson.inventoryTotals(s).get("bread"));
        assertEquals("bread", s.get("inventoryItemIds").get(1).asText());
        assertEquals(10, s.get("inventoryQuantities").get(1).asInt());
        assertEquals(10, s.get("inventoryQuantities").get(2).asInt());
    }

    @Test
    void removeMultiItemNeedInOnePass() {
        // Trade-shaped removal: both offered items leave in a single call.
        ObjectNode s = state("""
            {"inventoryItemIds":["sword","bread"],
             "inventoryQuantities":[1,10]}""");
        PlayerStateJson.removeItems(s, Map.of("sword", 1, "bread", 4));
        Map<String, Integer> totals = PlayerStateJson.inventoryTotals(s);
        assertNull(totals.get("sword"));
        assertEquals(6, totals.get("bread"));
    }

    @Test
    void removeOnMissingArraysIsNoOp() {
        ObjectNode s = state("{\"gold\":5}");
        assertDoesNotThrow(() -> PlayerStateJson.removeItem(s, "sword", 1));
        assertEquals(5L, PlayerStateJson.goldOf(s));
    }

    // ── addItem ──────────────────────────────────────────────────────────

    @Test
    void addAppendsWithoutMergingStacks() {
        // Append-not-merge is load-bearing: removeItems' left-to-right
        // drain and the client's compaction both assume entries are
        // independent.
        ObjectNode s = state("""
            {"inventoryItemIds":["stardust"],"inventoryQuantities":[30]}""");
        PlayerStateJson.addItem(s, "stardust", 50);
        assertEquals(2, s.get("inventoryItemIds").size());
        assertEquals(50, s.get("inventoryQuantities").get(1).asInt());
        assertEquals(80, PlayerStateJson.inventoryTotals(s).get("stardust"));
    }

    @Test
    void addCreatesArraysOnFreshState() {
        // Achievement reward landing on an account that never saved
        // inventory (brand-new character) — the grant must still work.
        ObjectNode s = state("{}");
        PlayerStateJson.addItem(s, "stardust", 50);
        assertEquals(50, PlayerStateJson.inventoryTotals(s).get("stardust"));
    }

    @Test
    void addIgnoresInvalidInput() {
        ObjectNode s = state("{}");
        PlayerStateJson.addItem(s, "", 5);
        PlayerStateJson.addItem(s, null, 5);
        PlayerStateJson.addItem(s, "sword", 0);
        PlayerStateJson.addItem(s, "sword", -3);
        assertTrue(PlayerStateJson.inventoryTotals(s).isEmpty());
    }

    // ── end-to-end shapes (manager flows in miniature) ───────────────────

    @Test
    void tradeShapedSwapConservesItems() {
        // A gives sword + 100g, B gives 50 stardust + 20g. Mirrors
        // TradeManager.executeLocked's mutation sequence; total item count
        // across both states must be conserved (the dupe bug detector).
        ObjectNode a = state("""
            {"gold":500,"inventoryItemIds":["sword"],"inventoryQuantities":[1]}""");
        ObjectNode b = state("""
            {"gold":80,"inventoryItemIds":["stardust"],"inventoryQuantities":[60]}""");

        assertTrue(PlayerStateJson.contains(a, Map.of("sword", 1)));
        assertTrue(PlayerStateJson.contains(b, Map.of("stardust", 50)));

        PlayerStateJson.removeItems(a, Map.of("sword", 1));
        PlayerStateJson.removeItems(b, Map.of("stardust", 50));
        PlayerStateJson.addItem(a, "stardust", 50);
        PlayerStateJson.addItem(b, "sword", 1);
        PlayerStateJson.setGold(a, PlayerStateJson.goldOf(a) - 100 + 20);
        PlayerStateJson.setGold(b, PlayerStateJson.goldOf(b) - 20 + 100);

        assertEquals(420L, PlayerStateJson.goldOf(a));
        assertEquals(160L, PlayerStateJson.goldOf(b));
        assertEquals(50, PlayerStateJson.inventoryTotals(a).get("stardust"));
        assertNull(PlayerStateJson.inventoryTotals(a).get("sword"));
        assertEquals(1, PlayerStateJson.inventoryTotals(b).get("sword"));
        assertEquals(10, PlayerStateJson.inventoryTotals(b).get("stardust"));
        // Conservation: 1 sword + 60 stardust before == after, across both.
        int swords = PlayerStateJson.inventoryTotals(a).getOrDefault("sword", 0)
                   + PlayerStateJson.inventoryTotals(b).getOrDefault("sword", 0);
        int dust = PlayerStateJson.inventoryTotals(a).getOrDefault("stardust", 0)
                 + PlayerStateJson.inventoryTotals(b).getOrDefault("stardust", 0);
        assertEquals(1, swords);
        assertEquals(60, dust);
        assertEquals(580L, PlayerStateJson.goldOf(a) + PlayerStateJson.goldOf(b));
    }

    @Test
    void auctionShapedCancelRestoresExactly() {
        // register (remove) followed by cancel (add) must restore the same
        // totals — the expiry sweeper takes the identical path.
        ObjectNode s = state("""
            {"gold":0,"inventoryItemIds":["stardust","stardust"],
             "inventoryQuantities":[30,25]}""");
        PlayerStateJson.removeItem(s, "stardust", 40); // register
        assertEquals(15, PlayerStateJson.inventoryTotals(s).get("stardust"));
        PlayerStateJson.addItem(s, "stardust", 40);    // cancel/expiry refund
        assertEquals(55, PlayerStateJson.inventoryTotals(s).get("stardust"));
    }
}
