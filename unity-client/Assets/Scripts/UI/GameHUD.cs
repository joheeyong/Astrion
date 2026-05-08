using UnityEngine;
using UnityEngine.UI;

namespace Astrion.UI
{
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] private Image hpFill;
        [SerializeField] private Image mpFill;
        [SerializeField] private Text hpText;
        [SerializeField] private Text mpText;
        [SerializeField] private Text charNameText;
        [SerializeField] private Text charLevelText;
        [SerializeField] private Text coordsText;
        [SerializeField] private RawImage minimapImage;

        private Transform _player;
        private float _maxHp = 100f;
        private float _currentHp = 100f;
        private float _maxMp = 50f;
        private float _currentMp = 50f;
        private Camera _minimapCam;

        // Desktop UI references (found at runtime)
        private GameObject _desktopChat;
        private InputField _chatInputField;
        private Text _chatMessages;
        private Text _fpsText;
        private bool _chatFocused;
        private float _fpsTimer;
        private int _fpsCount;

        // Hotbar slot highlights
        private Image[] _hotbarSlotHighlights;
        private float _hotbarCooldownTimer;
        private int _lastPressedSlot = -1;

        // Target panel
        private GameObject _targetPanel;
        private Image _targetHpFill;
        private Text _targetNameText;
        private Text _targetLevelText;

        // Buff bar
        private RectTransform _buffBar;

        private void Start()
        {
            string charName = PlayerPrefs.GetString("characterName", "Adventurer");
            string charClass = PlayerPrefs.GetString("characterClass", "Warrior");
            if (charNameText) charNameText.text = charName;
            if (charLevelText) charLevelText.text = $"Lv.1 {charClass}";
            UpdateBars();

            bool isMobile = Application.isMobilePlatform;

            if (!isMobile)
            {
                HideByName("JoystickArea");
                HideByName("JoyInnerBg");
                HideByName("ActionArea");
                HideByName("ChatBar");
                HideByName("MobileMenu");
                ShowByName("DesktopHotbar");
                ShowByName("DesktopChat");
                ShowByName("DesktopMenu");
                ShowByName("TargetPanel");
                ShowByName("BuffBar");
                ShowByName("FPSCounter");
                ShowByName("QuestTracker");
                ShowByName("BagBar");

                // Cache desktop references
                var chatT = transform.Find("DesktopChat");
                if (chatT != null)
                {
                    _desktopChat = chatT.gameObject;
                    var inputBar = chatT.Find("InputBar");
                    if (inputBar != null)
                        _chatInputField = inputBar.GetComponent<InputField>();
                    var msgs = chatT.Find("Messages");
                    if (msgs != null)
                        _chatMessages = msgs.GetComponent<Text>();
                }

                var fpsT = transform.Find("FPSCounter");
                if (fpsT != null)
                    _fpsText = fpsT.GetComponent<Text>();

                // Target panel (hidden until target selected)
                var targetT = transform.Find("TargetPanel");
                if (targetT != null)
                {
                    _targetPanel = targetT.gameObject;
                    _targetPanel.SetActive(false);
                    var tf = targetT.Find("HPBar/Fill");
                    if (tf != null) _targetHpFill = tf.GetComponent<Image>();
                    var tn = targetT.Find("TargetName");
                    if (tn != null) _targetNameText = tn.GetComponent<Text>();
                    var tl = targetT.Find("TargetLevel");
                    if (tl != null) _targetLevelText = tl.GetComponent<Text>();
                }

                // Buff bar
                var buffT = transform.Find("BuffBar");
                if (buffT != null)
                    _buffBar = buffT.GetComponent<RectTransform>();

                // Cache hotbar highlights
                CacheHotbarHighlights();
            }
            else
            {
                HideByName("DesktopHotbar");
                HideByName("DesktopChat");
                HideByName("DesktopMenu");
                HideByName("TargetPanel");
                HideByName("BuffBar");
                HideByName("FPSCounter");
                HideByName("QuestTracker");
                HideByName("BagBar");
            }
        }

        private void CacheHotbarHighlights()
        {
            var hotbar = transform.Find("DesktopHotbar");
            if (hotbar == null) return;
            _hotbarSlotHighlights = new Image[10];
            for (int i = 0; i < 10; i++)
            {
                var slot = hotbar.Find($"Slot_{i}");
                if (slot != null)
                {
                    var hl = slot.Find("Highlight");
                    if (hl != null) _hotbarSlotHighlights[i] = hl.GetComponent<Image>();
                }
            }
        }

        private void Update()
        {
            if (Application.isMobilePlatform) return;

            HandleFPS();
            HandleDesktopInput();
        }

        private void HandleFPS()
        {
            _fpsCount++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 0.5f)
            {
                int fps = Mathf.RoundToInt(_fpsCount / _fpsTimer);
                if (_fpsText != null)
                    _fpsText.text = $"{fps} FPS";
                _fpsTimer = 0;
                _fpsCount = 0;
            }
        }

        private void HandleDesktopInput()
        {
            // Chat toggle with Enter
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_chatInputField != null)
                {
                    if (_chatFocused)
                    {
                        // Send message
                        string msg = _chatInputField.text;
                        if (!string.IsNullOrEmpty(msg))
                        {
                            if (_chatMessages != null)
                                _chatMessages.text += $"\n<color=#e8e4dc>[You]</color> {msg}";
                            _chatInputField.text = "";
                        }
                        _chatInputField.DeactivateInputField();
                        _chatFocused = false;
                    }
                    else
                    {
                        _chatInputField.ActivateInputField();
                        _chatFocused = true;
                    }
                }
            }

            // Escape to close chat or deselect target
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_chatFocused)
                {
                    _chatInputField.DeactivateInputField();
                    _chatFocused = false;
                }
                else if (_targetPanel != null && _targetPanel.activeSelf)
                {
                    _targetPanel.SetActive(false);
                }
            }

            // Don't process hotkeys while chatting
            if (_chatFocused) return;

            // Hotbar keys 1-0
            for (int i = 0; i < 10; i++)
            {
                KeyCode key = (i < 9) ? KeyCode.Alpha1 + i : KeyCode.Alpha0;
                if (Input.GetKeyDown(key))
                {
                    OnHotbarSlotPressed(i);
                }
            }

            // Menu shortcuts
            if (Input.GetKeyDown(KeyCode.B)) TogglePanel("BagPanel");
            if (Input.GetKeyDown(KeyCode.K)) TogglePanel("SkillPanel");
            if (Input.GetKeyDown(KeyCode.O)) TogglePanel("SocialPanel");
            if (Input.GetKeyDown(KeyCode.H)) TogglePanel("HomePanel");
            if (Input.GetKeyDown(KeyCode.N)) TogglePanel("MailPanel");
            if (Input.GetKeyDown(KeyCode.M)) ToggleMinimapZoom();
            if (Input.GetKeyDown(KeyCode.Tab)) CycleTarget();
        }

        private void OnHotbarSlotPressed(int slot)
        {
            _lastPressedSlot = slot;
            _hotbarCooldownTimer = 0.15f;

            // Flash highlight
            if (_hotbarSlotHighlights != null && slot < _hotbarSlotHighlights.Length
                && _hotbarSlotHighlights[slot] != null)
            {
                _hotbarSlotHighlights[slot].color = new Color(1, 1, 1, 0.4f);
            }
        }

        private void TogglePanel(string panelName)
        {
            // Placeholder for future panel system
        }

        private void ToggleMinimapZoom()
        {
            if (_minimapCam != null)
            {
                float size = _minimapCam.orthographicSize;
                _minimapCam.orthographicSize = size > 80 ? 40 : size + 20;
            }
        }

        private void CycleTarget()
        {
            // Placeholder for tab-targeting
        }

        public void ShowTarget(string name, int level, float hpPercent)
        {
            if (_targetPanel != null)
            {
                _targetPanel.SetActive(true);
                if (_targetNameText != null) _targetNameText.text = name;
                if (_targetLevelText != null) _targetLevelText.text = $"Lv.{level}";
                if (_targetHpFill != null) _targetHpFill.fillAmount = hpPercent;
            }
        }

        private void HideByName(string objName)
        {
            var t = transform.Find(objName);
            if (t != null) t.gameObject.SetActive(false);
        }

        private void ShowByName(string objName)
        {
            var t = transform.Find(objName);
            if (t != null) t.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (_player == null)
            {
                var p = GameObject.Find("PlayerPrefab");
                if (p == null) p = GameObject.Find("LocalPlayer");
                if (p != null) _player = p.transform;
                else return;
            }

            if (coordsText)
            {
                var pos = _player.position;
                coordsText.text = $"X:{pos.x:F0}  Z:{pos.z:F0}";
            }

            if (_minimapCam != null)
            {
                var pos = _player.position;
                _minimapCam.transform.position = new Vector3(pos.x, pos.y + 80, pos.z);
            }

            // Fade hotbar highlight
            if (_lastPressedSlot >= 0 && _hotbarSlotHighlights != null)
            {
                _hotbarCooldownTimer -= Time.deltaTime;
                if (_hotbarCooldownTimer <= 0 && _lastPressedSlot < _hotbarSlotHighlights.Length
                    && _hotbarSlotHighlights[_lastPressedSlot] != null)
                {
                    var c = _hotbarSlotHighlights[_lastPressedSlot].color;
                    c.a = Mathf.Max(0, c.a - Time.deltaTime * 3f);
                    _hotbarSlotHighlights[_lastPressedSlot].color = c;
                    if (c.a <= 0) _lastPressedSlot = -1;
                }
            }
        }

        public void SetMinimapCamera(Camera cam)
        {
            _minimapCam = cam;
        }

        private void UpdateBars()
        {
            if (hpFill) hpFill.fillAmount = _currentHp / _maxHp;
            if (mpFill) mpFill.fillAmount = _currentMp / _maxMp;
            if (hpText) hpText.text = $"{(int)_currentHp}/{(int)_maxHp}";
            if (mpText) mpText.text = $"{(int)_currentMp}/{(int)_maxMp}";
        }
    }
}
