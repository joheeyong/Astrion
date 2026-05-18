using UnityEngine;
using UnityEngine.EventSystems;

namespace Astrion.UI
{
    /// Add alongside an ItemSlotRef. On pointer enter, asks ItemTooltipUI to display
    /// the item's tooltip; hides it on exit.
    [RequireComponent(typeof(RectTransform))]
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private ItemSlotRef _ref;

        private void Awake()
        {
            _ref = GetComponent<ItemSlotRef>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_ref == null || string.IsNullOrEmpty(_ref.itemId)) return;
            var tip = ItemTooltipUI.Instance;
            if (tip == null) return;
            tip.Show(_ref.itemId, (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ItemTooltipUI.Instance?.Hide();
        }
    }
}
