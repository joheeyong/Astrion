using UnityEngine;

namespace Astrion.Game
{
    /// City innkeeper. Walk up, press E, get a greeting + a full HP/MP
    /// restore on dialogue dismiss. Free in this version (matches old-
    /// school MapleStory behaviour); a future patch can swap to a gold-
    /// cost variant by checking PlayerStats.Gold before the heal.
    ///
    /// Companion to ShopNPC2D and the quest-driven NPC2D. Same trigger /
    /// 'press E' hint pattern; different action on confirm.
    [RequireComponent(typeof(Collider2D))]
    public class InnkeeperNPC2D : MonoBehaviour
    {
        [SerializeField] private string npcName = "여관 주인";
        [SerializeField, TextArea(2, 6)] private string[] greetingLines = {
            "어서 오십시오, 여행자.",
            "지친 몸을 잠시 쉬어가시지요. 회복은 무료입니다."
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
                Astrion.UI.DialogueUI.Instance?.ShowHint($"{npcName} (여관)");
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
            {
                dlg.StartDialogue(npcName, greetingLines, OnDialogueClosed);
            }
        }

        private void OnDialogueClosed()
        {
            var stats = PlayerStats.Instance;
            if (stats == null) return;
            // Full restore via the existing public API. Heal/RestoreMp clamp
            // to MaxHp / MaxMp internally so over-asking is fine.
            stats.Heal(stats.MaxHp);
            stats.RestoreMp(stats.MaxMp);
            Astrion.UI.ToastUI.Instance?.Show("HP/MP 회복", new Color(0.55f, 1f, 0.55f));
        }
    }
}
