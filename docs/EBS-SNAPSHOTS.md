# EBS Snapshot Automation

AWS-side disaster recovery for the EC2 instance's root volume. Snapshots
are stored in S3 by AWS, isolated from the EBS volume itself, and
survive instance termination — which the Mac-local rsync of Redis dumps
does not. This is the *real* off-site backup; the rsync is a convenience.

> **Why this isn't a code change**: AWS Data Lifecycle Manager (DLM) is
> the managed service that runs the schedule. It's configured once in
> the Console and then it just works — no script to maintain, no cron
> entry that can break, no IAM credential to rotate.

---

## Instance you're protecting

| Item | Value |
|---|---|
| Instance ID | `i-019ad171813210fae` |
| Region | `ap-northeast-2` (Seoul) |
| AZ | `ap-northeast-2d` |
| Root volume | 20 GB (gp3 / gp2), ~18% used |

The root volume is the only one attached. Snapshotting it captures
everything: game-server jar, `~/logs/`, `~/backups/`, `~/.config/astrion/`,
the system itself, and `/var/lib/redis/dump.rdb` + the AOF.

---

## One-time setup (AWS Console, ~5 min)

### 1. Tag the EBS volume

DLM targets resources by tag, not by ID. We give the volume a tag so
the policy has something to match against.

1. AWS Console → EC2 → **Volumes** (left nav)
2. Filter by attached instance `i-019ad171813210fae`
3. Select the root volume → **Tags** tab → **Manage tags**
4. Add:
   - Key: `Backup`
   - Value: `daily`
5. Save

(Direct link, after login)
```
https://ap-northeast-2.console.aws.amazon.com/ec2/home?region=ap-northeast-2#Volumes:instanceId=i-019ad171813210fae
```

### 2. Make sure the DLM role exists

DLM needs an IAM role to do its work. AWS provides a default one.

1. AWS Console → IAM → **Roles**
2. Search for `AWSDataLifecycleManagerDefaultRole`
3. If it exists → done. If not, the DLM policy creation wizard (next
   step) offers to create it with one click — accept that prompt.

### 3. Create the lifecycle policy

1. AWS Console → EC2 → **Lifecycle Manager** (left nav, under
   *Elastic Block Store*)
2. **Create lifecycle policy** → **EBS snapshot policy** → Next
3. Fill in:

   | Field | Value |
   |---|---|
   | Description | `Astrion root volume daily snapshots` |
   | Target resource type | `Volume` |
   | Target resource tags | `Backup` : `daily` |
   | IAM role | `Default` (uses AWSDataLifecycleManagerDefaultRole) |
   | Policy status | `Enabled` |

4. Add schedule:

   | Field | Value |
   |---|---|
   | Schedule name | `daily-2am` |
   | Frequency | `Daily` |
   | Every | `24 hours` starting at `02:00 UTC` (= 11:00 KST) |
   | Retention | `7` snapshots (rolling) |
   | Snapshot tags | Key=`Source` Value=`DLM-astrion-daily` (optional, makes them easy to filter in the console) |
   | Copy tags from source | ✓ on (carries the `Backup=daily` tag forward) |

5. (Optional, cheap insurance) Add a second schedule:

   | Field | Value |
   |---|---|
   | Schedule name | `weekly-sunday` |
   | Frequency | `Weekly` on Sunday 02:00 UTC |
   | Retention | `4` snapshots (≈ 1 month) |

6. **Review and create**.

---

## Verification

Console:

```
https://ap-northeast-2.console.aws.amazon.com/ec2/home?region=ap-northeast-2#Snapshots:visibility=owned-by-me
```

After the first scheduled run you'll see a snapshot with description
`Created for policy: …` and the `Source=DLM-astrion-daily` tag.

To force one immediately and confirm the wiring works (no need to wait
for 02:00 UTC):

1. Volumes → select your volume → **Actions** → **Create snapshot**
2. Add tag `Source=manual-test`
3. Description "manual smoke test"

The DLM-managed snapshots are independent of this manual one.

---

## Cost

EBS snapshots are billed per GB-month of *changed data*, not the full
volume each time. After the initial full snapshot (~3.4 GB of real
content out of the 20 GB volume), subsequent daily snapshots only
record the deltas (typically tens of MB given our log/backup rotation
caps).

A realistic monthly bill for this policy:

| Item | Cost |
|---|---|
| First snapshot (full) | ~3.4 GB × $0.05 = **$0.17** |
| 6 daily incrementals (delta ~50 MB) | 0.3 GB × $0.05 = **$0.015** |
| 4 weekly incrementals | similar, **$0.01** |
| Total | **≈ $0.20 / month** |

Free tier doesn't cover EBS snapshots beyond the first GB, so this is
real money — but it's $2.40/year for proper disaster recovery.

---

## Restore procedure

If the live instance is unrecoverable (terminated, root volume
corrupted, snapshotted state needed for forensics):

1. EC2 Console → **Snapshots** → pick the snapshot
2. **Actions** → **Create volume from snapshot**
   - Choose AZ `ap-northeast-2d` (must match the new instance)
3. Launch a fresh EC2 instance (same instance type, Ubuntu 24.04)
4. After it boots, **Stop** it
5. Detach the new instance's root volume, attach the restored volume
   as `/dev/sda1`
6. Start the instance
7. SSH in, update the public IP / DNS in `astrion-game-server.service`
   if it changed, restart the service

Alternative (faster, more wasteful): **Restore volume to a new
instance** wizard in newer AWS Console UI does steps 3-6 in one go.

---

## Cross-link

This complements the local backup chain in OPERATIONS.md §5
(Redis-level AOF + RDB + cron + mac rsync). Each layer answers a
different failure mode:

| Failure | What saves you |
|---|---|
| Redis BGSAVE wrote corrupt RDB | AOF replay or earlier rsync'd dump |
| Disk filled, `dump.rdb` lost | rsync'd dump on Mac (last 90 days) |
| EBS volume corruption | this — EBS snapshot |
| Instance terminated by mistake | this |
| AWS region outage | cross-region snapshot copy (not configured; flag if/when this becomes a real concern) |

For most operational mishaps, the Mac rsync chain recovers first.
EBS snapshot is the slow-but-bulletproof path when everything closer
is also damaged.
