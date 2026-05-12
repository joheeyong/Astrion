using UnityEngine;

namespace Astrion.Game
{
    [RequireComponent(typeof(Collider2D))]
    public class OneWayPlatform2D : MonoBehaviour
    {
        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            var eff = GetComponent<PlatformEffector2D>();
            if (eff == null) eff = gameObject.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 170f;
            eff.useColliderMask = false;
            col.usedByEffector = true;
            col.isTrigger = false;
        }
    }
}
