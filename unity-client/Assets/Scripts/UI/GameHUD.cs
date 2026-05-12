using UnityEngine;
using UnityEngine.UI;

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
        private float _maxHp = 100f, _currentHp = 100f;
        private float _maxMp = 50f, _currentMp = 50f;
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
            if (hpFill) hpFill.fillAmount = _currentHp / _maxHp;
            if (mpFill) mpFill.fillAmount = _currentMp / _maxMp;
            if (expFill) expFill.fillAmount = _currentExp;
            if (hpText) hpText.text = $"{(int)_currentHp}/{(int)_maxHp}";
            if (mpText) mpText.text = $"{(int)_currentMp}/{(int)_maxMp}";
            if (expText) expText.text = $"{_currentExp * 100f:F1}%";
        }
    }
}
