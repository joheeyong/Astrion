using UnityEngine;

namespace Astrion.Game
{
    /// Smith (무기 장인) — Solaria-only NPC who enhances the player's
    /// currently-equipped weapon. Walk-up + E shows a short greeting,
    /// closing the dialogue opens WeaponEnhanceUI. Same trigger pattern
    /// as the Sage / Herald.
    [RequireComponent(typeof(Collider2D))]
    public class SmithNPC2D : MonoBehaviour
    {
        [SerializeField] private string npcName = "무기 장인";
        [SerializeField, TextArea(2, 6)] private string[] greetingLines = {
            "쇠는 별 가루에 굶주려 있다네.",
            "자네 무기에 별을 한 조각 박아 보겠나?"
        };
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private bool _playerNearby;

        public string NpcName => npcName;

        private void Awake() { GetComponent<Collider2D>().isTrigger = true; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() != null)
            {
                _playerNearby = true;
                Astrion.UI.DialogueUI.Instance?.ShowHint($"{npcName} (강화)");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() != null)
            {
                _playerNearby = false;
                Astrion.UI.DialogueUI.Instance?.HideHint();
            }
        }

        private void Update()
        {
            if (!_playerNearby) return;
            var dlg = Astrion.UI.DialogueUI.Instance;
            if (dlg == null || dlg.IsOpen) return;
            if (Input.GetKeyDown(interactKey))
                dlg.StartDialogue(npcName, greetingLines, OnDialogueClosed);
        }

        private void OnDialogueClosed()
        {
            Astrion.UI.WeaponEnhanceUI.Instance?.Open();
        }
    }
}
