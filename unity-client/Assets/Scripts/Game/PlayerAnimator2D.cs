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

        private float _attackStartAt = -1f;
        private const float AttackDuration = 0.25f;

        public void TriggerAttackMotion()
        {
            _attackStartAt = Time.time;
        }

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
                // Climbing pose: both arms reaching up, alternating which hand
                // grips higher; legs make small stepping motions; body bobs.
                _phase += Time.deltaTime * walkCycleSpeed * 0.55f;
                float s = Mathf.Sin(_phase);
                // Arms held overhead (~ ±160°) with ±10° alternation so one
                // hand is always slightly higher than the other.
                targetLA =  160f + s * 12f;
                targetRA = -160f - s * 12f;
                // Legs alternate up/down by ~22° — feet planted on rungs
                targetLL = -s * 22f;
                targetRL =  s * 22f;
                // Small body bob — slight rise/fall as you grip rungs
                targetBob = s * 0.03f;
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

            // Attack motion overrides arm targets (legs/body keep their underlying motion)
            float attackElapsed = Time.time - _attackStartAt;
            if (_attackStartAt >= 0f && attackElapsed < AttackDuration)
            {
                float t = Mathf.Clamp01(attackElapsed / AttackDuration);
                // Two-stage swing: 0..0.4 wind-up back, 0.4..1 sweep forward
                float swingArc;
                if (t < 0.4f)
                {
                    float u = t / 0.4f;
                    swingArc = Mathf.Lerp(0f, -95f, u); // pull arm back
                }
                else
                {
                    float u = (t - 0.4f) / 0.6f;
                    // ease-out forward sweep
                    float ease = 1f - (1f - u) * (1f - u);
                    swingArc = Mathf.Lerp(-95f, 70f, ease);
                }
                // Right arm does the swing (matches starbolt origin offset +0.35x)
                targetRA = swingArc;
                // Left arm braces slightly back
                targetLA = Mathf.Lerp(targetLA, -15f, 0.7f);
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
