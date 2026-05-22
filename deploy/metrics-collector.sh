#!/usr/bin/env bash
# Snapshot /metrics once per cron tick and append a single JSONL line to
# ~/logs/metrics-history.jsonl. Pairs with deploy/astrion-dashboard.sh on
# the operator's Mac, which mirrors this file and renders a chart.
#
# A pure-shell, single-file approach keeps the operational surface tiny:
#   - no extra service to keep running
#   - no DB to back up
#   - logrotate / cron-based pruning handles disk
set -uo pipefail

OUT="$HOME/logs/metrics-history.jsonl"
mkdir -p "$(dirname "$OUT")"

# 3s timeout — /metrics is normally <10ms, this is just to keep cron from
# hanging if the server is wedged (which is itself useful signal).
JSON=$(curl -s --max-time 3 http://localhost:9001/metrics)
if [[ -z "$JSON" ]]; then
    # Don't write a corrupt entry. Record a tombstone so the chart shows
    # the gap rather than silently interpolating across it.
    echo "{\"timestamp\":$(date -u +%s),\"down\":true}" >> "$OUT"
    exit 0
fi

# Inject timestamp into the front of the existing JSON object. /metrics
# returns '{...}' so we strip the leading '{' and prepend ours.
TS=$(date -u +%s)
echo "{\"timestamp\":$TS,${JSON#\{}" >> "$OUT"

# Prune entries older than 30 days. JSONL is line-oriented; we keep the
# last N lines where N covers a 30-day window at 1-minute cadence
# (= 43200 lines). A bit of slack for double-collection retries:
MAX_LINES=45000
LINES=$(wc -l < "$OUT" 2>/dev/null || echo 0)
if (( LINES > MAX_LINES )); then
    TMP=$(mktemp)
    tail -n "$MAX_LINES" "$OUT" > "$TMP" && mv "$TMP" "$OUT"
fi
