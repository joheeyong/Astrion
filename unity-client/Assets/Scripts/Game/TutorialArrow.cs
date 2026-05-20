using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    /// Hovers a "▼ [NPC]" hint above an NPC for new players (no quest started yet).
    /// Auto-hides once the player has taken any quest (PlayerState.questId set).
    public class TutorialArrow : MonoBehaviour
    {
        [SerializeField] private float baseY = 1.5f;
        [SerializeField] private float bobAmount = 0.10f;
        [SerializeField] private float bobSpeed = 3f;

        private MeshRenderer _mr;
        private Vector3 _baseLocal;

        private void Awake()
        {
            _mr = GetComponent<MeshRenderer>();
            _baseLocal = new Vector3(0f, baseY, 0f);
        }

        private void Update()
        {
            // Bobbing
            transform.localPosition = _baseLocal + new Vector3(0f, Mathf.Sin(Time.time * bobSpeed) * bobAmount, 0f);

            // Hide once any quest has been picked up
            var psm = PlayerStateManager.Instance;
            bool show = psm != null && psm.IsLoaded
                        && (psm.State == null || string.IsNullOrEmpty(psm.State.questId));
            if (_mr != null && _mr.enabled != show) _mr.enabled = show;
        }
    }
}
