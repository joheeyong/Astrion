using UnityEngine;
using UnityEngine.UI;

namespace Astrion.UI
{
    /// World-space label that floats above a player (local or remote): nickname + optional HP bar.
    public class PlayerNameTag : MonoBehaviour
    {
        [SerializeField] private float yOffset = 1.1f;
        [SerializeField] private bool showHpBar = true;

        private Canvas _canvas;
        private Text _nameText;
        private Image _hpFill;
        private RectTransform _root;

        private void Awake()
        {
            BuildOverlay();
        }

        private void LateUpdate()
        {
            if (_root == null) return;
            _root.localPosition = new Vector3(0f, yOffset, 0f);
        }

        private void BuildOverlay()
        {
            var canvasGo = new GameObject("NameTag");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0, yOffset, 0);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 20;
            canvasGo.AddComponent<GraphicRaycaster>();
            _root = canvasGo.GetComponent<RectTransform>();
            _root.sizeDelta = new Vector2(2.0f, 0.6f);
            // 1 world unit = 100 reference px → keep small
            _root.localScale = new Vector3(0.012f, 0.012f, 1f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Name text (above bar)
            var nameRT = new GameObject("Name").AddComponent<RectTransform>();
            nameRT.SetParent(_root, false);
            nameRT.anchorMin = new Vector2(0, 0.45f); nameRT.anchorMax = new Vector2(1, 1f);
            nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;
            _nameText = nameRT.gameObject.AddComponent<Text>();
            _nameText.font = font; _nameText.fontSize = 16; _nameText.fontStyle = FontStyle.Bold;
            _nameText.color = new Color(0.96f, 0.92f, 0.78f);
            _nameText.alignment = TextAnchor.LowerCenter;
            var outline = nameRT.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            _nameText.text = "—";

            // HP bar
            if (showHpBar)
            {
                var barBgRT = new GameObject("HpBg").AddComponent<RectTransform>();
                barBgRT.SetParent(_root, false);
                barBgRT.anchorMin = new Vector2(0.1f, 0.05f); barBgRT.anchorMax = new Vector2(0.9f, 0.35f);
                barBgRT.offsetMin = barBgRT.offsetMax = Vector2.zero;
                var bg = barBgRT.gameObject.AddComponent<Image>();
                bg.color = new Color(0.05f, 0.05f, 0.07f, 0.85f);

                var fillRT = new GameObject("HpFill").AddComponent<RectTransform>();
                fillRT.SetParent(barBgRT, false);
                fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
                fillRT.offsetMin = new Vector2(1, 1); fillRT.offsetMax = new Vector2(-1, -1);
                _hpFill = fillRT.gameObject.AddComponent<Image>();
                _hpFill.color = new Color(0.85f, 0.30f, 0.30f);
                _hpFill.type = Image.Type.Filled;
                _hpFill.fillMethod = Image.FillMethod.Horizontal;
                _hpFill.fillAmount = 1f;
            }
        }

        public void SetName(string text)
        {
            if (_nameText != null) _nameText.text = text ?? "";
        }

        public void SetHp(int hp, int maxHp)
        {
            if (_hpFill == null) return;
            float ratio = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
            _hpFill.fillAmount = ratio;
            // Color shift: green > yellow > red as HP drops
            if (ratio > 0.6f) _hpFill.color = new Color(0.40f, 0.85f, 0.40f);
            else if (ratio > 0.3f) _hpFill.color = new Color(0.95f, 0.78f, 0.30f);
            else _hpFill.color = new Color(0.95f, 0.30f, 0.30f);
        }

        public void SetNameColor(Color c)
        {
            if (_nameText != null) _nameText.color = c;
        }

        public void ShowHpBar(bool show)
        {
            showHpBar = show;
            if (_hpFill != null) _hpFill.transform.parent.gameObject.SetActive(show);
        }
    }
}
