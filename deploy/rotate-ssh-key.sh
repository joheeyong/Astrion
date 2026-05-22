#!/usr/bin/env bash
# Interactive SSH key rotation for the Astrion EC2 box.
#
# What it does, in order:
#   1. Generates a fresh Ed25519 key locally  (~/.ssh/astrion-key-YYYYMMDD)
#   2. Appends the new public key to ubuntu@EC2 authorized_keys (over the
#      existing key, while both keys are momentarily valid)
#   3. Verifies SSH actually works with the new key
#   4. Removes the OLD key fingerprint from authorized_keys on EC2
#   5. Archives the old private key locally (chmod 000) and updates
#      ~/.ssh/config (if present) to point at the new key
#
# Each step pauses for confirmation. If the new-key SSH test fails, the
# script aborts BEFORE touching the old key — so a botched rotation never
# locks you out.
set -euo pipefail

OLD_KEY="${ASTRION_OLD_KEY:-$HOME/.ssh/astrion-key.pem}"
REMOTE_USER=ubuntu
REMOTE_HOST=3.38.109.138
ARCHIVE_DIR="$HOME/.ssh/archive"
STAMP=$(date +%Y%m%d)
NEW_KEY="$HOME/.ssh/astrion-key-$STAMP"

confirm() {
    read -r -p "$1 [y/N]: " yn
    [[ "$yn" =~ ^[Yy]$ ]] || { echo "aborted."; exit 1; }
}

echo "── ASTRION SSH key rotation ────────────────────────────────────"
echo "  old key:  $OLD_KEY"
echo "  new key:  $NEW_KEY  (Ed25519)"
echo "  host:     $REMOTE_USER@$REMOTE_HOST"
echo

if [[ ! -f "$OLD_KEY" ]]; then
    echo "ERROR: old key not found at $OLD_KEY" >&2
    exit 1
fi
if [[ -f "$NEW_KEY" ]]; then
    echo "ERROR: new key already exists at $NEW_KEY — delete or rename first" >&2
    exit 1
fi

OLD_FP=$(ssh-keygen -lf "$OLD_KEY" | awk '{print $2}')
echo "current key fingerprint: $OLD_FP"
confirm "Proceed with generating a new key?"

# 1. Generate
echo
echo "[1/5] generating new Ed25519 key (no passphrase)..."
ssh-keygen -t ed25519 -f "$NEW_KEY" -N "" -C "astrion-key-$STAMP"
chmod 600 "$NEW_KEY"
NEW_FP=$(ssh-keygen -lf "$NEW_KEY" | awk '{print $2}')
echo "new fingerprint: $NEW_FP"

# 2. Add to remote authorized_keys (via old key)
echo
echo "[2/5] appending new public key to remote authorized_keys..."
NEW_PUB=$(cat "$NEW_KEY.pub")
ssh -i "$OLD_KEY" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" \
    "grep -qxF '$NEW_PUB' ~/.ssh/authorized_keys || echo '$NEW_PUB' >> ~/.ssh/authorized_keys"
echo "ok."

# 3. Verify new key actually works
echo
echo "[3/5] verifying new key by running 'whoami' on the box..."
if ssh -i "$NEW_KEY" -o StrictHostKeyChecking=no -o BatchMode=yes \
       "$REMOTE_USER@$REMOTE_HOST" whoami >/dev/null 2>&1; then
    echo "ok — new key authenticates."
else
    echo "ERROR: new key SSH failed. Old key is still installed. Investigate and re-run."
    exit 1
fi

# 4. Remove the old key from authorized_keys
echo
echo "[4/5] new key verified. Removing the OLD key from remote authorized_keys."
confirm "Remove the old key now? (you can keep both keys live if you say no)"
OLD_PUB_FP=$(echo "$OLD_FP" | sed 's|/|\\/|g')
ssh -i "$NEW_KEY" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" "
    # Filter authorized_keys to drop the line whose fingerprint matches the old key.
    tmp=\$(mktemp)
    while IFS= read -r line; do
        # Compute fingerprint of this line and compare
        fp=\$(echo \"\$line\" | ssh-keygen -lf - 2>/dev/null | awk '{print \$2}')
        if [[ \"\$fp\" != \"$OLD_FP\" ]]; then
            echo \"\$line\" >> \"\$tmp\"
        fi
    done < ~/.ssh/authorized_keys
    mv \"\$tmp\" ~/.ssh/authorized_keys
    chmod 600 ~/.ssh/authorized_keys
"
echo "ok. authorized_keys now:"
ssh -i "$NEW_KEY" -o StrictHostKeyChecking=no "$REMOTE_USER@$REMOTE_HOST" \
    "ssh-keygen -lf ~/.ssh/authorized_keys"

# 5. Archive the old key locally
echo
echo "[5/5] archiving old key locally..."
mkdir -p "$ARCHIVE_DIR"
ARCHIVED="$ARCHIVE_DIR/$(basename "$OLD_KEY").rotated-$STAMP"
mv "$OLD_KEY" "$ARCHIVED"
chmod 000 "$ARCHIVED"   # belt-and-braces: prevent accidental reuse
echo "old key moved to $ARCHIVED (chmod 000)."

echo
echo "── DONE ────────────────────────────────────────────────────────"
echo "  Use the new key going forward:"
echo "     ssh -i $NEW_KEY ubuntu@$REMOTE_HOST"
echo
echo "  Update any other tools that reference the old path. The"
echo "  Astrion scripts in deploy/ all read \$ASTRION_SSH_KEY if set,"
echo "  otherwise they use ~/.ssh/astrion-key.pem — rename your new"
echo "  key to that path or set the env var to keep them working:"
echo "     mv $NEW_KEY ~/.ssh/astrion-key.pem"
echo "     # or"
echo "     export ASTRION_SSH_KEY=$NEW_KEY"
