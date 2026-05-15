using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Astrion.Game
{
    /// Handles HP=0 death: shows a fade-in death overlay, then respawns the player.
    public class DeathSystem : MonoBehaviour
    {
        public static DeathSystem Instance { get; private set; }

        [SerializeField] private float respawnDelay = 3.0f;
        [SerializeField] private string respawnScene = "MainScene";

        private Canvas _canvas;
        private CanvasGroup _group;
        private Image _bg;
        private Text _title;
        private Text _subtitle;
        private bool _busy;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
        }

        private void Start()
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnDied += OnDied;
        }

        private void OnDestroy()
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnDied -= OnDied;
            if (Instance == this) Instance = null;
        }

        private void BuildOverlay()
        {
            var canvasGo = new GameObject("DeathCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9000;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var groupGo = new GameObject("Group");
            groupGo.transform.SetParent(canvasGo.transform, false);
            var groupRT = groupGo.AddComponent<RectTransform>();
            groupRT.anchorMin = Vector2.zero; groupRT.anchorMax = Vector2.one;
            groupRT.offsetMin = groupRT.offsetMax = Vector2.zero;
            _group = groupGo.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(groupGo.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            _bg = bgGo.AddComponent<Image>();
            _bg.color = new Color(0f, 0f, 0f, 0.85f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(groupGo.transform, false);
            var titleRT = titleGo.AddComponent<RectTransform>();
            titleRT.anchorMin = titleRT.anchorMax = new Vector2(0.5f, 0.5f);
            titleRT.pivot = new Vector2(0.5f, 0.5f);
            titleRT.anchoredPosition = new Vector2(0, 24);
            titleRT.sizeDelta = new Vector2(800, 80);
            _title = titleGo.AddComponent<Text>();
            _title.font = font;
            _title.fontSize = 56;
            _title.fontStyle = FontStyle.Bold;
            _title.color = new Color(0.95f, 0.30f, 0.30f);
            _title.alignment = TextAnchor.MiddleCenter;
            _title.text = "사 망";

            var subGo = new GameObject("Subtitle");
            subGo.transform.SetParent(groupGo.transform, false);
            var subRT = subGo.AddComponent<RectTransform>();
            subRT.anchorMin = subRT.anchorMax = new Vector2(0.5f, 0.5f);
            subRT.pivot = new Vector2(0.5f, 0.5f);
            subRT.anchoredPosition = new Vector2(0, -28);
            subRT.sizeDelta = new Vector2(600, 40);
            _subtitle = subGo.AddComponent<Text>();
            _subtitle.font = font;
            _subtitle.fontSize = 18;
            _subtitle.color = new Color(0.85f, 0.78f, 0.55f);
            _subtitle.alignment = TextAnchor.MiddleCenter;
            _subtitle.text = "잠시 후 등대섬에서 깨어납니다...";
        }

        private void OnDied()
        {
            if (_busy) return;
            StartCoroutine(DeathFlow());
        }

        private IEnumerator DeathFlow()
        {
            _busy = true;
            _group.blocksRaycasts = true;

            // Fade in
            float t = 0f;
            float fadeIn = 0.5f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            _group.alpha = 1f;

            // Hold
            float hold = Mathf.Max(0f, respawnDelay - fadeIn);
            yield return new WaitForSecondsRealtime(hold);

            // Respawn: load scene + restore HP/MP
            if (SceneManager.GetActiveScene().name != respawnScene)
            {
                SceneManager.LoadScene(respawnScene);
            }
            PlayerStats.Instance?.RespawnRestore();

            // Fade out
            t = 0f;
            float fadeOut = 0.6f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Clamp01(1f - t / fadeOut);
                yield return null;
            }
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _busy = false;
        }
    }
}
