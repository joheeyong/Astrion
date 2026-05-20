#!/usr/bin/env bash
# Pull Redis backups from the Astrion EC2 instance into a local off-EBS copy.
#
# Why this exists:
#   EC2's ~/backups/redis is on the same EBS volume as the running Redis.
#   An EBS corruption, accidental volume detach, or terminate-on-shutdown
#   takes the backups with it. Mirroring to this Mac is the cheapest
#   off-site option — no AWS surface to configure, no IAM, no S3 cost.
#
# Limitation: if the Mac is asleep or off at run time, that day's pull
# is skipped. Next successful run still catches everything (rsync sees
# files on the remote it doesn't have locally and grabs them).
#
# Restore: pick a dump-*.rdb.gz, scp it back to ec2, follow the procedure
# in deploy/backup-redis.sh's header.
set -euo pipefail

REMOTE_USER=ubuntu
REMOTE_HOST=3.38.109.138
# Absolute path — rsync expands a leading ~ on the LOCAL side, which would
# resolve to /Users/.../backups/redis here. We want the remote home.
REMOTE_DIR=/home/ubuntu/backups/redis/
LOCAL_DIR="$HOME/Astrion-Backups/redis"
KEEP_DAYS=90
SSH_KEY="$HOME/.ssh/astrion-key.pem"

mkdir -p "$LOCAL_DIR"

# rsync delta-syncs cheaply; only new dump-*.rdb.gz files cross the wire.
# Existing files are skipped with no transfer cost.
rsync -a --partial \
    -e "ssh -i $SSH_KEY -o StrictHostKeyChecking=no" \
    "$REMOTE_USER@$REMOTE_HOST:$REMOTE_DIR" "$LOCAL_DIR/"

# Local retention: 90 days. EC2 itself keeps 14 days; the local copy
# extends history so the 'restore to last Tuesday' window is wider.
find "$LOCAL_DIR" -maxdepth 1 -name 'dump-*.rdb.gz' -mtime "+$KEEP_DAYS" -delete

COUNT=$(find "$LOCAL_DIR" -maxdepth 1 -name 'dump-*.rdb.gz' | wc -l | tr -d ' ')
echo "$(date -Iseconds) astrion-backup-sync: $COUNT snapshots in $LOCAL_DIR"
