using UnityEngine;

namespace Astrion.Game
{
    public class MMORPGCamera : MonoBehaviour
    {
        [SerializeField] private float distance = 10f;
        [SerializeField] private float minDistance = 3f;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private float height = 5f;
        [SerializeField] private float rotationSpeed = 3f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private float minVerticalAngle = -10f;
        [SerializeField] private float maxVerticalAngle = 60f;

        private Transform _target;
        private float _currentYaw;
        private float _currentPitch = 25f;
        private float _currentDistance;
        private bool _isDragging;
        private Vector2 _lastTouchPos;

        private void Start()
        {
            _currentDistance = distance;
            var player = GameObject.Find("PlayerPrefab");
            if (player != null)
                _target = player.transform;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                var player = GameObject.Find("PlayerPrefab");
                if (player != null)
                    _target = player.transform;
                else
                    return;
            }

            HandleInput();

            // Calculate desired camera position
            Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0);
            Vector3 offset = rotation * new Vector3(0, 0, -_currentDistance);
            Vector3 targetPos = _target.position + Vector3.up * 1.5f; // Look at chest height
            Vector3 desiredPos = targetPos + offset;

            // Smooth follow
            transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
            transform.LookAt(targetPos);
        }

        private void HandleInput()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
        }

        private void HandleMouseInput()
        {
            // Right mouse button to rotate camera
            if (Input.GetMouseButton(1))
            {
                _currentYaw += Input.GetAxis("Mouse X") * rotationSpeed * 2f;
                _currentPitch -= Input.GetAxis("Mouse Y") * rotationSpeed * 2f;
                _currentPitch = Mathf.Clamp(_currentPitch, minVerticalAngle, maxVerticalAngle);
            }

            // Scroll wheel to zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _currentDistance -= scroll * zoomSpeed * 5f;
                _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
            }
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                // Only rotate if touching the right half of the screen (left half is joystick)
                if (touch.phase == TouchPhase.Began && touch.position.x > Screen.width * 0.4f)
                    _isDragging = true;

                if (_isDragging && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
                {
                    _currentYaw += touch.deltaPosition.x * rotationSpeed * 0.1f;
                    _currentPitch -= touch.deltaPosition.y * rotationSpeed * 0.1f;
                    _currentPitch = Mathf.Clamp(_currentPitch, minVerticalAngle, maxVerticalAngle);
                }

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    _isDragging = false;
            }
            else if (Input.touchCount == 2)
            {
                // Pinch to zoom
                _isDragging = false;
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                float prevDist = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
                float currDist = (t0.position - t1.position).magnitude;
                float diff = currDist - prevDist;

                _currentDistance -= diff * zoomSpeed * 0.01f;
                _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
            }
            else
            {
                _isDragging = false;
            }
        }
    }
}
