#!/usr/bin/env bash
# Mac-local dev game-server. Listens on localhost:9000 (same port as prod,
# so the only thing that distinguishes a dev session from prod on the
# client side is the host — which the ASTRION_DEV build switches to
# 'localhost').
#
# Talks to a Mac-local Redis (no AUTH for dev convenience). TLS is off
# for the same reason — cert provisioning per developer is friction the
# dev loop doesn't need, and the connection is loopback-only.
#
# Sister scripts:
#   dev-server-stop.sh   — graceful kill
#   ProjectSetup → Build macOS (Dev → localhost)  — matching client
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PID_FILE="/tmp/astrion-dev-server.pid"
LOG_FILE="/tmp/astrion-dev-server.log"

cd "$PROJECT_ROOT"

# Bail if a previous dev server is still alive (would clash on :9000).
if [[ -f "$PID_FILE" ]]; then
    OLD_PID=$(cat "$PID_FILE")
    if kill -0 "$OLD_PID" 2>/dev/null; then
        echo "dev server already running (pid $OLD_PID). Stop it first:"
        echo "  $(dirname "$0")/dev-server-stop.sh"
        exit 1
    fi
    rm -f "$PID_FILE"
fi

# Mac-local Redis is a prerequisite. brew install redis && brew services
# start redis is the one-time setup.
if ! redis-cli ping >/dev/null 2>&1; then
    cat >&2 <<EOF
ERROR: Redis is not running on localhost.
One-time install:   brew install redis
Start as service:   brew services start redis
Or foreground:      redis-server
EOF
    exit 1
fi

echo "[dev] building game-server jar (incremental) ..."
./gradlew -q :game-server:installDist

# TLS off in dev — the matching client (ASTRION_DEV build) bypasses TLS
# in NetworkManager. ASTRION_REDIS_PASSWORD intentionally unset so the
# server connects to the dev Redis without AUTH.
export ASTRION_TLS_CERT=""
export ASTRION_TLS_KEY=""
unset ASTRION_REDIS_PASSWORD

echo "[dev] starting game-server on localhost:9000 (TLS off) ..."
nohup ./game-server/build/install/game-server/bin/game-server \
    > "$LOG_FILE" 2>&1 &
echo $! > "$PID_FILE"

sleep 2
if ! kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
    echo "dev server failed to start. Tail of $LOG_FILE:"
    tail -20 "$LOG_FILE"
    exit 1
fi

echo
echo "── dev server up ──"
echo "  pid:   $(cat "$PID_FILE")"
echo "  log:   $LOG_FILE       (tail -f to follow)"
echo "  ports: 9000 (game), 9001 (/metrics), 9002 (/health)"
echo "  client: build via Unity menu → Astrion/Build macOS (Dev → localhost)"
echo "  stop:  $(dirname "$0")/dev-server-stop.sh"
