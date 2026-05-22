# Dev / Prod environments

Two environments, distinguished by where the game-server runs and by a
single build-time switch in the client.

| | Dev | Prod |
|---|---|---|
| Game server | mac local (`localhost:9000`) | EC2 `3.38.109.138:9000` |
| Redis | mac local (no AUTH) | EC2 (`requirepass`) |
| TLS | **off** (loopback only) | on + client fingerprint pinning |
| Client app | `Astrion-Dev.app` | `Astrion.app` |
| Switch | `ASTRION_DEV` scripting define | (default) |

The prod env is what's been documented elsewhere (`OPERATIONS.md`).
This file is the dev env.

---

## One-time setup

```bash
# 1. Redis on the mac (dev backing store)
brew install redis
brew services start redis        # auto-start, or just `redis-server`

# 2. Sanity check
redis-cli ping                   # → PONG
```

That's it. The dev game-server is launched by a script, not a service.

---

## Day-to-day

### Start the dev server

```bash
~/projects/Astrion/deploy/dev-server-start.sh
```

What it does:
- builds the game-server jar (`./gradlew :game-server:installDist`)
- checks Redis is alive, errors if not
- launches `bin/game-server` in the background with TLS *off*
  (`ASTRION_TLS_CERT=""`, `ASTRION_TLS_KEY=""`) and Redis AUTH *off*
  (`ASTRION_REDIS_PASSWORD` unset)
- writes pid → `/tmp/astrion-dev-server.pid`, log → `/tmp/astrion-dev-server.log`

Tail the log:
```bash
tail -f /tmp/astrion-dev-server.log
```

### Stop the dev server

```bash
~/projects/Astrion/deploy/dev-server-stop.sh
```

SIGTERM, waits 15s for graceful shutdown, escalates to SIGKILL if still
alive.

### Build the matching dev client

Unity Editor → **Astrion → Build macOS (Dev → localhost)**

The dev menu sets the `ASTRION_DEV` scripting define before the build:

- `NetworkConfig.DefaultHost` becomes `localhost`
- `NetworkManager.ConnectWithRetry` skips the `SslStream` wrap — talks
  plain TCP to the local server
- output path is `Builds/macOS-dev/Astrion-Dev.app` (separate from prod)
- bundle id is `com.astrion.game.dev`, product name `Astrion-Dev` so
  the dev app shows up distinct in the dock and in `~/Library/Logs/`

The editor define is reset after the build so subsequent prod builds
aren't accidentally dev.

---

## What's intentionally different in dev

| Concern | Dev | Why |
|---|---|---|
| TLS | off | cert provisioning per developer is friction; loopback only |
| Redis AUTH | off | a fresh `brew install redis` has none; matches expectation |
| Rate limit (IP / username / lockout) | active | still useful — tests the same paths prod uses |
| Backup cron | not running | dev data is throwaway |
| Discord error webhook | not configured | noise; dev cycles fire ERRORs all the time |
| Account lockout (Redis) | active | dev wipes via `redis-cli flushdb` if it gets in the way |

---

## Resetting dev state

```bash
# Wipe local Redis (drops all accounts, characters, lockouts)
redis-cli flushdb

# Stop + rebuild game-server, fresh start
~/projects/Astrion/deploy/dev-server-stop.sh
~/projects/Astrion/deploy/dev-server-start.sh
```

---

## Don't mix the two

The dev app is fingerprint-pin-blind (TLS off) and talks plain to
whichever host its NetworkConfig points at. If you launched the dev
binary and somehow pointed `ASTRION_SERVER_HOST` at the prod address,
it would refuse — prod requires TLS and would reject the plaintext
TCP attempt at the SslHandler. So accidental cross-talk fails closed,
not silently.

The prod build does the inverse — TLS-only, fingerprint-pinned,
won't talk to a TLS-off dev server.

That symmetry is the whole point of using a build-time define rather
than a runtime env var: a single binary can never serve both roles.
