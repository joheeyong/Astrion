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
        [SerializeField] private GameObject starBoltPrefab;
        [SerializeField] private float skillCooldown = 0.45f;
        [SerializeField] private float homingRange = 9f;

        private float _lastSkillTime = -10f;

        private Rigidbody2D _rb;
        private Transform _spriteContainer;
        private bool _isGrounded;
        private bool _onLadder;
        private bool _climbing;
        private float _baseGravity;
        private Collider2D _currentLadder;
        private bool _facingRight = true;

        public bool IsGrounded => _isGrounded;
        public bool IsClimbing => _climbing;
        public bool FacingRight => _facingRight;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 3f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _baseGravity = _rb.gravityScale;
            _spriteContainer = transform.Find("SpriteContainer");
        }

        private void Update()
        {
            // Fall-out-of-world death (only fires once; PlayerStats guards re-entry)
            if (transform.position.y < -15f)
            {
                PlayerStats.Instance?.ApplyDamage(99999);
            }

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

            if (h < -0.1f) SetFacing(false);
            else if (h > 0.1f) SetFacing(true);

            // Skills (suppressed while typing in chat or while system menu is open)
            bool sysOpen = Astrion.UI.SystemMenuUI.Instance != null && Astrion.UI.SystemMenuUI.Instance.IsOpen;
            if (!Astrion.UI.GameHUD.IsChatFocused && !sysOpen)
            {
                // Q = quick-cast starbolt (always)
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    SkillCaster.Instance?.Cast("starbolt");
                }
                // 1~5 = hotbar slots
                if      (Input.GetKeyDown(KeyCode.Alpha1)) HotbarSystem.Instance?.TryTrigger(0);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) HotbarSystem.Instance?.TryTrigger(1);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) HotbarSystem.Instance?.TryTrigger(2);
                else if (Input.GetKeyDown(KeyCode.Alpha4)) HotbarSystem.Instance?.TryTrigger(3);
                else if (Input.GetKeyDown(KeyCode.Alpha5)) HotbarSystem.Instance?.TryTrigger(4);
            }
        }

        // Called by SkillCaster (MP/cooldown already checked). Returns false if prefab missing.
        public bool FireStarBoltExternal()
        {
            if (starBoltPrefab == null) return false;
            Vector3 origin = transform.position + new Vector3(_facingRight ? 0.35f : -0.35f, 0.15f, 0f);
            int dir = _facingRight ? 1 : -1;

            var go = Instantiate(starBoltPrefab, origin, Quaternion.identity);
            go.SetActive(true);
            var bolt = go.GetComponent<StarBolt2D>();
            if (bolt != null) bolt.Init(dir, FindHomingTarget(origin));

            var nm = Astrion.Network.NetworkManager.Instance;
            if (nm != null && nm.IsConnected)
            {
                string payload = "{\"x\":" + origin.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                    + ",\"y\":" + origin.y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                    + ",\"dir\":" + dir + ",\"type\":\"starbolt\"}";
                nm.SendPacket(Astrion.Network.PacketType.SkillCast, payload);
            }
            return true;
        }

        // Meteor placeholder visual: fan out 8 visual-only starbolts upward+forward
        public void FireMeteorVisualBurst(int count)
        {
            if (starBoltPrefab == null) return;
            Vector3 origin = transform.position + new Vector3(0f, 0.25f, 0f);
            float facing = _facingRight ? 1f : -1f;
            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(starBoltPrefab, origin + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0f, 0.4f), 0f), Quaternion.identity);
                go.SetActive(true);
                var b = go.GetComponent<StarBolt2D>();
                if (b != null) b.Init(facing, null, visualOnly: true);
            }
        }

        // Heal placeholder visual: 4 visual bolts rising from feet
        public void FireHealVisualBurst()
        {
            if (starBoltPrefab == null) return;
            Vector3 origin = transform.position + new Vector3(0f, -0.3f, 0f);
            for (int i = 0; i < 4; i++)
            {
                var go = Instantiate(starBoltPrefab, origin + new Vector3(Random.Range(-0.25f, 0.25f), 0f, 0f), Quaternion.identity);
                go.SetActive(true);
                var b = go.GetComponent<StarBolt2D>();
                if (b != null) b.Init(0.01f, null, visualOnly: true);
            }
        }

        private Transform FindHomingTarget(Vector2 origin)
        {
            var monsters = Object.FindObjectsOfType<ServerMonster2D>();
            Transform best = null;
            float bestDist = float.MaxValue;
            float facing = _facingRight ? 1f : -1f;
            foreach (var m in monsters)
            {
                if (m == null) continue;
                Vector2 to = (Vector2)m.transform.position - origin;
                if (to.x * facing < 0f) continue; // not in front
                float d = to.magnitude;
                if (d > homingRange) continue;
                if (d < bestDist) { bestDist = d; best = m.transform; }
            }
            return best;
        }

        private void SetFacing(bool right)
        {
            if (_facingRight == right) return;
            _facingRight = right;
            if (_spriteContainer != null)
            {
                var s = _spriteContainer.localScale;
                s.x = right ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
                _spriteContainer.localScale = s;
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
                bool sysMenuOpen = Astrion.UI.SystemMenuUI.Instance != null && Astrion.UI.SystemMenuUI.Instance.IsOpen;
                if (Astrion.UI.GameHUD.IsChatFocused || sysMenuOpen)
                {
                    h = 0; v = 0; jump = false;
                    return;
                }
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
