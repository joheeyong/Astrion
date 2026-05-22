#!/usr/bin/env bash
# Astrion backup verification. Run monthly on the Mac to make sure the
# rsync'd Redis dumps in ~/Astrion-Backups/redis/ aren't silently empty
# or corrupted. Catches the failure mode where the live Redis got wiped
# (this happened once already — see git history) but the backup cron
# kept faithfully copying the empty dump.
#
# Pipeline:
#   1. Pick newest dump-*.rdb.gz from the local mirror
#   2. gunzip into a tempdir
#   3. redis-check-rdb integrity scan
#   4. Boot a temp redis-server on port 26379 isolated to the tempdir,
#      loading the dump
#   5. Count the keys that matter (account / player:state) — warn if zero
#   6. Tear down the temp instance, log the result
#   7. If WEBHOOK is configured, push pass/fail to Discord
#
# Designed to be cron-safe (set -e, traps clean up, no interactive prompts).
set -euo pipefail

BACKUP_DIR="$HOME/Astrion-Backups/redis"
LOG_FILE="$HOME/Astrion-Backups/verify.log"
WEBHOOK_FILE="$HOME/.config/astrion/webhook.url"
TEST_PORT=26379
NOW=$(date -Iseconds)
TMP_DIR=$(mktemp -d -t astrion-verify.XXXXXX)
TEST_PID=""

cleanup() {
    if [[ -n "$TEST_PID" ]] && kill -0 "$TEST_PID" 2>/dev/null; then
        kill -TERM "$TEST_PID" 2>/dev/null || true
        sleep 1
        kill -KILL "$TEST_PID" 2>/dev/null || true
    fi
    rm -rf "$TMP_DIR"
}
trap cleanup EXIT

mkdir -p "$(dirname "$LOG_FILE")"
log() { echo "[$NOW] $*" | tee -a "$LOG_FILE"; }

push_webhook() {
    local emoji="$1"; local msg="$2"
    local url=""
    if [[ -r "$WEBHOOK_FILE" ]]; then
        url=$(grep -m1 -E '^https://discord(app)?\.com/api/webhooks/' "$WEBHOOK_FILE" 2>/dev/null | tr -d '[:space:]')
    fi
    [[ -z "$url" ]] && return  # silently no-op when webhook not set
    local payload
    payload=$(python3 -c "
import json, sys
print(json.dumps({'content': sys.argv[1] + ' **[backup-verify]** ' + sys.argv[2]}))
" "$emoji" "$msg")
    curl -s -X POST -H 'Content-Type: application/json' -d "$payload" "$url" >/dev/null || true
}

# Pre-flight: required commands present?
for cmd in redis-server redis-cli redis-check-rdb gunzip python3; do
    if ! command -v "$cmd" >/dev/null 2>&1; then
        log "FAIL — required command missing: $cmd"
        push_webhook "🚨" "missing command on host: $cmd"
        exit 1
    fi
done

# 1. Latest backup
LATEST=$(/bin/ls -t "$BACKUP_DIR"/dump-*.rdb.gz 2>/dev/null | head -1)
if [[ -z "$LATEST" ]]; then
    log "FAIL — no backup files in $BACKUP_DIR"
    push_webhook "🚨" "no backup files found in $BACKUP_DIR"
    exit 1
fi
FILE_SIZE=$(stat -f %z "$LATEST" 2>/dev/null || stat -c %s "$LATEST")
FILE_AGE_DAYS=$(( ($(date +%s) - $(stat -f %m "$LATEST" 2>/dev/null || stat -c %Y "$LATEST")) / 86400 ))
log "candidate: $(basename "$LATEST") ($FILE_SIZE bytes, $FILE_AGE_DAYS days old)"

if (( FILE_AGE_DAYS > 2 )); then
    log "WARN — newest backup is $FILE_AGE_DAYS days old; sync may be broken"
    push_webhook "⚠️" "newest backup is $FILE_AGE_DAYS days old — check the rsync cron"
fi

# 2. Decompress
DUMP_FILE="$TMP_DIR/dump.rdb"
if ! gunzip -c "$LATEST" > "$DUMP_FILE"; then
    log "FAIL — gunzip rejected $LATEST"
    push_webhook "🚨" "gunzip failed for $(basename "$LATEST")"
    exit 1
fi

# 3. redis-check-rdb
if ! redis-check-rdb "$DUMP_FILE" >/dev/null 2>&1; then
    log "FAIL — redis-check-rdb rejected the dump (corruption?)"
    push_webhook "🚨" "redis-check-rdb rejected $(basename "$LATEST") — corruption"
    exit 1
fi
log "redis-check-rdb: OK"

# 4. Boot temp redis on isolated port + dir
cd "$TMP_DIR"
redis-server --port "$TEST_PORT" --dir "$TMP_DIR" --dbfilename dump.rdb \
             --logfile /dev/null --save "" --appendonly no \
             --bind 127.0.0.1 --protected-mode yes \
             --daemonize no >/dev/null 2>&1 &
TEST_PID=$!
sleep 2

if ! redis-cli -p "$TEST_PORT" ping >/dev/null 2>&1; then
    log "FAIL — temp redis didn't come up on port $TEST_PORT"
    push_webhook "🚨" "temp redis failed to start during verify"
    exit 1
fi

# 5. Inspect keys. Filters out the fails:/locked:/cheats: noise from the
# rate-limit + anti-cheat layers so 'account:*' is the real-account count.
# awk filter (not grep -v) so 'no match' exits 0 — under set -e an
# empty backup would otherwise abort the script here before the WARN
# branch below could fire.
TOTAL=$(redis-cli -p "$TEST_PORT" dbsize | awk '{print $1}')
ACCOUNTS=$(redis-cli -p "$TEST_PORT" --scan --pattern 'account:*' 2>/dev/null \
           | awk '!/^account:(fails|locked|cheats):/' \
           | wc -l | tr -d ' ')
PLAYER_STATE=$(redis-cli -p "$TEST_PORT" --scan --pattern 'player:state:*' 2>/dev/null \
               | wc -l | tr -d ' ')

log "keys: total=$TOTAL real_accounts=$ACCOUNTS player_states=$PLAYER_STATE"

if (( TOTAL == 0 )); then
    log "WARN — backup contains zero keys. Live Redis might have been empty (e.g. the May 20 wipe) or sync is broken."
    push_webhook "⚠️" "backup $(basename "$LATEST") is empty (0 keys) — verify live Redis"
    # Treat empty backup as soft-fail. Operator decides whether to escalate.
    exit 0
fi

log "PASS"
push_webhook "✅" "monthly verify ok — $(basename "$LATEST"): $TOTAL keys, $ACCOUNTS accounts, $PLAYER_STATE player states"
