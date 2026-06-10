package com.astrion.e2e;

import com.astrion.common.Version;
import com.astrion.common.packet.PacketType;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.AfterAll;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.Timeout;

import static org.junit.jupiter.api.Assertions.*;

/// End-to-end smoke: two headless bot clients drive the live social stack
/// — register/login → zone enter → friend request/accept → party invite/
/// accept → full trade (offer + gold + lock + double-confirm) — against
/// the real server pipeline and a real Redis. Every hop is asserted on
/// the wire, then the final inventories are verified straight from Redis
/// (ground truth, not client echo).
///
/// What this catches that the unit suite can't: packet-enum drift between
/// handlers, codec framing regressions, lock/executor deadlocks across
/// the manager seams, and any handler forgetting to broadcast its state
/// update. Runs via :game-server:e2eTest (needs Redis on localhost:6379;
/// the CI job provides a service container).
@Tag("e2e")
@Timeout(90)
class SmokeFlowTest {

    private static final ObjectMapper M = new ObjectMapper();
    private static TestServerHarness server;

    @BeforeAll
    static void up() throws Exception { server = TestServerHarness.start(); }

    @AfterAll
    static void down() { if (server != null) server.stop(); }

    @Test
    void friendPartyTradeFlow() throws Exception {
        // Unique names per run — local Redis persists across runs and
        // re-registering an existing account is a login failure.
        String runId = Long.toHexString(System.nanoTime());
        String nameA = "smokeA_" + runId;
        String nameB = "smokeB_" + runId;

        // Seed inventories straight into Redis. iron_dagger is the traded
        // marker item — deliberately NOT stardust, because achievement
        // rewards (FRIEND_1 / PARTY_FIRST / TRADE_FIRST fire during this
        // very flow) grant stardust and would mask a duplication bug.
        server.redis().savePlayerState(nameA,
            "{\"gold\":500,\"inventoryItemIds\":[\"iron_dagger\",\"bread\"],\"inventoryQuantities\":[1,3]}");
        server.redis().savePlayerState(nameB,
            "{\"gold\":80,\"inventoryItemIds\":[],\"inventoryQuantities\":[]}");

        try (BotClient a = new BotClient("A", "127.0.0.1", server.port());
             BotClient b = new BotClient("B", "127.0.0.1", server.port())) {

            // ── register + login + zone ────────────────────────────────
            registerAndEnter(a, nameA);
            registerAndEnter(b, nameB);

            // ── friend: A requests, B accepts, both see each other ─────
            a.send(PacketType.FRIEND_ADD, "{\"target\":\"" + nameB + "\"}");
            b.waitFor(PacketType.FRIEND_REQUEST_FROM, j -> nameA.equals(j.path("from").asText()));
            b.send(PacketType.FRIEND_ACCEPT, "{\"target\":\"" + nameA + "\"}");
            a.waitFor(PacketType.FRIEND_LIST_DATA, j -> friendsContain(j, nameB));
            b.waitFor(PacketType.FRIEND_LIST_DATA, j -> friendsContain(j, nameA));

            // ── party: A invites, B accepts, both rosters show 2 ───────
            a.send(PacketType.PARTY_INVITE, "{\"target\":\"" + nameB + "\"}");
            b.waitFor(PacketType.PARTY_INVITE_FROM, j -> nameA.equals(j.path("from").asText()));
            b.send(PacketType.PARTY_ACCEPT, "{\"from\":\"" + nameA + "\"}");
            a.waitFor(PacketType.PARTY_UPDATE, j -> partyHasBoth(j, nameA, nameB));
            b.waitFor(PacketType.PARTY_UPDATE, j -> partyHasBoth(j, nameA, nameB));

            // ── trade: A gives dagger + 100g for nothing ───────────────
            a.send(PacketType.TRADE_REQUEST, "{\"target\":\"" + nameB + "\"}");
            b.waitFor(PacketType.TRADE_REQUEST_FROM, j -> nameA.equals(j.path("from").asText()));
            b.send(PacketType.TRADE_ACCEPT, "{\"from\":\"" + nameA + "\"}");
            a.waitFor(PacketType.TRADE_OPEN, j -> nameB.equals(j.path("partner").asText()));
            b.waitFor(PacketType.TRADE_OPEN, j -> nameA.equals(j.path("partner").asText()));

            // A == session side 'a' (the inviter).
            a.send(PacketType.TRADE_OFFER, "{\"slot\":0,\"itemId\":\"iron_dagger\",\"qty\":1}");
            a.waitFor(PacketType.TRADE_STATE,
                j -> "iron_dagger".equals(j.path("aOffer").path(0).path("itemId").asText()));
            a.send(PacketType.TRADE_GOLD, "{\"gold\":100}");
            a.waitFor(PacketType.TRADE_STATE, j -> j.path("aGold").asLong() == 100);

            a.send(PacketType.TRADE_LOCK, "{}");
            b.send(PacketType.TRADE_LOCK, "{}");
            a.waitFor(PacketType.TRADE_STATE,
                j -> j.path("aLocked").asBoolean() && j.path("bLocked").asBoolean());

            a.send(PacketType.TRADE_CONFIRM, "{}");
            b.waitFor(PacketType.TRADE_STATE, j -> j.path("aConfirmed").asBoolean());
            b.send(PacketType.TRADE_CONFIRM, "{}");

            a.waitFor(PacketType.TRADE_RESULT, j -> j.path("success").asBoolean());
            JsonNode bResult = b.waitFor(PacketType.TRADE_RESULT, j -> j.path("success").asBoolean());
            assertEquals(100L, bResult.path("gainedGold").asLong(),
                "B must receive A's offered gold");
            assertEquals("iron_dagger", bResult.path("gainedItems").path(0).path("id").asText());
            assertEquals(1, bResult.path("gainedItems").path(0).path("qty").asInt());

            // ── ground truth: read both states back from Redis ─────────
            JsonNode sa = M.readTree(server.redis().getPlayerState(nameA));
            JsonNode sb = M.readTree(server.redis().getPlayerState(nameB));

            assertEquals(400L, sa.path("gold").asLong(), "A paid 100g");
            assertEquals(180L, sb.path("gold").asLong(), "B gained 100g (no fee on direct trade)");

            assertEquals(0, totalOf(sa, "iron_dagger"), "dagger must leave A");
            assertEquals(1, totalOf(sb, "iron_dagger"), "dagger must arrive at B");
            assertEquals(1, totalOf(sa, "iron_dagger") + totalOf(sb, "iron_dagger"),
                "conservation: exactly one dagger in the world");
            assertEquals(3, totalOf(sa, "bread"), "untraded items untouched");
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private void registerAndEnter(BotClient bot, String name) throws Exception {
        // Server stores the password string verbatim (the real client sends
        // a SHA-256 hex; any stable string works for a throwaway account).
        bot.send(PacketType.LOGIN, M.writeValueAsString(java.util.Map.of(
            "username", name,
            "password", "e2e_dummy_digest",
            "isRegister", true,
            "clientVersion", Version.CURRENT)));
        JsonNode login = bot.waitFor(PacketType.LOGIN_RESULT, j -> true);
        assertTrue(login.path("success").asBoolean(),
            "register/login failed for " + name + ": " + login.path("message").asText());

        bot.send(PacketType.ZONE_ENTER, M.writeValueAsString(java.util.Map.of(
            "zoneId", "solaria",
            "nickname", name,
            "className", "Warrior")));
    }

    private static boolean friendsContain(JsonNode listData, String name) {
        for (JsonNode f : listData.path("friends")) {
            if (name.equals(f.path("name").asText())) return true;
        }
        return false;
    }

    private static boolean partyHasBoth(JsonNode update, String a, String b) {
        JsonNode members = update.path("members");
        if (members.size() != 2) return false;
        boolean hasA = false, hasB = false;
        for (JsonNode m : members) {
            if (a.equals(m.path("name").asText())) hasA = true;
            if (b.equals(m.path("name").asText())) hasB = true;
        }
        return hasA && hasB;
    }

    private static int totalOf(JsonNode state, String itemId) {
        JsonNode ids = state.path("inventoryItemIds");
        JsonNode qts = state.path("inventoryQuantities");
        int total = 0;
        for (int i = 0; i < Math.min(ids.size(), qts.size()); i++) {
            if (itemId.equals(ids.get(i).asText())) total += qts.get(i).asInt();
        }
        return total;
    }
}
