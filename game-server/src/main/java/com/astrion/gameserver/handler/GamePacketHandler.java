package com.astrion.gameserver.handler;

import com.astrion.common.model.Position;
import com.astrion.common.packet.GamePacket;
import com.astrion.common.packet.PacketType;
import com.astrion.gameserver.redis.RedisManager;
import com.astrion.gameserver.world.MonsterManager;
import com.astrion.gameserver.world.PlayerSession;
import com.astrion.gameserver.world.WorldManager;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ArrayNode;
import com.fasterxml.jackson.databind.node.ObjectNode;
import io.netty.channel.ChannelHandlerContext;
import io.netty.channel.SimpleChannelInboundHandler;
import io.netty.handler.timeout.IdleStateEvent;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;

public class GamePacketHandler extends SimpleChannelInboundHandler<GamePacket> {

    private static final Logger log = LoggerFactory.getLogger(GamePacketHandler.class);
    /** Dedicated stream for client-forwarded Exceptions (writes to
     *  ~/logs/client-errors.log via logback config). additivity=false in
     *  logback.xml keeps these out of server.log / errors.log. */
    private static final Logger clientLog = LoggerFactory.getLogger("CLIENT_ERR");
    /** Per-player rate limit on client logs — a NRE in Update fires every
     *  frame; without throttling the client would spam the wire. Same
     *  policy class as login limiters: 10 reports / 60s, 60s block on excess. */
    private static final LoginRateLimiter clientLogLimiter =
        new LoginRateLimiter(10, 60_000L, 60_000L);
    private static final ObjectMapper mapper = new ObjectMapper();
    private static final float BROADCAST_RANGE = 100f;
    // Shared across all handler instances — connection-level state for the whole server.
    // Two layers of brute-force defence with deliberately different policies:
    //   ipLimiter        — punishes one source IP hammering LOGIN
    //   usernameLimiter  — punishes a single account being attacked from anywhere
    // The username layer catches the distributed case the IP layer misses
    // (botnet spraying one victim from 1000 IPs, each well under the IP cap).
    private static final LoginRateLimiter ipLimiter = new LoginRateLimiter(5, 60_000L, 5L * 60_000L);
    private static final LoginRateLimiter usernameLimiter = new LoginRateLimiter(10, 60_000L, 15L * 60_000L);

    private final WorldManager worldManager;
    private final RedisManager redisManager;
    private final MonsterManager monsterManager;
    private final AccountLockout accountLockout;

    public GamePacketHandler(WorldManager worldManager, RedisManager redisManager, MonsterManager monsterManager) {
        this.worldManager = worldManager;
        this.redisManager = redisManager;
        this.monsterManager = monsterManager;
        // AccountLockout is stateless apart from the redis handle, so one
        // per handler instance is fine — Lettuce's RedisCommands inside
        // RedisManager is what carries the actual state.
        this.accountLockout = new AccountLockout(redisManager);
    }

    @Override
    protected void channelRead0(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        switch (packet.getType()) {
            case LOGIN -> handleLogin(ctx, packet);
            case MOVE -> handleMove(ctx, packet);
            case CHAT -> handleChat(ctx, packet);
            case ATTACK -> handleAttack(ctx, packet);
            case CHARACTER_LIST -> handleCharacterList(ctx, packet);
            case CHARACTER_CREATE -> handleCharacterCreate(ctx, packet);
            case CHARACTER_DELETE -> handleCharacterDelete(ctx, packet);
            case STATE_REQUEST -> handleStateRequest(ctx, packet);
            case STATE_SAVE -> handleStateSave(ctx, packet);
            case ZONE_ENTER -> handleZoneEnter(ctx, packet);
            case MONSTER_HIT -> handleMonsterHit(ctx, packet);
            case SKILL_CAST -> handleSkillCast(ctx, packet);
            case DROP_CLAIM -> handleDropClaim(ctx, packet);
            case STATUS_UPDATE -> handleStatusUpdate(ctx, packet);
            case CLIENT_LOG -> handleClientLog(ctx, packet);
            case FRIEND_ADD -> handleFriendAdd(ctx, packet);
            case FRIEND_REMOVE -> handleFriendRemove(ctx, packet);
            case FRIEND_LIST_REQUEST -> handleFriendListRequest(ctx, packet);
            case FRIEND_ACCEPT -> handleFriendAccept(ctx, packet);
            case FRIEND_REJECT -> handleFriendReject(ctx, packet);
            case FRIEND_CANCEL -> handleFriendCancel(ctx, packet);
            case WHISPER -> handleWhisper(ctx, packet);
            case PARTY_INVITE -> handlePartyInvite(ctx, packet);
            case PARTY_ACCEPT -> handlePartyAccept(ctx, packet);
            case PARTY_REJECT -> handlePartyReject(ctx, packet);
            case PARTY_LEAVE -> handlePartyLeave(ctx, packet);
            case PARTY_KICK -> handlePartyKick(ctx, packet);
            case PARTY_REQUEST -> handlePartyRequest(ctx, packet);
            default -> log.warn("Unhandled packet type: {}", packet.getType());
        }
    }

    // ── Friend system ──────────────────────────────────────────────────────
    private static final int MAX_FRIENDS = 50;
    private static final int MAX_OUTGOING_REQUESTS = 20;

    /// FRIEND_ADD semantics: this is now a *request* — both sides have to
    /// agree before mutual friendship is recorded. The target sees the
    /// pending entry in their incoming list; either side can withdraw it
    /// (cancel / reject) and only an accept moves the relationship into
    /// the real friends sets.
    ///
    /// Special case: if 'target' already has an outgoing request TO 'self',
    /// we auto-accept (both wanted each other). Saves a click and avoids
    /// the deadlock where two people send simultaneously and neither knows.
    private void handleFriendAdd(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        String self = session.getPlayerId();
        JsonNode node = mapper.readTree(packet.getPayload());
        String target = node.has("target") ? node.get("target").asText().trim() : "";

        if (target.isEmpty() || target.equals(self)) {
            sendFriendError(ctx, "잘못된 친구 이름입니다.");
            return;
        }
        if (redisManager.get("account:" + target) == null) {
            sendFriendError(ctx, "그런 모험가는 없습니다.");
            return;
        }
        if (redisManager.areFriends(self, target)) {
            sendFriendError(ctx, "이미 친구입니다.");
            return;
        }
        if (redisManager.hasOutgoingRequest(self, target)) {
            sendFriendError(ctx, "이미 요청을 보냈습니다.");
            return;
        }
        // Mutual-want shortcut.
        if (redisManager.hasIncomingRequest(self, target)) {
            // target had asked us; treat this as an accept.
            acceptInternal(self, target);
            sendFriendList(ctx.channel(), self);
            PlayerSession ts = worldManager.getSessionByPlayerId(target);
            if (ts != null) {
                sendFriendList(ts.getChannel(), target);
                pushNotification(ts.getChannel(), PacketType.FRIEND_ADDED_BY, new FriendAddedByPayload(self));
            }
            return;
        }
        if (redisManager.friendCount(self) >= MAX_FRIENDS) {
            sendFriendError(ctx, "친구가 너무 많습니다 (최대 " + MAX_FRIENDS + ").");
            return;
        }
        if (redisManager.outgoingRequests(self).size() >= MAX_OUTGOING_REQUESTS) {
            sendFriendError(ctx, "보낸 요청이 너무 많습니다 (최대 " + MAX_OUTGOING_REQUESTS + ").");
            return;
        }

        redisManager.addFriendRequest(self, target);

        // Echo updated state to both sides.
        sendFriendList(ctx.channel(), self);
        PlayerSession ts = worldManager.getSessionByPlayerId(target);
        if (ts != null) {
            sendFriendList(ts.getChannel(), target);
            pushNotification(ts.getChannel(), PacketType.FRIEND_REQUEST_FROM, new FriendRequestFromPayload(self));
        }
    }

    private void handleFriendAccept(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        String self = session.getPlayerId();
        JsonNode node = mapper.readTree(packet.getPayload());
        String from = node.has("target") ? node.get("target").asText().trim() : "";
        if (from.isEmpty() || from.equals(self)) {
            sendFriendError(ctx, "잘못된 이름입니다.");
            return;
        }
        if (!redisManager.hasIncomingRequest(self, from)) {
            sendFriendError(ctx, "그 사용자로부터 요청이 없습니다.");
            return;
        }
        if (redisManager.friendCount(self) >= MAX_FRIENDS) {
            sendFriendError(ctx, "친구가 너무 많습니다 (최대 " + MAX_FRIENDS + ").");
            return;
        }

        acceptInternal(self, from);
        sendFriendList(ctx.channel(), self);
        PlayerSession fs = worldManager.getSessionByPlayerId(from);
        if (fs != null) {
            sendFriendList(fs.getChannel(), from);
            pushNotification(fs.getChannel(), PacketType.FRIEND_ADDED_BY, new FriendAddedByPayload(self));
        }
    }

    private void handleFriendReject(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        String self = session.getPlayerId();
        JsonNode node = mapper.readTree(packet.getPayload());
        String from = node.has("target") ? node.get("target").asText().trim() : "";
        if (from.isEmpty()) { sendFriendError(ctx, "잘못된 이름입니다."); return; }
        if (!redisManager.hasIncomingRequest(self, from)) {
            sendFriendError(ctx, "그 사용자로부터 요청이 없습니다.");
            return;
        }
        redisManager.removeFriendRequest(from, self);
        sendFriendList(ctx.channel(), self);
        // No notification to the sender; rejection stays quiet by design.
    }

    private void handleFriendCancel(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        String self = session.getPlayerId();
        JsonNode node = mapper.readTree(packet.getPayload());
        String to = node.has("target") ? node.get("target").asText().trim() : "";
        if (to.isEmpty()) { sendFriendError(ctx, "잘못된 이름입니다."); return; }
        if (!redisManager.hasOutgoingRequest(self, to)) {
            sendFriendError(ctx, "그 사용자에게 보낸 요청이 없습니다.");
            return;
        }
        redisManager.removeFriendRequest(self, to);
        sendFriendList(ctx.channel(), self);
        // If target is online, refresh their view so the cancelled request
        // disappears from their incoming list immediately.
        PlayerSession ts = worldManager.getSessionByPlayerId(to);
        if (ts != null) sendFriendList(ts.getChannel(), to);
    }

    private void acceptInternal(String a, String b) {
        // Clear any pending in either direction, then add to friends both sides.
        redisManager.removeFriendRequest(a, b);
        redisManager.removeFriendRequest(b, a);
        redisManager.addFriendBoth(a, b);
    }

    private void pushNotification(io.netty.channel.Channel ch, PacketType type, Object payload) {
        try {
            ch.writeAndFlush(new GamePacket(type, mapper.writeValueAsString(payload)));
        } catch (Exception ignored) { /* best effort */ }
    }

    // ── Whisper (1:1 chat) ────────────────────────────────────────────────
    private static final int MAX_WHISPER_BYTES = 500;

    private void handleWhisper(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession sender = worldManager.getSession(ctx.channel());
        if (sender == null) return;
        JsonNode node = mapper.readTree(packet.getPayload());
        String target  = node.has("target")  ? node.get("target").asText().trim()  : "";
        String message = node.has("message") ? node.get("message").asText() : "";
        if (target.isEmpty() || message.isEmpty()) {
            sendWhisperError(ctx, "잘못된 귓속말 형식입니다.");
            return;
        }
        if (target.equals(sender.getPlayerId())) {
            sendWhisperError(ctx, "자기 자신에게 귓속말 할 수 없습니다.");
            return;
        }
        // Truncate runaway pastes so a copy-paste of a megabyte doesn't
        // flood the recipient. 500 bytes is a UTF-8-safe cap; the server
        // is the source of truth and the client should also limit its
        // own input field length but we don't trust that.
        if (message.length() > MAX_WHISPER_BYTES) {
            message = message.substring(0, MAX_WHISPER_BYTES);
        }

        PlayerSession targetSession = worldManager.getSessionByPlayerId(target);
        if (targetSession == null) {
            sendWhisperError(ctx, target + " 님은 접속 중이 아닙니다.");
            return;
        }

        // Deliver to recipient, then echo to sender so the sender's own
        // chat panel shows what they just said in the same whisper colour.
        String incoming = mapper.writeValueAsString(
            new WhisperResultPayload(sender.getPlayerId(), target, message, "incoming", null));
        targetSession.getChannel().writeAndFlush(new GamePacket(PacketType.WHISPER_RESULT, incoming));

        String echo = mapper.writeValueAsString(
            new WhisperResultPayload(sender.getPlayerId(), target, message, "echo", null));
        ctx.writeAndFlush(new GamePacket(PacketType.WHISPER_RESULT, echo));
    }

    private void sendWhisperError(ChannelHandlerContext ctx, String reason) {
        try {
            String json = mapper.writeValueAsString(
                new WhisperResultPayload("", "", "", "error", reason));
            ctx.writeAndFlush(new GamePacket(PacketType.WHISPER_RESULT, json));
        } catch (Exception ignored) { /* best effort */ }
    }

    private record WhisperResultPayload(String from, String to, String message, String kind, String error) {}

    // ── Party system ──────────────────────────────────────────────────────
    private static final int MAX_PARTY_SIZE = 4;

    /// Invite path. Anyone can invite — if the inviter isn't in a party yet
    /// the accept side will materialise one. If they already lead a party,
    /// the acceptee joins it. Non-leaders trying to invite while in a
    /// party get rejected so the leader keeps roster control.
    private void handlePartyInvite(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        String self = session.getPlayerId();
        JsonNode node = mapper.readTree(packet.getPayload());
        String target = node.has("target") ? node.get("target").asText().trim() : "";

        if (target.isEmpty() || target.equals(self)) {
            sendPartyError(ctx, "잘못된 대상입니다.");
            return;
        }
        if (redisManager.get("account:" + target) == null) {
            sendPartyError(ctx, "그런 모험가는 없습니다.");
            return;
        }
        // Leader gate — if I already have a party, I must be the leader.
        String myParty = redisManager.getPartyOf(self);
        if (myParty != null && !myParty.isEmpty()) {
            String leader = redisManager.getPartyLeader(myParty);
            if (leader != null && !leader.equals(self)) {
                sendPartyError(ctx, "파티장만 초대할 수 있습니다.");
                return;
            }
            long size = redisManager.partyMemberCount(myParty);
            if (size >= MAX_PARTY_SIZE) {
                sendPartyError(ctx, "파티가 가득 찼습니다 (최대 " + MAX_PARTY_SIZE + "명).");
                return;
            }
        }
        // Target gate — they must not already be in a party.
        String targetParty = redisManager.getPartyOf(target);
        if (targetParty != null && !targetParty.isEmpty()) {
            sendPartyError(ctx, target + " 님은 이미 다른 파티에 있습니다.");
            return;
        }
        PlayerSession targetSession = worldManager.getSessionByPlayerId(target);
        if (targetSession == null) {
            sendPartyError(ctx, target + " 님은 접속 중이 아닙니다.");
            return;
        }

        // Materialise a partyId now so the accept side knows which party to
        // join. If we already lead one, reuse it; otherwise mint a new id
        // *but don't actually create the party until accept* — that keeps
        // stale invites from leaving orphaned empty parties in Redis.
        String partyId = (myParty != null && !myParty.isEmpty())
            ? myParty
            : "p_" + java.util.UUID.randomUUID().toString().substring(0, 12);

        redisManager.addPartyInvite(target, self, partyId);
        pushNotification(targetSession.getChannel(), PacketType.PARTY_INVITE_FROM,
            new PartyInviteFromPayload(self));
    }

    private void handlePartyAccept(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        String self = session.getPlayerId();
        JsonNode node = mapper.readTree(packet.getPayload());
        String inviter = node.has("from") ? node.get("from").asText().trim() : "";
        if (inviter.isEmpty()) { sendPartyError(ctx, "잘못된 초대입니다."); return; }
        if (!redisManager.hasPartyInvite(self, inviter)) {
            sendPartyError(ctx, "유효한 초대가 없습니다 (만료되었을 수 있음).");
            return;
        }
        String partyId = redisManager.getInvitedPartyId(self, inviter);
        redisManager.removePartyInvite(self, inviter);
        if (partyId == null || partyId.isEmpty()) {
            sendPartyError(ctx, "초대가 만료되었습니다.");
            return;
        }
        if (redisManager.getPartyOf(self) != null) {
            sendPartyError(ctx, "이미 파티에 속해 있습니다.");
            return;
        }

        // Lazy materialisation — the inviter may not have been in a party
        // when they sent the invite. Add them now and mark as leader.
        if (redisManager.partyMemberCount(partyId) == 0) {
            redisManager.addPartyMember(partyId, inviter);
            redisManager.setPartyLeader(partyId, inviter);
            redisManager.setPartyOf(inviter, partyId);
        }
        if (redisManager.partyMemberCount(partyId) >= MAX_PARTY_SIZE) {
            sendPartyError(ctx, "파티가 가득 찼습니다.");
            return;
        }
        redisManager.addPartyMember(partyId, self);
        redisManager.setPartyOf(self, partyId);
        broadcastPartyUpdate(partyId);
    }

    private void handlePartyReject(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        String self = session.getPlayerId();
        JsonNode node = mapper.readTree(packet.getPayload());
        String inviter = node.has("from") ? node.get("from").asText().trim() : "";
        if (inviter.isEmpty()) return;
        redisManager.removePartyInvite(self, inviter);
        // Quiet rejection — no notification back to the inviter on purpose.
    }

    private void handlePartyLeave(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        removeFromParty(session.getPlayerId());
    }

    private void handlePartyKick(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        String self = session.getPlayerId();
        JsonNode node = mapper.readTree(packet.getPayload());
        String target = node.has("target") ? node.get("target").asText().trim() : "";
        if (target.isEmpty() || target.equals(self)) {
            sendPartyError(ctx, "잘못된 대상입니다.");
            return;
        }
        String partyId = redisManager.getPartyOf(self);
        if (partyId == null || partyId.isEmpty()) {
            sendPartyError(ctx, "파티에 속해 있지 않습니다.");
            return;
        }
        String leader = redisManager.getPartyLeader(partyId);
        if (leader == null || !leader.equals(self)) {
            sendPartyError(ctx, "파티장만 강퇴할 수 있습니다.");
            return;
        }
        String targetParty = redisManager.getPartyOf(target);
        if (targetParty == null || !targetParty.equals(partyId)) {
            sendPartyError(ctx, "같은 파티가 아닙니다.");
            return;
        }
        removeFromParty(target);
    }

    private void handlePartyRequest(ChannelHandlerContext ctx, GamePacket packet) {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        sendPartyState(ctx.channel(), session.getPlayerId());
    }

    /// Public so channelInactive can call it on disconnect — same semantics
    /// as a voluntary leave, but no error toast on failure (the user is
    /// gone). Promotes a new leader if the leaving member was leader, and
    /// dissolves the party entirely when only one member would remain.
    public void removeFromParty(String username) {
        String partyId = redisManager.getPartyOf(username);
        if (partyId == null || partyId.isEmpty()) return;
        redisManager.removePartyMember(partyId, username);
        redisManager.clearPartyOf(username);

        long remaining = redisManager.partyMemberCount(partyId);
        if (remaining <= 1) {
            // Solo party isn't a party — dissolve and free the lone member.
            java.util.Set<String> rest = redisManager.getPartyMembers(partyId);
            for (String r : rest) {
                redisManager.clearPartyOf(r);
                PlayerSession s = worldManager.getSessionByPlayerId(r);
                if (s != null) sendPartyState(s.getChannel(), r);
            }
            redisManager.deleteParty(partyId);
            // Push an empty-party update to the user who left, too.
            PlayerSession leaver = worldManager.getSessionByPlayerId(username);
            if (leaver != null) sendPartyState(leaver.getChannel(), username);
            return;
        }

        // Promote next leader if the leaver was the leader.
        String leader = redisManager.getPartyLeader(partyId);
        if (leader == null || leader.equals(username)) {
            String next = null;
            for (String m : redisManager.getPartyMembers(partyId)) { next = m; break; }
            if (next != null) redisManager.setPartyLeader(partyId, next);
        }
        broadcastPartyUpdate(partyId);
        // The leaver gets a cleared view too.
        PlayerSession leaver = worldManager.getSessionByPlayerId(username);
        if (leaver != null) sendPartyState(leaver.getChannel(), username);
    }

    private void broadcastPartyUpdate(String partyId) {
        java.util.Set<String> members = redisManager.getPartyMembers(partyId);
        for (String m : members) {
            PlayerSession s = worldManager.getSessionByPlayerId(m);
            if (s != null) sendPartyState(s.getChannel(), m);
        }
    }

    private void sendPartyState(io.netty.channel.Channel ch, String username) {
        try {
            String partyId = redisManager.getPartyOf(username);
            String leader = (partyId != null && !partyId.isEmpty()) ? redisManager.getPartyLeader(partyId) : "";
            java.util.Set<String> nameSet = (partyId != null && !partyId.isEmpty())
                ? redisManager.getPartyMembers(partyId) : java.util.Collections.emptySet();

            java.util.List<PartyMemberEntry> members = new java.util.ArrayList<>(nameSet.size());
            for (String name : nameSet) {
                PlayerSession s = worldManager.getSessionByPlayerId(name);
                boolean online = s != null;
                String zone = online ? s.getZoneId() : "";
                int hp = 0, maxHp = 0, level = 1;
                if (online) {
                    hp = s.lastHp;
                    maxHp = s.lastMaxHp;
                    level = s.level;
                }
                members.add(new PartyMemberEntry(name, online, zone, hp, maxHp, level));
            }
            // Stable ordering: leader first, then alphabetical.
            String capturedLeader = leader == null ? "" : leader;
            members.sort((a, b) -> {
                if (a.name.equals(capturedLeader)) return -1;
                if (b.name.equals(capturedLeader)) return 1;
                return a.name.compareToIgnoreCase(b.name);
            });

            String json = mapper.writeValueAsString(new PartyUpdatePayload(
                partyId == null ? "" : partyId,
                capturedLeader,
                members));
            ch.writeAndFlush(new GamePacket(PacketType.PARTY_UPDATE, json));
        } catch (Exception e) {
            log.warn("sendPartyState for {} failed: {}", username, e.getMessage());
        }
    }

    private void sendPartyError(ChannelHandlerContext ctx, String msg) {
        try {
            String json = mapper.writeValueAsString(new PartyErrorPayload(msg));
            ctx.writeAndFlush(new GamePacket(PacketType.PARTY_ERROR, json));
        } catch (Exception ignored) { /* best effort */ }
    }

    private record PartyMemberEntry(String name, boolean online, String zone,
                                     int hp, int maxHp, int level) {}
    private record PartyUpdatePayload(String partyId, String leader,
                                       java.util.List<PartyMemberEntry> members) {}
    private record PartyInviteFromPayload(String from) {}
    private record PartyErrorPayload(String message) {}

    private void handleFriendRemove(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        String self = session.getPlayerId();
        JsonNode node = mapper.readTree(packet.getPayload());
        String target = node.has("target") ? node.get("target").asText().trim() : "";
        if (target.isEmpty() || target.equals(self)) {
            sendFriendError(ctx, "잘못된 이름입니다.");
            return;
        }
        if (!redisManager.areFriends(self, target)) {
            sendFriendError(ctx, "친구 목록에 없습니다.");
            return;
        }
        redisManager.removeFriendBoth(self, target);
        sendFriendList(ctx.channel(), self);
        PlayerSession targetSession = worldManager.getSessionByPlayerId(target);
        if (targetSession != null) sendFriendList(targetSession.getChannel(), target);
    }

    private void handleFriendListRequest(ChannelHandlerContext ctx, GamePacket packet) {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        sendFriendList(ctx.channel(), session.getPlayerId());
    }

    private void sendFriendList(io.netty.channel.Channel ch, String username) {
        try {
            java.util.Set<String> names = redisManager.getFriends(username);
            java.util.List<FriendEntry> entries = new java.util.ArrayList<>(names.size());
            for (String name : names) {
                PlayerSession s = worldManager.getSessionByPlayerId(name);
                boolean online = s != null;
                String zone = online ? s.getZoneId() : "";
                entries.add(new FriendEntry(name, online, zone));
            }
            entries.sort((a, b) -> {
                if (a.online != b.online) return a.online ? -1 : 1;
                return a.name.compareToIgnoreCase(b.name);
            });
            // Pending invites — both directions, alphabetised. UI shows the
            // incoming set as actionable rows, outgoing as info-only.
            java.util.List<String> incoming = new java.util.ArrayList<>(redisManager.incomingRequests(username));
            java.util.List<String> outgoing = new java.util.ArrayList<>(redisManager.outgoingRequests(username));
            java.util.Collections.sort(incoming, String.CASE_INSENSITIVE_ORDER);
            java.util.Collections.sort(outgoing, String.CASE_INSENSITIVE_ORDER);

            String json = mapper.writeValueAsString(new FriendListPayload(entries, incoming, outgoing));
            ch.writeAndFlush(new GamePacket(PacketType.FRIEND_LIST_DATA, json));
        } catch (Exception e) {
            log.warn("sendFriendList for {} failed: {}", username, e.getMessage());
        }
    }

    private void sendFriendError(ChannelHandlerContext ctx, String msg) {
        try {
            String json = mapper.writeValueAsString(new FriendErrorPayload(msg));
            ctx.writeAndFlush(new GamePacket(PacketType.FRIEND_ERROR, json));
        } catch (Exception ignored) { /* best effort */ }
    }

    private record FriendEntry(String name, boolean online, String zone) {}
    private record FriendListPayload(java.util.List<FriendEntry> friends,
                                     java.util.List<String> incoming,
                                     java.util.List<String> outgoing) {}
    private record FriendErrorPayload(String message) {}
    private record FriendAddedByPayload(String by) {}
    private record FriendRequestFromPayload(String from) {}

    private void handleClientLog(ChannelHandlerContext ctx, GamePacket packet) {
        PlayerSession session = worldManager.getSession(ctx.channel());
        // Pre-login crashes still useful; tag them with the IP instead of player.
        String who = session != null ? session.getPlayerId() : "anon@" + clientIpOf(ctx);

        // Throttle so one runaway Update loop doesn't fill the disk before the
        // 10/min ceiling at the client trims its own end. Defence in depth.
        if (!clientLogLimiter.check(who).allowed) return;

        try {
            JsonNode node = mapper.readTree(packet.getPayload());
            String level   = node.has("level")      ? node.get("level").asText()      : "?";
            String message = node.has("message")    ? node.get("message").asText()    : "";
            String stack   = node.has("stackTrace") ? node.get("stackTrace").asText() : "";
            // Server-side trim as well — never trust the client's truncation.
            if (message.length() > 800)  message = message.substring(0, 800) + "...(truncated)";
            if (stack.length()   > 3000) stack   = stack.substring(0, 3000) + "...(truncated)";
            clientLog.info("[{}] [{}] {}\n{}", who, level, message, stack);
        } catch (Exception e) {
            log.warn("malformed CLIENT_LOG from {}: {}", who, e.getMessage());
        }
    }

    private void handleStatusUpdate(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        JsonNode node = mapper.readTree(packet.getPayload());
        int hp = node.has("hp") ? node.get("hp").asInt() : 0;
        int maxHp = node.has("maxHp") ? node.get("maxHp").asInt() : 0;
        // Snapshot for the party widget — same reporting rate (~3 Hz) the
        // visible HP bars already use, so no extra network cost.
        session.lastHp = hp;
        session.lastMaxHp = maxHp;
        // Trigger a party update when HP or level changes substantially — the
        // widget pulls from these fields next refresh. Throttled implicitly
        // by the status-update rate limit on the client.
        String myParty = redisManager.getPartyOf(session.getPlayerId());
        if (myParty != null && !myParty.isEmpty()) {
            broadcastPartyUpdate(myParty);
        }

        // Combat stats — used later to cap damage. Clamp to sane ranges so a forged
        // STATUS_UPDATE can't enable arbitrary damage either.
        if (node.has("level"))      session.level      = Math.min(100, Math.max(1, node.get("level").asInt()));
        if (node.has("intStat"))    session.intStat    = Math.min(999, Math.max(1, node.get("intStat").asInt()));
        if (node.has("weaponDmg"))  session.weaponDmg  = Math.min(500, Math.max(0, node.get("weaponDmg").asInt()));
        if (node.has("starboltLv")) session.starboltLv = Math.min(10,  Math.max(1, node.get("starboltLv").asInt()));
        if (node.has("className"))        session.className        = nullToEmpty(node.get("className").asText());
        if (node.has("equippedWeaponId")) session.equippedWeaponId = nullToEmpty(node.get("equippedWeaponId").asText());
        if (node.has("equippedHelmetId")) session.equippedHelmetId = nullToEmpty(node.get("equippedHelmetId").asText());
        if (node.has("equippedArmorId"))  session.equippedArmorId  = nullToEmpty(node.get("equippedArmorId").asText());
        if (node.has("equippedRingId"))   session.equippedRingId   = nullToEmpty(node.get("equippedRingId").asText());

        String zoneId = session.getZoneId();
        if (zoneId == null || zoneId.isEmpty()) return;
        String payload = mapper.writeValueAsString(new PlayerStatus(
            session.getPlayerId(), hp, maxHp,
            session.className,
            session.equippedWeaponId, session.equippedHelmetId,
            session.equippedArmorId, session.equippedRingId));
        worldManager.broadcastToZoneExcept(zoneId,
            new GamePacket(PacketType.PLAYER_STATUS, payload), session.getPlayerId());
    }

    private static String nullToEmpty(String s) { return s == null ? "" : s; }

    // Authoritative max damage the client is allowed to claim. Formula mirrors
    // PlayerStats.ComputeBoltDamage with a 1.3× safety margin to absorb variance + skill bonus.
    private static int maxAllowedDamage(PlayerSession s) {
        // Base formula: 5 + INT*2 + LV*3 + weaponDmg + (starboltLv-1)*5
        int base = 5 + s.intStat * 2 + s.level * 3 + s.weaponDmg + (s.starboltLv - 1) * 5;
        // Variance up to ±20%, plus small headroom for legitimate skills (e.g. meteor)
        return Math.max(10, (int) Math.ceil(base * 1.6));
    }

    private void handleDropClaim(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        JsonNode node = mapper.readTree(packet.getPayload());
        String dropId = node.get("dropId").asText();
        monsterManager.onDropClaim(session, dropId);
    }

    private void handleSkillCast(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        JsonNode node = mapper.readTree(packet.getPayload());
        float x = (float) node.get("x").asDouble();
        float y = (float) node.get("y").asDouble();
        int dir = node.has("dir") ? node.get("dir").asInt() : 1;
        String skillType = node.has("type") ? node.get("type").asText() : "starbolt";
        String broadcastPayload = mapper.writeValueAsString(
            new SkillCastBroadcast(session.getPlayerId(), x, y, dir, skillType));
        worldManager.broadcastToZone(session.getZoneId(),
            new GamePacket(PacketType.SKILL_BROADCAST, broadcastPayload));
    }

    record SkillCastBroadcast(String playerId, float x, float y, int dir, String type) {}

    private void handleZoneEnter(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        JsonNode node = mapper.readTree(packet.getPayload());
        String newZone = node.has("zoneId") ? node.get("zoneId").asText() : "";
        if (node.has("nickname")) {
            String nick = node.get("nickname").asText();
            if (nick != null && !nick.isEmpty()) session.setNickname(nick);
        }
        if (node.has("className"))        session.className        = nullToEmpty(node.get("className").asText());
        if (node.has("equippedWeaponId")) session.equippedWeaponId = nullToEmpty(node.get("equippedWeaponId").asText());
        if (node.has("equippedHelmetId")) session.equippedHelmetId = nullToEmpty(node.get("equippedHelmetId").asText());
        if (node.has("equippedArmorId"))  session.equippedArmorId  = nullToEmpty(node.get("equippedArmorId").asText());
        if (node.has("equippedRingId"))   session.equippedRingId   = nullToEmpty(node.get("equippedRingId").asText());
        String oldZone = session.getZoneId();

        if (!java.util.Objects.equals(oldZone, newZone)) {
            // Tell players in old zone this one is gone
            if (oldZone != null && !oldZone.isEmpty()) {
                String despawnData = "{\"playerId\":\"" + session.getPlayerId() + "\"}";
                worldManager.broadcastToZone(oldZone,
                    new GamePacket(PacketType.DESPAWN_PLAYER, despawnData));
            }
            // Route through WorldManager so the zone-keyed broadcast index
            // stays in sync — calling session.setZoneId directly would skip it.
            worldManager.setZoneId(session, newZone);
            // Zone change is a legitimate "teleport" — skip the next move validation
            session.lastMoveAt = 0L;
            // Announce this player to the new zone
            String spawnData = mapper.writeValueAsString(new SpawnData(session.getPlayerId(), session.getNickname(), session.className,
                              session.equippedWeaponId, session.equippedHelmetId,
                              session.equippedArmorId, session.equippedRingId,
                              session.getPosition()));
            worldManager.broadcastToZone(newZone,
                new GamePacket(PacketType.SPAWN_PLAYER, spawnData));
            // Send back a snapshot of existing players in this zone (so the new arrival sees them)
            sendPlayerSnapshot(session);
        }

        log.info("Player {} entered zone: {}", session.getPlayerId(), newZone);
        monsterManager.onPlayerEnteredZone(session);
    }

    private void sendPlayerSnapshot(PlayerSession self) throws Exception {
        String selfId = self.getPlayerId();
        String zoneId = self.getZoneId();
        if (zoneId == null || zoneId.isEmpty()) return;
        for (PlayerSession other : worldManager.getAllSessions()) {
            if (other.getPlayerId().equals(selfId)) continue;
            if (!zoneId.equals(other.getZoneId())) continue;
            String spawnData = mapper.writeValueAsString(new SpawnData(other.getPlayerId(), other.getNickname(), other.className,
                          other.equippedWeaponId, other.equippedHelmetId,
                          other.equippedArmorId, other.equippedRingId,
                          other.getPosition()));
            self.getChannel().writeAndFlush(new GamePacket(PacketType.SPAWN_PLAYER, spawnData));
        }
    }

    private void handleMonsterHit(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        JsonNode node = mapper.readTree(packet.getPayload());
        String monsterId = node.get("id").asText();
        int claimed = node.has("damage") ? node.get("damage").asInt() : 1;
        boolean crit = node.has("crit") && node.get("crit").asBoolean();
        // Cap allows up to 1.7x for crits on top of the normal anti-cheat ceiling
        int baseCap = maxAllowedDamage(session);
        int cap = crit ? Math.round(baseCap * 1.7f) : baseCap;
        int applied = Math.max(1, Math.min(claimed, cap));
        if (claimed > cap) {
            log.warn("[anti-cheat] {} claimed dmg {} crit={} -> capped to {} (LV{} INT{} WPN{} BoltLv{})",
                session.getPlayerId(), claimed, crit, cap,
                session.level, session.intStat, session.weaponDmg, session.starboltLv);
        }
        monsterManager.onMonsterHit(session, monsterId, applied, crit);
    }

    private void handleStateRequest(ChannelHandlerContext ctx, GamePacket packet) {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        String json = redisManager.getPlayerState(session.getPlayerId());
        if (json == null) json = "{}";
        ctx.writeAndFlush(new GamePacket(PacketType.STATE_DATA, json));
    }

    private void handleStateSave(ChannelHandlerContext ctx, GamePacket packet) {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;
        String json = packet.getPayload();
        String saveId = null;
        JsonNode node;
        try {
            node = mapper.readTree(json);
        } catch (Exception e) {
            log.warn("[anti-cheat] {} STATE_SAVE unparseable, dropping", session.getPlayerId());
            return;
        }

        // Pull saveId off the node so the client gets ACK'd by id, and so the
        // value we persist to Redis doesn't carry per-attempt metadata.
        if (node != null && node.has("saveId")) {
            String s = node.get("saveId").asText();
            if (s != null && !s.isEmpty()) {
                saveId = s;
                if (node instanceof ObjectNode) {
                    ((ObjectNode) node).remove("saveId");
                    try { json = mapper.writeValueAsString(node); }
                    catch (Exception ignored) { /* keep original raw json */ }
                }
            }
        }

        // Sanity-validate before persisting. A modified client can otherwise
        // mail us gold=999_999_999 and we'd dutifully store it. Caps below
        // are generous enough that legitimate late-game play stays well
        // inside, tight enough that overflow- or zero-knowledge-spray cheats
        // get refused. Missing fields are OK — clients sometimes send
        // partial state during certain transitions.
        if (!isStateSavePlausible(node, session.getPlayerId())) {
            // Track suspicion so an operator can see who keeps tripping the
            // gate. 24h horizon so noise from one bad save doesn't haunt
            // a player forever.
            try {
                String suspKey = "account:cheats:" + session.getPlayerId();
                redisManager.incr(suspKey);
                redisManager.expire(suspKey, 24 * 3600L);
            } catch (Exception ignored) { /* never break save flow on logging */ }
            // No ACK — the client's retry loop will try again with the same
            // payload, which will also fail. A modded client gets stuck
            // 'saving' forever, an honest one gets repaired by the next
            // server-authoritative event that refreshes state.
            return;
        }

        redisManager.savePlayerState(session.getPlayerId(), json);

        if (saveId != null) {
            try {
                String ack = "{\"saveId\":\"" + saveId + "\"}";
                ctx.writeAndFlush(new GamePacket(PacketType.STATE_ACK, ack));
            } catch (Exception e) { /* ignore */ }
        }
    }

    /** Caps tracked against PlayerState.cs. Update both files together when
     *  legit content pushes the ceiling — currently sized for level 200,
     *  ~1B gold, and 100 inventory slots. */
    private boolean isStateSavePlausible(JsonNode node, String playerId) {
        if (node == null || !node.isObject()) return false;
        if (!checkRange(node, "level",       1,  200,            playerId)) return false;
        if (!checkRange(node, "exp",         0,  1_000_000_000L, playerId)) return false;
        if (!checkRange(node, "gold",        0,  1_000_000_000L, playerId)) return false;
        if (!checkRange(node, "statStr",     0,  10_000,         playerId)) return false;
        if (!checkRange(node, "statDex",     0,  10_000,         playerId)) return false;
        if (!checkRange(node, "statInt",     0,  10_000,         playerId)) return false;
        if (!checkRange(node, "statLuk",     0,  10_000,         playerId)) return false;
        if (!checkRange(node, "statPoints",  0,  10_000,         playerId)) return false;
        if (!checkRange(node, "skillPoints", 0,  10_000,         playerId)) return false;
        if (!checkRange(node, "hp",          0,  1_000_000,      playerId)) return false;
        if (!checkRange(node, "maxHp",       0,  1_000_000,      playerId)) return false;
        if (!checkRange(node, "mp",          0,  1_000_000,      playerId)) return false;
        if (!checkRange(node, "maxMp",       0,  1_000_000,      playerId)) return false;

        // Inventory: array length cap + per-slot quantity sanity + paired arrays.
        JsonNode ids = node.get("inventoryItemIds");
        JsonNode qts = node.get("inventoryQuantities");
        if (ids != null && ids.isArray()) {
            if (ids.size() > 100) {
                log.warn("[anti-cheat] {} inventory size {} > 100", playerId, ids.size());
                return false;
            }
            if (qts == null || !qts.isArray() || qts.size() != ids.size()) {
                log.warn("[anti-cheat] {} inventory ids/qty mismatch", playerId);
                return false;
            }
            for (int i = 0; i < qts.size(); i++) {
                int q = qts.get(i).asInt();
                if (q < 0 || q > 99_999) {
                    log.warn("[anti-cheat] {} inventory[{}] qty={} out of [0,99999]", playerId, i, q);
                    return false;
                }
            }
        }
        return true;
    }

    /** Returns true if {@code field} is absent (partial save tolerated) or in [min, max]. */
    private boolean checkRange(JsonNode node, String field, long min, long max, String playerId) {
        if (!node.has(field)) return true;
        long v = node.get(field).asLong();
        if (v < min || v > max) {
            log.warn("[anti-cheat] {} {}={} outside [{}, {}]", playerId, field, v, min, max);
            return false;
        }
        return true;
    }

    private void handleLogin(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        JsonNode node = mapper.readTree(packet.getPayload());
        String username = node.get("username").asText();
        String password = node.get("password").asText();
        boolean isRegister = node.has("isRegister") && node.get("isRegister").asBoolean();
        String clientVersion = node.has("clientVersion") ? node.get("clientVersion").asText() : "";
        String clientIp = clientIpOf(ctx);

        // IP gate first. Version + credential checks would otherwise act as
        // a 'username/password valid?' oracle for a brute-forcer.
        LoginRateLimiter.Result ipRes = ipLimiter.check(clientIp);
        if (!ipRes.allowed) {
            String msg = "로그인 시도가 너무 많습니다. " + ipRes.secondsLeft + "초 후 다시 시도해 주세요.";
            String result = mapper.writeValueAsString(new LoginResult(false, null, msg));
            ctx.writeAndFlush(new GamePacket(PacketType.LOGIN_RESULT, result));
            log.warn("Rate-limited login from {} (user='{}'): IP-locked {}s", clientIp, username, ipRes.secondsLeft);
            return;
        }

        // Username gate — catches one account being sprayed from many IPs.
        // Counts both wrong-password and account-not-found because an attacker
        // can't distinguish them either, so accidentally counting non-existent
        // accounts doesn't help username-enumeration attacks.
        LoginRateLimiter.Result userRes = usernameLimiter.check(username);
        if (!userRes.allowed) {
            String msg = "이 계정의 로그인 시도가 너무 많습니다. " + userRes.secondsLeft + "초 후 다시 시도해 주세요.";
            String result = mapper.writeValueAsString(new LoginResult(false, null, msg));
            ctx.writeAndFlush(new GamePacket(PacketType.LOGIN_RESULT, result));
            log.warn("Rate-limited login for user='{}' from {}: user-locked {}s", username, clientIp, userRes.secondsLeft);
            return;
        }

        // Reject before touching credentials when the build is wire-incompatible
        if (!com.astrion.common.Version.CURRENT.equals(clientVersion)) {
            String msg = "버전 불일치 — 클라이언트를 업데이트해 주세요. ("
                + "client " + (clientVersion.isEmpty() ? "?" : clientVersion)
                + " ≠ server " + com.astrion.common.Version.CURRENT + ")";
            String result = mapper.writeValueAsString(new LoginResult(false, null, msg));
            ctx.writeAndFlush(new GamePacket(PacketType.LOGIN_RESULT, result));
            log.warn("Rejected login from {}: version mismatch (client={} server={})",
                username, clientVersion, com.astrion.common.Version.CURRENT);
            return;
        }

        // The client hashes the password before sending now (SHA-256 hex,
        // see Unity PasswordHasher). The payload field already IS the
        // digest — don't hash again. Existing on-disk accounts were
        // stored as the same SHA-256 digest of plaintext (the previous
        // server-side hashPassword call produced an identical value),
        // so this is a wire-only change with no data migration.
        String hashedPassword = password;
        String accountKey = "account:" + username;

        if (isRegister) {
            // Register
            String existing = redisManager.get(accountKey);
            if (existing != null) {
                String result = mapper.writeValueAsString(new LoginResult(false, null, "Username already exists."));
                ctx.writeAndFlush(new GamePacket(PacketType.LOGIN_RESULT, result));
                return;
            }
            redisManager.set(accountKey, hashedPassword);
            log.info("New account registered: {}", username);
        } else {
            // Login
            String storedPassword = redisManager.get(accountKey);
            if (storedPassword == null) {
                // Account not found is reported the same way as a wrong
                // password would be to avoid leaking which usernames exist
                // — but we deliberately do NOT call accountLockout.record-
                // Failure here. Locking on missing accounts would let a
                // spray attack DoS arbitrary nicknames out of existence.
                String result = mapper.writeValueAsString(new LoginResult(false, null, "Account not found."));
                ctx.writeAndFlush(new GamePacket(PacketType.LOGIN_RESULT, result));
                return;
            }
            // Account exists — consult persistent lockout BEFORE comparing
            // the password. If we got past the in-memory username rate gate
            // it might still be that this account is locked from earlier
            // attempts that crossed a server restart.
            long lockLeft = accountLockout.lockSecondsLeft(username);
            if (lockLeft > 0) {
                long mins = (lockLeft + 59) / 60;
                String msg = "계정이 잠겼습니다. 약 " + mins + "분 후 다시 시도해 주세요.";
                String result = mapper.writeValueAsString(new LoginResult(false, null, msg));
                ctx.writeAndFlush(new GamePacket(PacketType.LOGIN_RESULT, result));
                log.warn("Account-locked login user='{}' from {}: {}s left", username, clientIp, lockLeft);
                return;
            }
            if (!storedPassword.equals(hashedPassword)) {
                accountLockout.recordFailure(username);
                String result = mapper.writeValueAsString(new LoginResult(false, null, "Wrong password."));
                ctx.writeAndFlush(new GamePacket(PacketType.LOGIN_RESULT, result));
                return;
            }
        }

        // Check if already logged in
        if (worldManager.getSessionByPlayerId(username) != null) {
            String result = mapper.writeValueAsString(new LoginResult(false, null, "Already logged in."));
            ctx.writeAndFlush(new GamePacket(PacketType.LOGIN_RESULT, result));
            return;
        }

        // Login success — actual SPAWN_PLAYER broadcast happens on ZONE_ENTER
        PlayerSession session = worldManager.addPlayer(username, ctx.channel());
        redisManager.setPlayerOnline(username);

        String result = mapper.writeValueAsString(new LoginResult(true, username, "OK"));
        ctx.writeAndFlush(new GamePacket(PacketType.LOGIN_RESULT, result));

        // Legitimate user — drop accumulated failure counts on all three layers.
        ipLimiter.onSuccess(clientIp);
        usernameLimiter.onSuccess(username);
        accountLockout.clear(username);

        log.info("Player {} logged in", username);
    }

    private static String clientIpOf(ChannelHandlerContext ctx) {
        try {
            java.net.SocketAddress addr = ctx.channel().remoteAddress();
            if (addr instanceof java.net.InetSocketAddress) {
                return ((java.net.InetSocketAddress) addr).getAddress().getHostAddress();
            }
            return addr == null ? "unknown" : addr.toString();
        } catch (Exception e) {
            return "unknown";
        }
    }

    private String hashPassword(String password) {
        try {
            MessageDigest md = MessageDigest.getInstance("SHA-256");
            byte[] hash = md.digest(password.getBytes(StandardCharsets.UTF_8));
            StringBuilder sb = new StringBuilder();
            for (byte b : hash) {
                sb.append(String.format("%02x", b));
            }
            return sb.toString();
        } catch (Exception e) {
            throw new RuntimeException("Failed to hash password", e);
        }
    }

    // Movement anti-cheat thresholds — wide enough to absorb 1s lag spikes and
    // the player's worst-case dash + jump combo, tight enough to reject teleports.
    private static final double MAX_MOVE_SPEED = 12.0;   // world units per second
    private static final double MOVE_GRACE_SECONDS = 0.5; // soft margin (lag spike absorber)
    private static final double MOVE_FIXED_TOLERANCE = 1.0; // extra constant pad in units

    private void handleMove(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;

        JsonNode node = mapper.readTree(packet.getPayload());
        Position newPos = new Position(
                (float) node.get("x").asDouble(),
                (float) node.get("y").asDouble(),
                (float) node.get("z").asDouble()
        );
        int facing = node.has("facing") ? node.get("facing").asInt() : 1;

        long now = System.currentTimeMillis();

        // Validate against last accepted move (skipped on the very first move and right after a zone change).
        if (session.lastMoveAt > 0) {
            double dt = (now - session.lastMoveAt) / 1000.0;
            double dx = newPos.getX() - session.lastValidPos.getX();
            double dy = newPos.getY() - session.lastValidPos.getY();
            double dist = Math.sqrt(dx * dx + dy * dy);
            double allowed = MAX_MOVE_SPEED * (dt + MOVE_GRACE_SECONDS) + MOVE_FIXED_TOLERANCE;
            if (dist > allowed)
            {
                log.warn("[anti-cheat] {} teleport rejected dist={} dt={}s allowed={} ({},{}) -> ({},{})",
                    session.getPlayerId(),
                    String.format("%.2f", dist),
                    String.format("%.3f", dt),
                    String.format("%.2f", allowed),
                    session.lastValidPos.getX(), session.lastValidPos.getY(),
                    newPos.getX(), newPos.getY());
                // Drop the move: server keeps the last valid position; nothing is
                // broadcast. The cheating client will look frozen to other players
                // until they fall back into the allowed envelope.
                return;
            }
        }

        session.lastMoveAt = now;
        session.lastValidPos = newPos;
        session.setPosition(newPos);
        redisManager.updatePlayerPosition(session.getPlayerId(), newPos);

        String moveData = mapper.writeValueAsString(new MoveData(session.getPlayerId(), newPos, facing));
        worldManager.broadcastNearby(session.getZoneId(), newPos, BROADCAST_RANGE,
                new GamePacket(PacketType.PLAYER_MOVED, moveData), session.getPlayerId());
    }

    private void handleChat(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;

        JsonNode node = mapper.readTree(packet.getPayload());
        String message = node.get("message").asText();

        String chatData = mapper.writeValueAsString(new ChatData(session.getPlayerId(), message));
        GamePacket out = new GamePacket(PacketType.CHAT_MESSAGE, chatData);

        String zoneId = session.getZoneId();
        if (zoneId == null || zoneId.isEmpty()) {
            // Sender hasn't picked a zone yet — echo only to themselves.
            ctx.writeAndFlush(out);
        } else {
            worldManager.broadcastToZone(zoneId, out);
        }
    }

    private void handleAttack(ChannelHandlerContext ctx, GamePacket packet) {
        log.info("Attack packet received from {}", ctx.channel().id().asShortText());
    }

    private void handleCharacterList(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;

        var chars = redisManager.getCharacters(session.getPlayerId());
        StringBuilder sb = new StringBuilder("{\"characters\":[");
        for (int i = 0; i < chars.size(); i++) {
            if (i > 0) sb.append(",");
            sb.append(chars.get(i));
        }
        sb.append("]}");

        ctx.writeAndFlush(new GamePacket(PacketType.CHARACTER_LIST_RESULT, sb.toString()));
        log.info("Character list sent to {}: {} characters", session.getPlayerId(), chars.size());
    }

    private void handleCharacterCreate(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;

        JsonNode node = mapper.readTree(packet.getPayload());
        String charName = node.get("name").asText();
        String charClass = node.get("className").asText();

        // Validate name
        if (charName.length() < 2 || charName.length() > 16) {
            String result = mapper.writeValueAsString(new CharacterCreateResult(false, "Name must be 2-16 characters."));
            ctx.writeAndFlush(new GamePacket(PacketType.CHARACTER_CREATE_RESULT, result));
            return;
        }

        // Check max characters (4)
        var existing = redisManager.getCharacters(session.getPlayerId());
        if (existing.size() >= 4) {
            String result = mapper.writeValueAsString(new CharacterCreateResult(false, "Maximum 4 characters allowed."));
            ctx.writeAndFlush(new GamePacket(PacketType.CHARACTER_CREATE_RESULT, result));
            return;
        }

        // Check duplicate name
        if (redisManager.characterExists(session.getPlayerId(), charName)) {
            String result = mapper.writeValueAsString(new CharacterCreateResult(false, "Character name already exists."));
            ctx.writeAndFlush(new GamePacket(PacketType.CHARACTER_CREATE_RESULT, result));
            return;
        }

        // Save character
        redisManager.saveCharacter(session.getPlayerId(), charName, charClass, 1);

        // Grant a starter kit (bread + 4 bound boxes) for every NEW character on this account.
        // Inventory is shared per account, so we APPEND to whatever's already there.
        // Gold is only granted the first time (when the account has no state yet).
        try {
            String stateJson = redisManager.getPlayerState(session.getPlayerId());
            ObjectNode obj;
            boolean firstEver = (stateJson == null);
            if (firstEver) {
                obj = mapper.createObjectNode();
            } else {
                JsonNode loaded = mapper.readTree(stateJson);
                obj = (loaded instanceof ObjectNode) ? (ObjectNode) loaded : mapper.createObjectNode();
            }

            ArrayNode ids = obj.has("inventoryItemIds") && obj.get("inventoryItemIds").isArray()
                ? (ArrayNode) obj.get("inventoryItemIds") : mapper.createArrayNode();
            ArrayNode qtys = obj.has("inventoryQuantities") && obj.get("inventoryQuantities").isArray()
                ? (ArrayNode) obj.get("inventoryQuantities") : mapper.createArrayNode();

            String[] starterIds  = { "bread", "weapon_box", "helmet_box", "armor_box", "ring_box" };
            int[]    starterQtys = { 3, 1, 1, 1, 1 };
            for (int i = 0; i < starterIds.length; i++) {
                ids.add(starterIds[i]);
                qtys.add(starterQtys[i]);
            }

            // Class-specific starter: weapon + main stat bonus
            String starterWeapon = null;
            int str = 5, dex = 5, intStat = 5, luk = 5;
            int maxMp = 50;
            if ("Warrior".equals(charClass)) {
                starterWeapon = "warrior_sword_bound";
                str = 10;
            } else if ("Mage".equals(charClass)) {
                starterWeapon = "mage_staff_bound";
                intStat = 10;
                maxMp = 70;
            } else if ("Archer".equals(charClass)) {
                starterWeapon = "star_bow_bound";
                dex = 10;
            } else if ("Thief".equals(charClass)) {
                starterWeapon = "bronze_dagger_bound";
                luk = 10;
            }
            if (starterWeapon != null) {
                ids.add(starterWeapon);
                qtys.add(1);
                obj.put("equippedWeaponId", starterWeapon);
            }
            // Stats / MP only set on the brand-new account so older accounts
            // don't get reset when they roll a new character.
            if (firstEver) {
                obj.put("statStr", str);
                obj.put("statDex", dex);
                obj.put("statInt", intStat);
                obj.put("statLuk", luk);
                obj.put("maxMp", maxMp);
                obj.put("mp", maxMp);
            }

            obj.set("inventoryItemIds", ids);
            obj.set("inventoryQuantities", qtys);

            if (firstEver) obj.put("gold", 50);

            redisManager.savePlayerState(session.getPlayerId(), mapper.writeValueAsString(obj));
            log.info("Starter kit appended for {} on new character {} (firstEver={})",
                session.getPlayerId(), charName, firstEver);
        } catch (Exception e) {
            log.warn("Failed to append starter kit for {}: {}", session.getPlayerId(), e.getMessage());
        }

        String result = mapper.writeValueAsString(new CharacterCreateResult(true, "Character created!"));
        ctx.writeAndFlush(new GamePacket(PacketType.CHARACTER_CREATE_RESULT, result));
        log.info("Character created for {}: {} ({})", session.getPlayerId(), charName, charClass);
    }

    private void handleCharacterDelete(ChannelHandlerContext ctx, GamePacket packet) throws Exception {
        PlayerSession session = worldManager.getSession(ctx.channel());
        if (session == null) return;

        JsonNode node = mapper.readTree(packet.getPayload());
        String charName = node.get("name").asText();

        boolean deleted = redisManager.deleteCharacter(session.getPlayerId(), charName);
        String msg = deleted ? "Character deleted." : "Character not found.";
        ctx.writeAndFlush(new GamePacket(PacketType.CHARACTER_DELETE_RESULT,
                "{\"success\":" + deleted + ",\"message\":\"" + msg + "\"}"));
        log.info("Character delete for {}: {} ({})", session.getPlayerId(), charName, deleted);
    }

    @Override
    public void channelInactive(ChannelHandlerContext ctx) {
        PlayerSession session = worldManager.removePlayer(ctx.channel());
        if (session != null) {
            // Pull them out of any active party first so the remaining
            // members get an updated roster with the leaver removed. Same
            // shape as a voluntary /leave.
            try { removeFromParty(session.getPlayerId()); }
            catch (Exception e) { log.warn("party cleanup for {} failed: {}", session.getPlayerId(), e.getMessage()); }
            redisManager.setPlayerOffline(session.getPlayerId());
            String despawnData = "{\"playerId\":\"" + session.getPlayerId() + "\"}";
            worldManager.broadcastAll(new GamePacket(PacketType.DESPAWN_PLAYER, despawnData), session.getPlayerId());
            log.info("Player {} disconnected", session.getPlayerId());
        }
    }

    @Override
    public void userEventTriggered(ChannelHandlerContext ctx, Object evt) {
        if (evt instanceof IdleStateEvent) {
            log.info("Idle connection detected, closing: {}", ctx.channel().id().asShortText());
            ctx.close();
        }
    }

    @Override
    public void exceptionCaught(ChannelHandlerContext ctx, Throwable cause) {
        String chId = ctx.channel().id().asShortText();
        if (isExpectedClientFault(cause)) {
            // Port scanners, half-broken clients, plaintext probes against TLS:
            // we close the channel as expected. Don't pollute errors.log — these
            // are normal background hum, not bugs to triage.
            log.warn("Closing channel {} ({}): {}",
                chId, cause.getClass().getSimpleName(), cause.getMessage());
        } else {
            // Anything we don't recognise is real. Log with full stack trace
            // (third arg to log.error) so it lands in errors.log with %ex
            // expansion, and we can debug from a single tail.
            log.error("Error in channel {}: {}", chId, cause.getMessage(), cause);
        }
        ctx.close();
    }

    /** Classifies remote-induced channel faults so they don't get treated as
     *  server bugs. Anything matching here is a client-side mistake we already
     *  defend against (TLS misuse, decoder mismatch, peer disconnect). */
    private static boolean isExpectedClientFault(Throwable cause) {
        if (cause instanceof io.netty.handler.ssl.NotSslRecordException) return true;
        if (cause instanceof javax.net.ssl.SSLException) return true;
        if (cause instanceof io.netty.handler.codec.DecoderException) {
            Throwable inner = cause.getCause();
            if (inner instanceof javax.net.ssl.SSLException) return true;
            if (inner instanceof io.netty.handler.ssl.NotSslRecordException) return true;
        }
        if (cause instanceof java.io.IOException) {
            String m = cause.getMessage();
            if (m != null && (m.contains("Connection reset")
                           || m.contains("Broken pipe")
                           || m.contains("forcibly closed"))) return true;
        }
        return false;
    }

    // DTO records
    record LoginResult(boolean success, String playerId, String message) {}
    record SpawnData(String playerId, String nickname, String className,
                     String equippedWeaponId, String equippedHelmetId,
                     String equippedArmorId, String equippedRingId,
                     Position position) {}
    record MoveData(String playerId, Position position, int facing) {}
    record ChatData(String playerId, String message) {}
    record CharacterCreateResult(boolean success, String message) {}
    record PlayerStatus(String playerId, int hp, int maxHp, String className,
                        String equippedWeaponId, String equippedHelmetId,
                        String equippedArmorId, String equippedRingId) {}
}
