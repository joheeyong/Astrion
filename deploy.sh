#!/usr/bin/env bash
# Prod deploy in one command. Replaces the four-line
# 'gradlew → scp → ssh tar → ssh systemctl restart' dance.
#
# Usage:
#   ./deploy.sh                  build + ship + restart + health check
#   ./deploy.sh --no-build       skip gradlew (use existing distributions tar)
#   ./deploy.sh --restart-only   skip everything, just restart the service
#   ./deploy.sh -h               this help
#
# Aborts cleanly at each step with a single-line cause. Health probe at
# the end retries 5×3s — if the service comes back stuck, the script
# tells you instead of pretending everything is fine.
set -euo pipefail

REMOTE_USER=ubuntu
REMOTE_HOST=3.38.109.138
SSH_KEY="${ASTRION_SSH_KEY:-$HOME/.ssh/astrion-key.pem}"
TAR="game-server/build/distributions/game-server-0.1.0.tar"
HEALTH_URL="http://${REMOTE_HOST}:9002/health"

# ── flags ──────────────────────────────────────────────────────────
NO_BUILD=0
RESTART_ONLY=0
for arg in "$@"; do
    case "$arg" in
        -h|--help)
            sed -n '2,12p' "$0" | sed 's/^# \{0,1\}//'
            exit 0
            ;;
        --no-build)    NO_BUILD=1 ;;
        --restart-only) RESTART_ONLY=1 ;;
        *) echo "unknown arg: $arg (try -h)" >&2; exit 2 ;;
    esac
done

# ── sanity ─────────────────────────────────────────────────────────
if [[ ! -r "$SSH_KEY" ]]; then
    echo "ERROR: SSH key not readable at $SSH_KEY" >&2
    echo "       set ASTRION_SSH_KEY=/path/to/key to override" >&2
    exit 1
fi
cd "$(dirname "$0")"

# ── 1. build ───────────────────────────────────────────────────────
if (( RESTART_ONLY == 1 )); then
    echo "[1/4] skip build (restart-only)"
elif (( NO_BUILD == 1 )); then
    echo "[1/4] skip build (--no-build)"
    [[ -f "$TAR" ]] || { echo "ERROR: $TAR missing — drop --no-build or run gradlew first" >&2; exit 1; }
else
    echo "[1/4] building game-server jar ..."
    if ! ./gradlew -q :game-server:clean :game-server:distTar; then
        echo "ERROR: gradle build failed" >&2
        exit 1
    fi
fi

# ── 2. ship ────────────────────────────────────────────────────────
if (( RESTART_ONLY == 1 )); then
    echo "[2/4] skip scp (restart-only)"
else
    echo "[2/4] uploading tar ..."
    scp -q -i "$SSH_KEY" -o StrictHostKeyChecking=no \
        "$TAR" "$REMOTE_USER@$REMOTE_HOST:~/game-server-0.1.0-new.tar"
fi

# ── 3. apply ───────────────────────────────────────────────────────
echo "[3/4] applying on EC2 ..."
if (( RESTART_ONLY == 1 )); then
    ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no \
        "$REMOTE_USER@$REMOTE_HOST" "sudo systemctl restart astrion-game-server"
else
    ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no \
        "$REMOTE_USER@$REMOTE_HOST" "
            set -e
            rm -rf ~/game-server-0.1.0
            tar -xf ~/game-server-0.1.0-new.tar -C ~/
            sudo systemctl restart astrion-game-server
        "
fi

# ── 4. verify ──────────────────────────────────────────────────────
echo "[4/4] health-checking $HEALTH_URL ..."
ok=0
for i in 1 2 3 4 5; do
    sleep 3
    if curl -fsS --max-time 3 "$HEALTH_URL" 2>/dev/null | grep -q '"status":"ok"'; then
        ok=1
        echo "  attempt $i: ok"
        break
    fi
    echo "  attempt $i: not ready yet"
done

if (( ok == 0 )); then
    echo "ERROR: service did not return healthy in 15s. Tail recent logs with:" >&2
    echo "  ssh -i $SSH_KEY $REMOTE_USER@$REMOTE_HOST 'sudo journalctl -u astrion-game-server -n 30'" >&2
    exit 1
fi

echo
echo "── deploy complete ──"
echo "  client should reconnect automatically (ReconnectSystem retries up to 8×2s)."
