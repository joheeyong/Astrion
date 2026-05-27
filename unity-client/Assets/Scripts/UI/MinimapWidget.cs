using UnityEngine;
using UnityEngine.UI;

namespace Astrion.UI
{
    /// Tiny runtime minimap. Reads the local player's world position every
    /// frame, maps it into the panel's pixel space using a fixed world
    /// bounds (matches the procedural city/hunting scene size in
    /// ProjectSetupWorldMap), and moves a gold 'you' dot accordingly.
    ///
    /// Placement: dropped on the HUD's MinimapPanel by ProjectSetup. The
    /// 'youDot' is a small child Image; if either reference is unset the
    /// widget no-ops silently (safe on legacy scenes that pre-date this
    /// component).
    public class MinimapWidget : MonoBehaviour
    {
        [SerializeField] private RectTransform mapArea;  // the drawable rect (background)
        [SerializeField] private RectTransform youDot;   // small image we move

        // World extents that map onto the panel. Generous enough to cover
        // the auto-generated city / hunting layouts (Ground_Main is
        // 50 wide × 5 tall around y=-3.5; jump platforms reach ~y=5.5).
        private const float WorldMinX = -26f;
        private const float WorldMaxX =  26f;
        private const float WorldMinY = -5f;
        private const float WorldMaxY =  7f;

        private Transform _playerCached;

        private void Update()
        {
            if (mapArea == null || youDot == null) return;
            var player = ResolvePlayer();
            if (player == null) { youDot.gameObject.SetActive(false); return; }
            youDot.gameObject.SetActive(true);

            // Normalised 0..1 in world space, then mapped into half-extent
            // pixel space inside mapArea. anchoredPosition uses (0,0) as
            // center (mapArea is set up with center pivot in the builder).
            Vector3 wp = player.position;
            float tx = Mathf.InverseLerp(WorldMinX, WorldMaxX, wp.x);
            float ty = Mathf.InverseLerp(WorldMinY, WorldMaxY, wp.y);
            Vector2 size = mapArea.rect.size;
            float dotX = (tx - 0.5f) * size.x;
            float dotY = (ty - 0.5f) * size.y;
            youDot.anchoredPosition = new Vector2(dotX, dotY);
        }

        private Transform ResolvePlayer()
        {
            // FindObjectOfType every frame would burn for nothing; cache.
            if (_playerCached != null && _playerCached.gameObject.activeInHierarchy)
                return _playerCached;
            var p = Object.FindObjectOfType<Astrion.Game.PlayerController2D>();
            if (p != null) _playerCached = p.transform;
            return _playerCached;
        }
    }
}
