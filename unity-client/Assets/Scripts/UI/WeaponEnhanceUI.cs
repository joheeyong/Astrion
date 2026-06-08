using UnityEngine;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    /// Modal panel for the Smith NPC. Shows the currently-equipped weapon,
    /// its current +N, the next tier's cost + success rate, and a single
    /// '강화' button that fires PlayerStats.TryEnhanceWeapon. Subscribes to
    /// PlayerStats.OnChanged / InventorySystem.OnChanged so the row stays
    /// fresh through swap/upgrade/stardust changes.
    /// DDOL singleton — built once on the login scene.
    public class WeaponEnhanceUI : MonoBehaviour
    {
        public static WeaponEnhanceUI Instance { get; private set; }

        private static readonly Color PanelBg     = new Color(0.10f, 0.08f, 0.06f, 0.96f);
        private static readonly Color PanelInner  = new Color(0.13f, 0.10f, 0.07f, 0.85f);
        private static readonly Color AccentGold  = new Color(0.85f, 0.65f, 0.22f, 1f);
        private static readonly Color AccentDim   = new Color(0.85f, 0.65f, 0.22f, 0.30f);
        private static readonly Color TextLight   = new Color(0.94f, 0.88f, 0.74f);
        private static readonly Color TextMuted   = new Color(0.62f, 0.55f, 0.42f);
        private static readonly Color RowBg       = new Color(0.16f, 0.13f, 0.10f, 1f);
        private static readonly Color BtnEnabled  = new Color(0.42f, 0.32f, 0.18f, 1f);
        private static readonly Color BtnDisabled = new Color(0.18f, 0.16f, 0.16f, 1f);

        private CanvasGroup _group;
        private bool _open;

        private Text _weaponNameText;
        private Text _weaponStatsText;
        private Text _nextTierText;
        private Text _costText;
        private Text _rateText;
        private Text _stardustText;
        private Button _enhanceBtn;
        private Image _enhanceBtnBg;
        private Text _enhanceBtnLbl;
        private Text _statusText;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
        }

        private void Start()
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnChanged += OnChanged;
            if (InventorySystem.Instance != null) InventorySystem.Instance.OnChanged += OnChanged;
        }

        private void OnDestroy()
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnChanged -= OnChanged;
            if (InventorySystem.Instance != null) InventorySystem.Instance.OnChanged -= OnChanged;
            if (Instance == this) Instance = null;
        }

        private void OnChanged() { if (_open) Refresh(); }

        public void Open()
        {
            _open = true;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            Refresh();
        }

        public void Close()
        {
            _open = false;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        private void Update()
        {
            if (_open && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        // ──────────────────────── BUILD ────────────────────────

        private void Build()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("EnhanceCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9110;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var groupGo = new GameObject("Group");
            groupGo.transform.SetParent(canvasGo.transform, false);
            var groupRt = groupGo.AddComponent<RectTransform>();
            Stretch(groupRt);
            _group = groupGo.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            var dimGo = new GameObject("Dim");
            dimGo.transform.SetParent(groupGo.transform, false);
            var dimRt = dimGo.AddComponent<RectTransform>();
            Stretch(dimRt);
            var dim = dimGo.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.65f);

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(groupGo.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(620, 460);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = PanelBg;
            var panelOl = panelGo.AddComponent<Outline>();
            panelOl.effectColor = AccentDim;
            panelOl.effectDistance = new Vector2(1, 1);

            AddCornerDeco(panelGo.transform, new Vector2(0, 1), new Vector2(8, -8));
            AddCornerDeco(panelGo.transform, new Vector2(1, 1), new Vector2(-8, -8));
            AddCornerDeco(panelGo.transform, new Vector2(0, 0), new Vector2(8, 8));
            AddCornerDeco(panelGo.transform, new Vector2(1, 0), new Vector2(-8, 8));
            AddAccentLine(panelGo.transform, new Vector2(0.15f, 1f), new Vector2(0.85f, 1f), AccentGold, 2);

            AddText(panelGo.transform, "Eyebrow", "—  W E A P O N   E N H A N C E  —",
                font, 11, FontStyle.Normal, TextAnchor.MiddleCenter, AccentGold,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -22), new Vector2(420, 22));
            AddText(panelGo.transform, "Title", "★  무기 강화",
                font, 24, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -52), new Vector2(560, 36));

            // Weapon row
            var rowGo = new GameObject("WeaponRow");
            rowGo.transform.SetParent(panelGo.transform, false);
            var rowRt = rowGo.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0, 1); rowRt.anchorMax = new Vector2(1, 1);
            rowRt.pivot = new Vector2(0.5f, 1);
            rowRt.offsetMin = new Vector2(30, 0); rowRt.offsetMax = new Vector2(-30, 0);
            rowRt.sizeDelta = new Vector2(0, 90);
            rowRt.anchoredPosition = new Vector2(0, -100);
            var rowImg = rowGo.AddComponent<Image>();
            rowImg.color = RowBg;
            var rowOl = rowGo.AddComponent<Outline>();
            rowOl.effectColor = AccentDim;
            rowOl.effectDistance = new Vector2(1, 1);

            _weaponNameText = AddText(rowGo.transform, "WName", "장착 무기 없음",
                font, 16, FontStyle.Bold, TextAnchor.MiddleLeft, AccentGold,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -16), new Vector2(540, 24));
            _weaponStatsText = AddText(rowGo.transform, "WStats", "",
                font, 12, FontStyle.Normal, TextAnchor.MiddleLeft, TextLight,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -42), new Vector2(540, 18));
            _nextTierText = AddText(rowGo.transform, "WNext", "",
                font, 12, FontStyle.Normal, TextAnchor.MiddleLeft, TextMuted,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -64), new Vector2(540, 18));

            // Cost / Rate / Stardust block
            float blockY = -210f;
            _costText = AddText(panelGo.transform, "Cost", "",
                font, 14, FontStyle.Bold, TextAnchor.MiddleLeft, AccentGold,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(40, blockY), new Vector2(280, 22));
            _rateText = AddText(panelGo.transform, "Rate", "",
                font, 14, FontStyle.Bold, TextAnchor.MiddleRight, TextLight,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-40, blockY), new Vector2(280, 22));
            _stardustText = AddText(panelGo.transform, "Dust", "",
                font, 12, FontStyle.Normal, TextAnchor.MiddleLeft, TextMuted,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(40, blockY - 26), new Vector2(540, 18));

            // Enhance button
            var btnGo = new GameObject("EnhanceBtn");
            btnGo.transform.SetParent(panelGo.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0);
            btnRt.pivot = new Vector2(0.5f, 0);
            btnRt.anchoredPosition = new Vector2(0, 110);
            btnRt.sizeDelta = new Vector2(280, 50);
            _enhanceBtnBg = btnGo.AddComponent<Image>();
            _enhanceBtnBg.color = BtnEnabled;
            var btnOl = btnGo.AddComponent<Outline>();
            btnOl.effectColor = AccentDim;
            btnOl.effectDistance = new Vector2(1, 1);
            _enhanceBtn = btnGo.AddComponent<Button>();
            _enhanceBtn.onClick.AddListener(OnEnhanceClicked);
            _enhanceBtnLbl = AddText(btnGo.transform, "L", "▶  봉헌하여 강화",
                font, 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.85f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            _statusText = AddText(panelGo.transform, "Status",
                "장착한 무기에 별 가루를 봉헌해 강화 단계를 올립니다. 실패해도 단계는 유지됩니다.",
                font, 11, FontStyle.Normal, TextAnchor.MiddleCenter, TextMuted,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(0, 78), new Vector2(0, 18));

            BuildCloseButton(panelGo.transform, font);
        }

        private void BuildCloseButton(Transform parent, Font font)
        {
            var go = new GameObject("CloseB");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-30, 22);
            rt.sizeDelta = new Vector2(120, 32);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.20f, 0.16f, 0.14f, 1f);
            var ol = go.AddComponent<Outline>();
            ol.effectColor = AccentDim;
            ol.effectDistance = new Vector2(1, 1);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(Close);
            AddText(go.transform, "L", "닫기  [Esc]",
                font, 12, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        // ──────────────────────── REFRESH ────────────────────────

        private void Refresh()
        {
            var stats = PlayerStats.Instance;
            var inv = InventorySystem.Instance;
            if (stats == null || inv == null) return;

            string wid = stats.EquippedWeaponId;
            var def = string.IsNullOrEmpty(wid) ? null : ItemDatabase.Get(wid);
            int dust = inv.CountOf("stardust");
            if (_stardustText != null) _stardustText.text = $"보유 별 가루: {dust:N0}";

            bool eligible = def != null && def.baseDamage > 0;
            if (!eligible)
            {
                if (_weaponNameText != null) _weaponNameText.text = def == null
                    ? "장착 무기 없음"
                    : def.displayName + "  (강화 불가)";
                if (_weaponStatsText != null) _weaponStatsText.text = "";
                if (_nextTierText != null) _nextTierText.text = "";
                if (_costText != null) _costText.text = "비용  ·  —";
                if (_rateText != null) _rateText.text = "성공률  ·  —";
                SetEnhanceButton(false, "무기 장착 필요", BtnDisabled);
                return;
            }

            int curLv = stats.GetEnhanceLv(wid);
            int baseDmg = def.baseDamage;
            int currentBonus = curLv * 3;
            string suffix = curLv > 0 ? $"  +{curLv}" : "";
            if (_weaponNameText != null) _weaponNameText.text = def.displayName + suffix;
            if (_weaponStatsText != null)
                _weaponStatsText.text = $"공격력  {baseDmg}  +  {currentBonus}  =  {baseDmg + currentBonus}";

            if (curLv >= PlayerStats.MAX_ENHANCE_LV)
            {
                if (_nextTierText != null) _nextTierText.text = "최대 강화 단계 도달";
                if (_costText != null) _costText.text = "비용  ·  —";
                if (_rateText != null) _rateText.text = "성공률  ·  —";
                SetEnhanceButton(false, "최대 단계", BtnDisabled);
                return;
            }

            int nextBonus = (curLv + 1) * 3;
            if (_nextTierText != null)
                _nextTierText.text = $"다음 단계  +{curLv + 1}  →  공격력  {baseDmg + nextBonus}";

            int cost = PlayerStats.EnhanceCost(curLv);
            float rate = PlayerStats.EnhanceSuccessRate(curLv);
            if (_costText != null) _costText.text = $"비용  ·  별 가루 {cost}";
            if (_rateText != null) _rateText.text = $"성공률  ·  {Mathf.RoundToInt(rate * 100f)}%";

            bool canAfford = dust >= cost;
            SetEnhanceButton(canAfford,
                canAfford ? "▶  봉헌하여 강화" : "재료 부족",
                canAfford ? BtnEnabled : BtnDisabled);
        }

        private void SetEnhanceButton(bool interactable, string label, Color bg)
        {
            if (_enhanceBtn != null) _enhanceBtn.interactable = interactable;
            if (_enhanceBtnBg != null) _enhanceBtnBg.color = bg;
            if (_enhanceBtnLbl != null) _enhanceBtnLbl.text = label;
        }

        private void OnEnhanceClicked()
        {
            PlayerStats.Instance?.TryEnhanceWeapon();
        }

        // ──────────────────── PRIMITIVES ────────────────────

        private static Text AddText(Transform parent, string name, string text,
            Font font, int size, FontStyle style, TextAnchor align, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            if (anchorMin.x != anchorMax.x) { rt.offsetMin = new Vector2(0, rt.offsetMin.y); rt.offsetMax = new Vector2(0, rt.offsetMax.y); }
            if (anchorMin.y != anchorMax.y) { rt.offsetMin = new Vector2(rt.offsetMin.x, 0); rt.offsetMax = new Vector2(rt.offsetMax.x, 0); }
            if (anchorMin == anchorMax) rt.sizeDelta = sizeDelta;
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.text = text;
            t.raycastTarget = false;
            return t;
        }
        private static void AddAccentLine(Transform parent, Vector2 a, Vector2 b, Color c, float h)
        {
            var go = new GameObject("AccentLine");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = a; rt.anchorMax = b;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, h);
            var img = go.AddComponent<Image>();
            img.color = c; img.raycastTarget = false;
        }
        private static void AddCornerDeco(Transform parent, Vector2 anchor, Vector2 offset)
        {
            var go = new GameObject("Corner");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(6, 6);
            var img = go.AddComponent<Image>();
            img.color = AccentGold;
            img.raycastTarget = false;
        }
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
