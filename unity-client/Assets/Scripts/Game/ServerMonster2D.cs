using UnityEngine;

namespace Astrion.Game
{
    public class ServerMonster2D : MonoBehaviour
    {
        public string Id { get; private set; }

        private Vector2 _targetPos;
        private SpriteRenderer _sr;
        private float _moveLerpSpeed = 2.5f; // a bit faster than server pace so we keep up

        public void Init(string id, float x, float y, int direction)
        {
            Id = id;
            transform.position = new Vector3(x, y, 0);
            _targetPos = new Vector2(x, y);
            _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr != null) _sr.flipX = direction < 0;
        }

        public void OnServerMove(float x, float y, int direction)
        {
            _targetPos = new Vector2(x, y);
            if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr != null) _sr.flipX = direction < 0;
        }

        public void OnHpChanged(int hp, int damage)
        {
            if (damage > 0)
                DamagePopup2D.Spawn(transform.position + Vector3.up * 0.6f, damage,
                                    new Color(1f, 0.95f, 0.30f));
            if (gameObject.activeInHierarchy) StartCoroutine(FlashRed());
        }

        public void OnDeath(int damage)
        {
            if (damage > 0)
                DamagePopup2D.Spawn(transform.position + Vector3.up * 0.6f, damage,
                                    new Color(1f, 0.35f, 0.30f), large: true);
            Destroy(gameObject);
        }

        private void Update()
        {
            var cur = (Vector2)transform.position;
            var next = Vector2.MoveTowards(cur, _targetPos, _moveLerpSpeed * Time.deltaTime);
            transform.position = new Vector3(next.x, next.y, 0);
        }

        private System.Collections.IEnumerator FlashRed()
        {
            if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr == null) yield break;
            var orig = _sr.color;
            _sr.color = new Color(1f, 0.5f, 0.5f);
            yield return new WaitForSeconds(0.1f);
            if (_sr != null) _sr.color = orig;
        }
    }
}
