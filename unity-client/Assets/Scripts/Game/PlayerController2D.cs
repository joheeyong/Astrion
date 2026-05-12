using UnityEngine;

namespace Astrion.Game
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 13f;
        [SerializeField] private float climbSpeed = 4f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.18f;
        [SerializeField] private Joystick joystick;

        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private bool _isGrounded;
        private bool _onLadder;
        private bool _climbing;
        private float _baseGravity;
        private Collider2D _currentLadder;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 3f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _baseGravity = _rb.gravityScale;
            _sr = GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            ReadInput(out float h, out float v, out bool jumpPressed);
            CheckGround();

            if (_climbing)
            {
                _rb.gravityScale = 0f;
                _rb.velocity = new Vector2(h * moveSpeed * 0.6f, v * climbSpeed);

                if (!_onLadder)
                {
                    _climbing = false;
                    _rb.gravityScale = _baseGravity;
                }
                else if (jumpPressed)
                {
                    _climbing = false;
                    _rb.gravityScale = _baseGravity;
                    _rb.velocity = new Vector2(_rb.velocity.x, jumpForce * 0.85f);
                }
            }
            else
            {
                _rb.velocity = new Vector2(h * moveSpeed, _rb.velocity.y);

                if (_onLadder && Mathf.Abs(v) > 0.5f)
                {
                    _climbing = true;
                    Vector3 p = transform.position;
                    p.x = _currentLadder.bounds.center.x;
                    transform.position = p;
                }
                else if (jumpPressed && _isGrounded)
                {
                    _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
                }
            }

            if (_sr != null)
            {
                if (h < -0.1f) _sr.flipX = true;
                else if (h > 0.1f) _sr.flipX = false;
            }
        }

        private void ReadInput(out float h, out float v, out bool jump)
        {
            bool isMobile = Application.isMobilePlatform;
            if (isMobile && joystick != null)
            {
                h = joystick.Horizontal;
                v = joystick.Vertical;
                jump = false; // Mobile uses dedicated jump button (TBD)
            }
            else
            {
                h = Input.GetAxisRaw("Horizontal");
                v = Input.GetAxisRaw("Vertical");
                jump = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftAlt);
            }
        }

        private void CheckGround()
        {
            if (groundCheck == null)
            {
                _isGrounded = false;
                return;
            }
            _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<Ladder2D>() != null)
            {
                _onLadder = true;
                _currentLadder = other;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other == _currentLadder)
            {
                _onLadder = false;
                _climbing = false;
                _rb.gravityScale = _baseGravity;
                _currentLadder = null;
            }
        }

        public void SetJoystick(Joystick j) => joystick = j;
    }
}
