using UnityEngine;

namespace Astrion.Game
{
    public class ServerMonster2D : MonoBehaviour
    {
        public string Id { get; private set; }

        private Vector2 _targetPos;
        private SpriteRenderer _sr;
        private float _moveLerpSpeed = 2.5f; // a bit faster than server pace so we keep up

        public void Init(string id, float x, float y, int direction)
        {
            Id = id;
            transform.position = new Vector3(x, y, 0);
            _targetPos = new Vector2(x, y);
            _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr != null) _sr.flipX = direction < 0;
        }

        public void OnServerMove(float x, float y, int direction)
        {
            _targetPos = new Vector2(x, y);
            if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr != null) _sr.flipX = direction < 0;
        }

        public void OnHpChanged(int hp, int damage)
        {
            if (damage > 0)
                DamagePopup2D.Spawn(transform.position + Vector3.up * 0.6f, damage,
                                    new Color(1f, 0.95f, 0.30f));
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(FlashRed());
                ApplyKnockback();
            }
            ShakeCameraIfOnScreen(0.10f, 0.10f);
        }

        public void OnDeath(int damage)
        {
            if (damage > 0)
                DamagePopup2D.Spawn(transform.position + Vector3.up * 0.6f, damage,
                                    new Color(1f, 0.35f, 0.30f), large: true);
            ShakeCameraIfOnScreen(0.22f, 0.18f);
            Destroy(gameObject);
        }

        private void Update()
        {
            var cur = (Vector2)transform.position;
            var next = Vector2.MoveTowards(cur, _targetPos, _moveLerpSpeed * Time.deltaTime);
            transform.position = new Vector3(next.x, next.y, 0);
        }

        private System.Collections.IEnumerator FlashRed()
        {
            if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr == null) yield break;
            var orig = _sr.color;
            _sr.color = new Color(1f, 0.30f, 0.30f);
            yield return new WaitForSeconds(0.08f);
            if (_sr != null) _sr.color = Color.Lerp(orig, new Color(1f, 0.45f, 0.45f), 0.5f);
            yield return new WaitForSeconds(0.07f);
            if (_sr != null) _sr.color = orig;
        }

        // Visual knockback only (sprite container offset; server position is authoritative)
        private void ApplyKnockback()
        {
            if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr == null) return;
            var player = Object.FindObjectOfType<PlayerController2D>();
            float dir = 1f;
            if (player != null)
                dir = transform.position.x >= player.transform.position.x ? 1f : -1f;
            StartCoroutine(KnockbackVisual(_sr.transform, dir));
        }

        private System.Collections.IEnumerator KnockbackVisual(Transform t, float dir)
        {
            Vector3 baseLocal = Vector3.zero;
            float duration = 0.14f;
            float peak = 0.20f; // world-units to nudge
            float elapsed = 0f;
            while (elapsed < duration && t != null)
            {
                elapsed += Time.deltaTime;
                float u = elapsed / duration;
                // ease-out: fast start, settle back to 0
                float curve = Mathf.Sin(u * Mathf.PI) * (1f - u * 0.4f);
                t.localPosition = baseLocal + new Vector3(dir * peak * curve, 0f, 0f);
                yield return null;
            }
            if (t != null) t.localPosition = baseLocal;
        }

        private void ShakeCameraIfOnScreen(float amount, float duration)
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 vp = cam.WorldToViewportPoint(transform.position);
            // Soft margin: just off-screen still shakes a bit so peripheral kills land
            if (vp.z > 0 && vp.x >= -0.15f && vp.x <= 1.15f && vp.y >= -0.15f && vp.y <= 1.15f)
                Camera2D.Shake(amount, duration);
        }
    }
}
