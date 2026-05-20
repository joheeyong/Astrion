using UnityEngine;

namespace Astrion.Game
{
    /// Sword trail — sits at the blade tip and emits only while the player
    /// is actually swinging. Activated by PlayerAnimator2D when the attack
    /// motion fires.
    [RequireComponent(typeof(TrailRenderer))]
    public class SwordTrailController : MonoBehaviour
    {
        private TrailRenderer _tr;
        private float _stopAt;

        private void Awake()
        {
            _tr = GetComponent<TrailRenderer>();
            // Make sure the trail has a material — assigning in Editor doesn't
            // persist for runtime-created GameObjects, so we build one here.
            if (_tr.sharedMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader != null) _tr.material = new Material(shader);
            }

            _tr.time = 0.18f;
            _tr.minVertexDistance = 0.01f;
            _tr.widthCurve = AnimationCurve.EaseInOut(0f, 0.18f, 1f, 0f);
            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1f, 0.96f, 0.85f), 0f),
                    new GradientColorKey(new Color(0.55f, 0.78f, 1f), 1f),
                },
                new[] {
                    new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(0f, 1f),
                });
            _tr.colorGradient = grad;
            _tr.sortingOrder = 15;
            _tr.emitting = false;
            _tr.Clear();
        }

        public void Trigger(float duration)
        {
            if (_tr == null) return;
            _tr.Clear();
            _tr.emitting = true;
            _stopAt = Time.time + duration;
        }

        private void Update()
        {
            if (_tr != null && _tr.emitting && Time.time >= _stopAt)
                _tr.emitting = false;
        }
    }
}
