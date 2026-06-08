using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astrion.Game
{
    /// Tiny companion that floats next to the player and auto-fetches nearby
    /// drops. Visualises what AutoPickup is doing (previously items vanished
    /// silently when touched) and extends the effective pickup reach: the
    /// pet darts out to ~5 units, claims the loot, drifts back.
    ///
    /// DDOL singleton — built once on the login scene boot path with the
    /// rest of the persistent UI/system layer. Pet visual is procedurally
    /// generated (small filled circle, gold-tinted) so no asset import is
    /// needed.
    public class PetSystem : MonoBehaviour
    {
        public static PetSystem Instance { get; private set; }

        // Player-tunable, but bound to PlayerPrefs so the choice survives
        // restarts. Toggle with V (V for "vassal"... 'vampire' is taken).
        private const string PrefVisible = "petVisible";

        // Tuning — kept here so a designer can iterate without digging into
        // multiple game objects.
        private const float FollowOffsetX = 0.9f;     // pet sits slightly behind
        private const float FollowOffsetY = 0.55f;    // and above the head
        private const float FollowLerp    = 7.5f;    // higher = snappier
        private const float ScanInterval  = 0.30f;    // seconds between drop scans
        private const float DetectRadius  = 5.0f;     // how far the pet sees drops
        private const float ReachRadius   = 0.55f;    // close enough to claim
        private const float ChaseSpeed    = 9.5f;
        private const float BobAmplitude  = 0.05f;
        private const float BobSpeed      = 3.6f;

        public static bool Visible
        {
            get => PlayerPrefs.GetInt(PrefVisible, 1) == 1;
            set { PlayerPrefs.SetInt(PrefVisible, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        private GameObject _petGo;
        private SpriteRenderer _petSr;
        private Transform _player;
        private float _nextScan;
        private NetworkDrop2D _chasing;
        private float _phase;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildPet();
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            if (Instance == this) Instance = null;
        }

        private void OnSceneChanged(Scene prev, Scene next)
        {
            // Hide on non-game scenes — login/character-select don't need a
            // floating fox in the corner. Re-enable on actual gameplay scenes.
            string n = next.name;
            bool gameScene = n != "LoginScene"
                          && n != "CharacterSelectScene"
                          && n != "CharacterCreateScene";
            if (_petGo != null) _petGo.SetActive(gameScene && Visible);
            _player = null; // will re-resolve in Update
            _chasing = null;
        }

        private void BuildPet()
        {
            _petGo = new GameObject("Pet_StarlightFox");
            DontDestroyOnLoad(_petGo);
            _petGo.transform.localScale = Vector3.one * 0.55f;
            _petSr = _petGo.AddComponent<SpriteRenderer>();
            _petSr.sprite = MakePetSprite();
            _petSr.color = new Color(0.95f, 0.85f, 0.42f);
            _petSr.sortingOrder = 5;
        }

        private static Sprite MakePetSprite()
        {
            // 24x24 soft circle with a brighter inner ring — reads as a
            // glowing orb at this scale. Cached on the texture so the GC
            // doesn't churn frame to frame.
            const int size = 24;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                float d = Mathf.Sqrt(dx*dx + dy*dy);
                float r = d / c;
                float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.70f, 1f, r));
                // Brighter inner ring → 'core' look.
                float core = 1f - Mathf.SmoothStep(0f, 0.45f, r);
                Color col = new Color(1f, 0.95f + 0.05f * core, 0.75f + 0.20f * core, a);
                tex.SetPixel(x, y, col);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
        }

        private void Update()
        {
            if (_petGo == null) return;
            if (!Visible) { if (_petGo.activeSelf) _petGo.SetActive(false); return; }

            // Find the local player. Same pattern GameHUD uses to bind to
            // it lazily, so spawn ordering doesn't matter.
            if (_player == null)
            {
                var p = GameObject.Find("PlayerPrefab") ?? GameObject.Find("LocalPlayer");
                if (p == null) { _petGo.SetActive(false); return; }
                _player = p.transform;
                _petGo.SetActive(true);
                _petGo.transform.position = _player.position + Vector3.up * 0.8f;
            }

            // Idle pickup AI — periodically scan for nearby drops to chase.
            // While chasing, hold the target until claimed or out of range.
            if (AutoPickup.Enabled && _chasing == null && Time.time >= _nextScan)
            {
                _nextScan = Time.time + ScanInterval;
                _chasing = FindNearestUnclaimedDrop();
            }
            if (_chasing != null && (!_chasing.gameObject.activeInHierarchy))
                _chasing = null;

            Vector3 currentPos = _petGo.transform.position;
            Vector3 nextPos;
            if (_chasing != null)
            {
                Vector3 dropPos = _chasing.transform.position;
                nextPos = Vector3.MoveTowards(currentPos, dropPos, ChaseSpeed * Time.deltaTime);
                if (Vector2.Distance(nextPos, dropPos) <= ReachRadius)
                {
                    Astrion.Network.DropNetworkManager.Instance?.SendClaim(_chasing.DropId);
                    _chasing = null;
                }
            }
            else
            {
                var pc = _player.GetComponent<PlayerController2D>();
                int sign = (pc != null && pc.FacingRight) ? -1 : 1;
                Vector3 desired = _player.position
                    + new Vector3(sign * FollowOffsetX, FollowOffsetY, 0);
                nextPos = Vector3.Lerp(currentPos, desired, FollowLerp * Time.deltaTime);
            }

            // Small idle bob so the pet reads as 'alive' even when stationary.
            _phase += BobSpeed * Time.deltaTime;
            nextPos.y += Mathf.Sin(_phase) * BobAmplitude;
            // Keep on a fixed Z plane so it composites between background
            // and player without z-fighting.
            nextPos.z = 0f;
            _petGo.transform.position = nextPos;

            // V toggles visibility — suppressed while a text input is focused
            // so chat doesn't kill the fox.
            if (Input.GetKeyDown(KeyCode.V) && !Astrion.UI.GameHUD.IsChatFocused)
            {
                Visible = !Visible;
                _petGo.SetActive(Visible);
                Astrion.UI.ToastUI.Instance?.Show(
                    Visible ? "★ 별빛 여우 등장" : "★ 별빛 여우 숨김",
                    new Color(0.95f, 0.82f, 0.35f));
            }
        }

        private NetworkDrop2D FindNearestUnclaimedDrop()
        {
            if (_player == null) return null;
            var drops = Object.FindObjectsOfType<NetworkDrop2D>();
            if (drops == null || drops.Length == 0) return null;
            NetworkDrop2D best = null;
            float bestDist = DetectRadius;
            Vector2 origin = _player.position;
            foreach (var d in drops)
            {
                if (d == null) continue;
                // Inventory-room gate — if we can't carry it, don't chase.
                // Touch-pickup respects HasFreeSlotFor; the pet should too.
                var inv = InventorySystem.Instance;
                if (inv != null && !inv.HasFreeSlotFor(d.ItemId)) continue;
                float dist = Vector2.Distance(origin, d.transform.position);
                if (dist < bestDist) { bestDist = dist; best = d; }
            }
            return best;
        }
    }
}
