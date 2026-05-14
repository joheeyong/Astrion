using UnityEngine;

namespace Astrion.Game
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class StarBolt2D : MonoBehaviour
    {
        [SerializeField] private float speed = 16f;
        [SerializeField] private float lifetime = 1.6f;
        [SerializeField] private float homingTurnRate = 60f; // units/sec velocity-vector lerp

        private float _spawnedAt;
        private float _dir = 1f;
        private Transform _target;
        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private Transform _visual;
        private bool _visualOnly;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
            _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr != null) _visual = _sr.transform;
            _spawnedAt = Time.time;
        }

        public void Init(float direction, Transform target = null, bool visualOnly = false)
        {
            _dir = Mathf.Sign(direction);
            if (_dir == 0) _dir = 1f;
            _target = target;
            _visualOnly = visualOnly;
            _rb.velocity = new Vector2(_dir * speed, 0f);
            if (_sr != null) _sr.flipX = _dir < 0;
        }

        private void Update()
        {
            if (Time.time - _spawnedAt > lifetime) { Destroy(gameObject); return; }

            // Homing steering
            if (_target != null)
            {
                Vector2 toTarget = (Vector2)_target.position - (Vector2)transform.position;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    Vector2 desired = toTarget.normalized * speed;
                    _rb.velocity = Vector2.MoveTowards(_rb.velocity, desired, homingTurnRate * Time.deltaTime);
                }
            }

            // Rotate visual to face velocity
            if (_visual != null && _rb.velocity.sqrMagnitude > 0.01f)
            {
                float ang = Mathf.Atan2(_rb.velocity.y, Mathf.Abs(_rb.velocity.x)) * Mathf.Rad2Deg;
                _visual.localEulerAngles = new Vector3(0, 0, ang * (_rb.velocity.x < 0 ? -1 : 1));
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_visualOnly) return; // remote-player visualization — no damage
            var dummy = other.GetComponent<TargetDummy2D>();
            if (dummy != null)
            {
                dummy.OnHit();
                Destroy(gameObject);
                return;
            }
            var serverMon = other.GetComponent<ServerMonster2D>();
            if (serverMon != null)
            {
                int damage = PlayerStats.Instance != null
                    ? PlayerStats.Instance.ComputeBoltDamage()
                    : Random.Range(15, 21);
                Astrion.Network.MonsterNetworkManager.Instance?.SendHit(serverMon.Id, damage);
                Destroy(gameObject);
                return;
            }
            var localMon = other.GetComponent<Monster2D>();
            if (localMon != null)
            {
                localMon.TakeDamage(1);
                Destroy(gameObject);
            }
        }
    }
}
