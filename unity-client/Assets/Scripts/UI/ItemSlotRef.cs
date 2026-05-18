using UnityEngine;

namespace Astrion.UI
{
    /// Attach to an inventory or shop slot to expose which item it currently represents.
    /// InventoryUI/ShopUI updates this on Refresh; TooltipTrigger reads it.
    public class ItemSlotRef : MonoBehaviour
    {
        public string itemId = "";
    }
}
