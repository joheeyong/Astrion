using UnityEngine;

namespace Astrion.Game
{
    [RequireComponent(typeof(Collider2D))]
    public class ShopNPC2D : MonoBehaviour
    {
        [SerializeField] private string npcName = "상인";
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private bool _playerNearby;

        public string NpcName => npcName;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() != null)
            {
                _playerNearby = true;
                Astrion.UI.DialogueUI.Instance?.ShowHint($"{npcName} (상점)");
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
            var shop = Astrion.UI.ShopUI.Instance;
            if (shop == null || shop.IsOpen) return;
            if (Input.GetKeyDown(interactKey)) shop.Open(npcName);
        }
    }
}
