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
        }

        private void AcquireTarget()
        {
            var player = GameObject.Find("PlayerPrefab") ?? GameObject.Find("LocalPlayer");
            if (player != null) _target = player.transform;
        }

        public void SetTarget(Transform t) => _target = t;
    }
}
