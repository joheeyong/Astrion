using UnityEngine;

namespace Astrion.Game
{
    /// Auctioneer (경매인) — present in every hub city. Same NPC behaviour
    /// pattern as Smith/Sage/Herald: walk-up + E + short greeting + opens
    /// AuctionUI on dialogue close. All instances share the same global
    /// auction queue server-side.
    [RequireComponent(typeof(Collider2D))]
    public class AuctioneerNPC2D : MonoBehaviour
    {
        [SerializeField] private string npcName = "경매인";
        [SerializeField, TextArea(2, 6)] private string[] greetingLines = {
            "거래하는 손길은 어디서나 환영이오, 모험가.",
            "별빛 시장은 잠들지 않는다네 — 무엇이든 가져오시오."
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
                Astrion.UI.DialogueUI.Instance?.ShowHint($"{npcName} (경매장)");
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
            Astrion.UI.AuctionUI.Instance?.Open();
        }
    }
}
