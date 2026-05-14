using UnityEngine;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] private Image hpFill;
        [SerializeField] private Image mpFill;
        [SerializeField] private Image expFill;
        [SerializeField] private Text hpText;
        [SerializeField] private Text mpText;
        [SerializeField] private Text expText;
        [SerializeField] private Text charNameText;
        [SerializeField] private Text charLevelText;
        [SerializeField] private Text coordsText;

        private Transform _player;
        private float _currentExp = 0.35f;

        private InputField _chatInputField;
        private Text _chatMessages;
        private Text _fpsText;
        private bool _chatFocused;
        private float _fpsTimer;
        private int _fpsCount;

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
                if (_chatFocused)
                {
                    string msg = _chatInputField.text;
                    if (!string.IsNullOrEmpty(msg) && _chatMessages != null)
                        _chatMessages.text += $"\n<color=#ffd060>[You]</color> {msg}";
                    _chatInputField.text = "";
                    _chatInputField.DeactivateInputField();
                    _chatFocused = false;
                }
                else
                {
                    _chatInputField.ActivateInputField();
                    _chatFocused = true;
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape) && _chatFocused)
            {
                _chatInputField.DeactivateInputField();
                _chatFocused = false;
            }
        }

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
            if (hpFill) hpFill.fillAmount = maxHp > 0 ? (float)curHp / maxHp : 0f;
            if (mpFill) mpFill.fillAmount = maxMp > 0 ? (float)curMp / maxMp : 0f;
            if (expFill) expFill.fillAmount = _currentExp;
            if (hpText) hpText.text = $"{curHp}/{maxHp}";
            if (mpText) mpText.text = $"{curMp}/{maxMp}";
            if (expText) expText.text = $"{_currentExp * 100f:F1}%";
        }
    }
}
