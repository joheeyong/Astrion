using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    public class ItemTooltipUI : MonoBehaviour
    {
        public static ItemTooltipUI Instance { get; private set; }

        [SerializeField] private RectTransform panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text rarityText;
        [SerializeField] private Text bodyText;

        private RectTransform _canvasRT;
        private Canvas _canvas;

        private void Awake()
        {
            Instance = this;
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null) _canvasRT = _canvas.GetComponent<RectTransform>();
            if (panel) panel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(string itemId, RectTransform anchor)
        {
            var def = ItemDatabase.Get(itemId);
            if (def == null || panel == null) { Hide(); return; }

            // Title (rarity-colored)
            Color rcol = ItemDatabase.RarityColor(def.rarity);
            if (titleText)
            {
                titleText.color = rcol;
                titleText.text = def.displayName;
            }
            if (rarityText)
            {
                rarityText.color = rcol;
                string typeStr = string.IsNullOrEmpty(def.itemType) ? "" : $"  ·  {def.itemType}";
                rarityText.text = $"{ItemDatabase.RarityLabel(def.rarity)}{typeStr}";
            }
            if (bodyText)
            {
                var sb = new StringBuilder();
                if (def.baseDamage > 0) sb.Append("공격력  +").Append(def.baseDamage).Append('\n');
                if (def.healAmount > 0) sb.Append("HP 회복  +").Append(def.healAmount).Append('\n');
                if (def.manaAmount > 0) sb.Append("MP 회복  +").Append(def.manaAmount).Append('\n');
                if (!string.IsNullOrEmpty(def.description))
                {
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(def.description);
                }
                bodyText.text = sb.ToString().TrimEnd();
            }

            panel.gameObject.SetActive(true);

            // Force layout so size is current
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

            PositionNear(anchor);
        }

        public void Hide()
        {
            if (panel) panel.gameObject.SetActive(false);
        }

        private void PositionNear(RectTransform anchor)
        {
            if (anchor == null || _canvasRT == null) return;
            // Place the tooltip to the right of the anchor; flip to left if it would clip
            Vector3[] anchorCorners = new Vector3[4];
            anchor.GetWorldCorners(anchorCorners);
            // anchorCorners: 0=BL, 1=TL, 2=TR, 3=BR

            // Convert to canvas local space
            Vector2 anchorTopRight = WorldToCanvas(anchorCorners[2]);
            Vector2 anchorTopLeft = WorldToCanvas(anchorCorners[1]);

            Vector2 size = panel.rect.size;
            Vector2 canvasSize = _canvasRT.rect.size;

            // Default: to the right of the slot, top-aligned
            float x = anchorTopRight.x + 8f;
            float y = anchorTopRight.y;
            // Right-edge clamp → flip to left
            if (x + size.x > canvasSize.x * 0.5f) x = anchorTopLeft.x - size.x - 8f;
            // Bottom-edge clamp
            if (y - size.y < -canvasSize.y * 0.5f) y = -canvasSize.y * 0.5f + size.y;
            // Top-edge clamp
            if (y > canvasSize.y * 0.5f) y = canvasSize.y * 0.5f;

            panel.pivot = new Vector2(0, 1);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(x, y);
        }

        private Vector2 WorldToCanvas(Vector3 worldPos)
        {
            var screen = RectTransformUtility.WorldToScreenPoint(_canvas != null ? _canvas.worldCamera : null, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRT, screen, _canvas != null ? _canvas.worldCamera : null, out var local);
            return local;
        }
    }
}
