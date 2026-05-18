using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Astrion.Game
{
    /// Subscribes to PlayerStats.OnLeveledUp and plays a gold screen flash
    /// plus a radial burst of star sprites around the local player.
    public class LevelUpEffect : MonoBehaviour
    {
        public static LevelUpEffect Instance { get; private set; }

        [SerializeField] private int burstCount = 14;
        [SerializeField] private float flashDuration = 0.55f;

        private Canvas _canvas;
        private CanvasGroup _group;
        private Image _flash;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
        }

        private void Start()
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnLeveledUp += OnLeveled;
        }

        private void OnDestroy()
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnLeveledUp -= OnLeveled;
            if (Instance == this) Instance = null;
        }

        private void BuildOverlay()
        {
            var canvasGo = new GameObject("LevelUpFlashCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 8000; // below DeathSystem (9000), above HUD (10)
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var bgGo = new GameObject("Flash");
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            _group = bgGo.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _flash = bgGo.AddComponent<Image>();
            _flash.color = new Color(1f, 0.85f, 0.30f, 1f);
            _flash.raycastTarget = false;
        }

        private void OnLeveled()
        {
            StartCoroutine(FlashCoroutine());
            SpawnParticleBurst();
        }

        private IEnumerator FlashCoroutine()
        {
            // Fast peak (gold), slower fade
            float t = 0f;
            float peak = 0.10f;
            float fade = flashDuration - peak;
            while (t < peak)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Clamp01(t / peak) * 0.55f; // cap < full so HUD remains visible
                yield return null;
            }
            t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Clamp01(0.55f * (1f - t / fade));
                yield return null;
            }
            _group.alpha = 0f;
        }

        private void SpawnParticleBurst()
        {
            var player = Object.FindObjectOfType<PlayerController2D>();
            if (player == null) return;
            // Reuse the star bolt prefab — we don't need a new sprite for this.
            // Find any active StarBoltPrefab in the scene (created per game scene).
            var template = GameObject.Find("StarBoltPrefab");
            if (template == null) return;

            Vector3 origin = player.transform.position + new Vector3(0f, 0.4f, 0f);
            for (int i = 0; i < burstCount; i++)
            {
                float ang = (i / (float)burstCount) * Mathf.PI * 2f;
                Vector3 off = new Vector3(Mathf.Cos(ang) * 0.15f, Mathf.Sin(ang) * 0.15f, 0f);
                var go = Instantiate(template, origin + off, Quaternion.identity);
                go.SetActive(true);
                var bolt = go.GetComponent<StarBolt2D>();
                if (bolt != null)
                {
                    // Slight angle: use horizontal sign as the bolt's flight direction
                    float dir = Mathf.Cos(ang);
                    if (Mathf.Abs(dir) < 0.01f) dir = 0.01f * Mathf.Sign(Random.Range(-1f, 1f));
                    bolt.Init(dir, null, visualOnly: true);
                }
            }
        }
    }
}
