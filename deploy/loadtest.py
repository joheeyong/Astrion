#!/usr/bin/env python3
"""
Astrion game server load test.

Drives N concurrent TLS+LOGIN+MOVE sessions against the game server and reports
how many of them stayed connected for the duration, plus packet send rates.

Runs from INSIDE the EC2 instance — the rate limiters bypass 127.0.0.1 so the
test exercises the server's capacity, not its anti-abuse gates.

Usage:  python3 loadtest.py <N> <duration_seconds>
        python3 loadtest.py 100 60
"""
import json
import socket
import ssl
import struct
import sys
import threading
import time

HOST = "localhost"
PORT = 9000
SNI  = "astrion.game"
TEST_PASSWORD = "loadtest_pw"
ZONE = "beacon_of_winds"
MOVE_INTERVAL_S = 1.0     # one move/sec per user — typical idle-walking client

PKT_LOGIN       = 0x01
PKT_MOVE        = 0x02
PKT_ZONE_ENTER  = 0x0B
PKT_LOGIN_RESULT= 0x81

NUM_USERS = int(sys.argv[1]) if len(sys.argv) > 1 else 30
DURATION  = int(sys.argv[2]) if len(sys.argv) > 2 else 60

stats = {
    "connect_ok": 0, "connect_fail": 0,
    "login_ok": 0,   "login_fail": 0, "register_ok": 0, "register_fail": 0,
    "move_sent": 0,  "move_err": 0,
    "dropped_mid_run": 0,
}
stats_lock = threading.Lock()
def bump(k, n=1):
    with stats_lock: stats[k] += n

def pack(type_byte, payload_dict):
    body = json.dumps(payload_dict).encode()
    return struct.pack(">I", 1 + len(body)) + bytes([type_byte]) + body

def read_packet(s, timeout=5.0):
    s.settimeout(timeout)
    hdr = b""
    while len(hdr) < 4:
        chunk = s.recv(4 - len(hdr))
        if not chunk: return None, None
        hdr += chunk
    ln = struct.unpack(">I", hdr)[0]
    body = b""
    while len(body) < ln:
        chunk = s.recv(ln - len(body))
        if not chunk: return None, None
        body += chunk
    return body[0], body[1:]

def open_tls():
    ctx = ssl.create_default_context()
    ctx.check_hostname = False
    ctx.verify_mode = ssl.CERT_NONE
    raw = socket.create_connection((HOST, PORT), timeout=10)
    return ctx.wrap_socket(raw, server_hostname=SNI)

def ensure_account(uname):
    """Register if missing. Returns True on success or 'already exists'."""
    try:
        s = open_tls()
    except Exception:
        return False
    try:
        s.sendall(pack(PKT_LOGIN, {"username": uname, "password": TEST_PASSWORD,
                                    "isRegister": True, "clientVersion": "0.1.0"}))
        typ, body = read_packet(s, timeout=5)
        if typ != PKT_LOGIN_RESULT:
            return False
        # success OR "Username already exists" — both fine
        return True
    finally:
        try: s.close()
        except Exception: pass

def user_worker(idx, deadline):
    uname = f"loadtest_{idx}"
    try:
        s = open_tls()
        bump("connect_ok")
    except Exception:
        bump("connect_fail")
        return

    try:
        # Login
        s.sendall(pack(PKT_LOGIN, {"username": uname, "password": TEST_PASSWORD,
                                    "isRegister": False, "clientVersion": "0.1.0"}))
        typ, body = read_packet(s, timeout=5)
        if not typ or json.loads(body).get("success") is not True:
            bump("login_fail")
            try: s.close()
            except Exception: pass
            return
        bump("login_ok")

        # Enter zone
        s.sendall(pack(PKT_ZONE_ENTER, {"zoneId": ZONE, "nickname": uname[:16]}))

        # Move loop until deadline
        x = (idx % 50) - 25  # spread starting positions
        while time.time() < deadline:
            try:
                s.sendall(pack(PKT_MOVE, {"x": float(x), "y": -2.0, "z": 0.0, "facing": 1}))
                bump("move_sent")
            except Exception:
                bump("move_err")
                bump("dropped_mid_run")
                return
            time.sleep(MOVE_INTERVAL_S)
    finally:
        try: s.close()
        except Exception: pass

def main():
    print(f"== Astrion load test: N={NUM_USERS}, duration={DURATION}s ==")
    print(f"Step 1/3: ensuring {NUM_USERS} test accounts exist...")
    # Sequential register — fast over loopback, no rate-limit concern.
    for i in range(NUM_USERS):
        if not ensure_account(f"loadtest_{i}"):
            print(f"  account loadtest_{i} setup FAILED")
    print(f"Step 2/3: starting {NUM_USERS} concurrent sessions")
    start = time.time()
    deadline = start + DURATION
    threads = []
    for i in range(NUM_USERS):
        t = threading.Thread(target=user_worker, args=(i, deadline), daemon=True)
        t.start()
        threads.append(t)
        time.sleep(0.01)   # 100/sec ramp — gentle on the boss group accept queue
    for t in threads: t.join(timeout=DURATION + 30)
    elapsed = time.time() - start

    print(f"Step 3/3: done in {elapsed:.1f}s")
    print("--- stats ---")
    for k, v in stats.items(): print(f"  {k:<20} {v}")
    moves_per_sec = stats["move_sent"] / elapsed if elapsed > 0 else 0
    print(f"  moves/sec (server-side ingress): {moves_per_sec:.1f}")
    print(f"  expected: ~{NUM_USERS / MOVE_INTERVAL_S:.0f}")

if __name__ == "__main__":
    main()
