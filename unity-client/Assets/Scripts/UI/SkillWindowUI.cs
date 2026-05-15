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
            if (HotbarSystem.Instance != null) HotbarSystem.Instance.OnChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (SkillSystem.Instance != null) SkillSystem.Instance.OnChanged -= Refresh;
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnChanged -= Refresh;
            if (HotbarSystem.Instance != null) HotbarSystem.Instance.OnChanged -= Refresh;
        }

        private void WireRowButtons()
        {
            if (rowsRoot == null) return;
            for (int i = 0; i < _skillOrder.Length; i++)
            {
                var row = rowsRoot.Find($"Row_{i}");
                if (row == null) continue;
                string id = _skillOrder[i];

                var plusBtn = row.Find("Plus")?.GetComponent<Button>();
                if (plusBtn != null)
                {
                    plusBtn.onClick.RemoveAllListeners();
                    plusBtn.onClick.AddListener(() => OnPlusClicked(id));
                }

                for (int s = 0; s < HotbarSystem.SLOT_COUNT; s++)
                {
                    int slotIdx = s; // capture
                    var hotBtn = row.Find($"Hot_{s}")?.GetComponent<Button>();
                    if (hotBtn != null)
                    {
                        hotBtn.onClick.RemoveAllListeners();
                        hotBtn.onClick.AddListener(() => OnHotClicked(id, slotIdx));
                    }
                }
            }
        }

        private void OnPlusClicked(string skillId)
        {
            SkillSystem.Instance?.LearnOrLevelUp(skillId);
        }

        private void OnHotClicked(string skillId, int slot)
        {
            var hb = HotbarSystem.Instance;
            if (hb == null) return;
            // Toggle: if this skill is already in this slot, unbind. Otherwise, bind.
            if (hb.GetSkillIdAt(slot) == skillId) hb.Unbind(slot);
            else hb.Bind(slot, skillId);
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
            var hb = HotbarSystem.Instance;
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

                // Hotbar slot buttons
                bool learned = lv > 0;
                int boundSlot = hb != null ? hb.GetSlotOf(id) : -1;
                for (int s = 0; s < HotbarSystem.SLOT_COUNT; s++)
                {
                    var hotRT = row.Find($"Hot_{s}");
                    if (hotRT == null) continue;
                    var hotBtn = hotRT.GetComponent<Button>();
                    var hotImg = hotRT.GetComponent<Image>();
                    if (hotBtn != null) hotBtn.interactable = learned;
                    if (hotImg != null)
                    {
                        // Highlight if this skill is bound to this slot
                        hotImg.color = (boundSlot == s)
                            ? new Color(0.95f, 0.78f, 0.30f, 1f)   // active gold
                            : new Color(0.30f, 0.24f, 0.16f, 1f);  // dim
                    }
                }
            }
        }
    }
}
