using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    [RequireComponent(typeof(Collider2D))]
    public class WorldItemPickup2D : MonoBehaviour
    {
        [SerializeField] private string pickupId = "pickup_0";
        [SerializeField] private string itemId = "bread";
        [SerializeField] private int quantity = 1;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobSpeed = 2.4f;

        private Vector3 _basePos;
        private float _phase;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            _basePos = transform.position;
            _phase = Random.value * Mathf.PI * 2f;
        }

        private void Start()
        {
            var psm = PlayerStateManager.Instance;
            if (psm != null)
            {
                if (psm.IsLoaded) CheckCollected();
                else psm.OnLoaded += CheckCollected;
            }
        }

        private void OnDestroy()
        {
            var psm = PlayerStateManager.Instance;
            if (psm != null) psm.OnLoaded -= CheckCollected;
        }

        private void CheckCollected()
        {
            if (PlayerStateManager.Instance != null && PlayerStateManager.Instance.IsPickupCollected(pickupId))
                Destroy(gameObject);
        }

        private void Update()
        {
            _phase += Time.deltaTime * bobSpeed;
            var p = _basePos;
            p.y += Mathf.Sin(_phase) * bobAmplitude;
            transform.position = p;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() == null) return;
            if (InventorySystem.Instance == null) return;
            InventorySystem.Instance.Add(itemId, quantity);
            PlayerStateManager.Instance?.MarkPickupCollected(pickupId);
            Destroy(gameObject);
        }
    }
}
