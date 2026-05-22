# SSH Key Rotation Runbook

Procedure for swapping the SSH key used to access the EC2 game-server
box. Two paths: the automated one (recommended) and the manual one (for
when something has already gone wrong and you can't run the script).

> **Why rotate**: scheduled hygiene (annually), suspected leak, laptop
> theft, contractor offboarding, or moving from RSA to Ed25519.

---

## Path A — automated (downtime 0)

Runs locally on the operator's Mac. Walks through new-key generation,
adds the new key to the remote `authorized_keys`, verifies it works,
then removes the old key. **Aborts before touching the old key if the
new one fails verification**, so a botched rotation can't lock you out.

```bash
# From any directory on the Mac:
~/projects/Astrion/deploy/rotate-ssh-key.sh
```

By default it uses `~/.ssh/astrion-key.pem` as the source-of-truth old
key. Override with `ASTRION_OLD_KEY=/path/to/key`.

The script:
1. Generates `~/.ssh/astrion-key-YYYYMMDD` (Ed25519, no passphrase)
2. Appends its public part to `ubuntu@EC2:~/.ssh/authorized_keys`
3. Confirms SSH works with the new key (`whoami` round-trip)
4. Removes the old key's line from `authorized_keys` (matched by fingerprint)
5. Moves the old `.pem` to `~/.ssh/archive/` with `chmod 000`

After it finishes you'll typically want either:

```bash
# Rename so the deploy/ scripts find it at the default path:
mv ~/.ssh/astrion-key-YYYYMMDD ~/.ssh/astrion-key.pem

# Or point the env var at the new file:
echo 'export ASTRION_SSH_KEY=~/.ssh/astrion-key-YYYYMMDD' >> ~/.zshrc
```

---

## Path B — manual (use when the script can't run)

Same five steps, by hand. Useful when the laptop with the old key is
gone (use a freshly authorized teammate's key or AWS Session Manager
to get in), or when you want to add a new operator alongside the
existing one without removing anything.

### 0. Pre-flight

```bash
# What fingerprint are we replacing?
ssh-keygen -lf ~/.ssh/astrion-key.pem

# What's on the box right now?
ssh -i ~/.ssh/astrion-key.pem ubuntu@3.38.109.138 \
    'ssh-keygen -lf ~/.ssh/authorized_keys'
```

### 1. Generate a new key locally

```bash
ssh-keygen -t ed25519 -f ~/.ssh/astrion-key-new -N "" -C "astrion-$(date +%Y%m%d)"
chmod 600 ~/.ssh/astrion-key-new
```

### 2. Add new public key to the EC2 box

```bash
PUB=$(cat ~/.ssh/astrion-key-new.pub)
ssh -i ~/.ssh/astrion-key.pem ubuntu@3.38.109.138 \
    "echo '$PUB' >> ~/.ssh/authorized_keys"
```

### 3. Verify new key works *before* touching the old one

```bash
ssh -i ~/.ssh/astrion-key-new ubuntu@3.38.109.138 'whoami'
# expect: ubuntu
```

If this fails, stop. The old key is still installed — diagnose the
new key before continuing.

### 4. Remove the old key from `authorized_keys`

The simplest correct way is to log in with the new key and rewrite
the file with only the line you want kept:

```bash
ssh -i ~/.ssh/astrion-key-new ubuntu@3.38.109.138 "
    # Keep only the line that matches the NEW key fingerprint
    NEW_FP='$(ssh-keygen -lf ~/.ssh/astrion-key-new.pub | awk '{print \$2}')'
    awk -v fp=\"\$NEW_FP\" '
        {
            cmd = \"echo \" \$0 \" | ssh-keygen -lf - 2>/dev/null | awk \\047{print \$2}\\047\"
            cmd | getline this_fp
            close(cmd)
            if (this_fp == fp) print
        }
    ' ~/.ssh/authorized_keys > /tmp/ak.new
    mv /tmp/ak.new ~/.ssh/authorized_keys
    chmod 600 ~/.ssh/authorized_keys
"
```

If you suspect the old key is compromised but want to be conservative,
**add the new one first, then come back and remove the old later**
(after a day of confirmed normal access on the new key).

### 5. Archive the old key

```bash
mkdir -p ~/.ssh/archive
mv ~/.ssh/astrion-key.pem ~/.ssh/archive/astrion-key.pem.rotated-$(date +%Y%m%d)
chmod 000 ~/.ssh/archive/astrion-key.pem.rotated-$(date +%Y%m%d)
```

The `chmod 000` is a small but useful belt-and-braces — even if you
later accidentally `ssh -i path/to/archived/key`, OpenSSH will refuse
to read it instead of silently using a key you intended to retire.

---

## Emergency: lost laptop (no usable key at all)

You have three escape hatches, in order of preference:

1. **AWS Systems Manager Session Manager** — if the EC2 has the SSM
   agent installed (it usually is by default on Ubuntu AMIs), open
   the instance in AWS Console → Connect → Session Manager. No SSH
   needed. From the shell that opens:
   ```bash
   cat new_pub_key >> ~/.ssh/authorized_keys
   ```

2. **EC2 user-data swap** (requires stop/start, ~30s downtime). AWS
   Console → Instance → Stop → Actions → Edit user data. Paste a
   cloud-init that injects a new pub key. Start. SSH back in.

3. **Detach the EBS volume** and mount on a temporary instance you
   control, edit `/home/ubuntu/.ssh/authorized_keys` directly, reattach.
   Last resort — works but takes longer.

If neither (1) nor (2) is available, the recovery procedure is more
involved; design it once you have working access again rather than
under emergency.

---

## What else to rotate alongside

The SSH key isn't the only credential on the box. When rotating because
of a suspected breach, also consider:

| Credential | Where | How to rotate |
|---|---|---|
| TLS server cert | `~/game-server-cert/` | regenerate openssl x509 + update client fingerprint pin + rebuild client (see OPERATIONS.md §3) |
| Redis password | `/etc/redis/redis.conf` + `~/.config/astrion/redis-password.env` | regenerate hex + restart redis + restart game-server (see OPERATIONS.md §3) |
| Discord webhook URL | `~/.config/astrion/webhook.url` | regenerate in Discord channel settings, overwrite the file |

Rotating the SSH key alone is enough for "lost laptop" / "scheduled
hygiene" cases. Rotate the full set when you suspect the box itself
was accessed.
