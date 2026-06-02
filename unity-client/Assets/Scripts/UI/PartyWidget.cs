using UnityEngine;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    /// Top-left party roster (auto-shows when in a party) + centred invite
    /// modal (auto-shows when someone invites you). Single DDOL singleton —
    /// built once on the login scene, survives every portal hop the way
    /// WorldMapUI does. Subscribes to PartySystem events and rebuilds on
    /// each PARTY_UPDATE.
    public class PartyWidget : MonoBehaviour
    {
        public static PartyWidget Instance { get; private set; }

        private const int MAX_MEMBERS = 4;

        // Same medieval palette the rest of the HUD uses.
        private static readonly Color PanelBg     = new Color(0.10f, 0.08f, 0.06f, 0.88f);
        private static readonly Color RowBg       = new Color(0.16f, 0.13f, 0.10f, 0.95f);
        private static readonly Color RowBgLeader = new Color(0.22f, 0.18f, 0.12f, 0.95f);
        private static readonly Color AccentGold  = new Color(0.85f, 0.65f, 0.22f, 1f);
        private static readonly Color AccentDim   = new Color(0.85f, 0.65f, 0.22f, 0.35f);
        private static readonly Color TextLight   = new Color(0.94f, 0.88f, 0.74f);
        private static readonly Color TextMuted   = new Color(0.62f, 0.55f, 0.42f);
        private static readonly Color HpFull      = new Color(0.60f, 0.85f, 0.40f);
        private static readonly Color HpLow       = new Color(0.85f, 0.40f, 0.30f);
        private static readonly Color HpOffline   = new Color(0.45f, 0.42f, 0.38f);

        private CanvasGroup _widgetGroup;
        private Text _titleText;
        private GameObject _widgetRoot;

        private CanvasGroup _modalGroup;
        private Text _modalText;
        private Text _modalSubText;
        private bool _modalOpen;

        // Per-row controls so Refresh just edits text/fills.
        private class Row
        {
            public GameObject root;
            public Image bg;
            public Text leader;
            public Text name;
            public Text zone;
            public Image hpFill;
            public Text hpText;
        }
        private Row[] _rows;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
        }

        private void Start()
        {
            var ps = PartySystem.Instance;
            if (ps != null)
            {
                ps.OnPartyUpdated += Refresh;
                ps.OnInviteFrom   += OnInviteFrom;
                ps.OnPartyError   += OnPartyError;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            var ps = PartySystem.Instance;
            if (ps != null)
            {
                ps.OnPartyUpdated -= Refresh;
                ps.OnInviteFrom   -= OnInviteFrom;
                ps.OnPartyError   -= OnPartyError;
            }
            if (Instance == this) Instance = null;
        }

        // ─────────────────────────── BUILD ───────────────────────────

        private void Build()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("PartyCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 8500;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            BuildWidget(canvasGo.transform, font);
            BuildModal(canvasGo.transform, font);
        }

        private void BuildWidget(Transform parent, Font font)
        {
            _widgetRoot = new GameObject("Widget");
            _widgetRoot.transform.SetParent(parent, false);
            var rootRt = _widgetRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0, 1);
            rootRt.pivot = new Vector2(0, 1);
            rootRt.anchoredPosition = new Vector2(18, -260);
            rootRt.sizeDelta = new Vector2(260, 200);
            _widgetGroup = _widgetRoot.AddComponent<CanvasGroup>();
            _widgetGroup.alpha = 0f;
            _widgetGroup.blocksRaycasts = false;

            var bg = _widgetRoot.AddComponent<Image>();
            bg.color = PanelBg;
            var ol = _widgetRoot.AddComponent<Outline>();
            ol.effectColor = AccentDim;
            ol.effectDistance = new Vector2(1, 1);

            // Title row
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_widgetRoot.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1); titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = new Vector2(0, -2);
            titleRt.sizeDelta = new Vector2(0, 20);
            _titleText = titleGo.AddComponent<Text>();
            _titleText.font = font;
            _titleText.fontSize = 11;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = AccentGold;
            _titleText.text = "PARTY";

            // Leave button (small ✕ top-right of widget)
            var leaveGo = new GameObject("LeaveB");
            leaveGo.transform.SetParent(_widgetRoot.transform, false);
            var leaveRt = leaveGo.AddComponent<RectTransform>();
            leaveRt.anchorMin = leaveRt.anchorMax = new Vector2(1, 1);
            leaveRt.pivot = new Vector2(1, 1);
            leaveRt.anchoredPosition = new Vector2(-4, -4);
            leaveRt.sizeDelta = new Vector2(20, 18);
            var leaveImg = leaveGo.AddComponent<Image>();
            leaveImg.color = new Color(0.45f, 0.18f, 0.18f, 0.85f);
            var leaveBtn = leaveGo.AddComponent<Button>();
            leaveBtn.onClick.AddListener(() => PartySystem.Instance?.Leave());
            var leaveLbl = new GameObject("L");
            leaveLbl.transform.SetParent(leaveGo.transform, false);
            var leaveLblRt = leaveLbl.AddComponent<RectTransform>();
            leaveLblRt.anchorMin = Vector2.zero; leaveLblRt.anchorMax = Vector2.one;
            leaveLblRt.offsetMin = leaveLblRt.offsetMax = Vector2.zero;
            var leaveLblTxt = leaveLbl.AddComponent<Text>();
            leaveLblTxt.font = font;
            leaveLblTxt.fontSize = 11;
            leaveLblTxt.fontStyle = FontStyle.Bold;
            leaveLblTxt.alignment = TextAnchor.MiddleCenter;
            leaveLblTxt.color = new Color(1f, 0.92f, 0.65f);
            leaveLblTxt.text = "✕";

            // 4 rows
            _rows = new Row[MAX_MEMBERS];
            for (int i = 0; i < MAX_MEMBERS; i++)
                _rows[i] = BuildRow(_widgetRoot.transform, font, i);
        }

        private Row BuildRow(Transform parent, Font font, int index)
        {
            float rowH = 38f;
            float gap = 4f;
            float yTop = -26f; // below title
            float y = yTop - index * (rowH + gap);

            var rowGo = new GameObject($"Row_{index}");
            rowGo.transform.SetParent(parent, false);
            var rowRt = rowGo.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0, 1); rowRt.anchorMax = new Vector2(1, 1);
            rowRt.pivot = new Vector2(0.5f, 1);
            rowRt.offsetMin = new Vector2(6, 0); rowRt.offsetMax = new Vector2(-6, 0);
            rowRt.sizeDelta = new Vector2(0, rowH);
            rowRt.anchoredPosition = new Vector2(0, y);
            var bg = rowGo.AddComponent<Image>();
            bg.color = RowBg;

            // Leader badge ◈
            var leadGo = new GameObject("Lead");
            leadGo.transform.SetParent(rowGo.transform, false);
            var leadRt = leadGo.AddComponent<RectTransform>();
            leadRt.anchorMin = leadRt.anchorMax = new Vector2(0, 0.5f);
            leadRt.pivot = new Vector2(0, 0.5f);
            leadRt.anchoredPosition = new Vector2(6, 0);
            leadRt.sizeDelta = new Vector2(14, 14);
            var leadText = leadGo.AddComponent<Text>();
            leadText.font = font;
            leadText.fontSize = 12;
            leadText.alignment = TextAnchor.MiddleCenter;
            leadText.color = AccentGold;
            leadText.text = "◈";

            // Name
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(rowGo.transform, false);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0.5f); nameRt.anchorMax = new Vector2(0, 0.5f);
            nameRt.pivot = new Vector2(0, 0.5f);
            nameRt.anchoredPosition = new Vector2(24, 8);
            nameRt.sizeDelta = new Vector2(160, 16);
            var nameText = nameGo.AddComponent<Text>();
            nameText.font = font;
            nameText.fontSize = 12;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = TextLight;
            nameText.text = "—";

            // Zone (small, below name)
            var zoneGo = new GameObject("Zone");
            zoneGo.transform.SetParent(rowGo.transform, false);
            var zoneRt = zoneGo.AddComponent<RectTransform>();
            zoneRt.anchorMin = new Vector2(0, 0.5f); zoneRt.anchorMax = new Vector2(0, 0.5f);
            zoneRt.pivot = new Vector2(0, 0.5f);
            zoneRt.anchoredPosition = new Vector2(24, -7);
            zoneRt.sizeDelta = new Vector2(160, 12);
            var zoneText = zoneGo.AddComponent<Text>();
            zoneText.font = font;
            zoneText.fontSize = 9;
            zoneText.alignment = TextAnchor.MiddleLeft;
            zoneText.color = TextMuted;
            zoneText.text = "";

            // HP bar (right side)
            var barBgGo = new GameObject("HpBg");
            barBgGo.transform.SetParent(rowGo.transform, false);
            var barBgRt = barBgGo.AddComponent<RectTransform>();
            barBgRt.anchorMin = barBgRt.anchorMax = new Vector2(1, 0.5f);
            barBgRt.pivot = new Vector2(1, 0.5f);
            barBgRt.anchoredPosition = new Vector2(-6, 0);
            barBgRt.sizeDelta = new Vector2(60, 10);
            var barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.color = new Color(0.04f, 0.04f, 0.04f, 0.9f);

            var fillGo = new GameObject("HpFill");
            fillGo.transform.SetParent(barBgGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(1, 1); fillRt.offsetMax = new Vector2(-1, -1);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = HpFull;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;

            var hpTxtGo = new GameObject("HpText");
            hpTxtGo.transform.SetParent(rowGo.transform, false);
            var hpTxtRt = hpTxtGo.AddComponent<RectTransform>();
            hpTxtRt.anchorMin = hpTxtRt.anchorMax = new Vector2(1, 0.5f);
            hpTxtRt.pivot = new Vector2(1, 0.5f);
            hpTxtRt.anchoredPosition = new Vector2(-6, -10);
            hpTxtRt.sizeDelta = new Vector2(60, 12);
            var hpTxt = hpTxtGo.AddComponent<Text>();
            hpTxt.font = font;
            hpTxt.fontSize = 9;
            hpTxt.alignment = TextAnchor.MiddleRight;
            hpTxt.color = TextMuted;
            hpTxt.text = "";

            return new Row {
                root = rowGo, bg = bg, leader = leadText,
                name = nameText, zone = zoneText,
                hpFill = fillImg, hpText = hpTxt,
            };
        }

        private void BuildModal(Transform parent, Font font)
        {
            var modalGo = new GameObject("InviteModal");
            modalGo.transform.SetParent(parent, false);
            var modalRt = modalGo.AddComponent<RectTransform>();
            modalRt.anchorMin = Vector2.zero; modalRt.anchorMax = Vector2.one;
            modalRt.offsetMin = modalRt.offsetMax = Vector2.zero;
            _modalGroup = modalGo.AddComponent<CanvasGroup>();
            _modalGroup.alpha = 0f;
            _modalGroup.blocksRaycasts = false;

            var dimGo = new GameObject("Dim");
            dimGo.transform.SetParent(modalGo.transform, false);
            var dimRt = dimGo.AddComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero; dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = dimRt.offsetMax = Vector2.zero;
            var dim = dimGo.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.55f);

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(modalGo.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(440, 180);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = PanelBg;
            var panelOl = panelGo.AddComponent<Outline>();
            panelOl.effectColor = AccentDim;
            panelOl.effectDistance = new Vector2(1, 1);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var tRt = titleGo.AddComponent<RectTransform>();
            tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -16);
            tRt.sizeDelta = new Vector2(400, 22);
            var titleT = titleGo.AddComponent<Text>();
            titleT.font = font; titleT.fontSize = 12;
            titleT.alignment = TextAnchor.MiddleCenter;
            titleT.color = AccentGold;
            titleT.text = "—  P A R T Y   I N V I T E  —";

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(panelGo.transform, false);
            var bRt = bodyGo.AddComponent<RectTransform>();
            bRt.anchorMin = bRt.anchorMax = new Vector2(0.5f, 0.5f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.anchoredPosition = new Vector2(0, 18);
            bRt.sizeDelta = new Vector2(400, 30);
            _modalText = bodyGo.AddComponent<Text>();
            _modalText.font = font; _modalText.fontSize = 17;
            _modalText.fontStyle = FontStyle.Bold;
            _modalText.alignment = TextAnchor.MiddleCenter;
            _modalText.color = TextLight;
            _modalText.text = "";

            var subGo = new GameObject("Sub");
            subGo.transform.SetParent(panelGo.transform, false);
            var sRt = subGo.AddComponent<RectTransform>();
            sRt.anchorMin = sRt.anchorMax = new Vector2(0.5f, 0.5f);
            sRt.pivot = new Vector2(0.5f, 0.5f);
            sRt.anchoredPosition = new Vector2(0, -10);
            sRt.sizeDelta = new Vector2(400, 18);
            _modalSubText = subGo.AddComponent<Text>();
            _modalSubText.font = font; _modalSubText.fontSize = 11;
            _modalSubText.alignment = TextAnchor.MiddleCenter;
            _modalSubText.color = TextMuted;
            _modalSubText.text = "같은 지역에서 사냥하면 EXP / Gold 가 50% 분배됩니다.";

            BuildModalButton(panelGo.transform, font, "수락 [Y]", new Vector2(-66, 32),
                new Color(0.20f, 0.45f, 0.20f, 1f), () => {
                    PartySystem.Instance?.AcceptPending();
                    HideModal();
                });
            BuildModalButton(panelGo.transform, font, "거절 [N]", new Vector2(66, 32),
                new Color(0.45f, 0.18f, 0.18f, 1f), () => {
                    PartySystem.Instance?.RejectPending();
                    HideModal();
                });
        }

        private void BuildModalButton(Transform parent, Font font, string label,
            Vector2 offsetFromBottom, Color tint, System.Action onClick)
        {
            var go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = offsetFromBottom;
            rt.sizeDelta = new Vector2(120, 38);
            var img = go.AddComponent<Image>();
            img.color = tint;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = AccentDim;
            ol.effectDistance = new Vector2(1, 1);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var lblGo = new GameObject("L");
            lblGo.transform.SetParent(go.transform, false);
            var lblRt = lblGo.AddComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = font; lbl.fontSize = 13;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color = new Color(1f, 0.95f, 0.85f);
            lbl.text = label;
        }

        // ─────────────────────────── REFRESH ───────────────────────────

        private void Refresh()
        {
            var ps = PartySystem.Instance;
            if (ps == null || _rows == null) return;
            bool show = ps.InParty;
            _widgetGroup.alpha = show ? 1f : 0f;
            _widgetGroup.blocksRaycasts = show;
            if (!show) return;

            int n = Mathf.Min(ps.Members.Count, MAX_MEMBERS);
            _titleText.text = $"PARTY  ({n}/{MAX_MEMBERS})";

            for (int i = 0; i < MAX_MEMBERS; i++)
            {
                var r = _rows[i];
                bool active = i < n;
                r.root.SetActive(active);
                if (!active) continue;
                var m = ps.Members[i];
                bool isLeader = !string.IsNullOrEmpty(ps.Leader) && m.name == ps.Leader;
                r.bg.color = isLeader ? RowBgLeader : RowBg;
                r.leader.text = isLeader ? "◈" : "·";
                r.leader.color = isLeader ? AccentGold : TextMuted;
                r.name.text = $"{m.name}   <color=#9b8755>Lv.{m.level}</color>";
                r.name.supportRichText = true;
                r.zone.text = m.online ? PrettyZone(m.zone) : "(offline)";

                if (!m.online)
                {
                    r.hpFill.fillAmount = 0f;
                    r.hpFill.color = HpOffline;
                    r.hpText.text = "—";
                }
                else
                {
                    float ratio = m.maxHp > 0 ? (float)m.hp / m.maxHp : 0f;
                    ratio = Mathf.Clamp01(ratio);
                    r.hpFill.fillAmount = ratio;
                    r.hpFill.color = Color.Lerp(HpLow, HpFull, ratio);
                    r.hpText.text = m.maxHp > 0 ? $"{m.hp}/{m.maxHp}" : "—";
                }
            }
        }

        private void OnInviteFrom(string from)
        {
            if (_modalText != null) _modalText.text = $"{from} 님이 파티에 초대했습니다.";
            ShowModal();
        }

        private void OnPartyError(string msg)
        {
            Astrion.UI.ToastUI.Instance?.Show($"[파티]  {msg}",
                new Color(0.95f, 0.55f, 0.30f));
        }

        private void ShowModal()
        {
            _modalOpen = true;
            _modalGroup.alpha = 1f;
            _modalGroup.blocksRaycasts = true;
        }

        private void HideModal()
        {
            _modalOpen = false;
            _modalGroup.alpha = 0f;
            _modalGroup.blocksRaycasts = false;
        }

        private void Update()
        {
            if (_modalOpen)
            {
                if (Input.GetKeyDown(KeyCode.Y))
                {
                    PartySystem.Instance?.AcceptPending();
                    HideModal();
                }
                else if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Escape))
                {
                    PartySystem.Instance?.RejectPending();
                    HideModal();
                }
            }
        }

        private static string PrettyZone(string zone)
        {
            if (string.IsNullOrEmpty(zone)) return "";
            switch (zone)
            {
                case "beacon_of_winds":   return "Beacon of Winds";
                case "solaria":           return "Solaria";
                case "pyresummit":        return "Pyresummit";
                case "verdaglen":         return "Verdaglen";
                case "nightport":         return "Nightport";
                case "tidehaven":         return "Tidehaven";
                case "forgotten_woods":   return "Forgotten Woods";
                case "citadel_of_dawn":   return "Citadel of Dawn";
                default: return zone.Replace('_', ' ');
            }
        }
    }
}
