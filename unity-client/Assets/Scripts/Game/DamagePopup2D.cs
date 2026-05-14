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

        public static void Spawn(Vector3 worldPos, int damage, Color color, bool large = false)
        {
            if (damage <= 0) return;
            var go = new GameObject("DamagePopup");
            go.transform.position = worldPos;
            // Slight random horizontal offset so overlapping hits don't stack exactly
            go.transform.position += new Vector3(Random.Range(-0.15f, 0.15f), 0, 0);
            var tm = go.AddComponent<TextMesh>();
            tm.text = damage.ToString();
            tm.fontSize = large ? 48 : 36;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sortingOrder = 25;
            go.transform.localScale = Vector3.one * (large ? 0.055f : 0.045f);
            go.AddComponent<DamagePopup2D>();
        }

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

            // Pop-in scale
            float popT = Mathf.Min(age / popInDuration, 1f);
            float scaleMul = popT < 1f ? Mathf.SmoothStep(0.5f, 1.2f, popT) : Mathf.Lerp(1.2f, 1.0f, (popT - 1f));
            transform.localScale = _baseScale * scaleMul;

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
