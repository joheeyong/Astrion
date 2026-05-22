#!/usr/bin/env bash
# Watches ~/logs/errors.log and forwards every new ERROR-level line to a
# Discord webhook. Lets the operator find out about real server bugs
# without having to tail logs by hand.
#
# Webhook URL lives in ~/.config/astrion/webhook.url (one line, no quotes).
# Falls back to the DISCORD_WEBHOOK_URL env var if the file isn't there.
# If neither is set the script exits 0 — no spam, no error.
#
# Deduplicates: a line whose md5 was seen in the last DEDUPE_SECONDS is
# silently dropped, so an exception storm doesn't fire 1000 Discord
# messages.
#
# Truncates: Discord caps message content at 2000 chars. We trim to 1800
# to leave room for our prefix and code-block markup.
set -uo pipefail

WEBHOOK_FILE="$HOME/.config/astrion/webhook.url"
LOG_FILE="$HOME/logs/errors.log"
DEDUPE_SECONDS=60
HOST_TAG="$(hostname)"

read_webhook_url() {
    local url=""
    if [[ -r "$WEBHOOK_FILE" ]]; then
        # First line that looks like an actual https URL. Skips '#' comments and
        # blank lines so the placeholder template doesn't get misread as a URL.
        url=$(grep -m1 -E '^https://discord(app)?\.com/api/webhooks/' "$WEBHOOK_FILE" | tr -d '[:space:]')
    fi
    if [[ -z "$url" ]]; then
        url="${DISCORD_WEBHOOK_URL:-}"
    fi
    printf '%s' "$url"
}

# Idle-poll for the webhook URL on startup. The systemd unit is enabled at
# image setup, but the operator usually fills the URL in later. Sitting in
# a 5-minute wait beats the alternatives:
#   - exit 0 → no journal noise but Restart=always isn't useful
#   - exit 1 → systemd restart loop, fills journal with failures
#   - fail with RestartSec=10 → same as above, noisier
WEBHOOK_URL=$(read_webhook_url)
while [[ -z "$WEBHOOK_URL" ]]; do
    echo "[errors-watcher] waiting for webhook URL in $WEBHOOK_FILE ..." >&2
    sleep 300
    WEBHOOK_URL=$(read_webhook_url)
done
echo "[errors-watcher] webhook URL detected — starting tail on $LOG_FILE" >&2

declare -A last_pushed

push_to_discord() {
    local raw_line="$1"
    # md5 of the message text → dedupe key. A repeated NRE in Update fires
    # 60 times/sec; we want exactly one Discord message until the storm
    # quiets down.
    local key
    key=$(printf '%s' "$raw_line" | md5sum | cut -d' ' -f1)
    local now
    now=$(date +%s)
    if [[ -n "${last_pushed[$key]:-}" ]]; then
        local since=$(( now - last_pushed[$key] ))
        if (( since < DEDUPE_SECONDS )); then return; fi
    fi
    last_pushed[$key]=$now

    # Truncate after escaping the JSON-special chars. We use python for the
    # JSON encoding because bash quoting + Discord backticks is a footgun.
    local trimmed="${raw_line:0:1800}"
    local payload
    payload=$(python3 -c "
import json, sys
content = '**[' + sys.argv[1] + ']** errors.log\n\`\`\`\n' + sys.argv[2] + '\n\`\`\`'
print(json.dumps({'content': content}))
" "$HOST_TAG" "$trimmed")

    curl -s -X POST -H 'Content-Type: application/json' \
        -d "$payload" "$WEBHOOK_URL" >/dev/null || true
}

# Wait for the file to exist on first boot (logback creates it on demand).
while [[ ! -f "$LOG_FILE" ]]; do sleep 5; done

# tail -F survives logrotate (file replaced under our nose).
# -n 0 means we don't replay the entire log on startup — only new entries.
tail -F -n 0 "$LOG_FILE" 2>/dev/null | while IFS= read -r line; do
    # logback ERROR pattern: 'yyyy-MM-dd HH:mm:ss [thread] ERROR logger - msg'
    # Match the literal ' ERROR ' to ignore lines that just mention the word.
    if [[ "$line" == *" ERROR "* ]]; then
        push_to_discord "$line"
    fi
done
