using UnityEngine;

namespace Astrion.Game
{
    public class DamagePopup2D : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.9f;
        [SerializeField] private float floatSpeed = 1.8f;
        [SerializeField] private float popInDuration = 0.12f;

        private TextMesh _tm;
        private MeshRenderer _mr;
        private float _spawnedAt;
        private Vector3 _baseScale;

        public static void Spawn(Vector3 worldPos, int damage, Color color, bool large = false, bool isCritical = false)
        {
            if (damage <= 0) return;
            var go = new GameObject("DamagePopup");
            go.transform.position = worldPos;
            // Slight random horizontal offset so overlapping hits don't stack exactly
            go.transform.position += new Vector3(Random.Range(-0.15f, 0.15f), 0, 0);
            var tm = go.AddComponent<TextMesh>();
            // Critical hits get a "CRIT" prefix and red color; normal hits keep the caller's color
            if (isCritical)
            {
                tm.text = damage.ToString();
                color = new Color(1f, 0.30f, 0.18f); // crimson red
            }
            else
            {
                tm.text = damage.ToString();
            }
            tm.fontSize = isCritical ? 60 : (large ? 48 : 36);
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sortingOrder = isCritical ? 27 : 25;
            float scale = isCritical ? 0.065f : (large ? 0.055f : 0.045f);
            go.transform.localScale = Vector3.one * scale;
            var popup = go.AddComponent<DamagePopup2D>();
            popup._isCritical = isCritical;
        }

        private bool _isCritical;

        private void Awake()
        {
            _tm = GetComponent<TextMesh>();
            _mr = GetComponent<MeshRenderer>();
            _spawnedAt = Time.time;
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            float age = Time.time - _spawnedAt;
            float t = age / lifetime;
            if (t >= 1f) { Destroy(gameObject); return; }

            // Float up
            transform.position += Vector3.up * floatSpeed * (1f - t) * Time.deltaTime;

            // Pop-in scale — crits punch larger and shake on the way up
            float popT = Mathf.Min(age / popInDuration, 1f);
            float peakScale = _isCritical ? 1.5f : 1.2f;
            float scaleMul = popT < 1f
                ? Mathf.SmoothStep(0.5f, peakScale, popT)
                : Mathf.Lerp(peakScale, 1.0f, (popT - 1f));
            transform.localScale = _baseScale * scaleMul;
            // Small wobble for crits at the start
            if (_isCritical && age < 0.25f)
            {
                float wobble = Mathf.Sin(age * 40f) * 0.04f * (1f - age / 0.25f);
                transform.position += new Vector3(wobble, 0f, 0f);
            }

            // Fade
            if (_tm != null)
            {
                var c = _tm.color;
                c.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                _tm.color = c;
            }
        }
    }
}
