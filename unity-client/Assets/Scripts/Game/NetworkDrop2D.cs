using UnityEngine;

namespace Astrion.Game
{
    [RequireComponent(typeof(Collider2D))]
    public class NetworkDrop2D : MonoBehaviour
    {
        public string DropId { get; private set; }
        public string ItemId { get; private set; }
        public int Quantity { get; private set; }

        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobSpeed = 2.4f;

        private Vector3 _basePos;
        private float _phase;
        private bool _claimed;
        private Transform _visual;

        public void Init(string dropId, string itemId, int qty)
        {
            DropId = dropId;
            ItemId = itemId;
            Quantity = qty;
            _basePos = transform.position;
            _phase = Random.value * Mathf.PI * 2f;
            _visual = transform.childCount > 0 ? transform.GetChild(0) : null;
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void Update()
        {
            _phase += Time.deltaTime * bobSpeed;
            var p = _basePos;
            p.y += Mathf.Sin(_phase) * bobAmplitude;
            transform.position = p;
            if (_visual != null)
                _visual.localEulerAngles = new Vector3(0, 0, _visual.localEulerAngles.z + 40f * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_claimed) return;
            if (other.GetComponent<PlayerController2D>() == null) return;
            _claimed = true;
            Astrion.Network.DropNetworkManager.Instance?.SendClaim(DropId);
        }
    }
}
