using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    public class SkillWindowUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform rowsRoot;
        [SerializeField] private Text spText;
        [SerializeField] private Button closeButton;
        [SerializeField] private KeyCode toggleKey = KeyCode.K;

        // skillId order matches row indices in UI (built by ProjectSetup)
        private readonly string[] _skillOrder = { "starbolt", "meteor", "stellar_heal" };

        private void Awake()
        {
            if (panel) panel.SetActive(false);
        }

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            WireRowButtons();
            if (SkillSystem.Instance != null) SkillSystem.Instance.OnChanged += Refresh;
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (SkillSystem.Instance != null) SkillSystem.Instance.OnChanged -= Refresh;
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnChanged -= Refresh;
        }

        private void WireRowButtons()
        {
            if (rowsRoot == null) return;
            for (int i = 0; i < _skillOrder.Length; i++)
            {
                var row = rowsRoot.Find($"Row_{i}");
                if (row == null) continue;
                var plusBtn = row.Find("Plus")?.GetComponent<Button>();
                if (plusBtn != null)
                {
                    string id = _skillOrder[i];
                    plusBtn.onClick.RemoveAllListeners();
                    plusBtn.onClick.AddListener(() => OnPlusClicked(id));
                }
            }
        }

        private void OnPlusClicked(string skillId)
        {
            SkillSystem.Instance?.LearnOrLevelUp(skillId);
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
            return sel != null && sel.GetComponent<InputField>() != null;
        }

        public bool IsOpen() => panel != null && panel.activeSelf;
        public void Toggle() { if (panel) panel.SetActive(!panel.activeSelf); }
        public void Close() { if (panel) panel.SetActive(false); }

        private void Refresh()
        {
            if (rowsRoot == null) return;
            var sys = SkillSystem.Instance;
            var stats = PlayerStats.Instance;
            int sp = stats != null ? stats.SkillPoints : 0;
            if (spText) spText.text = $"남은 스킬 포인트 : {sp}";

            for (int i = 0; i < _skillOrder.Length; i++)
            {
                var row = rowsRoot.Find($"Row_{i}");
                if (row == null) continue;
                string id = _skillOrder[i];
                var def = SkillDatabase.Get(id);
                if (def == null) continue;
                int lv = sys != null ? sys.GetLevel(id) : 0;

                var levelT = row.Find("Level")?.GetComponent<Text>();
                if (levelT != null) levelT.text = $"Lv.{lv}/{def.maxLevel}";

                var requirementT = row.Find("Requirement")?.GetComponent<Text>();
                if (requirementT != null)
                {
                    if (stats != null && stats.Level < def.unlockLevel)
                        requirementT.text = $"잠금 — Lv.{def.unlockLevel} 필요";
                    else if (lv >= def.maxLevel)
                        requirementT.text = "MAX";
                    else
                        requirementT.text = $"SP {def.spCostPerLevel} 소비";
                }

                var plusBtn = row.Find("Plus")?.GetComponent<Button>();
                if (plusBtn != null)
                {
                    bool canLevel = lv < def.maxLevel
                                    && (stats == null || stats.Level >= def.unlockLevel)
                                    && sp >= def.spCostPerLevel;
                    plusBtn.interactable = canLevel;
                }
            }
        }
    }
}
