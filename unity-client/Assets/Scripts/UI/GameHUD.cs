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
        [SerializeField] private Text goldText;
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
                case "CitadelOfDawnScene":  return "여명의 성채";
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

        /// Whisper line — purple to distinguish from zone chat. Direction
        /// is encoded in the label: 'from→to'. Either side of the arrow
        /// can be 'You' depending on who's reading.
        public void AppendWhisperLine(string from, string to, string message)
        {
            if (_chatMessages == null) return;
            string color = "#d9a0ff";
            string line = $"\n<color={color}>[{from}→{to}]</color> {message}";
            _chatMessages.text += line;
            TrimChatBuffer();
        }

        /// External entry point — used by FriendsUI's whisper shortcut. Drops
        /// the supplied text into the chat input and focuses it so the player
        /// can finish the message.
        public void FocusChatWith(string prefill)
        {
            if (_chatInputField == null) return;
            _chatInputField.text = prefill ?? "";
            _chatInputField.ActivateInputField();
            _chatInputField.caretPosition = _chatInputField.text.Length;
            IsChatFocused = true;
        }

        public void AppendSystemLine(string message)
        {
            if (_chatMessages == null) return;
            string line = $"\n<color=#ff9090>[System]</color> {message}";
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
                            // /block <name> /unblock <name> /blocks — mute commands.
                            // Parsed before whisper so '/w' style prefixes can't
                            // collide accidentally.
                            if (msg.Equals("/blocks", System.StringComparison.OrdinalIgnoreCase))
                            {
                                Astrion.UI.BlockListUI.Instance?.Open();
                            }
                            else if (msg.StartsWith("/block ", System.StringComparison.OrdinalIgnoreCase))
                            {
                                string t = msg.Substring(7).Trim();
                                if (!string.IsNullOrEmpty(t))
                                {
                                    Astrion.Game.BlockSystem.Instance?.Block(t);
                                    AppendSystemLine($"{t} 님을 차단합니다.");
                                }
                                else AppendSystemLine("형식: /block 닉네임");
                            }
                            else if (msg.StartsWith("/unblock ", System.StringComparison.OrdinalIgnoreCase))
                            {
                                string t = msg.Substring(9).Trim();
                                if (!string.IsNullOrEmpty(t))
                                {
                                    Astrion.Game.BlockSystem.Instance?.Unblock(t);
                                    AppendSystemLine($"{t} 님의 차단을 해제합니다.");
                                }
                                else AppendSystemLine("형식: /unblock 닉네임");
                            }
                            // /w nickname message → whisper packet
                            else if (msg.StartsWith("/w ") || msg.StartsWith("/W "))
                            {
                                int sp = msg.IndexOf(' ', 3);
                                if (sp > 3)
                                {
                                    string target = msg.Substring(3, sp - 3).Trim();
                                    string body   = msg.Substring(sp + 1).Trim();
                                    if (!string.IsNullOrEmpty(target) && !string.IsNullOrEmpty(body))
                                    {
                                        string wpayload = JsonUtility.ToJson(
                                            new WhisperRequest { target = target, message = body });
                                        nm.SendPacket(PacketType.Whisper, wpayload);
                                    }
                                    else AppendSystemLine("형식: /w 닉네임 메시지");
                                }
                                else AppendSystemLine("형식: /w 닉네임 메시지");
                            }
                            else
                            {
                                string payload = JsonUtility.ToJson(new ChatRequest { message = msg });
                                nm.SendPacket(PacketType.Chat, payload);
                            }
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
        [System.Serializable] private class WhisperRequest { public string target; public string message; }

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
            if (goldText) goldText.text = $"◆ {(stats != null ? stats.Gold : 0):N0} G";
            // Find LvlBadge num if present
            var badgeNum = transform.Find("CharPanel/LvlBadge/Num")?.GetComponent<Text>();
            if (badgeNum != null) badgeNum.text = level.ToString();
        }
    }
}
