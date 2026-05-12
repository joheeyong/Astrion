using UnityEngine;

namespace Astrion.Game
{
    public class Joystick : MonoBehaviour
    {
        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }

        private RectTransform _background;
        private RectTransform _handle;
        private Canvas _canvas;
        private Camera _cam;
        private int _touchId = -1;
        private Vector2 _startPos;
        private float _radius;

        public void Init(RectTransform background, RectTransform handle)
        {
            _background = background;
            _handle = handle;
            _canvas = background.GetComponentInParent<Canvas>();
            _cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            _radius = background.sizeDelta.x * 0.5f;
            _startPos = _background.anchoredPosition;
        }

        private void Update()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouse();
#else
            HandleTouch();
#endif
        }

        private void HandleMouse()
        {
            if (Input.GetMouseButtonDown(0) && IsInJoystickArea(Input.mousePosition))
            {
                _touchId = 0;
                OnPointerDown(Input.mousePosition);
            }
            if (_touchId == 0 && Input.GetMouseButton(0))
                OnDrag(Input.mousePosition);
            if (_touchId == 0 && Input.GetMouseButtonUp(0))
                OnPointerUp();
        }

        private void HandleTouch()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began && _touchId == -1 && IsInJoystickArea(t.position))
                {
                    _touchId = t.fingerId;
                    OnPointerDown(t.position);
                }
                if (t.fingerId == _touchId)
                {
                    if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                        OnDrag(t.position);
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                        OnPointerUp();
                }
            }
        }

        private bool IsInJoystickArea(Vector2 screenPos)
        {
            return screenPos.x < Screen.width * 0.4f && screenPos.y < Screen.height * 0.45f;
        }

        private void OnPointerDown(Vector2 screenPos)
        {
            OnDrag(screenPos);
        }

        private void OnDrag(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background, screenPos, _cam, out Vector2 localPoint);

            Vector2 clampedDir = Vector2.ClampMagnitude(localPoint, _radius);
            _handle.anchoredPosition = clampedDir;

            Horizontal = clampedDir.x / _radius;
            Vertical = clampedDir.y / _radius;
        }

        private void OnPointerUp()
        {
            _touchId = -1;
            Horizontal = 0;
            Vertical = 0;
            _handle.anchoredPosition = Vector2.zero;
        }
    }
}
