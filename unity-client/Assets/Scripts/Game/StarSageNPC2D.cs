using UnityEngine;

namespace Astrion.Game
{
    /// Star Sage — Solaria-only NPC who exchanges stardust for permanent
    /// stat boosts. Walk-up + E shows a short greeting; closing the
    /// dialogue opens the AstralImbueUI which handles the actual exchange.
    /// Same trigger / hint pattern as InnkeeperNPC2D and ShopNPC2D.
    [RequireComponent(typeof(Collider2D))]
    public class StarSageNPC2D : MonoBehaviour
    {
        [SerializeField] private string npcName = "별빛 신관";
        [SerializeField, TextArea(2, 6)] private string[] greetingLines = {
            "별의 자녀여, 그대의 영혼에 별빛이 깃들기를.",
            "별 가루를 봉헌하면, 그대에게 영원한 축복을 새기겠소."
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
                Astrion.UI.DialogueUI.Instance?.ShowHint($"{npcName} (별빛 각인)");
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
            Astrion.UI.AstralImbueUI.Instance?.Open();
        }
    }
}
