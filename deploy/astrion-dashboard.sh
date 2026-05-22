#!/usr/bin/env bash
# Pull the latest metrics-history.jsonl from EC2, render a self-contained
# HTML dashboard with embedded data + Chart.js (CDN), open it in the
# default browser. No HTTP server needed — the file is opened as file://
# with the data baked in, so no CORS issues.
set -euo pipefail

REMOTE_USER=ubuntu
REMOTE_HOST=3.38.109.138
SSH_KEY="$HOME/.ssh/astrion-key.pem"
LOCAL_JSONL="$HOME/Astrion-Backups/metrics-history.jsonl"
OUT_HTML="/tmp/astrion-dashboard.html"

mkdir -p "$(dirname "$LOCAL_JSONL")"

echo "syncing metrics-history.jsonl from EC2 ..."
rsync -a --partial \
    -e "ssh -i $SSH_KEY -o StrictHostKeyChecking=no" \
    "$REMOTE_USER@$REMOTE_HOST:/home/ubuntu/logs/metrics-history.jsonl" \
    "$LOCAL_JSONL"

echo "rendering dashboard → $OUT_HTML"
python3 - "$LOCAL_JSONL" "$OUT_HTML" <<'PY'
import json, sys, pathlib, html

src = pathlib.Path(sys.argv[1])
out = pathlib.Path(sys.argv[2])
samples = []
for line in src.read_text().splitlines():
    line = line.strip()
    if not line: continue
    try: samples.append(json.loads(line))
    except Exception: pass

# Cap to last 7 days at most (10080 minutes). Chart.js handles 10k points
# fine on modern machines; older browsers can choke past that.
samples = samples[-10080:]

doc = """<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>ASTRION — metrics</title>
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>
<style>
  body { background:#181410; color:#e6d9b9; font-family:-apple-system, sans-serif;
         margin:0; padding:24px; }
  h1 { font-size:18px; letter-spacing:.06em; font-weight:600; margin:0 0 8px;
       color:#d8b870; }
  .sub { color:#8e7a55; font-size:12px; margin-bottom:24px; }
  .grid { display:grid; grid-template-columns:1fr 1fr; gap:18px; }
  .card { background:#221b14; border:1px solid #3b2d1a; border-radius:8px;
          padding:14px 18px; }
  .card h2 { color:#d8b870; font-size:13px; font-weight:600; margin:0 0 10px;
             letter-spacing:.04em; }
  canvas { max-height:240px; }
  .stat { font-size:13px; color:#a8956a; }
  .stat b { color:#e6d9b9; }
</style>
</head>
<body>
  <h1>✦  ASTRION — metrics history</h1>
  <div class="sub" id="hdr"></div>

  <div class="grid">
    <div class="card"><h2>Players online</h2><canvas id="players"></canvas></div>
    <div class="card"><h2>Heap used (MB)</h2><canvas id="heap"></canvas></div>
    <div class="card"><h2>Monsters alive</h2><canvas id="monsters"></canvas></div>
    <div class="card"><h2>Active drops</h2><canvas id="drops"></canvas></div>
  </div>

<script>
const RAW = __SAMPLES__;
const labels = RAW.map(s => {
  const d = new Date(s.timestamp * 1000);
  return d.toISOString().slice(5, 16).replace('T', ' ');
});
function series(key) { return RAW.map(s => s.down ? null : (s[key] ?? null)); }
function asLine(elId, label, color, data) {
  new Chart(document.getElementById(elId), {
    type: 'line',
    data: { labels: labels, datasets: [{
      label: label, data: data, borderColor: color,
      backgroundColor: color + '22', borderWidth: 1.5, pointRadius: 0,
      tension: 0.18, spanGaps: false,
    }]},
    options: {
      responsive: true, maintainAspectRatio: false,
      scales: {
        x: { ticks: { color:'#8e7a55', maxTicksLimit: 8 }, grid: { color:'#2a2218' }},
        y: { ticks: { color:'#8e7a55' }, grid: { color:'#2a2218' }, beginAtZero: true },
      },
      plugins: { legend: { display: false }},
    }
  });
}
asLine('players',  'players',  '#d8b870', series('players_online'));
asLine('heap',     'heap MB',  '#c87858', series('heap_used_mb'));
asLine('monsters', 'monsters', '#7ba85f', series('monsters'));
asLine('drops',    'drops',    '#8aa8c8', series('active_drops'));

document.getElementById('hdr').textContent =
  `${RAW.length} samples · ` +
  (RAW[0] ? new Date(RAW[0].timestamp*1000).toLocaleString() + ' → '
                  + new Date(RAW.at(-1).timestamp*1000).toLocaleString()
          : '(no data)');
</script>
</body>
</html>
"""

out.write_text(doc.replace("__SAMPLES__", json.dumps(samples)))
print(f"{len(samples)} samples rendered into {out}")
PY

# Open in the default browser
open "$OUT_HTML"
