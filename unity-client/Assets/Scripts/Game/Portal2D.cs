using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astrion.Game
{
    /// Stores the originating scene name across a portal-driven scene load
    /// so the arrival portal in the new scene can position the player next to it.
    public static class PortalTransition
    {
        public static string FromScene = "";
    }

    [RequireComponent(typeof(Collider2D))]
    public class Portal2D : MonoBehaviour
    {
        [SerializeField] private string targetScene = "ForgottenWoodsScene";
        [SerializeField] private float cooldown = 1.5f;
        [SerializeField] private float arrivalOffset = 1.5f;

        private static float _lastTransitionTime = -100f;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Start()
        {
            // Are we the arrival portal for the previous scene?
            if (!string.IsNullOrEmpty(PortalTransition.FromScene)
                && targetScene == PortalTransition.FromScene)
            {
                var player = Object.FindObjectOfType<PlayerController2D>();
                if (player != null)
                {
                    // Push player toward the interior of the map (opposite the portal edge)
                    float sign = transform.position.x > 0f ? -1f : 1f;
                    var pos = transform.position + new Vector3(sign * arrivalOffset, 0.3f, 0f);
                    player.transform.position = pos;

                    // Bump start of cooldown so the player isn't immediately re-triggered
                    _lastTransitionTime = Time.time;
                }
                PortalTransition.FromScene = "";
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() == null) return;
            if (Time.time - _lastTransitionTime < cooldown) return;
            _lastTransitionTime = Time.time;
            PortalTransition.FromScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[Portal] {PortalTransition.FromScene} → {targetScene}");
            SceneManager.LoadScene(targetScene);
        }
    }
}
