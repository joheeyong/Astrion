# ASTRION — Operations Guide

운영자가 알아야 할 모든 것 — 인프라 / 보안 / 백업 / 배포 / 트러블슈팅.

---

## 0. 인프라 개요

| 자원 | 위치 / 값 |
|---|---|
| 게임 서버 호스트 | `3.38.109.138` (AWS EC2 ap-northeast-2, t3-class, 2 vCPU / 1.8 GiB) |
| 게임 포트 (TLS) | `9000` |
| 운영 HTTP 포트 | `9001` (`/health`, `/metrics`) — **운영자 IP만 SG에서 허용 권장** |
| 공개 liveness 포트 | `9002` (`/health` 만) — **외부 모니터링용, 0.0.0.0/0 허용 가능** |
| Redis | `localhost:6379` (외부 노출 X) |
| 운영 사용자 | `ubuntu` |
| 작업 디렉토리 | `~/game-server-0.1.0/` |
| SSH 키 | `~/.ssh/astrion-key.pem` (개발자 mac) |

---

## 1. 서비스 관리 (systemd)

unit 파일: `deploy/astrion-game-server.service` → `/etc/systemd/system/`

```bash
# 상태 확인
sudo systemctl status astrion-game-server

# 재시작 (배포 후)
sudo systemctl restart astrion-game-server

# 부팅 시 자동 시작
sudo systemctl enable astrion-game-server

# 무한 크래시 루프 후 복구
sudo systemctl reset-failed astrion-game-server
sudo systemctl start astrion-game-server
```

**자동 복구 정책**:
- JVM 크래시 / OOM → 5초 후 자동 재시작 (`Restart=always`, `RestartSec=5`)
- OOM 시 JVM 즉시 종료 (`-XX:+ExitOnOutOfMemoryError`)
- EC2 재부팅 → 부팅 시 자동 시작 (`enabled`)
- 60초 내 5회 실패 → `failed` 상태로 정지 (CPU 보호)
- SIGTERM → 30초 안에 graceful shutdown (player offline + Redis flush + Netty drain)

---

## 2. 로그

```
~/logs/server.log              모든 INFO/DEBUG (50MB / 14일 / 1GB cap, gzip)
~/logs/errors.log              ERROR + uncaught exception (20MB / 30일 / 200MB cap)
~/logs/client-errors.log       클라이언트 NRE/Exception 자동 수집 (50MB / 30일 / 500MB cap)
~/logs/jvm-crash-<PID>.log     JVM hotspot 크래시 (segfault, internal bug)
~/logs/java_pid<PID>.hprof     OOM heap dump
```

**라이브 모니터링**:
```bash
tail -f ~/logs/errors.log              # 진짜 에러만
tail -f ~/logs/client-errors.log       # 클라 측 NRE
tail -f ~/logs/server.log              # 모든 흐름
sudo journalctl -u astrion-game-server -f  # systemd 레벨 (시작/크래시)
```

---

## 3. 보안

### TLS (9000번)
- 자체 서명 인증서, 만료 **2036-05-17** (~10년)
- 파일: `~/game-server-cert/server.{crt,key}`
- SHA-256 fingerprint: `1EEA59A85846E2450BA226E03141113B72B16F2171D8C986734B3F94CA569DDE`
- 클라이언트가 fingerprint **pinning** — MITM 방어 (위조 CA cert도 거부)
- 갱신 시: 인증서 재생성 → 클라 `NetworkManager.cs:ServerCertSha256` 업데이트 → 클라 재배포

```bash
# 인증서 확인
openssl x509 -in ~/game-server-cert/server.crt -noout -enddate
openssl x509 -in ~/game-server-cert/server.crt -noout -fingerprint -sha256
```

### 보안 layer (login 흐름)

| Layer | 한도 | 차단 | 저장 | 잡는 공격 |
|---|---|---|---|---|
| Connection rate (TCP) | 30 conn / 분 | 60초 | 메모리 | SYN flood, port scan |
| IP login rate | 5 시도 / 분 | 5분 | 메모리 | 단일 IP brute-force |
| Username rate | 10 시도 / 분 | 15분 | 메모리 | 분산 spray (botnet) |
| Account lockout | 10 누적 wrong-pw / 24h | 1시간 | **Redis** (영속) | 느린 stealth 공격 |

→ Loopback (`127.0.0.1`)은 모든 layer bypass (로컬 부하 테스트용).

### 클라이언트 wire 보호
- **버전 매칭**: LOGIN 패킷에 `clientVersion` 첨부, 서버 `common.Version.CURRENT`와 정확 일치 안 하면 자격증명 검증 자체 안 함
- 한 번에 갱신: `./bump-version.sh 0.2.0` → Java + C# + root build.gradle 동기화

### Anti-cheat
- **이동 속도 cap**: 12 unit/s + lag grace
- **STATE_SAVE 검증**: 모든 필드 범위 검사 (level 1~200, gold 0~1B, inventory ≤100슬롯 등). 비정상 거부 + `account:cheats:{user}` Redis 카운터 ++
- **WARN으로 노이즈 분리**: TLS handshake 실패 / 잘못된 packet / connection reset은 ERROR가 아닌 WARN. `errors.log`는 진짜 버그만.

---

## 4. 모니터링

```bash
# 공개 liveness probe — 누구나
curl http://3.38.109.138:9002/health
# → {"status":"ok"}

# 운영 메트릭 — SG에서 제한
curl http://3.38.109.138:9001/metrics
# → {
#     "version": "0.1.0",
#     "uptime_seconds": ...,
#     "players_online": N,
#     "monsters": N,
#     "active_drops": N,
#     "heap_used_mb": N,
#     "heap_max_mb": 512,
#     "players_by_zone": { ... },
#     "monsters_by_zone": { ... },
#     "drops_by_zone": { ... }
#   }
```

외부 모니터링 도구 연동 예시:
- **UptimeRobot** — URL: `http://3.38.109.138:9002/health`, Keyword: `ok`
- **AWS Route 53 health check** — HTTP `:9002/health`, search string `ok`

---

## 5. 백업

### 3중 방어 (Redis persistence)

| Layer | 무엇 | 손실 윈도우 |
|---|---|---|
| AOF | 모든 write를 디스크에 append (fsync everysec) | **1초** |
| RDB auto-snapshot | `save 900 1 / 300 10 / 60 10000` | 1~15분 |
| Cron 6시간 백업 | `~/backups/redis/dump-UTC.rdb.gz` (14일 보관) | 6시간 |
| Mac off-site rsync | `~/Astrion-Backups/redis/` (90일 보관) | 1일 |

### 백업 명령

```bash
# 수동 백업 (서버)
~/scripts/backup-redis.sh

# 수동 동기화 (mac)
~/scripts/astrion-backup-sync.sh

# 백업 목록
ls ~/Astrion-Backups/redis/
```

### 복구 절차

```bash
# 1. 백업 선택
ls ~/Astrion-Backups/redis/

# 2. EC2에 전송
scp -i ~/.ssh/astrion-key.pem dump-XXX.rdb.gz ubuntu@3.38.109.138:~/

# 3. Redis 정지 후 교체
ssh -i ~/.ssh/astrion-key.pem ubuntu@3.38.109.138
sudo systemctl stop redis-server
gunzip ~/dump-XXX.rdb.gz
sudo cp ~/dump-XXX.rdb /var/lib/redis/dump.rdb
sudo chown redis:redis /var/lib/redis/dump.rdb
sudo systemctl start redis-server
sudo systemctl restart astrion-game-server
```

---

## 6. 배포

### 서버 배포 (game-server)

```bash
# 빌드
./gradlew :game-server:clean :game-server:distTar

# 전송 + 적용
scp -i ~/.ssh/astrion-key.pem \
    game-server/build/distributions/game-server-0.1.0.tar \
    ubuntu@3.38.109.138:~/game-server-0.1.0-new.tar

ssh -i ~/.ssh/astrion-key.pem ubuntu@3.38.109.138 "
  rm -rf ~/game-server-0.1.0
  tar -xf ~/game-server-0.1.0-new.tar -C ~/
  sudo systemctl restart astrion-game-server
"
```

### 클라이언트 배포 (macOS)

```bash
# 빌드
/Applications/Unity/Hub/Editor/2022.3.25f1/Unity.app/Contents/MacOS/Unity \
    -batchmode -nographics -quit \
    -projectPath ~/projects/Astrion/unity-client \
    -executeMethod ProjectSetup.BuildMacOS \
    -logFile /tmp/astrion_build.log

# 배포 (다른 사용자에게)
cd ~/projects/Astrion/unity-client/Builds/macOS
zip -ry ~/Desktop/Astrion-mac.zip Astrion.app
# AirDrop / Dropbox 로 전달
```

**다른 mac 첫 실행** (Gatekeeper 우회):
- 우클릭 → 열기 → "열기" 확인
- 또는 `xattr -dr com.apple.quarantine /경로/Astrion.app`

### 버전 bump

```bash
./bump-version.sh 0.2.0
# → root build.gradle.kts, Version.java, Version.cs 동기화
git diff && git commit && git push
```

빌드 시 `:common:checkVersionSync` task가 세 곳 일치 검증, mismatch면 BUILD FAILED.

---

## 7. 부하 테스트

```bash
# EC2 안에서 실행 (loopback rate-limit bypass 활용)
scp deploy/loadtest.py ubuntu@3.38.109.138:~/
ssh ubuntu@3.38.109.138 'python3 ~/loadtest.py 200 60'   # N=200 동시, 60초
```

**측정된 한계 (현재 인스턴스)**:
- 🟢 **200 동시 player** — 안정 (heap 31% / CPU 19%)
- 🟡 300 — soft cap
- 🔴 500+ — 인스턴스 업그레이드 필요 (CPU + RAM saturate)

업그레이드 권장: **c5.large** (CPU 강화) — CPU가 bottleneck.

---

## 8. 트러블슈팅

### 서버 응답 없음 (`/health` timeout)
1. `sudo systemctl status astrion-game-server` — failed 상태? → `reset-failed` + `start`
2. `sudo journalctl -u astrion-game-server -n 50` — 최근 로그 확인
3. `~/logs/errors.log` — 진짜 에러
4. OOM 발생했는지: `ls ~/logs/*.hprof`
5. 메모리 / 디스크: `free -h && df -h /`

### 부하 테스트 후 서버 stuck
- 부하 테스트가 saturate시킨 후 클라가 timeout 받음
- 해결: `sudo systemctl restart astrion-game-server`

### 데이터 손실 의심
- `redis-cli dbsize` — 키 수 확인
- 백업 점검: `ls -la ~/backups/redis/`, `ls -la ~/Astrion-Backups/redis/`
- 복구: 위 §5 절차

### 사용자가 로그인 안 됨
- IP rate limit: 5회 실패 시 5분 차단 → `~/logs/server.log` grep `Rate-limited`
- Account lockout: 누적 10회 wrong-pw 시 1시간 → `redis-cli ttl account:locked:USERNAME`
- 강제 해제: `redis-cli del account:fails:USERNAME account:locked:USERNAME`

### 클라가 TLS 핸드셰이크 실패
- 서버 인증서 변경됨? 클라 fingerprint 불일치 → 클라 재배포 필수
- 사용자 클라 콘솔: `~/Library/Logs/Astrion/Astrion/Player.log`에 `TLS cert pin MISMATCH`

---

## 9. 변경 이력 (운영 인프라)

| 일자 | 변경 |
|---|---|
| 2026-05-20 | TLS 도입 + fingerprint pinning |
| 2026-05-20 | 버전 매칭 + 자동 동기화 (bump-version.sh) |
| 2026-05-20 | 로그인 rate limit (IP + username + account lockout) |
| 2026-05-20 | logback rolling (server / errors / client) |
| 2026-05-20 | `/health` + `/metrics` HTTP endpoint |
| 2026-05-20 | 버전 bump 자동화 |
| 2026-05-20 | systemd unit + 자동 재시작 |
| 2026-05-20 | 크래시 dump (JVM ErrorFile + HeapDump) |
| 2026-05-20 | Redis backup 자동화 + off-site rsync |
| 2026-05-20 | TLS noise → WARN 분류 |
| 2026-05-20 | Graceful shutdown (SIGTERM 처리) |
| 2026-05-20 | Public `/health` 분리 (9002) |
| 2026-05-20 | Connection rate limit (DDoS 보호) |
| 2026-05-20 | Account lockout (Redis 영속) |
| 2026-05-20 | STATE_SAVE 검증 (anti-cheat) |
| 2026-05-20 | API server 모듈 정리 + MySQL 비활성화 |
| 2026-05-20 | 부하 테스트 (loadtest.py) |
| 2026-05-20 | Broadcast zone-keyed 최적화 |
| 2026-05-20 | 클라이언트 Exception 서버 전송 |
| 2026-05-21 | OOM 자동 재시작 (`+ExitOnOutOfMemoryError`) |

---

## 10. SSH key rotation

See `docs/SSH-KEY-ROTATION.md` for the full runbook. Quick path:

```bash
# Local machine — interactive, downtime 0
~/projects/Astrion/deploy/rotate-ssh-key.sh
```

It generates an Ed25519 key, adds it to the EC2 authorized_keys,
verifies SSH works with the new key, then removes the old one and
archives the old `.pem` locally with `chmod 000`. Aborts before
touching the old key if the new key fails verification — botched
rotation can't lock you out.
