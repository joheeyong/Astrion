using UnityEngine;

namespace Astrion.Game
{
    /// Herald (랭킹 전령) — Solaria-only NPC who opens the ranking board.
    /// Walk-up + E shows a short greeting; closing the dialogue opens the
    /// RankingUI panel. Same trigger pattern as the other city NPCs.
    [RequireComponent(typeof(Collider2D))]
    public class HeraldNPC2D : MonoBehaviour
    {
        [SerializeField] private string npcName = "랭킹 전령";
        [SerializeField, TextArea(2, 6)] private string[] greetingLines = {
            "여행자여, 별의 명부를 보여드리리다.",
            "그대의 이름은 어디쯤 새겨졌소?"
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
                Astrion.UI.DialogueUI.Instance?.ShowHint($"{npcName} (랭킹)");
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
            Astrion.UI.RankingUI.Instance?.Open();
        }
    }
}
