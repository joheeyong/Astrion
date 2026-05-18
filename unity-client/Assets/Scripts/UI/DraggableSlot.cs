using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    public enum SlotKind { Inventory, Skill, Hotbar }

    /// Single-component drag&drop for inventory/skill/hotbar slots.
    /// Same component plays both source (drag) and destination (drop).
    public class DraggableSlot : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public SlotKind kind;
        public int slotIndex = -1;      // for Inventory / Hotbar
        public string skillId = "";     // for Skill rows (source only)

        private GameObject _ghost;

        public void OnBeginDrag(PointerEventData e)
        {
            if (!HasContent()) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var src = GetComponentInChildren<Image>();
            _ghost = new GameObject("DragGhost");
            _ghost.transform.SetParent(canvas.transform, false);
            var rt = _ghost.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(56, 56);
            var img = _ghost.AddComponent<Image>();
            img.raycastTarget = false;
            if (src != null)
            {
                img.sprite = src.sprite;
                img.color = new Color(src.color.r, src.color.g, src.color.b, 0.75f);
            }
            else
            {
                img.color = new Color(1f, 1f, 1f, 0.5f);
            }
            // Add a letter overlay so skill drags are recognisable
            string letter = GetLetter();
            if (!string.IsNullOrEmpty(letter))
            {
                var lblGo = new GameObject("L");
                lblGo.transform.SetParent(_ghost.transform, false);
                var lblRT = lblGo.AddComponent<RectTransform>();
                lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
                lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
                var lblT = lblGo.AddComponent<Text>();
                lblT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lblT.fontSize = 24; lblT.fontStyle = FontStyle.Bold;
                lblT.color = new Color(0.10f, 0.07f, 0.04f);
                lblT.alignment = TextAnchor.MiddleCenter;
                lblT.raycastTarget = false;
                lblT.text = letter;
            }
            rt.position = e.position;
        }

        public void OnDrag(PointerEventData e)
        {
            if (_ghost == null) return;
            _ghost.GetComponent<RectTransform>().position = e.position;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (_ghost != null) Destroy(_ghost);
            _ghost = null;
        }

        public void OnDrop(PointerEventData e)
        {
            var from = e.pointerDrag != null ? e.pointerDrag.GetComponent<DraggableSlot>() : null;
            if (from == null || from == this) return;
            HandleDrop(from, this);
        }

        // --- helpers ---

        private bool HasContent()
        {
            switch (kind)
            {
                case SlotKind.Inventory:
                    var inv = InventorySystem.Instance;
                    return inv != null && slotIndex >= 0 && slotIndex < InventorySystem.SLOT_COUNT
                           && !inv.Slots[slotIndex].IsEmpty;
                case SlotKind.Skill:
                    return !string.IsNullOrEmpty(skillId)
                           && SkillSystem.Instance != null
                           && SkillSystem.Instance.IsLearned(skillId);
                case SlotKind.Hotbar:
                    var hb = HotbarSystem.Instance;
                    return hb != null && slotIndex >= 0
                           && !string.IsNullOrEmpty(hb.GetSkillIdAt(slotIndex));
            }
            return false;
        }

        private string GetLetter()
        {
            switch (kind)
            {
                case SlotKind.Inventory:
                    var s = InventorySystem.Instance?.Slots[slotIndex];
                    if (s != null && !s.Value.IsEmpty)
                    {
                        var def = ItemDatabase.Get(s.Value.itemId);
                        return def?.iconLetter ?? "";
                    }
                    break;
                case SlotKind.Skill:
                    var sd = SkillDatabase.Get(skillId);
                    return sd?.iconLetter ?? "";
                case SlotKind.Hotbar:
                    var hid = HotbarSystem.Instance?.GetSkillIdAt(slotIndex);
                    if (!string.IsNullOrEmpty(hid))
                    {
                        var hd = SkillDatabase.Get(hid);
                        return hd?.iconLetter ?? "";
                    }
                    break;
            }
            return "";
        }

        private static void HandleDrop(DraggableSlot from, DraggableSlot to)
        {
            // Inventory ↔ Inventory: swap
            if (from.kind == SlotKind.Inventory && to.kind == SlotKind.Inventory)
            {
                InventorySystem.Instance?.SwapSlots(from.slotIndex, to.slotIndex);
                return;
            }
            // Skill → Hotbar: bind
            if (from.kind == SlotKind.Skill && to.kind == SlotKind.Hotbar)
            {
                if (!string.IsNullOrEmpty(from.skillId))
                    HotbarSystem.Instance?.Bind(to.slotIndex, from.skillId);
                return;
            }
            // Hotbar ↔ Hotbar: swap bindings
            if (from.kind == SlotKind.Hotbar && to.kind == SlotKind.Hotbar)
            {
                var hb = HotbarSystem.Instance;
                if (hb == null) return;
                string a = hb.GetSkillIdAt(from.slotIndex);
                string b = hb.GetSkillIdAt(to.slotIndex);
                if (string.IsNullOrEmpty(b)) hb.Unbind(from.slotIndex);
                else hb.Bind(from.slotIndex, b);
                hb.Bind(to.slotIndex, a);
                return;
            }
        }
    }
}
