using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    public class CharacterInfoUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private KeyCode toggleKey = KeyCode.C;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text nameText;
        [SerializeField] private Text levelText;

        private Text[] _statValues;       // STR, DEX, INT, LUK
        private Button[] _plusButtons;
        private Text _remPointsText;
        private string _charClass;

        private void Awake()
        {
            if (panel) panel.SetActive(false);
        }

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            string charName = PlayerPrefs.GetString("characterName", "Adventurer");
            _charClass = PlayerPrefs.GetString("characterClass", "Warrior");
            if (nameText) nameText.text = charName;

            CacheStatRefs();
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.OnChanged += Refresh;
                Refresh();
            }
            else
            {
                if (levelText) levelText.text = $"Lv.1  {_charClass}";
            }
        }

        private void OnDestroy()
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnChanged -= Refresh;
        }

        private void CacheStatRefs()
        {
            if (panel == null) return;
            string[] stats = { "STR", "DEX", "INT", "LUK" };
            _statValues = new Text[4];
            _plusButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                var row = panel.transform.Find($"StatsPanel/Stat_{stats[i]}");
                if (row == null) continue;
                _statValues[i] = row.Find("Val")?.GetComponent<Text>();
                _plusButtons[i] = row.Find("Plus")?.GetComponent<Button>();
                int idx = i;
                if (_plusButtons[i] != null)
                {
                    _plusButtons[i].onClick.RemoveAllListeners();
                    _plusButtons[i].onClick.AddListener(() => OnPlusClicked(stats[idx]));
                }
            }
            _remPointsText = panel.transform.Find("StatsPanel/RemPoints")?.GetComponent<Text>();
        }

        private void OnPlusClicked(string stat)
        {
            PlayerStats.Instance?.SpendStatPoint(stat);
        }

        private void Refresh()
        {
            var s = PlayerStats.Instance;
            if (s == null) return;
            if (levelText) levelText.text = $"Lv.{s.Level}  ·  {_charClass}";
            int[] vals = { s.Str, s.Dex, s.Intel, s.Luk };
            if (_statValues != null)
            {
                for (int i = 0; i < _statValues.Length && i < vals.Length; i++)
                    if (_statValues[i] != null) _statValues[i].text = vals[i].ToString();
            }
            if (_remPointsText != null)
                _remPointsText.text = $"남은 포인트 : {s.StatPoints}";
            if (_plusButtons != null)
            {
                foreach (var b in _plusButtons)
                    if (b != null) b.interactable = s.StatPoints > 0;
            }
        }

        private void Update()
        {
            if (IsTextInputFocused()) return;
            if (Input.GetKeyDown(toggleKey)) Toggle();
            else if (Input.GetKeyDown(KeyCode.Escape) && IsOpen()) Close();
        }

        private bool IsTextInputFocused()
        {
            if (EventSystem.current == null) return false;
            var sel = EventSystem.current.currentSelectedGameObject;
            if (sel == null) return false;
            return sel.GetComponent<InputField>() != null;
        }

        public bool IsOpen() => panel != null && panel.activeSelf;

        public void Toggle()
        {
            if (panel) panel.SetActive(!panel.activeSelf);
        }

        public void Close()
        {
            if (panel) panel.SetActive(false);
        }
    }
}
