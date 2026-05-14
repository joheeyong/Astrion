using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astrion.Game
{
    [RequireComponent(typeof(Collider2D))]
    public class Portal2D : MonoBehaviour
    {
        [SerializeField] private string targetScene = "ForgottenWoodsScene";
        [SerializeField] private float cooldown = 1.5f;

        private static float _lastTransitionTime = -100f;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() == null) return;
            if (Time.time - _lastTransitionTime < cooldown) return;
            _lastTransitionTime = Time.time;
            Debug.Log($"[Portal] Loading scene: {targetScene}");
            SceneManager.LoadScene(targetScene);
        }
    }
}
