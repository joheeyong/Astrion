using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Astrion.Network
{
    /// Detects unexpected disconnects, shows an overlay, and walks the client back
    /// to a usable state: TCP reconnect → re-LOGIN with cached credentials → ZONE_ENTER
    /// for the current scene. Only acts in-game (not on the Login / character-select scenes).
    public class ReconnectSystem : MonoBehaviour
    {
        public static ReconnectSystem Instance { get; private set; }

        [SerializeField] private int maxAttempts = 8;
        [SerializeField] private float attemptDelaySeconds = 2f;

        private Canvas _canvas;
        private CanvasGroup _group;
        private Text _statusText;
        private bool _busy;

        // login-result handshake plumbing
        private bool _loginAcked;
        private bool _loginOk;
        private string _loginMessage;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildOverlay();
        }

        private void Start()
        {
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnDisconnected += OnDisconnected;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnDisconnected -= OnDisconnected;
            if (Instance == this) Instance = null;
        }

        private void BuildOverlay()
        {
            var canvasGo = new GameObject("ReconnectCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9500; // above HUD, below DeathSystem
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

            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(groupGo.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.78f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var titleGo = new GameObject("Status");
            titleGo.transform.SetParent(groupGo.transform, false);
            var tRT = titleGo.AddComponent<RectTransform>();
            tRT.anchorMin = tRT.anchorMax = new Vector2(0.5f, 0.5f);
            tRT.pivot = new Vector2(0.5f, 0.5f);
            tRT.sizeDelta = new Vector2(800, 60);
            _statusText = titleGo.AddComponent<Text>();
            _statusText.font = font;
            _statusText.fontSize = 26;
            _statusText.fontStyle = FontStyle.Bold;
            _statusText.color = new Color(0.96f, 0.92f, 0.78f);
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.text = "재접속 중...";
        }

        private void OnDisconnected()
        {
            if (_busy) return;
            // Kicked-by-other-login: don't try to reconnect — the server
            // would only kick us right back as soon as we re-auth. Show
            // an explanatory modal and drop the player on the login screen.
            if (NetworkManager.KickedByOtherLogin)
            {
                ShowKickedOverlay(NetworkManager.KickedReason);
                StartCoroutine(ReturnToLoginAfterDelay());
                return;
            }
            if (!IsInGameScene()) return; // ignore disconnects on login/char-select; their UI handles it
            if (!SessionCredentials.HasCredentials)
            {
                ShowOverlay("연결이 끊겼습니다.");
                return;
            }
            StartCoroutine(ReconnectFlow());
        }

        private void ShowKickedOverlay(string reason)
        {
            string msg = string.IsNullOrEmpty(reason)
                ? "다른 곳에서 로그인되어 연결이 해제되었습니다."
                : reason;
            ShowOverlay("★  " + msg + "\n\n3초 후 로그인 화면으로 돌아갑니다.");
        }

        private IEnumerator ReturnToLoginAfterDelay()
        {
            _busy = true;
            yield return new WaitForSecondsRealtime(3f);
            NetworkManager.KickedByOtherLogin = false; // consume so a future fresh login isn't blocked
            NetworkManager.KickedReason = "";
            HideOverlay();
            _busy = false;
            SceneManager.LoadScene("LoginScene");
        }

        // (HideOverlay already defined below — kicked path reuses it.)

        private static bool IsInGameScene()
        {
            string n = SceneManager.GetActiveScene().name;
            return n == "MainScene" || n == "ForgottenWoodsScene" || n == "CitadelOfDawnScene";
        }

        private IEnumerator ReconnectFlow()
        {
            _busy = true;
            ShowOverlay("연결이 끊겼습니다. 재접속 중...");

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                _statusText.text = $"재접속 중...  ({attempt} / {maxAttempts})";

                // Brief pause between attempts
                yield return new WaitForSecondsRealtime(attemptDelaySeconds);

                // Try TCP connect (NetworkManager.Connect runs its own retry loop internally,
                // but we want our own granular attempt counter here)
                if (NetworkManager.Instance == null) { _busy = false; yield break; }
                if (!NetworkManager.Instance.IsConnected)
                    NetworkManager.Instance.Connect();

                // Wait up to 3s for the connection
                float wait = 0f;
                while (!NetworkManager.Instance.IsConnected && wait < 3f)
                {
                    wait += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (!NetworkManager.Instance.IsConnected) continue;

                // Connected — re-LOGIN
                _statusText.text = "재접속 중... 로그인";
                _loginAcked = false; _loginOk = false; _loginMessage = "";
                NetworkManager.Instance.OnPacketReceived += LoginAckHandler;
                string payload = JsonUtility.ToJson(new LoginReq {
                    username = SessionCredentials.Username,
                    password = SessionCredentials.Password,
                    isRegister = false,
                    clientVersion = Version.Current,
                });
                NetworkManager.Instance.SendPacket(PacketType.Login, payload);

                float loginWait = 0f;
                while (!_loginAcked && loginWait < 5f)
                {
                    loginWait += Time.unscaledDeltaTime;
                    yield return null;
                }
                NetworkManager.Instance.OnPacketReceived -= LoginAckHandler;
                if (!_loginAcked || !_loginOk)
                {
                    Debug.LogWarning($"[Reconnect] login failed: {_loginMessage}");
                    NetworkManager.Instance.Disconnect();
                    continue;
                }

                // Re-enter the current zone so the server adds us back to the snapshot
                _statusText.text = "재접속 중... 월드 복귀";
                string scene = SceneManager.GetActiveScene().name;
                string zoneId = SceneToZone(scene);
                string nickname = PlayerPrefs.GetString("characterName", "");
                string nickPart = string.IsNullOrEmpty(nickname) ? "" : ",\"nickname\":\"" + Escape(nickname) + "\"";
                string zonePayload = "{\"zoneId\":\"" + zoneId + "\"" + nickPart + "}";
                NetworkManager.Instance.SendPacket(PacketType.ZoneEnter, zonePayload);

                // Request fresh state too (server may have queued saves we missed)
                PlayerStateManager.Instance?.RequestLoad();

                yield return new WaitForSecondsRealtime(0.4f);
                HideOverlay();
                Astrion.UI.ToastUI.Instance?.Show("재접속됨", new Color(0.40f, 0.85f, 0.40f));
                _busy = false;
                yield break;
            }

            // All attempts failed
            _statusText.text = "재접속 실패. 로그인 화면으로 이동합니다.";
            yield return new WaitForSecondsRealtime(2f);
            HideOverlay();
            _busy = false;
            SceneManager.LoadScene("LoginScene");
        }

        private void LoginAckHandler(GamePacket packet)
        {
            if (packet.Type != PacketType.LoginResult) return;
            try
            {
                var r = JsonUtility.FromJson<LoginRes>(packet.Payload);
                _loginOk = r != null && r.success;
                _loginMessage = r != null ? r.message : "(no payload)";
            }
            catch { _loginOk = false; _loginMessage = "(parse error)"; }
            _loginAcked = true;
        }

        // Delegates to SceneZoneMap so the worldmap mapping lives in exactly
        // one file (see SceneZoneMap.cs).
        private static string SceneToZone(string sceneName) => SceneZoneMap.SceneToZone(sceneName);

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private void ShowOverlay(string msg)
        {
            if (_group == null) return;
            _statusText.text = msg;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
        }

        private void HideOverlay()
        {
            if (_group == null) return;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        [System.Serializable] private class LoginReq { public string username; public string password; public bool isRegister; public string clientVersion; }
        [System.Serializable] private class LoginRes { public bool success; public string playerId; public string message; }
    }
}
