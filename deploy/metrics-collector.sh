#!/usr/bin/env bash
# Snapshot /metrics + system metrics once per cron tick, append a single
# JSONL line to ~/logs/metrics-history.jsonl, and push Discord alerts on
# threshold breach. Replaces a separate CloudWatch alarm setup — same
# information, no AWS-side configuration, and we already have the Discord
# webhook channel up (see errors-watcher).
#
# Pairs with deploy/astrion-dashboard.sh on the operator's Mac for the
# chart view of the same JSONL.
set -uo pipefail

OUT="$HOME/logs/metrics-history.jsonl"
WEBHOOK_FILE="$HOME/.config/astrion/webhook.url"
ALERT_DIR="$HOME/.config/astrion/alerts"
ALERT_COOLDOWN_S=3600    # don't re-fire the same alert within 1 hour

mkdir -p "$(dirname "$OUT")" "$ALERT_DIR"

# ── thresholds ─────────────────────────────────────────────────────────
HEAP_PCT_ALERT=85          # JVM heap usage %
SYS_MEM_FREE_MB_ALERT=200  # available system memory MB
DISK_FREE_MB_ALERT=2048    # free disk on / in MB

HOST_TAG="$(hostname)"

push_discord_alert() {
    local key="$1"
    local msg="$2"

    local url=""
    if [[ -r "$WEBHOOK_FILE" ]]; then
        url=$(grep -m1 -E '^https://discord(app)?\.com/api/webhooks/' "$WEBHOOK_FILE" | tr -d '[:space:]')
    fi
    [[ -z "$url" ]] && return  # no webhook = no push, silently

    # Cooldown per-alert key so a sustained breach doesn't fire every minute.
    local stamp="$ALERT_DIR/$key.ts"
    local now
    now=$(date +%s)
    if [[ -f "$stamp" ]]; then
        local last
        last=$(cat "$stamp" 2>/dev/null || echo 0)
        if (( now - last < ALERT_COOLDOWN_S )); then return; fi
    fi
    echo "$now" > "$stamp"

    local payload
    payload=$(python3 -c "
import json, sys
content = '🚨 **[' + sys.argv[1] + ']** ' + sys.argv[2]
print(json.dumps({'content': content}))
" "$HOST_TAG" "$msg")
    curl -s -X POST -H 'Content-Type: application/json' \
        -d "$payload" "$url" >/dev/null || true
}

# ── collect game-server metrics (3s timeout in case the JVM is wedged) ─
JSON=$(curl -s --max-time 3 http://localhost:9001/metrics)
TS=$(date -u +%s)

# ── system metrics (free + df, always available) ───────────────────────
SYS_FREE_MB=$(free -m | awk '/^Mem:/ {print $7}')
DISK_FREE_MB=$(df -m / | awk 'NR==2 {print $4}')

if [[ -z "$JSON" ]]; then
    # Server didn't answer. Write a tombstone, fire the down alert.
    echo "{\"timestamp\":$TS,\"down\":true,\"sys_free_mb\":$SYS_FREE_MB,\"sys_disk_free_mb\":$DISK_FREE_MB}" >> "$OUT"
    push_discord_alert "server_down" "Game server unreachable — /metrics timeout"
else
    # Healthy. Prepend timestamp + system metrics into the existing object.
    echo "{\"timestamp\":$TS,\"sys_free_mb\":$SYS_FREE_MB,\"sys_disk_free_mb\":$DISK_FREE_MB,${JSON#\{}" >> "$OUT"

    # Parse heap % for alerting.
    HEAP_USED=$(echo "$JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("heap_used_mb", 0))' 2>/dev/null)
    HEAP_MAX=$(echo "$JSON" | python3 -c 'import json,sys; print(json.load(sys.stdin).get("heap_max_mb", 1))' 2>/dev/null)
    HEAP_USED=${HEAP_USED:-0}
    HEAP_MAX=${HEAP_MAX:-1}
    if (( HEAP_MAX > 0 )); then
        HEAP_PCT=$(( HEAP_USED * 100 / HEAP_MAX ))
        if (( HEAP_PCT > HEAP_PCT_ALERT )); then
            push_discord_alert "heap_high" \
                "JVM heap at ${HEAP_PCT}% (${HEAP_USED} / ${HEAP_MAX} MB) — approaching OOM"
        fi
    fi
fi

# System-level checks fire regardless of whether the game server responded.
if [[ -n "$SYS_FREE_MB" ]] && (( SYS_FREE_MB < SYS_MEM_FREE_MB_ALERT )); then
    push_discord_alert "sysmem_low" \
        "System memory low — only ${SYS_FREE_MB} MB available (threshold ${SYS_MEM_FREE_MB_ALERT})"
fi
if [[ -n "$DISK_FREE_MB" ]] && (( DISK_FREE_MB < DISK_FREE_MB_ALERT )); then
    push_discord_alert "disk_low" \
        "Disk filling — only ${DISK_FREE_MB} MB free on / (threshold ${DISK_FREE_MB_ALERT})"
fi

# Prune JSONL — keep ~31 days at 1/min cadence, plus a bit of slack.
MAX_LINES=45000
LINES=$(wc -l < "$OUT" 2>/dev/null || echo 0)
if (( LINES > MAX_LINES )); then
    TMP=$(mktemp)
    tail -n "$MAX_LINES" "$OUT" > "$TMP" && mv "$TMP" "$OUT"
fi
