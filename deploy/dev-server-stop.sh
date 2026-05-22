#!/usr/bin/env bash
# Graceful stop for the mac-local dev game-server started by
# dev-server-start.sh.
set -uo pipefail

PID_FILE="/tmp/astrion-dev-server.pid"

if [[ ! -f "$PID_FILE" ]]; then
    echo "no dev server pid file at $PID_FILE — nothing to stop"
    exit 0
fi

PID=$(cat "$PID_FILE")
if ! kill -0 "$PID" 2>/dev/null; then
    echo "dev server pid $PID is already gone"
    rm -f "$PID_FILE"
    exit 0
fi

echo "stopping dev server (pid $PID, SIGTERM) ..."
kill -TERM "$PID" 2>/dev/null

# The graceful-shutdown hook in GameServerMain takes ~3s in practice.
# Give it 15s before escalating to SIGKILL.
for i in {1..15}; do
    if ! kill -0 "$PID" 2>/dev/null; then
        break
    fi
    sleep 1
done

if kill -0 "$PID" 2>/dev/null; then
    echo "graceful shutdown timed out — sending SIGKILL"
    kill -KILL "$PID" 2>/dev/null
fi

rm -f "$PID_FILE"
echo "dev server stopped."
