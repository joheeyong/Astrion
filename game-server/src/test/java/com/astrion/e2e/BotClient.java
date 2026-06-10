package com.astrion.e2e;

import com.astrion.common.packet.PacketType;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;

import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.io.EOFException;
import java.io.IOException;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.TimeUnit;
import java.util.function.Predicate;

/// Minimal headless game client for E2E smoke tests. Speaks the exact wire
/// protocol (4-byte BE length covering type+payload, 1-byte type code,
/// UTF-8 JSON payload) over plain TCP — the test harness boots the server
/// without TLS certs, which drops the listener to plaintext, so no SSL
/// machinery is needed here.
///
/// A background reader thread parses every inbound packet into an inbox.
/// Tests consume via waitFor(type, predicate): non-matching packets are
/// stashed (broadcast noise — spawns, monster moves, achievement pushes —
/// arrives interleaved with what the test actually awaits) and re-scanned
/// on later calls, so ordering between unrelated packet streams never
/// flakes the test.
final class BotClient implements AutoCloseable {

    record Packet(PacketType type, JsonNode json) {}

    private static final ObjectMapper M = new ObjectMapper();
    private static final long DEFAULT_TIMEOUT_MS = 5_000;

    private final String label;
    private final Socket socket;
    private final DataOutputStream out;
    private final Thread reader;
    private final BlockingQueue<Packet> inbox = new LinkedBlockingQueue<>();
    private final List<Packet> stash = new ArrayList<>();

    BotClient(String label, String host, int port) throws IOException {
        this.label = label;
        this.socket = new Socket(host, port);
        this.socket.setTcpNoDelay(true);
        this.out = new DataOutputStream(socket.getOutputStream());
        DataInputStream in = new DataInputStream(socket.getInputStream());
        this.reader = new Thread(() -> readLoop(in), "bot-reader-" + label);
        this.reader.setDaemon(true);
        this.reader.start();
    }

    private void readLoop(DataInputStream in) {
        try {
            while (!socket.isClosed()) {
                int length = in.readInt();          // type(1) + payload(n)
                int code = in.readByte() & 0xFF;
                byte[] payload = new byte[length - 1];
                in.readFully(payload);
                PacketType type = PacketType.fromCode(code);
                String raw = new String(payload, StandardCharsets.UTF_8);
                JsonNode json = raw.isEmpty() ? M.createObjectNode() : M.readTree(raw);
                inbox.put(new Packet(type, json));
            }
        } catch (EOFException | java.net.SocketException e) {
            // normal teardown — server closed or we closed
        } catch (Exception e) {
            System.err.println("[" + label + "] reader died: " + e);
        }
    }

    void send(PacketType type, String json) throws IOException {
        byte[] payload = json.getBytes(StandardCharsets.UTF_8);
        synchronized (out) {
            out.writeInt(1 + payload.length);
            out.writeByte(type.getCode());
            out.write(payload);
            out.flush();
        }
    }

    JsonNode waitFor(PacketType type, Predicate<JsonNode> pred) {
        return waitFor(type, pred, DEFAULT_TIMEOUT_MS);
    }

    /// Blocks until a packet of {@code type} satisfying {@code pred} arrives
    /// (or already sits in the stash). Throws AssertionError with a summary
    /// of everything seen so far on timeout — the summary is what makes CI
    /// failures diagnosable without re-running locally.
    JsonNode waitFor(PacketType type, Predicate<JsonNode> pred, long timeoutMs) {
        long deadline = System.currentTimeMillis() + timeoutMs;
        synchronized (stash) {
            var it = stash.iterator();
            while (it.hasNext()) {
                Packet p = it.next();
                if (p.type() == type && pred.test(p.json())) { it.remove(); return p.json(); }
            }
        }
        while (true) {
            long left = deadline - System.currentTimeMillis();
            if (left <= 0) throw new AssertionError(
                "[" + label + "] timed out waiting for " + type + ". Seen so far: " + seenSummary());
            Packet p;
            try { p = inbox.poll(left, TimeUnit.MILLISECONDS); }
            catch (InterruptedException e) { Thread.currentThread().interrupt(); throw new AssertionError("interrupted"); }
            if (p == null) continue;
            if (p.type() == type && pred.test(p.json())) return p.json();
            synchronized (stash) { stash.add(p); }
        }
    }

    private String seenSummary() {
        synchronized (stash) {
            var counts = new java.util.TreeMap<String, Integer>();
            for (Packet p : stash) counts.merge(p.type().name(), 1, Integer::sum);
            return counts.toString();
        }
    }

    @Override
    public void close() {
        try { socket.close(); } catch (IOException ignored) {}
    }
}
