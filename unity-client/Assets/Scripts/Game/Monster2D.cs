using UnityEngine;

namespace Astrion.Game
{
    [RequireComponent(typeof(Collider2D))]
    public class Monster2D : MonoBehaviour
    {
        [SerializeField] private int maxHp = 2;
        [SerializeField] private float patrolRange = 3.5f;
        [SerializeField] private float speed = 1.6f;
        [SerializeField] private float hopAmplitude = 0.05f;
        [SerializeField] private float hopSpeed = 5f;

        private Vector3 _origin;
        private float _dir = 1f;
        private int _hp;
        private SpriteRenderer _sr;
        private float _phase;

        private void Awake()
        {
            _origin = transform.position;
            _hp = maxHp;
            _sr = GetComponentInChildren<SpriteRenderer>();
            _phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            transform.position += Vector3.right * _dir * speed * Time.deltaTime;
            if (Mathf.Abs(transform.position.x - _origin.x) > patrolRange) _dir = -_dir;
            if (_sr != null) _sr.flipX = _dir < 0;

            // Small hop bob
            _phase += Time.deltaTime * hopSpeed;
            var p = transform.position;
            float baseY = _origin.y + Mathf.Abs(Mathf.Sin(_phase)) * hopAmplitude;
            transform.position = new Vector3(p.x, baseY, p.z);
        }

        public void TakeDamage(int amount)
        {
            _hp -= amount;
            if (_hp <= 0)
            {
                Destroy(gameObject);
            }
            else if (_sr != null)
            {
                StartCoroutine(FlashRed());
            }
        }

        private System.Collections.IEnumerator FlashRed()
        {
            var orig = _sr.color;
            _sr.color = new Color(1f, 0.5f, 0.5f);
            yield return new WaitForSeconds(0.1f);
            _sr.color = orig;
        }
    }
}
