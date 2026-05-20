#!/usr/bin/env bash
# Snapshot Redis to a rotating local backup dir.
#
# Why not just rely on /var/lib/redis/dump.rdb?
#   - dump.rdb is overwritten in place on every BGSAVE, so a corruption
#     (or a 'oops, deleted all the keys' moment) is unrecoverable once
#     the next snapshot lands. This script keeps a 14-day window of
#     historical points so you can roll back to a known-good state.
#   - dump.rdb sits on the same disk and same instance. This script
#     copies into ~/backups so an off-instance sync (S3, scp to laptop)
#     has a single dir to mirror — wire that up in cron when you want
#     proper disaster recovery.
#
# Restore (in plain English):
#   sudo systemctl stop redis-server
#   sudo cp <chosen>.rdb /var/lib/redis/dump.rdb
#   sudo chown redis:redis /var/lib/redis/dump.rdb
#   sudo systemctl start redis-server
set -euo pipefail

BACKUP_DIR="$HOME/backups/redis"
KEEP_DAYS=14

mkdir -p "$BACKUP_DIR"
TS=$(date -u +%Y%m%d-%H%M%S)
OUT="$BACKUP_DIR/dump-$TS.rdb"

# Ask Redis to fork off a fresh snapshot. We don't wait for completion;
# `redis-cli --rdb` below does its own SYNC and gets a fully consistent
# copy regardless. BGSAVE here primarily refreshes the on-disk dump.rdb
# for anyone (us included) doing post-mortem reads on the live file.
redis-cli BGSAVE >/dev/null || true

# --rdb streams the current RDB over the wire to a destination of our
# choice. No sudo or filesystem permission juggling on /var/lib/redis,
# and no risk of partial-file races because Redis controls the framing.
redis-cli --rdb "$OUT" >/dev/null

if [[ ! -s "$OUT" ]]; then
    echo "$(date -Is) backup-redis: empty/missing $OUT" >&2
    exit 1
fi

gzip --force "$OUT"

# Retention: keep the newest KEEP_DAYS days of point-in-time snapshots.
find "$BACKUP_DIR" -maxdepth 1 -name 'dump-*.rdb.gz' -mtime "+$KEEP_DAYS" -delete

echo "$(date -Is) backup-redis: $(stat -c %s "$OUT.gz") bytes -> $OUT.gz"
