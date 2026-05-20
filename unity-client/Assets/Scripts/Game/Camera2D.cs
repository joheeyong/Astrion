using UnityEngine;

namespace Astrion.Game
{
    [RequireComponent(typeof(Camera))]
    public class Camera2D : MonoBehaviour
    {
        [SerializeField] private float smoothSpeed = 6f;
        [SerializeField] private Vector2 offset = new Vector2(0f, 1.2f);
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector2 boundsMin = new Vector2(-30, -10);
        [SerializeField] private Vector2 boundsMax = new Vector2(30, 20);

        private Transform _target;
        private Camera _cam;
        private float _shakeAmount;
        private float _shakeUntil;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
        }

        private void Start() => AcquireTarget();

        private void LateUpdate()
        {
            if (_target == null)
            {
                AcquireTarget();
                if (_target == null) return;
            }

            Vector3 desired = new Vector3(
                _target.position.x + offset.x,
                _target.position.y + offset.y,
                transform.position.z);

            if (useBounds)
            {
                float halfH = _cam.orthographicSize;
                float halfW = halfH * _cam.aspect;
                desired.x = Mathf.Clamp(desired.x, boundsMin.x + halfW, boundsMax.x - halfW);
                desired.y = Mathf.Clamp(desired.y, boundsMin.y + halfH, boundsMax.y - halfH);
            }

            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);

            // Apply shake on top of the smoothed position
            if (Time.time < _shakeUntil && _shakeAmount > 0f)
            {
                float falloff = Mathf.Clamp01((_shakeUntil - Time.time) / 0.20f);
                Vector2 jitter = Random.insideUnitCircle * _shakeAmount * falloff;
                transform.position += new Vector3(jitter.x, jitter.y, 0f);
            }
        }

        /// Trigger a small camera shake. Stacks (longer / stronger wins).
        public static void Shake(float amount, float duration)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var c2d = cam.GetComponent<Camera2D>();
            if (c2d == null) return;
            float until = Time.time + duration;
            if (until > c2d._shakeUntil) c2d._shakeUntil = until;
            if (amount > c2d._shakeAmount) c2d._shakeAmount = amount;
            // Reset amount when shake window expires (next call will set fresh)
            if (Time.time >= c2d._shakeUntil - duration) c2d._shakeAmount = Mathf.Max(c2d._shakeAmount, amount);
        }

        private void AcquireTarget()
        {
            var player = GameObject.Find("PlayerPrefab") ?? GameObject.Find("LocalPlayer");
            if (player != null) _target = player.transform;
        }

        public void SetTarget(Transform t) => _target = t;

        // === Hit-stop ===
        // Brief Time.timeScale freeze + zoom in, then ease zoom out.
        // Trigger from the attacker side (own attacks only), once per swing.
        private bool _hitStopRunning;

        public static void HitStop(float pauseDuration = 0.05f, float zoomDuration = 0.18f, float zoomAmount = 0.4f)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var c2d = cam.GetComponent<Camera2D>();
            if (c2d == null) return;
            c2d.StartHitStop(pauseDuration, zoomDuration, zoomAmount);
        }

        public void StartHitStop(float pauseDuration, float zoomDuration, float zoomAmount)
        {
            if (_hitStopRunning) return;
            StartCoroutine(HitStopCoroutine(pauseDuration, zoomDuration, zoomAmount));
        }

        private System.Collections.IEnumerator HitStopCoroutine(float pause, float zoomDuration, float zoomAmount)
        {
            _hitStopRunning = true;
            float origSize = _cam.orthographicSize;
            Time.timeScale = 0f;
            _cam.orthographicSize = Mathf.Max(0.5f, origSize - zoomAmount);
            yield return new WaitForSecondsRealtime(pause);
            Time.timeScale = 1f;
            float t = 0f;
            while (t < zoomDuration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / zoomDuration);
                _cam.orthographicSize = Mathf.Lerp(origSize - zoomAmount, origSize, u);
                yield return null;
            }
            _cam.orthographicSize = origSize;
            _hitStopRunning = false;
        }
    }
}
