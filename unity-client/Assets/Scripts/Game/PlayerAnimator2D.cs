using UnityEngine;

namespace Astrion.Game
{
    public class PlayerAnimator2D : MonoBehaviour
    {
        [SerializeField] private Transform body;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;

        [SerializeField] private float walkCycleSpeed = 9f;
        [SerializeField] private float armSwingAngle = 35f;
        [SerializeField] private float legSwingAngle = 40f;
        [SerializeField] private float lerpSpeed = 14f;
        [SerializeField] private float bodyBobAmount = 0.04f;

        private PlayerController2D _ctrl;
        private Rigidbody2D _rb;
        private float _phase;
        private float _bodyBaseY;
        private bool _hasBodyBaseY;

        private void Awake()
        {
            _ctrl = GetComponent<PlayerController2D>();
            _rb = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                _bodyBaseY = body.localPosition.y;
                _hasBodyBaseY = true;
            }
        }

        private void Update()
        {
            if (_ctrl == null || _rb == null) return;

            bool grounded = _ctrl.IsGrounded;
            bool climbing = _ctrl.IsClimbing;
            bool moving = Mathf.Abs(_rb.velocity.x) > 0.15f;

            float targetLA, targetRA, targetLL, targetRL, targetBob;

            if (climbing)
            {
                _phase += Time.deltaTime * walkCycleSpeed * 0.7f;
                float s = Mathf.Sin(_phase);
                targetLA = s * 70f;
                targetRA = -s * 70f;
                targetLL = -s * 20f;
                targetRL = s * 20f;
                targetBob = 0f;
            }
            else if (!grounded)
            {
                // Jumping/airborne pose
                bool ascending = _rb.velocity.y > 0;
                targetLA = ascending ? -50f : -20f;
                targetRA = ascending ? 50f : 20f;
                targetLL = ascending ? -20f : 10f;
                targetRL = ascending ? -20f : 10f;
                targetBob = 0f;
            }
            else if (moving)
            {
                _phase += Time.deltaTime * walkCycleSpeed;
                float s = Mathf.Sin(_phase);
                targetLA = s * armSwingAngle;
                targetRA = -s * armSwingAngle;
                targetLL = -s * legSwingAngle;
                targetRL = s * legSwingAngle;
                targetBob = Mathf.Abs(s) * bodyBobAmount;
            }
            else
            {
                _phase = 0;
                targetLA = 0f; targetRA = 0f;
                targetLL = 0f; targetRL = 0f;
                targetBob = 0f;
            }

            ApplyRotation(leftArm, targetLA);
            ApplyRotation(rightArm, targetRA);
            ApplyRotation(leftLeg, targetLL);
            ApplyRotation(rightLeg, targetRL);

            if (body != null && _hasBodyBaseY)
            {
                var p = body.localPosition;
                p.y = Mathf.Lerp(p.y, _bodyBaseY + targetBob, Time.deltaTime * lerpSpeed);
                body.localPosition = p;
            }
        }

        private void ApplyRotation(Transform t, float target)
        {
            if (t == null) return;
            var e = t.localEulerAngles;
            float cur = e.z > 180f ? e.z - 360f : e.z;
            cur = Mathf.Lerp(cur, target, Time.deltaTime * lerpSpeed);
            t.localEulerAngles = new Vector3(0, 0, cur);
        }
    }
}
