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
        private bool _wasGrounded;
        private bool _onLadder;
        private bool _climbing;
        private float _baseGravity;
        private Collider2D _currentLadder;
        private bool _facingRight = true;

        public bool IsGrounded => _isGrounded;
        public bool IsClimbing => _climbing;
        public bool FacingRight => _facingRight;

        private int _airJumpsRemaining;

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
                // Horizontal control only on the ground — once airborne, the
                // takeoff velocity is locked (no mid-air strafing).
                if (_isGrounded)
                {
                    _rb.velocity = new Vector2(h * moveSpeed, _rb.velocity.y);
                }

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
                    SpawnDust(spread: 0.30f, drift: -_rb.velocity.x * 0.15f);
                    // Refresh air jumps when leaving ground
                    int doubleJumpLv = SkillSystem.Instance != null
                        ? SkillSystem.Instance.GetLevel("double_jump") : 0;
                    _airJumpsRemaining = doubleJumpLv; // Lv1 = 1 extra, Lv2 = 2, Lv3 = 3
                }
                else if (jumpPressed && !_isGrounded
                         && SkillSystem.Instance != null
                         && SkillSystem.Instance.IsLearned("double_jump")
                         && _airJumpsRemaining > 0)
                {
                    // Route through SkillCaster so MP/cooldown are honored
                    SkillCaster.Instance?.Cast("double_jump");
                }
            }

            // Landing detection — air → ground transition this frame
            if (_isGrounded && !_wasGrounded)
            {
                SpawnDust(spread: 0.40f, drift: 0f);
                _airJumpsRemaining = 0; // reset on landing
            }
            _wasGrounded = _isGrounded;

            // Facing also locked while airborne — don't flip mid-jump
            if (_isGrounded || _climbing)
            {
                if (h < -0.1f) SetFacing(false);
                else if (h > 0.1f) SetFacing(true);
            }

            // Skills (suppressed while typing in chat or while a blocking modal is open)
            bool sysOpen = Astrion.UI.SystemMenuUI.Instance != null && Astrion.UI.SystemMenuUI.Instance.IsOpen;
            bool shopOpen = Astrion.UI.ShopUI.Instance != null && Astrion.UI.ShopUI.Instance.IsOpen;
            if (!Astrion.UI.GameHUD.IsChatFocused && !sysOpen && !shopOpen)
            {
                // Q = class-specific quick-cast (Warrior swings the sword; others fire starbolt)
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    string cls = PlayerPrefs.GetString("characterClass", "");
                    if (cls == "Warrior") SkillCaster.Instance?.Cast("sword_slash");
                    else                  SkillCaster.Instance?.Cast("starbolt");
                }
                // 1~5 = hotbar slots
                if      (Input.GetKeyDown(KeyCode.Alpha1)) HotbarSystem.Instance?.TryTrigger(0);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) HotbarSystem.Instance?.TryTrigger(1);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) HotbarSystem.Instance?.TryTrigger(2);
                else if (Input.GetKeyDown(KeyCode.Alpha4)) HotbarSystem.Instance?.TryTrigger(3);
                else if (Input.GetKeyDown(KeyCode.Alpha5)) HotbarSystem.Instance?.TryTrigger(4);

                // Z = toggle auto-pickup
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    bool now = !AutoPickup.Enabled;
                    AutoPickup.Enabled = now;
                    Astrion.UI.ToastUI.Instance?.Show(
                        $"자동 줍기 {(now ? "ON" : "OFF")}",
                        now ? new Color(0.40f, 0.85f, 0.40f) : new Color(0.78f, 0.72f, 0.55f));
                }

                // Quick consumables: R = HP potion, F = MP potion
                if (Input.GetKeyDown(KeyCode.R))
                {
                    var inv = InventorySystem.Instance;
                    if (inv != null && !inv.UseFirstConsumable(forHp: true))
                        Astrion.UI.ToastUI.Instance?.Show("회복 아이템이 없거나 HP가 가득합니다.", new Color(0.85f, 0.55f, 0.40f));
                }
                if (Input.GetKeyDown(KeyCode.F))
                {
                    var inv = InventorySystem.Instance;
                    if (inv != null && !inv.UseFirstConsumable(forHp: false))
                        Astrion.UI.ToastUI.Instance?.Show("MP 아이템이 없거나 MP가 가득합니다.", new Color(0.55f, 0.70f, 0.85f));
                }
            }
        }

        // Called by SkillCaster (MP/cooldown already checked). Returns false if prefab missing.
        public bool FireStarBoltExternal()
        {
            if (starBoltPrefab == null) return false;
            Vector3 origin = transform.position + new Vector3(_facingRight ? 0.35f : -0.35f, 0.15f, 0f);
            int dir = _facingRight ? 1 : -1;

            // Trigger arm-swing animation
            var animator = GetComponent<PlayerAnimator2D>();
            if (animator != null) animator.TriggerAttackMotion();

            var go = Instantiate(starBoltPrefab, origin, Quaternion.identity);
            go.SetActive(true);
            var bolt = go.GetComponent<StarBolt2D>();
            if (bolt != null) bolt.Init(dir, FindHomingTarget(origin));

            // Archer: swap the star projectile sprite for an arrow visual
            string cls = PlayerPrefs.GetString("characterClass", "");
            if (cls == "Archer")
            {
                var sr = go.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.sprite = GetArrowSprite();
            }

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
                bool shopOpen2 = Astrion.UI.ShopUI.Instance != null && Astrion.UI.ShopUI.Instance.IsOpen;
                if (Astrion.UI.GameHUD.IsChatFocused || sysMenuOpen || shopOpen2)
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

        public bool TryAirJump()
        {
            if (_isGrounded || _airJumpsRemaining <= 0) return false;
            _rb.velocity = new Vector2(_rb.velocity.x, jumpForce * 0.85f);
            _airJumpsRemaining--;
            SpawnDust(spread: 0.30f, drift: 0f);
            return true;
        }

        public void StartDashOrTeleport(float distance, float dir, bool instant)
        {
            if (instant)
            {
                Vector3 startPos = transform.position;
                Vector3 endPos = startPos + new Vector3(dir * distance, 0f, 0f);
                // Puff at both endpoints
                DustPuff2D.Spawn(startPos + new Vector3(0f, -0.3f, 0f));
                transform.position = endPos;
                DustPuff2D.Spawn(endPos + new Vector3(0f, -0.3f, 0f));
                Camera2D.Shake(0.12f, 0.10f);
            }
            else
            {
                StartCoroutine(DashCoroutine(distance, dir));
            }
        }

        private System.Collections.IEnumerator DashCoroutine(float distance, float dir)
        {
            float duration = 0.18f;
            float traveled = 0f;
            float speed = distance / duration;
            float dustTimer = 0f;
            float origGravity = _rb.gravityScale;
            _rb.gravityScale = 0f; // keep dash horizontal
            _rb.velocity = new Vector2(dir * speed, 0f);
            while (traveled < distance)
            {
                float step = speed * Time.deltaTime;
                traveled += step;
                dustTimer += Time.deltaTime;
                if (dustTimer > 0.04f)
                {
                    DustPuff2D.Spawn(transform.position + new Vector3(0f, -0.3f, 0f), -dir * 0.8f);
                    dustTimer = 0f;
                }
                yield return null;
            }
            _rb.gravityScale = origGravity;
            _rb.velocity = new Vector2(0f, _rb.velocity.y); // stop horizontal momentum
        }

        private static Sprite _arrowSprite;
        private static Sprite GetArrowSprite()
        {
            if (_arrowSprite != null) return _arrowSprite;
            int w = 36, h = 10;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color shaft = new Color(0.62f, 0.45f, 0.25f);
            Color shaftDark = new Color(0.40f, 0.28f, 0.14f);
            Color tip = new Color(0.85f, 0.85f, 0.92f);
            Color tipDark = new Color(0.30f, 0.30f, 0.35f);
            Color fletch = new Color(0.95f, 0.55f, 0.40f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, clear);

            int midY = h / 2;
            // Shaft (2 px thick brown line)
            for (int x = 6; x < w - 8; x++)
            {
                tex.SetPixel(x, midY,     shaft);
                tex.SetPixel(x, midY - 1, shaftDark);
            }
            // Arrowhead — pointed triangle on the right
            for (int dy = 0; dy < 4; dy++)
            {
                int xStart = w - 8 + dy;
                int xEnd = Mathf.Min(w - 1, w - 4 + dy);
                for (int x = xStart; x <= xEnd; x++)
                {
                    int yUp = midY + dy;
                    int yDn = midY - 1 - dy;
                    if (yUp >= 0 && yUp < h) tex.SetPixel(x, yUp, tip);
                    if (yDn >= 0 && yDn < h) tex.SetPixel(x, yDn, tip);
                }
            }
            // Tip outline
            for (int dy = 0; dy < 5; dy++)
            {
                int x = w - 1 - dy;
                if (x >= 0 && midY + dy < h) tex.SetPixel(x, midY + dy, tipDark);
                if (x >= 0 && midY - 1 - dy >= 0) tex.SetPixel(x, midY - 1 - dy, tipDark);
            }
            // Fletchings — three slanted stripes on the left tail
            for (int i = 0; i < 5; i++)
            {
                int x = i;
                int yUp = midY + i;
                int yDn = midY - 1 - i;
                if (yUp < h) tex.SetPixel(x, yUp, fletch);
                if (yDn >= 0) tex.SetPixel(x, yDn, fletch);
            }
            tex.Apply();
            _arrowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
            return _arrowSprite;
        }

        private void SpawnDust(float spread, float drift)
        {
            Vector3 feet = groundCheck != null
                ? groundCheck.position
                : transform.position + new Vector3(0f, -0.42f, 0f);
            // Two puffs slightly offset on either side
            DustPuff2D.Spawn(feet + new Vector3(-spread * 0.5f, 0f, 0f), -drift);
            DustPuff2D.Spawn(feet + new Vector3( spread * 0.5f, 0f, 0f),  drift);
        }
    }
}
