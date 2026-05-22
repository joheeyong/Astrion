# CI / CD on GitHub Actions

Two workflows live in `.github/workflows/`:

| File | Trigger | What it does |
|---|---|---|
| `ci.yml` | every push + PR to `main` | Gradle build (`:game-server:distTar`), version sync check, shell-script syntax. Uploads the built `.tar` as a workflow artifact for 7 days. |
| `deploy.yml` | manual (Actions tab → Run workflow) | SSH into EC2 and run `deploy.sh` with the selected mode (`full` / `no-build` / `restart-only`). |

CI runs immediately on existing repos; deploy needs a one-time secret
setup (below).

---

## CI — `ci.yml`

Always-on. Nothing for the operator to configure.

What it verifies before any commit lands on `main`:

- The Gradle wrapper hasn't been tampered with (`wrapper-validation-action`)
- `:common:checkVersionSync` — `build.gradle.kts` version, `Version.java`,
  and `Version.cs` are aligned. Catches forgetting `./bump-version.sh`.
- `:game-server:distTar` builds — the same artifact `deploy.sh` would
  send. If the build is broken, CI is red and the artifact upload step
  fails, so a bad commit can't quietly sit on main.
- `bash -n` on every `.sh` in `deploy/`, plus the root `deploy.sh` and
  `bump-version.sh`. Cheap, catches typos / missing quotes that the
  scripts won't surface until someone actually runs them.

Build cache lives in the workflow's `actions/cache@v4` — second-run CI
typically completes in ~1 minute once the Gradle deps are warm.

A failing CI doesn't block deploys (deploy is manual), but it does
show a red ✗ on the commit in the Actions tab and on the PR.

---

## Deploy — `deploy.yml` (manual)

One-time setup the maintainer needs to do in the repo settings before
this workflow can succeed:

### 1. Add the EC2 SSH key as a secret

`Settings → Secrets and variables → Actions → New repository secret`

| Name | Value |
|---|---|
| `ASTRION_SSH_KEY` | full contents of `~/.ssh/astrion-key.pem` on the operator's Mac, including the BEGIN/END lines |

> The secret is encrypted at rest and never logged. Workflow steps that
> reference it appear as `***` in run output.

### 2. (Optional) Require approval before deploys run

`Settings → Environments → New environment` → name it `prod` →
**Required reviewers** → add yourself.

With this set, clicking *Run workflow* puts the job into 'Waiting for
review' and you have to manually approve it from the Actions UI. A
nice safety net against an accidental click.

Without the env configured, the workflow runs straight away.

### 3. Trigger a deploy

`Actions tab → Deploy (manual) → Run workflow`

- pick the branch (`main`)
- pick a mode:
  - `full` — `./deploy.sh` (rebuild jar, ship, restart, verify)
  - `no-build` — skip Gradle (use the artifact from the latest CI run)
  - `restart-only` — config change only, just `sudo systemctl restart`
- Click **Run workflow**

The runner does the same dance `deploy.sh` does locally:
checkout → JDK 21 → gradle cache → install SSH key from secret →
`ASTRION_SSH_KEY=/home/runner/.ssh/astrion-key.pem ./deploy.sh`.

Health-check at the end happens via SSH to `localhost:9002` on EC2
(same as local deploys — independent of how 9002 is exposed in the SG).

---

## What this does NOT do

- **Unity client builds**. GameCI exists but needs a Unity license
  registered with the runner — the project doesn't have that yet. Mac
  build via the editor (`Astrion/Build macOS …`) stays local.
- **Auto-deploy on push**. Intentional. `deploy.yml` is
  `workflow_dispatch`-only — we don't ship every green CI to prod
  automatically. If you want auto-deploy on main, change `on:` to
  `push: branches: [main]` and add a `needs: server-build` to keep CI
  as a gate.
- **Run tests**. No JUnit / no Unity playmode tests today. If/when
  they exist, add them to `ci.yml` between the version-sync and
  build steps.

---

## Rotation note

If you ever rotate the SSH key (`docs/SSH-KEY-ROTATION.md`), update
the `ASTRION_SSH_KEY` secret in GitHub Settings at the same time.
Otherwise the next deploy.yml run will hit 'permission denied'.
