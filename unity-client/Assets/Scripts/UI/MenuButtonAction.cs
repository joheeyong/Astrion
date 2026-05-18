using UnityEngine;
using UnityEngine.UI;

namespace Astrion.UI
{
    /// Wires a HUD shortcut button (the row of C / I / K / M / G / ESC at the
    /// bottom of the action bar) to its target panel toggle.
    public class MenuButtonAction : MonoBehaviour
    {
        public enum Action { CharInfo, Inventory, Skills, Minimap, System }

        public Action action;

        private void Start()
        {
            var btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            switch (action)
            {
                case Action.CharInfo:
                    Object.FindObjectOfType<CharacterInfoUI>()?.Toggle();
                    break;
                case Action.Inventory:
                    Object.FindObjectOfType<InventoryUI>()?.Toggle();
                    break;
                case Action.Skills:
                    Object.FindObjectOfType<SkillWindowUI>()?.Toggle();
                    break;
                case Action.Minimap:
                    var mm = GameObject.Find("MinimapPanel");
                    if (mm != null) mm.SetActive(!mm.activeSelf);
                    break;
                case Action.System:
                    SystemMenuUI.Instance?.Toggle();
                    break;
            }
        }
    }
}
