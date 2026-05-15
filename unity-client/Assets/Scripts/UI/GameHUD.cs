using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Astrion.Game;
using Astrion.Network;

namespace Astrion.UI
{
    public class GameHUD : MonoBehaviour
    {
        public static GameHUD Instance { get; private set; }
        public static bool IsChatFocused { get; private set; }

        [SerializeField] private Image hpFill;
        [SerializeField] private Image mpFill;
        [SerializeField] private Image expFill;
        [SerializeField] private Text hpText;
        [SerializeField] private Text mpText;
        [SerializeField] private Text expText;
        [SerializeField] private Text charNameText;
        [SerializeField] private Text charLevelText;
        [SerializeField] private Text coordsText;
        [SerializeField] private Text mapNameText;

        private Transform _player;

        private InputField _chatInputField;
        private Text _chatMessages;
        private Text _fpsText;
        private float _fpsTimer;
        private int _fpsCount;
        private string _myPlayerId;
        private const int MaxChatLines = 30;

        private void Awake()
        {
            Instance = this;
            IsChatFocused = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            IsChatFocused = false;
        }

        private void Start()
        {
            string charName = PlayerPrefs.GetString("characterName", "Adventurer");
            string charClass = PlayerPrefs.GetString("characterClass", "Warrior");
            if (charNameText) charNameText.text = charName;
            if (charLevelText) charLevelText.text = $"Lv.1 {charClass}";

            // Subscribe to live stat changes
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.OnChanged += UpdateBars;
            UpdateBars();

            bool isMobile = Application.isMobilePlatform;
            HideShow("JoystickArea", isMobile);
            HideShow("MobileJumpBtn", isMobile);
            HideShow("ChatPanel", !isMobile);

            var chatT = transform.Find("ChatPanel");
            if (chatT != null)
            {
                _chatInputField = chatT.Find("InputBar")?.GetComponent<InputField>();
                _chatMessages = chatT.Find("Messages")?.GetComponent<Text>();
            }
            _fpsText = transform.Find("FPSCounter")?.GetComponent<Text>();

            _myPlayerId = PlayerPrefs.GetString("playerId", "");

            // Map name from current scene
            if (mapNameText != null)
                mapNameText.text = GetMapDisplayName(SceneManager.GetActiveScene().name);
        }

        private static string GetMapDisplayName(string sceneName)
        {
            switch (sceneName)
            {
                case "MainScene":           return "바람의 등대섬";
                case "ForgottenWoodsScene": return "잊혀진 숲";
                default:                    return sceneName;
            }
        }

        public void AppendChatLine(string speakerId, string message)
        {
            if (_chatMessages == null) return;
            bool isMine = !string.IsNullOrEmpty(_myPlayerId) && speakerId == _myPlayerId;
            string label = isMine ? "You" : speakerId;
            string color = isMine ? "#ffd060" : "#a8d4ff";
            string line = $"\n<color={color}>[{label}]</color> {message}";
            _chatMessages.text += line;
            TrimChatBuffer();
        }

        private void TrimChatBuffer()
        {
            if (_chatMessages == null) return;
            string txt = _chatMessages.text;
            int newlines = 0;
            for (int i = 0; i < txt.Length; i++) if (txt[i] == '\n') newlines++;
            if (newlines <= MaxChatLines) return;
            int toRemove = newlines - MaxChatLines;
            int idx = 0;
            for (int i = 0; i < toRemove; i++)
            {
                int next = txt.IndexOf('\n', idx);
                if (next < 0) break;
                idx = next + 1;
            }
            _chatMessages.text = txt.Substring(idx);
        }

        private void HideShow(string objName, bool show)
        {
            var t = transform.Find(objName);
            if (t != null) t.gameObject.SetActive(show);
        }

        private void Update()
        {
            _fpsCount++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f)
            {
                if (_fpsText != null)
                    _fpsText.text = $"{Mathf.RoundToInt(_fpsCount / _fpsTimer)} FPS";
                _fpsTimer = 0; _fpsCount = 0;
            }

            if (Application.isMobilePlatform) return;
            HandleChatInput();
        }

        private void HandleChatInput()
        {
            if (_chatInputField == null) return;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (IsChatFocused)
                {
                    string msg = _chatInputField.text?.Trim();
                    if (!string.IsNullOrEmpty(msg))
                    {
                        var nm = NetworkManager.Instance;
                        if (nm != null && nm.IsConnected)
                        {
                            string payload = JsonUtility.ToJson(new ChatRequest { message = msg });
                            nm.SendPacket(PacketType.Chat, payload);
                        }
                        else if (_chatMessages != null)
                        {
                            _chatMessages.text += "\n<color=#ff8080>[System]</color> Not connected.";
                        }
                    }
                    _chatInputField.text = "";
                    _chatInputField.DeactivateInputField();
                    IsChatFocused = false;
                }
                else
                {
                    _chatInputField.ActivateInputField();
                    IsChatFocused = true;
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape) && IsChatFocused)
            {
                _chatInputField.text = "";
                _chatInputField.DeactivateInputField();
                IsChatFocused = false;
            }
        }

        [System.Serializable] private class ChatRequest { public string message; }

        private void LateUpdate()
        {
            if (_player == null)
            {
                var p = GameObject.Find("PlayerPrefab") ?? GameObject.Find("LocalPlayer");
                if (p != null) _player = p.transform;
                else return;
            }
            if (coordsText)
            {
                var pos = _player.position;
                coordsText.text = $"X: {pos.x:F1}  Y: {pos.y:F1}";
            }
        }

        private void UpdateBars()
        {
            var stats = PlayerStats.Instance;
            int curHp = stats != null ? stats.Hp : 100;
            int maxHp = stats != null ? stats.MaxHp : 100;
            int curMp = stats != null ? stats.Mp : 50;
            int maxMp = stats != null ? stats.MaxMp : 50;
            int curExp = stats != null ? stats.Exp : 0;
            int nextExp = stats != null ? stats.ExpForNextLevel(stats.Level) : 100;
            int level = stats != null ? stats.Level : 1;
            float expRatio = nextExp > 0 ? (float)curExp / nextExp : 0f;

            if (hpFill) hpFill.fillAmount = maxHp > 0 ? (float)curHp / maxHp : 0f;
            if (mpFill) mpFill.fillAmount = maxMp > 0 ? (float)curMp / maxMp : 0f;
            if (expFill) expFill.fillAmount = expRatio;
            if (hpText) hpText.text = $"{curHp}/{maxHp}";
            if (mpText) mpText.text = $"{curMp}/{maxMp}";
            if (expText) expText.text = $"{expRatio * 100f:F1}%";

            // Level badge + char level subtext (top-left CharPanel)
            string charClass = PlayerPrefs.GetString("characterClass", "Warrior");
            if (charLevelText) charLevelText.text = $"Lv.{level} {charClass}";
            // Find LvlBadge num if present
            var badgeNum = transform.Find("CharPanel/LvlBadge/Num")?.GetComponent<Text>();
            if (badgeNum != null) badgeNum.text = level.ToString();
        }
    }
}
