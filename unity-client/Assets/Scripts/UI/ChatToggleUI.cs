using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Astrion.UI
{
    /// F8 hides / shows the chat panel. Useful for screenshots and for
    /// players who want a cleaner view during boss fights. Doesn't touch
    /// the chat send / receive logic — only the panel's GameObject active
    /// state. When chat is hidden, the Enter binding in GameHUD still
    /// runs but the input field isn't accessible, so the panel auto-
    /// reopens on Enter to keep the typing flow intact.
    public class ChatToggleUI : MonoBehaviour
    {
        [SerializeField] private GameObject chatPanel;

        private void Update()
        {
            if (chatPanel == null) return;
            if (Input.GetKeyDown(KeyCode.F8))
            {
                chatPanel.SetActive(!chatPanel.activeSelf);
            }
            // Enter while chat is hidden — reopen so the player can type.
            if (!chatPanel.activeSelf && Input.GetKeyDown(KeyCode.Return)
                && !IsAnotherInputFocused())
            {
                chatPanel.SetActive(true);
            }
        }

        private static bool IsAnotherInputFocused()
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var sel = es.currentSelectedGameObject;
            return sel != null && sel.GetComponent<InputField>() != null;
        }
    }
}
