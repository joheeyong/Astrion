using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    [RequireComponent(typeof(Collider2D))]
    public class StarFragment2D : MonoBehaviour
    {
        [SerializeField] private string fragmentId = "frag_0";
        [SerializeField] private string questId = "star_fragments";
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobSpeed = 2.2f;
        [SerializeField] private float spinSpeed = 60f;

        private Vector3 _basePos;
        private float _phase;
        private Transform _visual;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
            _basePos = transform.position;
            _phase = Random.value * Mathf.PI * 2f;
            _visual = transform.childCount > 0 ? transform.GetChild(0) : null;
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
            if (PlayerStateManager.Instance != null && PlayerStateManager.Instance.IsFragmentCollected(fragmentId))
                Destroy(gameObject);
        }

        private void Update()
        {
            _phase += Time.deltaTime * bobSpeed;
            var p = _basePos;
            p.y += Mathf.Sin(_phase) * bobAmplitude;
            transform.position = p;
            if (_visual != null)
                _visual.localEulerAngles = new Vector3(0, 0, _visual.localEulerAngles.z + spinSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() == null) return;
            QuestSystem.Instance?.AddProgress(questId, 1);
            PlayerStateManager.Instance?.MarkFragmentCollected(fragmentId);
            Destroy(gameObject);
        }
    }
}
