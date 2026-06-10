package com.astrion.gameserver.world;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.node.ArrayNode;
import com.fasterxml.jackson.databind.node.JsonNodeFactory;
import com.fasterxml.jackson.databind.node.ObjectNode;

import java.util.HashMap;
import java.util.Map;

/// Pure mutation/query helpers for the Redis-persisted player state JSON
/// (the blob behind STATE_SAVE / STATE_DATA). Trade, auction, and
/// achievement rewards all manipulate the same two parallel arrays —
///   inventoryItemIds:     ["sword", "", "stardust", ...]
///   inventoryQuantities:  [1,       0,  250,        ...]
/// — plus the scalar gold field. Before this class each manager carried
/// its own private copy of these helpers; three implementations of the
/// same stack-draining arithmetic is three places for an item-duplication
/// bug to hide. One shared, heavily-tested implementation now.
///
/// Conventions preserved from the original implementations:
///  • Slots are drained left-to-right; a fully-drained slot becomes
///    ("", 0) — entries are zeroed in place, never spliced out. The
///    client's RestoreFromState compacts on next load.
///  • Adds append a new entry rather than merging into existing stacks —
///    same reason, the client compacts.
///  • Mismatched array lengths are treated as min(len, len); the excess
///    tail is ignored rather than crashing.
///  • All methods are null-tolerant on missing/malformed fields: queries
///    return empty/zero, mutations become no-ops (except addItem, which
///    creates the arrays).
///
/// Callers are responsible for concurrency — every entry point that uses
/// these helpers must hold the player's PlayerStateLocks lock.
public final class PlayerStateJson {

    private static final JsonNodeFactory NODES = JsonNodeFactory.instance;

    private PlayerStateJson() {}

    // ── gold ─────────────────────────────────────────────────────────────

    public static long goldOf(ObjectNode state) {
        return state.has("gold") ? state.get("gold").asLong() : 0L;
    }

    public static void setGold(ObjectNode state, long gold) {
        state.put("gold", gold);
    }

    // ── queries ──────────────────────────────────────────────────────────

    /// Compacted (id → totalQty) view across all slots. Blank ids and
    /// non-positive quantities are skipped.
    public static Map<String, Integer> inventoryTotals(ObjectNode state) {
        Map<String, Integer> totals = new HashMap<>();
        JsonNode ids = state.get("inventoryItemIds");
        JsonNode qts = state.get("inventoryQuantities");
        if (ids == null || !ids.isArray() || qts == null || !qts.isArray()) return totals;
        int n = Math.min(ids.size(), qts.size());
        for (int i = 0; i < n; i++) {
            String id = ids.get(i).asText("");
            int q = qts.get(i).asInt(0);
            if (id == null || id.isEmpty() || q <= 0) continue;
            totals.merge(id, q, Integer::sum);
        }
        return totals;
    }

    /// True iff the inventory holds at least the requested quantity of
    /// every item in {@code need}. An empty need is trivially satisfied.
    public static boolean contains(ObjectNode state, Map<String, Integer> need) {
        if (need == null || need.isEmpty()) return true;
        Map<String, Integer> totals = inventoryTotals(state);
        for (var e : need.entrySet()) {
            if (totals.getOrDefault(e.getKey(), 0) < e.getValue()) return false;
        }
        return true;
    }

    public static boolean containsItem(ObjectNode state, String itemId, int qty) {
        if (itemId == null || itemId.isEmpty() || qty <= 0) return qty <= 0;
        return contains(state, Map.of(itemId, qty));
    }

    // ── mutations ────────────────────────────────────────────────────────

    /// Drains the requested quantities from the slots, left-to-right.
    /// Fully-drained slots become ("", 0); partially-drained keep their id
    /// with the reduced quantity. Callers must verify contains() first —
    /// if the inventory holds less than requested, this removes what's
    /// there and stops (it never goes negative).
    public static void removeItems(ObjectNode state, Map<String, Integer> need) {
        if (need == null || need.isEmpty()) return;
        JsonNode idsNode = state.get("inventoryItemIds");
        JsonNode qtsNode = state.get("inventoryQuantities");
        if (!(idsNode instanceof ArrayNode ids) || !(qtsNode instanceof ArrayNode qts)) return;

        Map<String, Integer> remaining = new HashMap<>(need);
        int n = Math.min(ids.size(), qts.size());
        for (int i = 0; i < n && !remaining.isEmpty(); i++) {
            String id = ids.get(i).asText("");
            int q = qts.get(i).asInt(0);
            if (id == null || id.isEmpty() || q <= 0) continue;
            int rem = remaining.getOrDefault(id, 0);
            if (rem <= 0) continue;
            int take = Math.min(rem, q);
            int left = q - take;
            ids.set(i, NODES.textNode(left > 0 ? id : ""));
            qts.set(i, NODES.numberNode(left));
            if (rem - take == 0) remaining.remove(id);
            else remaining.put(id, rem - take);
        }
    }

    public static void removeItem(ObjectNode state, String itemId, int qty) {
        if (itemId == null || itemId.isEmpty() || qty <= 0) return;
        removeItems(state, Map.of(itemId, qty));
    }

    /// Appends (itemId, qty) as a new slot entry. Creates the arrays when
    /// the state has none (fresh account). No stack-merge — see class doc.
    public static void addItem(ObjectNode state, String itemId, int qty) {
        if (itemId == null || itemId.isEmpty() || qty <= 0) return;
        ArrayNode ids = state.has("inventoryItemIds") && state.get("inventoryItemIds").isArray()
            ? (ArrayNode) state.get("inventoryItemIds")
            : state.putArray("inventoryItemIds");
        ArrayNode qts = state.has("inventoryQuantities") && state.get("inventoryQuantities").isArray()
            ? (ArrayNode) state.get("inventoryQuantities")
            : state.putArray("inventoryQuantities");
        ids.add(itemId);
        qts.add(qty);
    }
}
