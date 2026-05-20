using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Procedural generators for the v1 worldmap zones. Sits as a partial of
/// ProjectSetup so all the existing sprite/helper methods (TexToSprite,
/// MakePlayerParts, SpawnGround, SpawnPortal, CreateGameHUD, etc.) are in
/// scope. Twenty-two near-identical Unity scenes by hand is unmaintainable;
/// every scene here is built from one CreateZoneScene call with a per-zone
/// PortalSpec list and sky color.
///
/// See docs/WORLDMAP.md for the canonical layout this code targets.
public partial class ProjectSetup
{
    /// Portal description for procedural scene generation.
    private struct PortalSpec
    {
        public string targetScene;
        public Vector2 position;
        public Vector2 size;
        public static PortalSpec At(string scene, float x, float y) =>
            new PortalSpec { targetScene = scene, position = new Vector2(x, y), size = new Vector2(1.4f, 2.6f) };
    }

    public static void CreateWorldMapScenes()
    {
        // ── 5 hub cities ── flat ground + portals to neighbours.
        // Each city's left portal goes back toward Solaria (hub).

        CreateZoneScene("Solaria", new Color(0.40f, 0.55f, 0.30f), isCity: true,
            portals: new[] {
                PortalSpec.At("MainScene",              -22f, -2f),  // back to tutorial island
                PortalSpec.At("SolariaOutskirtsScene",   22f, -2f),  // forward into the level tree
                PortalSpec.At("VerdaglenScene",         -12f,  3f),  // west — mage hub
                PortalSpec.At("NightportScene",          12f,  3f),  // south — thief hub
            });

        CreateZoneScene("Pyresummit", new Color(0.45f, 0.20f, 0.18f), isCity: true,
            portals: new[] {
                PortalSpec.At("PinewoodTrailScene", -22f, -2f),  // back via Solaria tree
                PortalSpec.At("CinderRidgeScene",    22f, -2f),  // forward into volcano
            });

        CreateZoneScene("Verdaglen", new Color(0.18f, 0.40f, 0.30f), isCity: true,
            portals: new[] {
                PortalSpec.At("SolariaScene",   -22f, -2f),
                PortalSpec.At("MossgladeScene",  22f, -2f),
            });

        CreateZoneScene("Nightport", new Color(0.18f, 0.15f, 0.28f), isCity: true,
            portals: new[] {
                PortalSpec.At("SolariaScene",     -22f, -2f),
                PortalSpec.At("BackalleysScene",   22f, -2f),
                PortalSpec.At("TidehavenScene",    0f,   6f),  // ferry overhead
            });

        CreateZoneScene("Tidehaven", new Color(0.20f, 0.40f, 0.55f), isCity: true,
            portals: new[] {
                PortalSpec.At("NightportScene", -22f, -2f),
                PortalSpec.At("TideDocksScene",  22f, -2f),
            });

        // ── 16 hunting zones ── jump platforms, left=back / right=forward.
        // (Solaria tree)
        CreateZoneScene("SolariaOutskirts", new Color(0.45f, 0.55f, 0.32f), isCity: false,
            portals: new[] {
                PortalSpec.At("SolariaScene",      -22f, -2f),
                PortalSpec.At("SunlitPlainsScene",  22f, -2f),
            });
        CreateZoneScene("SunlitPlains", new Color(0.50f, 0.58f, 0.30f), isCity: false,
            portals: new[] {
                PortalSpec.At("SolariaOutskirtsScene", -22f, -2f),
                PortalSpec.At("WheatFieldsScene",       22f, -2f),
            });
        CreateZoneScene("WheatFields", new Color(0.62f, 0.55f, 0.25f), isCity: false,
            portals: new[] {
                PortalSpec.At("SunlitPlainsScene",   -22f, -2f),
                PortalSpec.At("PinewoodTrailScene",   22f, -2f),
            });
        CreateZoneScene("PinewoodTrail", new Color(0.22f, 0.35f, 0.20f), isCity: false,
            portals: new[] {
                PortalSpec.At("WheatFieldsScene", -22f, -2f),
                PortalSpec.At("PyresummitScene",   22f, -2f),
            });

        // (Pyresummit tree)
        CreateZoneScene("CinderRidge", new Color(0.45f, 0.25f, 0.20f), isCity: false,
            portals: new[] {
                PortalSpec.At("PyresummitScene",     -22f, -2f),
                PortalSpec.At("AshfallCliffsScene",   22f, -2f),
            });
        CreateZoneScene("AshfallCliffs", new Color(0.35f, 0.22f, 0.18f), isCity: false,
            portals: new[] {
                PortalSpec.At("CinderRidgeScene", -22f, -2f),
                PortalSpec.At("MagmaHollowScene",  22f, -2f),
            });
        CreateZoneScene("MagmaHollow", new Color(0.55f, 0.18f, 0.12f), isCity: false,
            portals: new[] {
                PortalSpec.At("AshfallCliffsScene", -22f, -2f),
            });

        // (Verdaglen tree — forgotten_woods is the terminal node, already authored elsewhere)
        CreateZoneScene("Mossglade", new Color(0.25f, 0.42f, 0.30f), isCity: false,
            portals: new[] {
                PortalSpec.At("VerdaglenScene",         -22f, -2f),
                PortalSpec.At("WhisperingBoughsScene",   22f, -2f),
            });
        CreateZoneScene("WhisperingBoughs", new Color(0.20f, 0.40f, 0.32f), isCity: false,
            portals: new[] {
                PortalSpec.At("MossgladeScene", -22f, -2f),
                PortalSpec.At("OldRootsScene",   22f, -2f),
            });
        CreateZoneScene("OldRoots", new Color(0.30f, 0.28f, 0.18f), isCity: false,
            portals: new[] {
                PortalSpec.At("WhisperingBoughsScene", -22f, -2f),
                PortalSpec.At("ForgottenWoodsScene",    22f, -2f),
            });

        // (Nightport tree)
        CreateZoneScene("Backalleys", new Color(0.20f, 0.18f, 0.25f), isCity: false,
            portals: new[] {
                PortalSpec.At("NightportScene",     -22f, -2f),
                PortalSpec.At("SewerTunnelsScene",   22f, -2f),
            });
        CreateZoneScene("SewerTunnels", new Color(0.18f, 0.22f, 0.22f), isCity: false,
            portals: new[] {
                PortalSpec.At("BackalleysScene",         -22f, -2f),
                PortalSpec.At("UndergroundVaultScene",    22f, -2f),
            });
        CreateZoneScene("UndergroundVault", new Color(0.15f, 0.15f, 0.18f), isCity: false,
            portals: new[] {
                PortalSpec.At("SewerTunnelsScene", -22f, -2f),
            });

        // (Tidehaven tree — citadel_of_dawn is the terminal node)
        CreateZoneScene("TideDocks", new Color(0.20f, 0.40f, 0.55f), isCity: false,
            portals: new[] {
                PortalSpec.At("TidehavenScene",        -22f, -2f),
                PortalSpec.At("DriftwoodBeachScene",    22f, -2f),
            });
        CreateZoneScene("DriftwoodBeach", new Color(0.55f, 0.50f, 0.35f), isCity: false,
            portals: new[] {
                PortalSpec.At("TideDocksScene",   -22f, -2f),
                PortalSpec.At("SunkenReefScene",   22f, -2f),
            });
        CreateZoneScene("SunkenReef", new Color(0.18f, 0.30f, 0.45f), isCity: false,
            portals: new[] {
                PortalSpec.At("DriftwoodBeachScene", -22f, -2f),
                PortalSpec.At("CitadelOfDawnScene",   22f, -2f),
            });
    }

    /// Procedural scene generator. Pulls in the same player/camera/HUD wiring
    /// CreateForgottenWoodsScene authored by hand, but driven by data so 22
    /// zones can land in one PR. Cities get a flat layout; huntings get jump
    /// platforms. Decoration is per-zone sky color only; rich biome art is a
    /// later phase.
    private static void CreateZoneScene(string baseName, Color skyColor, bool isCity, PortalSpec[] portals)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var lights = Object.FindObjectsOfType<Light>();
        foreach (var l in lights) if (l.type == LightType.Directional) Object.DestroyImmediate(l.gameObject);

        const int GROUND_LAYER = 8;
        var groundSpr   = TexToSprite(Make2DGroundTex(256, 64));
        var platformSpr = TexToSprite(Make2DPlatformTex(256, 32));
        var localParts  = MakePlayerParts(
            shirt: new Color(0.30f, 0.48f, 0.22f),
            hair:  new Color(0.32f, 0.22f, 0.12f),
            pants: new Color(0.38f, 0.26f, 0.16f));
        var remoteParts = MakePlayerParts(
            shirt: new Color(0.62f, 0.16f, 0.16f),
            hair:  new Color(0.32f, 0.22f, 0.12f),
            pants: new Color(0.38f, 0.26f, 0.16f));

        // World layout
        var worldRoot = new GameObject("World");
        SpawnGround("Ground_Main", worldRoot.transform, groundSpr,
            new Vector2(0, -3.5f), new Vector2(50, 1.5f), GROUND_LAYER, false);

        if (isCity)
        {
            // City: a low decorative bench so the layout isn't completely flat.
            SpawnGround("Bench", worldRoot.transform, platformSpr,
                new Vector2(0, -1f), new Vector2(8, 0.5f), GROUND_LAYER, true);
        }
        else
        {
            // Hunting: jump platforms identical to ForgottenWoods.
            SpawnGround("Platform_1", worldRoot.transform, platformSpr,
                new Vector2(-6, 1f),  new Vector2(8,  0.5f), GROUND_LAYER, true);
            SpawnGround("Platform_2", worldRoot.transform, platformSpr,
                new Vector2(10, 2.5f), new Vector2(8,  0.5f), GROUND_LAYER, true);
            SpawnGround("Platform_3", worldRoot.transform, platformSpr,
                new Vector2(0, 5f),   new Vector2(10, 0.5f), GROUND_LAYER, true);
        }

        foreach (var p in portals)
        {
            SpawnPortal($"Portal_To_{p.targetScene}", worldRoot.transform,
                p.position, p.size, p.targetScene);
        }

        // Player prefab — identical wiring to the manually-authored scenes.
        var playerPrefab = new GameObject("PlayerPrefab");
        playerPrefab.transform.position = new Vector3(0, 0, 0);
        BuildPlayerVisual(playerPrefab, localParts, out var pBody, out var pLArm, out var pRArm, out var pLLeg, out var pRLeg);
        var pBox = playerPrefab.AddComponent<BoxCollider2D>();
        pBox.size = new Vector2(0.40f, 0.84f);
        pBox.offset = new Vector2(0, 0.02f);
        var pRb = playerPrefab.AddComponent<Rigidbody2D>();
        pRb.gravityScale = 3f;
        pRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        pRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        pRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var pCtrl = playerPrefab.AddComponent<Astrion.Game.PlayerController2D>();
        playerPrefab.AddComponent<Astrion.Game.PlayerVisualTinter>();
        playerPrefab.AddComponent<Astrion.Game.PlayerEquipmentVisual>();
        var pAnim = playerPrefab.AddComponent<Astrion.Game.PlayerAnimator2D>();
        var groundCheckGo = new GameObject("GroundCheck");
        groundCheckGo.transform.SetParent(playerPrefab.transform, false);
        groundCheckGo.transform.localPosition = new Vector3(0, -0.42f, 0);
        var pSo = new SerializedObject(pCtrl);
        pSo.FindProperty("groundCheck").objectReferenceValue = groundCheckGo.transform;
        pSo.FindProperty("groundMask").intValue = 1 << GROUND_LAYER;
        pSo.ApplyModifiedPropertiesWithoutUndo();
        var pAnimSo = new SerializedObject(pAnim);
        pAnimSo.FindProperty("body").objectReferenceValue = pBody;
        pAnimSo.FindProperty("leftArm").objectReferenceValue = pLArm;
        pAnimSo.FindProperty("rightArm").objectReferenceValue = pRArm;
        pAnimSo.FindProperty("leftLeg").objectReferenceValue = pLLeg;
        pAnimSo.FindProperty("rightLeg").objectReferenceValue = pRLeg;
        pAnimSo.ApplyModifiedPropertiesWithoutUndo();

        // Remote prefab + StarBolt prefab
        var remotePrefab = new GameObject("RemotePlayerPrefab");
        remotePrefab.transform.position = new Vector3(100, 100, 0);
        BuildPlayerVisual(remotePrefab, remoteParts, out _, out _, out _, out _, out _);

        var boltSpr = TexToSprite(MakeStarBoltTex(32));
        var starBoltPrefab = new GameObject("StarBoltPrefab");
        starBoltPrefab.transform.position = new Vector3(200f, 200f, 0f);
        starBoltPrefab.SetActive(false);
        var boltVisual = new GameObject("Visual");
        boltVisual.transform.SetParent(starBoltPrefab.transform, false);
        var boltSR = boltVisual.AddComponent<SpriteRenderer>();
        boltSR.sprite = boltSpr;
        boltSR.sortingOrder = 12;
        var boltCol = starBoltPrefab.AddComponent<CircleCollider2D>();
        boltCol.radius = 0.18f;
        boltCol.isTrigger = true;
        var boltRb = starBoltPrefab.AddComponent<Rigidbody2D>();
        boltRb.gravityScale = 0f;
        boltRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        starBoltPrefab.AddComponent<Astrion.Game.StarBolt2D>();
        var pCtrlSo = new SerializedObject(pCtrl);
        pCtrlSo.FindProperty("starBoltPrefab").objectReferenceValue = starBoltPrefab;
        pCtrlSo.ApplyModifiedPropertiesWithoutUndo();

        // Camera
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 6.5f;
            mainCam.transform.position = new Vector3(0, 0, -10);
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = skyColor;
            mainCam.farClipPlane = 100f;
            mainCam.gameObject.AddComponent<Astrion.Game.Camera2D>();
        }

        // GameManager / Quest / Inventory / HUD
        var gameManagerGo = new GameObject("GameManager");
        var gm = gameManagerGo.AddComponent<Astrion.Game.GameManager>();
        var gmSo = new SerializedObject(gm);
        gmSo.FindProperty("remotePlayerPrefab").objectReferenceValue = remotePrefab;
        gmSo.FindProperty("starBoltPrefab").objectReferenceValue = starBoltPrefab;
        gmSo.ApplyModifiedPropertiesWithoutUndo();

        new GameObject("QuestSystem").AddComponent<Astrion.Game.QuestSystem>();
        new GameObject("InventorySystem").AddComponent<Astrion.Game.InventorySystem>();

        CreateGameHUD(playerPrefab, 0f);

        EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{baseName}Scene.unity");
        Debug.Log($"[Astrion] {baseName}Scene created.");
    }
}
