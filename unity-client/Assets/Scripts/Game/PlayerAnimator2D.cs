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
        private bool _attackBigSwing;
        private const float AttackDuration = 0.25f;
        private const float BigSwingDuration = 0.38f;

        public void TriggerAttackMotion(bool bigSwing = false)
        {
            _attackStartAt = Time.time;
            _attackBigSwing = bigSwing;
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
            float dur = _attackBigSwing ? BigSwingDuration : AttackDuration;
            float attackElapsed = Time.time - _attackStartAt;
            if (_attackStartAt >= 0f && attackElapsed < dur)
            {
                float t = Mathf.Clamp01(attackElapsed / dur);
                // Two-stage swing: 0..0.35 wind-up back, 0.35..1 sweep forward
                // Big swing uses much wider arc (135° back -> 120° forward = 255° total)
                float windAngle  = _attackBigSwing ? -135f : -95f;
                float swingAngle = _attackBigSwing ?  120f :  70f;
                float swingArc;
                if (t < 0.35f)
                {
                    float u = t / 0.35f;
                    // ease-in for the wind-up so it feels weighty
                    float ease = u * u;
                    swingArc = Mathf.Lerp(0f, windAngle, ease);
                }
                else
                {
                    float u = (t - 0.35f) / 0.65f;
                    // ease-out forward sweep — fast start, settle
                    float ease = 1f - (1f - u) * (1f - u) * (1f - u);
                    swingArc = Mathf.Lerp(windAngle, swingAngle, ease);
                }
                targetRA = swingArc;
                // For big swings the left arm also braces back further
                float leftBrace = _attackBigSwing ? -35f : -15f;
                targetLA = Mathf.Lerp(targetLA, leftBrace, 0.7f);
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
