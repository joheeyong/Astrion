using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    [RequireComponent(typeof(Collider2D))]
    public class TargetDummy2D : MonoBehaviour
    {
        [SerializeField] private string dummyId = "dummy_0";
        [SerializeField] private string questId = "awaken_power";

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void Start()
        {
            var psm = PlayerStateManager.Instance;
            if (psm != null)
            {
                if (psm.IsLoaded) CheckBroken();
                else psm.OnLoaded += CheckBroken;
            }
        }

        private void OnDestroy()
        {
            var psm = PlayerStateManager.Instance;
            if (psm != null) psm.OnLoaded -= CheckBroken;
        }

        private void CheckBroken()
        {
            if (PlayerStateManager.Instance != null && PlayerStateManager.Instance.IsTargetBroken(dummyId))
                Destroy(gameObject);
        }

        public void OnHit()
        {
            QuestSystem.Instance?.AddProgress(questId, 1);
            PlayerStateManager.Instance?.MarkTargetBroken(dummyId);
            Destroy(gameObject);
        }
    }
}
