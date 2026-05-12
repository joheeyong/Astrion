using UnityEngine;

namespace Astrion.Game
{
    public class Parallax2D : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Vector2 parallaxFactor = new Vector2(0.5f, 0f);

        private Vector3 _lastCamPos;
        private bool _initialized;

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            if (cameraTransform != null)
            {
                _lastCamPos = cameraTransform.position;
                _initialized = true;
            }
        }

        private void LateUpdate()
        {
            if (!_initialized)
            {
                if (Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                    _lastCamPos = cameraTransform.position;
                    _initialized = true;
                }
                return;
            }

            Vector3 delta = cameraTransform.position - _lastCamPos;
            transform.position += new Vector3(delta.x * parallaxFactor.x, delta.y * parallaxFactor.y, 0f);
            _lastCamPos = cameraTransform.position;
        }
    }
}
