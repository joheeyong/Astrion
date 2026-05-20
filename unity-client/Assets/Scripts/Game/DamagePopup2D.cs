using UnityEngine;

namespace Astrion.Game
{
    public class DamagePopup2D : MonoBehaviour
    {
        [SerializeField] private float lifetime = 1.0f;
        [SerializeField] private float popInDuration = 0.12f;

        private TextMesh _tm;
        private MeshRenderer _mr;
        private TextMesh[] _outlineTms;
        private MeshRenderer[] _outlineMrs;
        private float _spawnedAt;
        private Vector3 _baseScale;
        private Vector3 _spawnPos;
        private Vector3 _driftDir;   // horizontal drift per second (random left/right)
        private float _initialJumpY; // vertical kick at spawn
        private float _spinDeg;      // random tilt for crits
        private bool _isCritical;

        // Black outline 4 offsets — N/S/E/W. Diagonal versions would be thicker
        // but doubling the mesh count makes overlap noticeable at heavy combat.
        private static readonly Vector2[] OutlineOffsets = {
            new Vector2( 0.04f,  0f),  new Vector2(-0.04f, 0f),
            new Vector2( 0f,  0.04f),  new Vector2( 0f, -0.04f),
        };

        public static void Spawn(Vector3 worldPos, int damage, Color color, bool large = false, bool isCritical = false)
        {
            if (damage <= 0) return;

            var go = new GameObject("DamagePopup");
            // Random spawn x so multiple hits in the same frame don't stack to one pillar.
            go.transform.position = worldPos + new Vector3(Random.Range(-0.2f, 0.2f), 0, 0);

            // Tier the visual by damage band — bigger numbers look bigger and warmer,
            // mirroring the MapleStory family of feedback. Caller-supplied color
            // still applies to plain hits; crits override to crimson red.
            int fontSize;
            float scale;
            Color tierColor = color;
            if (isCritical)
            {
                tierColor = new Color(1f, 0.28f, 0.18f);   // crimson
                fontSize = 64;
                scale = 0.072f;
            }
            else if (damage >= 1000)
            {
                tierColor = new Color(1f, 0.85f, 0.20f);    // gold
                fontSize = 56; scale = 0.060f;
            }
            else if (damage >= 500)
            {
                tierColor = new Color(1f, 0.70f, 0.25f);    // orange
                fontSize = 50; scale = 0.055f;
            }
            else if (damage >= 100)
            {
                fontSize = large ? 48 : 42;
                scale    = large ? 0.052f : 0.048f;
            }
            else
            {
                fontSize = large ? 44 : 36;
                scale    = large ? 0.048f : 0.044f;
            }

            // Critical hits sometimes spit out an exclamation mark to make the
            // discrimination obvious without reading the colour.
            string text = isCritical ? damage.ToString() + "!" : damage.ToString();

            // Main glyph
            var mainTm = go.AddComponent<TextMesh>();
            mainTm.text = text;
            mainTm.fontSize = fontSize;
            mainTm.fontStyle = FontStyle.Bold;
            mainTm.anchor = TextAnchor.MiddleCenter;
            mainTm.alignment = TextAlignment.Center;
            mainTm.color = tierColor;
            var mainMr = go.GetComponent<MeshRenderer>();
            mainMr.sortingOrder = isCritical ? 28 : 26;
            go.transform.localScale = Vector3.one * scale;

            // Outline pass — four child TextMeshes nudged in cardinal directions.
            // Cheap, no shader required, works on every Unity build target.
            var outlineTms = new TextMesh[OutlineOffsets.Length];
            var outlineMrs = new MeshRenderer[OutlineOffsets.Length];
            for (int i = 0; i < OutlineOffsets.Length; i++)
            {
                var olGo = new GameObject("Outline");
                olGo.transform.SetParent(go.transform, false);
                olGo.transform.localPosition = new Vector3(OutlineOffsets[i].x, OutlineOffsets[i].y, 0.001f);
                var olTm = olGo.AddComponent<TextMesh>();
                olTm.text = text;
                olTm.fontSize = fontSize;
                olTm.fontStyle = FontStyle.Bold;
                olTm.anchor = TextAnchor.MiddleCenter;
                olTm.alignment = TextAlignment.Center;
                olTm.color = Color.black;
                var olMr = olGo.GetComponent<MeshRenderer>();
                olMr.sortingOrder = mainMr.sortingOrder - 1;
                outlineTms[i] = olTm;
                outlineMrs[i] = olMr;
            }

            var popup = go.AddComponent<DamagePopup2D>();
            popup._isCritical = isCritical;
            popup._outlineTms = outlineTms;
            popup._outlineMrs = outlineMrs;
            // Each popup gets its own arc: side picked at spawn so successive
            // hits feel different rather than marching straight up in a line.
            popup._driftDir   = new Vector3(Random.Range(-1.2f, 1.2f), 0, 0);
            popup._initialJumpY = isCritical ? 3.0f : 2.4f;
            popup._spinDeg    = isCritical ? Random.Range(-12f, 12f) : Random.Range(-4f, 4f);
        }

        private void Awake()
        {
            _tm = GetComponent<TextMesh>();
            _mr = GetComponent<MeshRenderer>();
            _spawnedAt = Time.time;
            _baseScale = transform.localScale;
            _spawnPos = transform.position;
            transform.rotation = Quaternion.Euler(0, 0, _spinDeg);
        }

        private void Update()
        {
            float age = Time.time - _spawnedAt;
            float t = age / lifetime;
            if (t >= 1f) { Destroy(gameObject); return; }

            // Parabolic arc: vertical velocity decays linearly while horizontal
            // drift stays roughly constant — gives that bounce-and-glide curve
            // that reads as 'damage popped out and landed' instead of 'text
            // teleporting upward'.
            float vy = _initialJumpY * (1f - t);  // 2.4 → 0 over lifetime
            float dy = _initialJumpY * t * (1f - 0.5f * t);  // antiderivative
            transform.position = _spawnPos
                + new Vector3(_driftDir.x * t, dy, 0)
                + new Vector3(0, age * 0.3f, 0);   // small upward continuation

            // Pop-in scale — punchier overshoot, then settles. Crits punch harder.
            float popT = Mathf.Min(age / popInDuration, 1f);
            float peak = _isCritical ? 1.6f : 1.25f;
            float scaleMul = popT < 1f
                ? Mathf.SmoothStep(0.4f, peak, popT)
                : Mathf.Lerp(peak, 1.0f, Mathf.Min((age - popInDuration) / 0.15f, 1f));
            transform.localScale = _baseScale * scaleMul;

            // Crit wobble — brief horizontal jitter on top of the parabola.
            if (_isCritical && age < 0.3f)
            {
                float wobble = Mathf.Sin(age * 50f) * 0.05f * (1f - age / 0.3f);
                transform.position += new Vector3(wobble, 0f, 0f);
            }

            // Fade — start opaque, ease to transparent in the last 35%.
            float alpha = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
            if (_tm != null)
            {
                var c = _tm.color; c.a = alpha; _tm.color = c;
            }
            if (_outlineTms != null)
            {
                for (int i = 0; i < _outlineTms.Length; i++)
                {
                    if (_outlineTms[i] == null) continue;
                    var c = _outlineTms[i].color; c.a = alpha; _outlineTms[i].color = c;
                }
            }
        }
    }
}
