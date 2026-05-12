using UnityEngine;

namespace Astrion.Game
{
    [RequireComponent(typeof(Collider2D))]
    public class Ladder2D : MonoBehaviour
    {
        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }
    }
}
