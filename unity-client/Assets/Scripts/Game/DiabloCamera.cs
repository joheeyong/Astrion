using UnityEngine;

namespace Astrion.Game
{
    public class DiabloCamera : MonoBehaviour
    {
        [Header("View Angle")]
        [SerializeField] private float pitch = 55f;
        [SerializeField] private float yaw = 0f;

        [Header("Distance")]
        [SerializeField] private float distance = 12f;
        [SerializeField] private float minDistance = 6f;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private float zoomSpeed = 2f;

        [Header("Follow")]
        [SerializeField] private float lookHeight = 1.2f;
        [SerializeField] private float smoothSpeed = 10f;

        private Transform _target;
        private float _currentDistance;

        private void Start()
        {
            _currentDistance = distance;
            AcquireTarget();
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                AcquireTarget();
                if (_target == null) return;
            }

            HandleZoom();

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -_currentDistance);
            Vector3 lookPoint = _target.position + Vector3.up * lookHeight;
            Vector3 desiredPos = lookPoint + offset;

            transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
            transform.rotation = rotation;
        }

        private void HandleZoom()
        {
            if (Application.isMobilePlatform)
            {
                if (Input.touchCount == 2)
                {
                    Touch t0 = Input.GetTouch(0);
                    Touch t1 = Input.GetTouch(1);
                    float prevDist = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
                    float currDist = (t0.position - t1.position).magnitude;
                    float diff = currDist - prevDist;
                    _currentDistance -= diff * zoomSpeed * 0.01f;
                    _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
                }
            }
            else
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _currentDistance -= scroll * zoomSpeed * 5f;
                    _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
                }
            }
        }

        private void AcquireTarget()
        {
            var player = GameObject.Find("PlayerPrefab");
            if (player == null) player = GameObject.Find("LocalPlayer");
            if (player != null) _target = player.transform;
        }
    }
}
