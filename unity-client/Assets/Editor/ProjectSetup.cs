using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class ProjectSetup
{
    // Medieval European color palette (heraldic + parchment + iron)
    private static readonly Color AccentGold = new Color(0.85f, 0.65f, 0.22f, 1f);       // gold leaf
    private static readonly Color AccentGoldDim = new Color(0.85f, 0.65f, 0.22f, 0.30f);
    private static readonly Color AccentGreen = new Color(0.30f, 0.50f, 0.22f, 1f);       // heraldic vert
    private static readonly Color PanelBg = new Color(0.10f, 0.08f, 0.06f, 0.92f);        // dark leather
    private static readonly Color PanelInner = new Color(0.13f, 0.10f, 0.07f, 0.7f);      // tanned leather
    private static readonly Color FieldBg = new Color(0.08f, 0.06f, 0.04f, 0.95f);        // dark parchment
    private static readonly Color FieldBorder = new Color(0.42f, 0.32f, 0.20f, 0.6f);     // bronze border
    private static readonly Color TextLight = new Color(0.92f, 0.86f, 0.72f, 1f);         // aged parchment
    private static readonly Color TextMuted = new Color(0.55f, 0.48f, 0.38f, 1f);         // sepia ink
    private static readonly Color BtnColor = new Color(0.42f, 0.28f, 0.15f, 1f);          // oak button

    [MenuItem("Astrion/Setup Project")]
    public static void Setup()
    {
        SetupBuildSettings();
        CreateLoginScene();
        CreateCharacterSelectScene();
        CreateCharacterCreateScene();
        CreateMainScene();
        CreateForgottenWoodsScene();
        CreateCitadelOfDawnScene();
        Debug.Log("[Astrion] Project setup complete!");
    }

    [MenuItem("Astrion/Build Android (Debug)")]
    public static void BuildAndroid()
    {
        var scenes = new[] { "Assets/Scenes/LoginScene.unity", "Assets/Scenes/CharacterSelectScene.unity", "Assets/Scenes/CharacterCreateScene.unity", "Assets/Scenes/MainScene.unity", "Assets/Scenes/ForgottenWoodsScene.unity", "Assets/Scenes/CitadelOfDawnScene.unity" };
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.astrion.game");
        PlayerSettings.productName = "Astrion";
        PlayerSettings.companyName = "Astrion";
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;

        BuildPipeline.BuildPlayer(scenes, "Builds/Android/Astrion.apk",
            BuildTarget.Android, BuildOptions.Development | BuildOptions.AllowDebugging);
        Debug.Log("[Astrion] Android build complete!");
    }

    [MenuItem("Astrion/Build macOS (Debug)")]
    public static void BuildMacOS()
    {
        var scenes = new[] { "Assets/Scenes/LoginScene.unity", "Assets/Scenes/CharacterSelectScene.unity", "Assets/Scenes/CharacterCreateScene.unity", "Assets/Scenes/MainScene.unity", "Assets/Scenes/ForgottenWoodsScene.unity", "Assets/Scenes/CitadelOfDawnScene.unity" };
        PlayerSettings.productName = "Astrion";
        PlayerSettings.companyName = "Astrion";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.astrion.game");
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;

        BuildPipeline.BuildPlayer(scenes, "Builds/macOS/Astrion.app",
            BuildTarget.StandaloneOSX, BuildOptions.Development | BuildOptions.AllowDebugging);
        Debug.Log("[Astrion] macOS build complete!");
    }

    [MenuItem("Astrion/Build Windows (Debug)")]
    public static void BuildWindows()
    {
        var scenes = new[] { "Assets/Scenes/LoginScene.unity", "Assets/Scenes/CharacterSelectScene.unity", "Assets/Scenes/CharacterCreateScene.unity", "Assets/Scenes/MainScene.unity", "Assets/Scenes/ForgottenWoodsScene.unity", "Assets/Scenes/CitadelOfDawnScene.unity" };
        PlayerSettings.productName = "Astrion";
        PlayerSettings.companyName = "Astrion";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.astrion.game");
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;

        BuildPipeline.BuildPlayer(scenes, "Builds/Windows/Astrion.exe",
            BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.AllowDebugging);
        Debug.Log("[Astrion] Windows build complete!");
    }

    [MenuItem("Astrion/Build iOS (Debug)")]
    public static void BuildIOS()
    {
        var scenes = new[] { "Assets/Scenes/LoginScene.unity", "Assets/Scenes/CharacterSelectScene.unity", "Assets/Scenes/CharacterCreateScene.unity", "Assets/Scenes/MainScene.unity", "Assets/Scenes/ForgottenWoodsScene.unity", "Assets/Scenes/CitadelOfDawnScene.unity" };
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.astrion.game");
        PlayerSettings.productName = "Astrion";
        PlayerSettings.companyName = "Astrion";
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;

        BuildPipeline.BuildPlayer(scenes, "Builds/iOS",
            BuildTarget.iOS, BuildOptions.Development | BuildOptions.AllowDebugging);
        Debug.Log("[Astrion] iOS Xcode project generated!");
    }

    private static void SetupBuildSettings()
    {
        PlayerSettings.productName = "Astrion";
        PlayerSettings.companyName = "Astrion";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.astrion.game");
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.astrion.game");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
    }

    private static void CreateLoginScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.008f, 0.04f, 0.03f); // #020a08
        cam.cullingMask = 1; // Default layer only
        camGo.AddComponent<AudioListener>();

        // LoginBackground (pollen particles)
        var loginBgGo = new GameObject("LoginBackground");
        loginBgGo.AddComponent<Astrion.UI.LoginBackground>();

        // NetworkManager
        var networkGo = new GameObject("NetworkManager");
        networkGo.AddComponent<Astrion.Network.NetworkManager>();

        // PlayerStateManager (persists across scenes via DontDestroyOnLoad)
        var stateGo = new GameObject("PlayerStateManager");
        stateGo.AddComponent<Astrion.Network.PlayerStateManager>();

        // MonsterNetworkManager (DDOL — handles MONSTER_SPAWN/MOVE/DIE/HP across scenes)
        var monNetGo = new GameObject("MonsterNetworkManager");
        monNetGo.AddComponent<Astrion.Network.MonsterNetworkManager>();

        // DropNetworkManager (DDOL — handles DROP_SPAWN/GRANTED/REMOVED)
        var dropNetGo = new GameObject("DropNetworkManager");
        dropNetGo.AddComponent<Astrion.Network.DropNetworkManager>();

        // PlayerStats (DDOL — HP/MP runtime + passive regen + autosave)
        var statsGo = new GameObject("PlayerStats");
        statsGo.AddComponent<Astrion.Game.PlayerStats>();

        // SkillSystem (DDOL — learned skills)
        var skillSysGo = new GameObject("SkillSystem");
        skillSysGo.AddComponent<Astrion.Game.SkillSystem>();

        // HotbarSystem (DDOL — 5-slot skill bindings)
        var hotbarGo = new GameObject("HotbarSystem");
        hotbarGo.AddComponent<Astrion.Game.HotbarSystem>();

        // SkillCaster (DDOL — actual skill execution dispatcher)
        var casterGo = new GameObject("SkillCaster");
        casterGo.AddComponent<Astrion.Game.SkillCaster>();

        // DeathSystem (DDOL — HP=0 overlay + respawn flow)
        var deathGo = new GameObject("DeathSystem");
        deathGo.AddComponent<Astrion.Game.DeathSystem>();

        // Canvas
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 100;
        canvasGo.AddComponent<CanvasScaler>();
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ===== BACKGROUND IMAGE (full screen) =====
        var bgImgGo = CreateUIElement("BackgroundImage", canvasGo.transform);
        var bgRawImg = bgImgGo.AddComponent<RawImage>();
        var bgTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scenes/login_bg.jpg");
        if (bgTex != null) bgRawImg.texture = bgTex;
        bgRawImg.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        StretchFull(bgImgGo);

        // ===== COLOR GRADE OVERLAY (green/teal tint) =====
        var gradeGo = CreateUIElement("GradeOverlay", canvasGo.transform);
        var gradeImg = gradeGo.AddComponent<Image>();
        gradeImg.color = new Color(0.16f, 0.31f, 0.35f, 0.12f);
        StretchFull(gradeGo);

        // ===== VIGNETTE OVERLAY (darken edges) =====
        var vigGo = CreateUIElement("VignetteOverlay", canvasGo.transform);
        var vigImg = vigGo.AddComponent<Image>();
        vigImg.color = new Color(0f, 0f, 0f, 0.15f);
        StretchFull(vigGo);

        // ===== LOGO: "ASTRION" (top center) =====
        var logoGo = CreateUIElement("LogoName", canvasGo.transform);
        var logoText = logoGo.AddComponent<Text>();
        logoText.text = "A S T R I O N";
        logoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        logoText.fontSize = 56;
        logoText.fontStyle = FontStyle.Bold;
        logoText.alignment = TextAnchor.MiddleCenter;
        logoText.color = new Color(1f, 0.95f, 0.82f); // warm gold-white
        SetRect(logoGo, 0, 440, 700, 70);

        // ===== Decorative line under logo =====
        var logoLineGo = CreateUIElement("LogoLine", canvasGo.transform);
        var logoLineImg = logoLineGo.AddComponent<Image>();
        logoLineImg.color = AccentGold;
        SetRect(logoLineGo, 0, 405, 180, 1);

        // ===== LOGO SUBTITLE =====
        var logoSubGo = CreateUIElement("LogoSubtitle", canvasGo.transform);
        var logoSubText = logoSubGo.AddComponent<Text>();
        logoSubText.text = "T H E   A S T R A L   V E I L";
        logoSubText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        logoSubText.fontSize = 13;
        logoSubText.alignment = TextAnchor.MiddleCenter;
        logoSubText.color = AccentGold;
        SetRect(logoSubGo, 0, 388, 400, 22);

        // ===== OUTER PANEL (dark frame with gold border) =====
        var panelGo = CreateUIElement("LoginPanel", canvasGo.transform);
        var panelImg = panelGo.AddComponent<Image>();
        panelImg.color = PanelBg;
        var panelOutline = panelGo.AddComponent<Outline>();
        panelOutline.effectColor = AccentGoldDim;
        panelOutline.effectDistance = new Vector2(1, 1);
        SetRect(panelGo, 0, -40, 750, 700);

        // ===== Inner panel layer (slight lighter, depth effect) =====
        var innerGo = CreateUIElement("InnerPanel", panelGo.transform);
        var innerImg = innerGo.AddComponent<Image>();
        innerImg.color = PanelInner;
        var innerRect = innerGo.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(12, 12);
        innerRect.offsetMax = new Vector2(-12, -12);

        // ===== Top gold accent line =====
        var topLineGo = CreateUIElement("TopAccentLine", panelGo.transform);
        var topLineImg = topLineGo.AddComponent<Image>();
        topLineImg.color = AccentGold;
        var topLineRect = topLineGo.GetComponent<RectTransform>();
        topLineRect.anchorMin = new Vector2(0.15f, 1f);
        topLineRect.anchorMax = new Vector2(0.85f, 1f);
        topLineRect.anchoredPosition = Vector2.zero;
        topLineRect.sizeDelta = new Vector2(0, 2);

        // ===== Bottom gold accent line =====
        var botLineGo = CreateUIElement("BotAccentLine", panelGo.transform);
        var botLineImg = botLineGo.AddComponent<Image>();
        botLineImg.color = new Color(0.85f, 0.72f, 0.40f, 0.15f);
        var botLineRect = botLineGo.GetComponent<RectTransform>();
        botLineRect.anchorMin = new Vector2(0.15f, 0f);
        botLineRect.anchorMax = new Vector2(0.85f, 0f);
        botLineRect.anchoredPosition = Vector2.zero;
        botLineRect.sizeDelta = new Vector2(0, 1);

        // ===== Corner decorations (4 small squares) =====
        CreateCornerDeco(panelGo.transform, new Vector2(0, 1), new Vector2(8, -8));   // top-left
        CreateCornerDeco(panelGo.transform, new Vector2(1, 1), new Vector2(-8, -8));  // top-right
        CreateCornerDeco(panelGo.transform, new Vector2(0, 0), new Vector2(8, 8));    // bot-left
        CreateCornerDeco(panelGo.transform, new Vector2(1, 0), new Vector2(-8, 8));   // bot-right

        // ===== Panel eyebrow =====
        var eyebrowGo = CreateUIElement("Eyebrow", panelGo.transform);
        var eyebrowText = eyebrowGo.AddComponent<Text>();
        eyebrowText.text = "\u2014  A W A K E N I N G  \u2014";
        eyebrowText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        eyebrowText.fontSize = 11;
        eyebrowText.alignment = TextAnchor.MiddleCenter;
        eyebrowText.color = AccentGold;
        SetRectInPanel(eyebrowGo, 0, 305, 350, 20);

        // ===== Panel title =====
        var panelTitleGo = CreateUIElement("PanelTitle", panelGo.transform);
        var panelTitleText = panelTitleGo.AddComponent<Text>();
        panelTitleText.text = "WALK THE PATH";
        panelTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        panelTitleText.fontSize = 30;
        panelTitleText.fontStyle = FontStyle.Bold;
        panelTitleText.alignment = TextAnchor.MiddleCenter;
        panelTitleText.color = TextLight;
        SetRectInPanel(panelTitleGo, 0, 268, 500, 42);

        // ===== Decorative line under title =====
        var titleLineGo = CreateUIElement("TitleLine", panelGo.transform);
        var titleLineImg = titleLineGo.AddComponent<Image>();
        titleLineImg.color = new Color(0.85f, 0.72f, 0.40f, 0.3f);
        SetRectInPanel(titleLineGo, 0, 245, 120, 1);

        // ===== USERNAME FIELD =====
        var usernameGo = CreateFieldWithIcon("UsernameInput", panelGo.transform, "\u25C8", "Adventurer ID", 185);

        // ===== PASSWORD FIELD =====
        var passwordGo = CreateFieldWithIcon("PasswordInput", panelGo.transform, "\u2726", "Password", 100);
        passwordGo.GetComponent<InputField>().contentType = InputField.ContentType.Password;

        // ===== Row: "Stay signed in" + "Forgot password?" =====
        var rowGo = CreateUIElement("OptionsRow", panelGo.transform);
        SetRectInPanel(rowGo, 0, 42, 600, 22);

        var stayGo = CreateUIElement("StaySignedIn", rowGo.transform);
        var stayText = stayGo.AddComponent<Text>();
        stayText.text = "\u25A1 Stay signed in";
        stayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stayText.fontSize = 13;
        stayText.alignment = TextAnchor.MiddleLeft;
        stayText.color = TextMuted;
        StretchFull(stayGo);

        var forgotGo = CreateUIElement("ForgotPassword", rowGo.transform);
        var forgotText = forgotGo.AddComponent<Text>();
        forgotText.text = "Forgot password?";
        forgotText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        forgotText.fontSize = 13;
        forgotText.alignment = TextAnchor.MiddleRight;
        forgotText.color = AccentGold;
        StretchFull(forgotGo);

        // ===== AWAKEN BUTTON =====
        var awakenBtnGo = CreateUIElement("LoginButton", panelGo.transform);
        var awakenBtnImg = awakenBtnGo.AddComponent<Image>();
        awakenBtnImg.color = BtnColor;
        var awakenBtn = awakenBtnGo.AddComponent<Button>();
        var btnColors = awakenBtn.colors;
        btnColors.highlightedColor = new Color(0.40f, 0.70f, 0.35f, 1f);
        btnColors.pressedColor = new Color(0.25f, 0.50f, 0.22f, 1f);
        awakenBtn.colors = btnColors;
        var awakenOutline = awakenBtnGo.AddComponent<Outline>();
        awakenOutline.effectColor = new Color(0.65f, 0.85f, 0.45f, 0.3f);
        awakenOutline.effectDistance = new Vector2(1, 1);
        SetRectInPanel(awakenBtnGo, 0, -22, 600, 68);

        var awakenTextGo = CreateUIElement("Text", awakenBtnGo.transform);
        var awakenText = awakenTextGo.AddComponent<Text>();
        awakenText.text = "\u2726  A W A K E N  \u2726";
        awakenText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        awakenText.fontSize = 20;
        awakenText.fontStyle = FontStyle.Bold;
        awakenText.alignment = TextAnchor.MiddleCenter;
        awakenText.color = new Color(1f, 1f, 0.95f);
        StretchFull(awakenTextGo);

        // ===== OR CONTINUE WITH divider =====
        var divGo = CreateUIElement("Divider", panelGo.transform);
        var divText = divGo.AddComponent<Text>();
        divText.text = "\u2500\u2500\u2500\u2500  OR CONTINUE WITH  \u2500\u2500\u2500\u2500";
        divText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        divText.fontSize = 10;
        divText.alignment = TextAnchor.MiddleCenter;
        divText.color = TextMuted;
        SetRectInPanel(divGo, 0, -92, 600, 20);

        // ===== SOCIAL BUTTONS =====
        var socialsGo = CreateUIElement("Socials", panelGo.transform);
        SetRectInPanel(socialsGo, 0, -140, 240, 52);

        CreateSocialButton("Google", socialsGo.transform, "G", new Color(0.12f, 0.10f, 0.15f, 0.9f), -64);
        CreateSocialButton("Apple", socialsGo.transform, "\uF8FF", new Color(0.05f, 0.05f, 0.05f, 0.9f), 0);
        CreateSocialButton("Facebook", socialsGo.transform, "f", new Color(0.10f, 0.38f, 0.75f, 0.9f), 64);

        // ===== "New traveler? Begin your saga" =====
        var newGo = CreateUIElement("RegisterButton", panelGo.transform);
        var newBtn = newGo.AddComponent<Button>();
        var newBtnColors = newBtn.colors;
        newBtnColors.normalColor = Color.clear;
        newBtnColors.highlightedColor = Color.clear;
        newBtnColors.pressedColor = Color.clear;
        newBtn.colors = newBtnColors;
        newGo.AddComponent<Image>().color = Color.clear;
        SetRectInPanel(newGo, 0, -198, 600, 28);

        var newTextGo = CreateUIElement("Text", newGo.transform);
        var newText = newTextGo.AddComponent<Text>();
        newText.text = "New traveler? <color=#d9b86c><b>Begin your saga</b></color>";
        newText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        newText.fontSize = 14;
        newText.alignment = TextAnchor.MiddleCenter;
        newText.color = TextMuted;
        newText.supportRichText = true;
        StretchFull(newTextGo);

        // ===== STATUS TEXT =====
        var statusGo = CreateUIElement("StatusText", canvasGo.transform);
        var statusText = statusGo.AddComponent<Text>();
        statusText.text = "";
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 16;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = new Color(1f, 0.4f, 0.3f);
        SetRect(statusGo, 0, -420, 500, 30);

        // ===== BOTTOM LEFT: Server status =====
        var metaLGo = CreateUIElement("ServerStatus", canvasGo.transform);
        var metaLText = metaLGo.AddComponent<Text>();
        metaLText.text = "<color=#7be09a>\u25CF</color> Server: Aetheria \u00B7 Online     v 0.1.0";
        metaLText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        metaLText.fontSize = 12;
        metaLText.alignment = TextAnchor.LowerLeft;
        metaLText.color = new Color(0.65f, 0.62f, 0.55f, 0.6f);
        metaLText.supportRichText = true;
        var metaLRect = metaLGo.GetComponent<RectTransform>();
        metaLRect.anchorMin = Vector2.zero;
        metaLRect.anchorMax = Vector2.zero;
        metaLRect.anchoredPosition = new Vector2(50, 25);
        metaLRect.sizeDelta = new Vector2(400, 25);

        // ===== BOTTOM RIGHT: Terms Privacy Support =====
        var metaRGo = CreateUIElement("FooterLinks", canvasGo.transform);
        var metaRText = metaRGo.AddComponent<Text>();
        metaRText.text = "Terms     Privacy     Support";
        metaRText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        metaRText.fontSize = 12;
        metaRText.alignment = TextAnchor.LowerRight;
        metaRText.color = new Color(0.65f, 0.62f, 0.55f, 0.6f);
        var metaRRect = metaRGo.GetComponent<RectTransform>();
        metaRRect.anchorMin = new Vector2(1, 0);
        metaRRect.anchorMax = new Vector2(1, 0);
        metaRRect.anchoredPosition = new Vector2(-50, 25);
        metaRRect.sizeDelta = new Vector2(300, 25);

        // ===== LoginUI wiring =====
        var loginUIGo = new GameObject("LoginUI");
        var loginUI = loginUIGo.AddComponent<Astrion.UI.LoginUI>();
        var so = new SerializedObject(loginUI);
        so.FindProperty("usernameInput").objectReferenceValue = usernameGo.GetComponent<InputField>();
        so.FindProperty("passwordInput").objectReferenceValue = passwordGo.GetComponent<InputField>();
        so.FindProperty("loginButton").objectReferenceValue = awakenBtnGo.GetComponent<Button>();
        so.FindProperty("registerButton").objectReferenceValue = newGo.GetComponent<Button>();
        so.FindProperty("statusText").objectReferenceValue = statusText;
        so.ApplyModifiedPropertiesWithoutUndo();

        // EventSystem
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/LoginScene.unity");
        Debug.Log("[Astrion] LoginScene created and saved.");
    }

    private static void CreateCharacterSelectScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.008f, 0.04f, 0.03f);
        cam.cullingMask = 1;
        camGo.AddComponent<AudioListener>();

        // Fireflies
        var bgGo = new GameObject("LoginBackground");
        bgGo.AddComponent<Astrion.UI.LoginBackground>();

        // Canvas
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 100;
        canvasGo.AddComponent<CanvasScaler>();
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Background image
        var bgImgGo = CreateUIElement("BackgroundImage", canvasGo.transform);
        var bgRawImg = bgImgGo.AddComponent<RawImage>();
        var bgTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scenes/login_bg.jpg");
        if (bgTex != null) bgRawImg.texture = bgTex;
        bgRawImg.color = new Color(0.65f, 0.65f, 0.65f, 1f);
        StretchFull(bgImgGo);

        // Dark overlay
        var overlayGo = CreateUIElement("Overlay", canvasGo.transform);
        var overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0.02f, 0.03f, 0.5f);
        StretchFull(overlayGo);

        // ===== LEFT PANEL: Character list =====
        var leftPanel = CreateUIElement("LeftPanel", canvasGo.transform);
        var leftPanelImg = leftPanel.AddComponent<Image>();
        leftPanelImg.color = PanelBg;
        var leftOutline = leftPanel.AddComponent<Outline>();
        leftOutline.effectColor = AccentGoldDim;
        leftOutline.effectDistance = new Vector2(1, 1);
        var leftRect = leftPanel.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0.05f);
        leftRect.anchorMax = new Vector2(0.35f, 0.95f);
        leftRect.offsetMin = new Vector2(40, 0);
        leftRect.offsetMax = new Vector2(0, 0);

        // Left panel inner
        var leftInner = CreateUIElement("LeftInner", leftPanel.transform);
        var leftInnerImg = leftInner.AddComponent<Image>();
        leftInnerImg.color = PanelInner;
        var liRect = leftInner.GetComponent<RectTransform>();
        liRect.anchorMin = Vector2.zero;
        liRect.anchorMax = Vector2.one;
        liRect.offsetMin = new Vector2(8, 8);
        liRect.offsetMax = new Vector2(-8, -8);

        // Top gold line
        var ltLine = CreateUIElement("LeftTopLine", leftPanel.transform);
        ltLine.AddComponent<Image>().color = AccentGold;
        var ltRect = ltLine.GetComponent<RectTransform>();
        ltRect.anchorMin = new Vector2(0.1f, 1f);
        ltRect.anchorMax = new Vector2(0.9f, 1f);
        ltRect.anchoredPosition = Vector2.zero;
        ltRect.sizeDelta = new Vector2(0, 2);

        // Corner decorations
        CreateCornerDeco(leftPanel.transform, new Vector2(0, 1), new Vector2(6, -6));
        CreateCornerDeco(leftPanel.transform, new Vector2(1, 1), new Vector2(-6, -6));
        CreateCornerDeco(leftPanel.transform, new Vector2(0, 0), new Vector2(6, 6));
        CreateCornerDeco(leftPanel.transform, new Vector2(1, 0), new Vector2(-6, 6));

        // Left panel title
        var lpTitle = CreateUIElement("ListTitle", leftPanel.transform);
        var lpTitleText = lpTitle.AddComponent<Text>();
        lpTitleText.text = "\u2014  C H A R A C T E R S  \u2014";
        lpTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lpTitleText.fontSize = 16;
        lpTitleText.alignment = TextAnchor.MiddleCenter;
        lpTitleText.color = AccentGold;
        var lpTitleRect = lpTitle.GetComponent<RectTransform>();
        lpTitleRect.anchorMin = new Vector2(0, 1);
        lpTitleRect.anchorMax = new Vector2(1, 1);
        lpTitleRect.anchoredPosition = new Vector2(0, -35);
        lpTitleRect.sizeDelta = new Vector2(0, 30);

        // Slot container
        var slotContainer = CreateUIElement("SlotContainer", leftPanel.transform);
        var scRect = slotContainer.GetComponent<RectTransform>();
        scRect.anchorMin = new Vector2(0, 0.12f);
        scRect.anchorMax = new Vector2(1, 0.88f);
        scRect.offsetMin = new Vector2(20, 0);
        scRect.offsetMax = new Vector2(-20, 0);

        // 4 character slots (vertical list)
        int slotCount = 4;
        for (int i = 0; i < slotCount; i++)
        {
            var slotGo = CreateUIElement($"Slot_{i}", slotContainer.transform);
            var slotImg = slotGo.AddComponent<Image>();
            slotImg.color = new Color(0.04f, 0.05f, 0.08f, 0.7f);
            var slotOl = slotGo.AddComponent<Outline>();
            slotOl.effectColor = new Color(0.55f, 0.50f, 0.35f, 0.2f);
            slotOl.effectDistance = new Vector2(1, 1);
            slotGo.AddComponent<Button>();

            var slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0, 1);
            slotRect.anchorMax = new Vector2(1, 1);
            slotRect.anchoredPosition = new Vector2(0, -50 - i * 110);
            slotRect.sizeDelta = new Vector2(0, 95);

            // Highlight
            var hlGo = CreateUIElement("Highlight", slotGo.transform);
            var hlImg = hlGo.AddComponent<Image>();
            hlImg.color = Color.clear;
            hlImg.raycastTarget = false;
            StretchFull(hlGo);

            // Character name
            var nameGo = CreateUIElement("Name", slotGo.transform);
            var nameText = nameGo.AddComponent<Text>();
            nameText.text = "";
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 22;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = TextLight;
            nameText.raycastTarget = false;
            var nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(20, 0);
            nameRect.offsetMax = new Vector2(-20, -8);

            // Class + Level
            var classGo = CreateUIElement("Class", slotGo.transform);
            var classText = classGo.AddComponent<Text>();
            classText.text = "";
            classText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            classText.fontSize = 15;
            classText.alignment = TextAnchor.MiddleLeft;
            classText.color = TextMuted;
            classText.raycastTarget = false;
            var classRect = classGo.GetComponent<RectTransform>();
            classRect.anchorMin = new Vector2(0, 0);
            classRect.anchorMax = new Vector2(0.6f, 0.5f);
            classRect.offsetMin = new Vector2(20, 8);
            classRect.offsetMax = Vector2.zero;

            // Level (right side)
            var lvlGo = CreateUIElement("Level", slotGo.transform);
            var lvlText = lvlGo.AddComponent<Text>();
            lvlText.text = "";
            lvlText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lvlText.fontSize = 15;
            lvlText.alignment = TextAnchor.MiddleRight;
            lvlText.color = AccentGold;
            lvlText.raycastTarget = false;
            var lvlRect = lvlGo.GetComponent<RectTransform>();
            lvlRect.anchorMin = new Vector2(0.6f, 0);
            lvlRect.anchorMax = new Vector2(1, 0.5f);
            lvlRect.offsetMin = new Vector2(0, 8);
            lvlRect.offsetMax = new Vector2(-20, 0);

            // Empty slot text
            var emptyGo = CreateUIElement("Empty", slotGo.transform);
            var emptyText = emptyGo.AddComponent<Text>();
            emptyText.text = "Empty Slot";
            emptyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            emptyText.fontSize = 17;
            emptyText.alignment = TextAnchor.MiddleCenter;
            emptyText.color = new Color(0.4f, 0.38f, 0.35f, 0.5f);
            emptyText.raycastTarget = false;
            StretchFull(emptyGo);
        }

        // ===== RIGHT SIDE: Selected character info + buttons =====

        // Selected info: name
        var infoNameGo = CreateUIElement("SelectedInfoName", canvasGo.transform);
        var infoNameText = infoNameGo.AddComponent<Text>();
        infoNameText.text = "Select a character";
        infoNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoNameText.fontSize = 38;
        infoNameText.fontStyle = FontStyle.Bold;
        infoNameText.alignment = TextAnchor.MiddleCenter;
        infoNameText.color = new Color(1f, 0.95f, 0.82f);
        var inRect = infoNameGo.GetComponent<RectTransform>();
        inRect.anchorMin = new Vector2(0.35f, 0.55f);
        inRect.anchorMax = new Vector2(1, 0.7f);
        inRect.offsetMin = Vector2.zero;
        inRect.offsetMax = new Vector2(-40, 0);

        // Selected info: detail
        var infoDetailGo = CreateUIElement("SelectedInfoDetail", canvasGo.transform);
        var infoDetailText = infoDetailGo.AddComponent<Text>();
        infoDetailText.text = "";
        infoDetailText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoDetailText.fontSize = 20;
        infoDetailText.alignment = TextAnchor.MiddleCenter;
        infoDetailText.color = TextMuted;
        var idRect = infoDetailGo.GetComponent<RectTransform>();
        idRect.anchorMin = new Vector2(0.35f, 0.45f);
        idRect.anchorMax = new Vector2(1, 0.55f);
        idRect.offsetMin = Vector2.zero;
        idRect.offsetMax = new Vector2(-40, 0);

        // ===== CREATE CHARACTER BUTTON =====
        var createGo = CreateUIElement("CreateButton", canvasGo.transform);
        var createImg = createGo.AddComponent<Image>();
        createImg.color = new Color(0.55f, 0.50f, 0.35f, 0.9f);
        var createBtn = createGo.AddComponent<Button>();
        var cBtnColors = createBtn.colors;
        cBtnColors.highlightedColor = new Color(0.70f, 0.62f, 0.42f, 1f);
        cBtnColors.pressedColor = new Color(0.45f, 0.40f, 0.28f, 1f);
        createBtn.colors = cBtnColors;
        var createOl = createGo.AddComponent<Outline>();
        createOl.effectColor = AccentGoldDim;
        createOl.effectDistance = new Vector2(1, 1);
        var crRect = createGo.GetComponent<RectTransform>();
        crRect.anchorMin = new Vector2(0.45f, 0.18f);
        crRect.anchorMax = new Vector2(0.78f, 0.26f);
        crRect.offsetMin = Vector2.zero;
        crRect.offsetMax = new Vector2(-40, 0);

        var createTextGo = CreateUIElement("Text", createGo.transform);
        var createText = createTextGo.AddComponent<Text>();
        createText.text = "+  CREATE CHARACTER";
        createText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        createText.fontSize = 18;
        createText.fontStyle = FontStyle.Bold;
        createText.alignment = TextAnchor.MiddleCenter;
        createText.color = new Color(1f, 0.95f, 0.85f);
        StretchFull(createTextGo);

        // ===== ENTER BUTTON =====
        var enterGo = CreateUIElement("EnterButton", canvasGo.transform);
        var enterImg = enterGo.AddComponent<Image>();
        enterImg.color = BtnColor;
        var enterBtn = enterGo.AddComponent<Button>();
        var eBtnColors = enterBtn.colors;
        eBtnColors.highlightedColor = new Color(0.40f, 0.70f, 0.35f, 1f);
        eBtnColors.pressedColor = new Color(0.25f, 0.50f, 0.22f, 1f);
        eBtnColors.disabledColor = new Color(0.15f, 0.18f, 0.15f, 0.5f);
        enterBtn.colors = eBtnColors;
        var enterOl = enterGo.AddComponent<Outline>();
        enterOl.effectColor = new Color(0.65f, 0.85f, 0.45f, 0.3f);
        enterOl.effectDistance = new Vector2(1, 1);
        var erRect = enterGo.GetComponent<RectTransform>();
        erRect.anchorMin = new Vector2(0.45f, 0.08f);
        erRect.anchorMax = new Vector2(0.78f, 0.17f);
        erRect.offsetMin = Vector2.zero;
        erRect.offsetMax = new Vector2(-40, 0);

        var enterTextGo = CreateUIElement("Text", enterGo.transform);
        var enterText = enterTextGo.AddComponent<Text>();
        enterText.text = "\u2726  E N T E R   G A M E  \u2726";
        enterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        enterText.fontSize = 20;
        enterText.fontStyle = FontStyle.Bold;
        enterText.alignment = TextAnchor.MiddleCenter;
        enterText.color = new Color(1f, 1f, 0.95f);
        StretchFull(enterTextGo);

        // ===== DELETE BUTTON =====
        var deleteGo = CreateUIElement("DeleteButton", canvasGo.transform);
        var deleteImg = deleteGo.AddComponent<Image>();
        deleteImg.color = new Color(0.55f, 0.12f, 0.10f, 0.92f);
        var deleteBtn = deleteGo.AddComponent<Button>();
        var dColors = deleteBtn.colors;
        dColors.highlightedColor = new Color(0.78f, 0.18f, 0.16f, 1f);
        dColors.pressedColor = new Color(0.42f, 0.06f, 0.06f, 1f);
        dColors.disabledColor = new Color(0.18f, 0.10f, 0.10f, 0.5f);
        deleteBtn.colors = dColors;
        var dRect = deleteGo.GetComponent<RectTransform>();
        dRect.anchorMin = new Vector2(0.45f, 0.005f);
        dRect.anchorMax = new Vector2(0.78f, 0.07f);
        dRect.offsetMin = Vector2.zero;
        dRect.offsetMax = new Vector2(-40, 0);

        var deleteTextGo = CreateUIElement("Text", deleteGo.transform);
        var deleteText = deleteTextGo.AddComponent<Text>();
        deleteText.text = "DELETE  CHARACTER";
        deleteText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        deleteText.fontSize = 14;
        deleteText.fontStyle = FontStyle.Bold;
        deleteText.alignment = TextAnchor.MiddleCenter;
        deleteText.color = new Color(1f, 0.92f, 0.82f);
        StretchFull(deleteTextGo);

        // ===== CONFIRM PANEL (modal) =====
        var confirmPanelGo = CreateUIElement("ConfirmPanel", canvasGo.transform);
        var confirmDim = confirmPanelGo.AddComponent<Image>();
        confirmDim.color = new Color(0, 0, 0, 0.65f);
        var cpRect = confirmPanelGo.GetComponent<RectTransform>();
        cpRect.anchorMin = Vector2.zero; cpRect.anchorMax = Vector2.one;
        cpRect.offsetMin = cpRect.offsetMax = Vector2.zero;

        var confirmBoxGo = CreateUIElement("Box", confirmPanelGo.transform);
        var confirmBoxImg = confirmBoxGo.AddComponent<Image>();
        confirmBoxImg.color = new Color(0.10f, 0.08f, 0.06f, 0.98f);
        var cbol = confirmBoxGo.AddComponent<Outline>();
        cbol.effectColor = new Color(0.85f, 0.65f, 0.22f, 0.8f);
        cbol.effectDistance = new Vector2(2, 2);
        var cbRect = confirmBoxGo.GetComponent<RectTransform>();
        cbRect.anchorMin = cbRect.anchorMax = new Vector2(0.5f, 0.5f);
        cbRect.pivot = new Vector2(0.5f, 0.5f);
        cbRect.sizeDelta = new Vector2(420, 220);
        cbRect.anchoredPosition = Vector2.zero;

        var confirmTextGo = CreateUIElement("Text", confirmBoxGo.transform);
        var confirmText = confirmTextGo.AddComponent<Text>();
        confirmText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        confirmText.fontSize = 15;
        confirmText.fontStyle = FontStyle.Bold;
        confirmText.alignment = TextAnchor.MiddleCenter;
        confirmText.color = TextLight;
        confirmText.text = "\uc815\ub9d0 \uce90\ub9ad\ud130\ub97c \uc0ad\uc81c\ud558\uc2dc\uaca0\uc2b5\ub2c8\uae4c?";
        var ctRect = confirmTextGo.GetComponent<RectTransform>();
        ctRect.anchorMin = new Vector2(0, 0.4f); ctRect.anchorMax = new Vector2(1, 1);
        ctRect.offsetMin = new Vector2(20, 0); ctRect.offsetMax = new Vector2(-20, -10);

        // Yes button (red)
        var yesGo = CreateUIElement("YesButton", confirmBoxGo.transform);
        var yesImg = yesGo.AddComponent<Image>();
        yesImg.color = new Color(0.78f, 0.18f, 0.16f, 0.95f);
        var yesBtn = yesGo.AddComponent<Button>();
        var yColors = yesBtn.colors;
        yColors.highlightedColor = new Color(0.92f, 0.28f, 0.24f, 1f);
        yesBtn.colors = yColors;
        var yRect = yesGo.GetComponent<RectTransform>();
        yRect.anchorMin = new Vector2(0.1f, 0.1f); yRect.anchorMax = new Vector2(0.46f, 0.32f);
        yRect.offsetMin = yRect.offsetMax = Vector2.zero;
        var yTextGo = CreateUIElement("Text", yesGo.transform);
        var yText = yTextGo.AddComponent<Text>();
        yText.text = "\uc0ad\uc81c";
        yText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        yText.fontSize = 16; yText.fontStyle = FontStyle.Bold;
        yText.alignment = TextAnchor.MiddleCenter;
        yText.color = new Color(1f, 0.96f, 0.90f);
        StretchFull(yTextGo);

        // No button (gray)
        var noGo = CreateUIElement("NoButton", confirmBoxGo.transform);
        var noImg = noGo.AddComponent<Image>();
        noImg.color = new Color(0.32f, 0.28f, 0.22f, 0.95f);
        var noBtn = noGo.AddComponent<Button>();
        var nColors = noBtn.colors;
        nColors.highlightedColor = new Color(0.48f, 0.42f, 0.32f, 1f);
        noBtn.colors = nColors;
        var nRect = noGo.GetComponent<RectTransform>();
        nRect.anchorMin = new Vector2(0.54f, 0.1f); nRect.anchorMax = new Vector2(0.9f, 0.32f);
        nRect.offsetMin = nRect.offsetMax = Vector2.zero;
        var nTextGo = CreateUIElement("Text", noGo.transform);
        var nText = nTextGo.AddComponent<Text>();
        nText.text = "\ucde8\uc18c";
        nText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nText.fontSize = 16; nText.fontStyle = FontStyle.Bold;
        nText.alignment = TextAnchor.MiddleCenter;
        nText.color = new Color(1f, 0.96f, 0.90f);
        StretchFull(nTextGo);

        confirmPanelGo.SetActive(false);

        // ===== Wire CharacterSelectUI =====
        var csUIGo = new GameObject("CharacterSelectUI");
        var csUI = csUIGo.AddComponent<Astrion.UI.CharacterSelectUI>();
        var so = new SerializedObject(csUI);

        so.FindProperty("slotContainer").objectReferenceValue = slotContainer.transform;
        so.FindProperty("enterButton").objectReferenceValue = enterBtn;
        so.FindProperty("createButton").objectReferenceValue = createBtn;
        so.FindProperty("deleteButton").objectReferenceValue = deleteBtn;
        so.FindProperty("selectedInfoName").objectReferenceValue = infoNameText;
        so.FindProperty("selectedInfoDetail").objectReferenceValue = infoDetailText;
        so.FindProperty("confirmPanel").objectReferenceValue = confirmPanelGo;
        so.FindProperty("confirmText").objectReferenceValue = confirmText;
        so.FindProperty("confirmYesButton").objectReferenceValue = yesBtn;
        so.FindProperty("confirmNoButton").objectReferenceValue = noBtn;

        so.ApplyModifiedPropertiesWithoutUndo();

        // EventSystem
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/CharacterSelectScene.unity");
        Debug.Log("[Astrion] CharacterSelectScene created and saved.");
    }

    private static void CreateCharacterCreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.008f, 0.04f, 0.03f);
        cam.cullingMask = 1;
        camGo.AddComponent<AudioListener>();

        // Fireflies
        var fxGo = new GameObject("LoginBackground");
        fxGo.AddComponent<Astrion.UI.LoginBackground>();

        // Canvas
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 100;
        canvasGo.AddComponent<CanvasScaler>();
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Background
        var bgImgGo = CreateUIElement("BackgroundImage", canvasGo.transform);
        var bgRawImg = bgImgGo.AddComponent<RawImage>();
        var bgTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scenes/login_bg.jpg");
        if (bgTex != null) bgRawImg.texture = bgTex;
        bgRawImg.color = new Color(0.55f, 0.55f, 0.55f, 1f);
        StretchFull(bgImgGo);

        var overlayGo = CreateUIElement("Overlay", canvasGo.transform);
        overlayGo.AddComponent<Image>().color = new Color(0f, 0.02f, 0.03f, 0.55f);
        StretchFull(overlayGo);

        // ===== TITLE =====
        var titleGo = CreateUIElement("Title", canvasGo.transform);
        var titleText = titleGo.AddComponent<Text>();
        titleText.text = "\u2014  C R E A T E   C H A R A C T E R  \u2014";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 34;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.95f, 0.82f);
        SetRect(titleGo, 0, 470, 900, 50);

        var tLineGo = CreateUIElement("TitleLine", canvasGo.transform);
        tLineGo.AddComponent<Image>().color = AccentGold;
        SetRect(tLineGo, 0, 443, 200, 1);

        // ===== NAME INPUT SECTION =====
        var nameLabel = CreateUIElement("NameLabel", canvasGo.transform);
        var nameLabelText = nameLabel.AddComponent<Text>();
        nameLabelText.text = "CHARACTER NAME";
        nameLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameLabelText.fontSize = 14;
        nameLabelText.alignment = TextAnchor.MiddleCenter;
        nameLabelText.color = AccentGold;
        SetRect(nameLabel, 0, 390, 400, 22);

        // Name input field
        var nameFieldGo = new GameObject("NameInput", typeof(RectTransform));
        nameFieldGo.transform.SetParent(canvasGo.transform, false);
        var nameFieldImg = nameFieldGo.AddComponent<Image>();
        nameFieldImg.color = FieldBg;
        var nameFieldOl = nameFieldGo.AddComponent<Outline>();
        nameFieldOl.effectColor = FieldBorder;
        nameFieldOl.effectDistance = new Vector2(1, 1);
        var nameInput = nameFieldGo.AddComponent<InputField>();
        nameInput.characterLimit = 16;
        SetRect(nameFieldGo, 0, 350, 500, 60);

        var nameTextGo = new GameObject("Text", typeof(RectTransform));
        nameTextGo.transform.SetParent(nameFieldGo.transform, false);
        var nText = nameTextGo.AddComponent<Text>();
        nText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nText.fontSize = 22;
        nText.color = TextLight;
        nText.alignment = TextAnchor.MiddleCenter;
        var ntRect = nameTextGo.GetComponent<RectTransform>();
        ntRect.anchorMin = Vector2.zero;
        ntRect.anchorMax = Vector2.one;
        ntRect.offsetMin = new Vector2(15, 5);
        ntRect.offsetMax = new Vector2(-15, -5);

        var namePlaceholderGo = new GameObject("Placeholder", typeof(RectTransform));
        namePlaceholderGo.transform.SetParent(nameFieldGo.transform, false);
        var npText = namePlaceholderGo.AddComponent<Text>();
        npText.text = "Enter a name...";
        npText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        npText.fontSize = 20;
        npText.color = new Color(0.4f, 0.38f, 0.35f, 0.6f);
        npText.alignment = TextAnchor.MiddleCenter;
        var npRect = namePlaceholderGo.GetComponent<RectTransform>();
        npRect.anchorMin = Vector2.zero;
        npRect.anchorMax = Vector2.one;
        npRect.offsetMin = new Vector2(15, 5);
        npRect.offsetMax = new Vector2(-15, -5);

        nameInput.textComponent = nText;
        nameInput.placeholder = npText;

        // ===== CLASS LABEL =====
        var classLabel = CreateUIElement("ClassLabel", canvasGo.transform);
        var classLabelText = classLabel.AddComponent<Text>();
        classLabelText.text = "SELECT CLASS";
        classLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        classLabelText.fontSize = 14;
        classLabelText.alignment = TextAnchor.MiddleCenter;
        classLabelText.color = AccentGold;
        SetRect(classLabel, 0, 290, 400, 22);

        // ===== 3 CLASS CARDS =====
        float cardW = 340;
        float cardH = 380;
        float cardSpacing = 380;
        float cardY = 50;

        string[] cNames = { "WARRIOR", "MAGE", "RANGER" };
        string[] cIcons = { "\u2694", "\u2726", "\u27B3" };
        string[] cShortDesc = { "Front-line Fighter", "Arcane Scholar", "Swift Hunter" };
        string[] cStats = { "STR 18  DEX 12  INT 8", "STR 8  DEX 10  INT 20", "STR 12  DEX 18  INT 10" };
        Color[] cColors = {
            new Color(0.85f, 0.30f, 0.25f),
            new Color(0.40f, 0.45f, 0.92f),
            new Color(0.30f, 0.78f, 0.42f)
        };

        var classButtons = new Button[3];
        var classHighlights = new Image[3];

        for (int i = 0; i < 3; i++)
        {
            float xPos = (i - 1) * cardSpacing;

            // Card frame
            var cardGo = CreateUIElement($"Class_{cNames[i]}", canvasGo.transform);
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = new Color(0.02f, 0.03f, 0.05f, 0.82f);
            var cardOl = cardGo.AddComponent<Outline>();
            cardOl.effectColor = AccentGoldDim;
            cardOl.effectDistance = new Vector2(1, 1);
            classButtons[i] = cardGo.AddComponent<Button>();
            SetRect(cardGo, xPos, cardY, cardW, cardH);

            // Highlight
            var hlGo = CreateUIElement("Highlight", cardGo.transform);
            classHighlights[i] = hlGo.AddComponent<Image>();
            classHighlights[i].color = Color.clear;
            classHighlights[i].raycastTarget = false;
            StretchFull(hlGo);

            // Inner
            var innerGo = CreateUIElement("Inner", cardGo.transform);
            var innerImg = innerGo.AddComponent<Image>();
            innerImg.color = PanelInner;
            innerImg.raycastTarget = false;
            var iRect = innerGo.GetComponent<RectTransform>();
            iRect.anchorMin = Vector2.zero;
            iRect.anchorMax = Vector2.one;
            iRect.offsetMin = new Vector2(6, 6);
            iRect.offsetMax = new Vector2(-6, -6);

            // Top color line
            var topLine = CreateUIElement("TopLine", cardGo.transform);
            topLine.AddComponent<Image>().color = cColors[i];
            topLine.GetComponent<Image>().raycastTarget = false;
            var tlr = topLine.GetComponent<RectTransform>();
            tlr.anchorMin = new Vector2(0.12f, 1f);
            tlr.anchorMax = new Vector2(0.88f, 1f);
            tlr.anchoredPosition = Vector2.zero;
            tlr.sizeDelta = new Vector2(0, 3);

            // Icon
            var iconGo = CreateUIElement("Icon", cardGo.transform);
            var iconT = iconGo.AddComponent<Text>();
            iconT.text = cIcons[i];
            iconT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconT.fontSize = 72;
            iconT.alignment = TextAnchor.MiddleCenter;
            iconT.color = cColors[i];
            iconT.raycastTarget = false;
            SetRectInPanel(iconGo, 0, 85, 180, 110);

            // Class name
            var cnGo = CreateUIElement("ClassName", cardGo.transform);
            var cnT = cnGo.AddComponent<Text>();
            cnT.text = cNames[i];
            cnT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cnT.fontSize = 24;
            cnT.fontStyle = FontStyle.Bold;
            cnT.alignment = TextAnchor.MiddleCenter;
            cnT.color = TextLight;
            cnT.raycastTarget = false;
            SetRectInPanel(cnGo, 0, 10, 280, 32);

            // Short description
            var sdGo = CreateUIElement("ShortDesc", cardGo.transform);
            var sdT = sdGo.AddComponent<Text>();
            sdT.text = cShortDesc[i];
            sdT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sdT.fontSize = 15;
            sdT.alignment = TextAnchor.MiddleCenter;
            sdT.color = cColors[i];
            sdT.raycastTarget = false;
            SetRectInPanel(sdGo, 0, -18, 280, 22);

            // Deco line
            var dlGo = CreateUIElement("DecoLine", cardGo.transform);
            dlGo.AddComponent<Image>().color = new Color(0.85f, 0.72f, 0.40f, 0.2f);
            dlGo.GetComponent<Image>().raycastTarget = false;
            SetRectInPanel(dlGo, 0, -40, 80, 1);

            // Stats
            var stGo = CreateUIElement("Stats", cardGo.transform);
            var stT = stGo.AddComponent<Text>();
            stT.text = cStats[i];
            stT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stT.fontSize = 14;
            stT.alignment = TextAnchor.MiddleCenter;
            stT.color = TextMuted;
            stT.raycastTarget = false;
            SetRectInPanel(stGo, 0, -65, 300, 22);

            // Corner decos
            CreateCornerDeco(cardGo.transform, new Vector2(0, 1), new Vector2(5, -5));
            CreateCornerDeco(cardGo.transform, new Vector2(1, 1), new Vector2(-5, -5));
            CreateCornerDeco(cardGo.transform, new Vector2(0, 0), new Vector2(5, 5));
            CreateCornerDeco(cardGo.transform, new Vector2(1, 0), new Vector2(-5, 5));
        }

        // ===== SELECTED CLASS INFO =====
        var selNameGo = CreateUIElement("SelClassName", canvasGo.transform);
        var selNameText = selNameGo.AddComponent<Text>();
        selNameText.text = "";
        selNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        selNameText.fontSize = 1;
        selNameText.color = Color.clear;
        SetRect(selNameGo, 0, -999, 1, 1);

        var selDescGo = CreateUIElement("SelClassDesc", canvasGo.transform);
        var selDescText = selDescGo.AddComponent<Text>();
        selDescText.text = "Choose a class";
        selDescText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        selDescText.fontSize = 1;
        selDescText.color = Color.clear;
        SetRect(selDescGo, 0, -998, 1, 1);

        // ===== STATUS TEXT =====
        var statusGo = CreateUIElement("StatusText", canvasGo.transform);
        var statusText = statusGo.AddComponent<Text>();
        statusText.text = "";
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 16;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = new Color(1f, 0.4f, 0.3f);
        SetRect(statusGo, 0, -215, 500, 30);

        // ===== BOTTOM BUTTONS =====
        // Back button
        var backGo = CreateUIElement("BackButton", canvasGo.transform);
        var backImg = backGo.AddComponent<Image>();
        backImg.color = new Color(0.25f, 0.22f, 0.20f, 0.9f);
        var backBtn = backGo.AddComponent<Button>();
        var bbColors = backBtn.colors;
        bbColors.highlightedColor = new Color(0.35f, 0.30f, 0.28f);
        bbColors.pressedColor = new Color(0.18f, 0.16f, 0.14f);
        backBtn.colors = bbColors;
        backGo.AddComponent<Outline>().effectColor = new Color(0.5f, 0.45f, 0.35f, 0.2f);
        SetRect(backGo, -200, -280, 300, 60);

        var backTextGo = CreateUIElement("Text", backGo.transform);
        var backText = backTextGo.AddComponent<Text>();
        backText.text = "\u25C0  BACK";
        backText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        backText.fontSize = 18;
        backText.fontStyle = FontStyle.Bold;
        backText.alignment = TextAnchor.MiddleCenter;
        backText.color = new Color(0.8f, 0.75f, 0.65f);
        StretchFull(backTextGo);

        // Create button
        var createGo = CreateUIElement("CreateButton", canvasGo.transform);
        var createImg = createGo.AddComponent<Image>();
        createImg.color = BtnColor;
        var createBtn = createGo.AddComponent<Button>();
        var cbColors = createBtn.colors;
        cbColors.highlightedColor = new Color(0.40f, 0.70f, 0.35f, 1f);
        cbColors.pressedColor = new Color(0.25f, 0.50f, 0.22f, 1f);
        cbColors.disabledColor = new Color(0.15f, 0.18f, 0.15f, 0.5f);
        createBtn.colors = cbColors;
        createGo.AddComponent<Outline>().effectColor = new Color(0.65f, 0.85f, 0.45f, 0.3f);
        SetRect(createGo, 200, -280, 300, 60);

        var createTextGo = CreateUIElement("Text", createGo.transform);
        var createText = createTextGo.AddComponent<Text>();
        createText.text = "\u2726  C R E A T E  \u2726";
        createText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        createText.fontSize = 20;
        createText.fontStyle = FontStyle.Bold;
        createText.alignment = TextAnchor.MiddleCenter;
        createText.color = new Color(1f, 1f, 0.95f);
        StretchFull(createTextGo);

        // ===== Wire CharacterCreateUI =====
        var uiGo = new GameObject("CharacterCreateUI");
        var ui = uiGo.AddComponent<Astrion.UI.CharacterCreateUI>();
        var so = new SerializedObject(ui);

        so.FindProperty("nameInput").objectReferenceValue = nameInput;

        var cbArr = so.FindProperty("classButtons");
        cbArr.arraySize = 3;
        for (int i = 0; i < 3; i++)
            cbArr.GetArrayElementAtIndex(i).objectReferenceValue = classButtons[i];

        var chArr = so.FindProperty("classHighlights");
        chArr.arraySize = 3;
        for (int i = 0; i < 3; i++)
            chArr.GetArrayElementAtIndex(i).objectReferenceValue = classHighlights[i];

        so.FindProperty("selectedClassName").objectReferenceValue = selNameText;
        so.FindProperty("selectedClassDesc").objectReferenceValue = selDescText;
        so.FindProperty("createButton").objectReferenceValue = createBtn;
        so.FindProperty("backButton").objectReferenceValue = backBtn;
        so.FindProperty("statusText").objectReferenceValue = statusText;

        so.ApplyModifiedPropertiesWithoutUndo();

        // EventSystem
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/CharacterCreateScene.unity");
        Debug.Log("[Astrion] CharacterCreateScene created and saved.");
    }

    private static void CreateMainScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Remove default 3D directional light (2D doesn't need it)
        var lights = Object.FindObjectsOfType<Light>();
        foreach (var l in lights) if (l.type == LightType.Directional) Object.DestroyImmediate(l.gameObject);

        const int GROUND_LAYER = 8;

        // === 2D Sprites (procedurally generated placeholders) ===
        var skySpr = TexToSprite(Make2DSkyTex(512, 256));
        var farMountainSpr = TexToSprite(Make2DMountainTex(1024, 256, new Color(0.32f, 0.30f, 0.42f), 0.6f));   // distant slate
        var midMountainSpr = TexToSprite(Make2DMountainTex(1024, 256, new Color(0.28f, 0.32f, 0.26f), 0.7f));   // deep forest
        var nearHillSpr   = TexToSprite(Make2DMountainTex(1024, 256, new Color(0.32f, 0.38f, 0.22f), 0.85f));   // pasture green
        var groundSpr = TexToSprite(Make2DGroundTex(256, 64));
        var platformSpr = TexToSprite(Make2DPlatformTex(256, 32));
        var ladderSpr = TexToSprite(Make2DLadderTex(64, 256));

        // Beacon-of-Winds island vista (artist-painted backdrop)
        Sprite islandBgSpr = LoadSpriteAsset("Assets/Scenes/main_bg.png");
        // Medieval peasant adventurer (forest green tunic + leather)
        var localParts = MakePlayerParts(
            shirt: new Color(0.30f, 0.48f, 0.22f),
            hair:  new Color(0.32f, 0.22f, 0.12f),
            pants: new Color(0.38f, 0.26f, 0.16f));
        // Remote players: heraldic crimson tunic (distinct from local)
        var remoteParts = MakePlayerParts(
            shirt: new Color(0.62f, 0.16f, 0.16f),
            hair:  new Color(0.32f, 0.22f, 0.12f),
            pants: new Color(0.38f, 0.26f, 0.16f));

        // === Background root ===
        var bgRoot = new GameObject("Background");

        if (islandBgSpr != null)
        {
            // Single artist backdrop — anchored to camera (parallax 1,1) so it always fills the view.
            // Image is 1536x1024 @ 100 PPU (15.36 x 10.24 units native); scale 2.5 → 38.4 x 25.6, comfortably covers ortho size 6.5.
            var island = SpawnSpriteSimple("IslandBackdrop", bgRoot.transform, islandBgSpr, new Vector3(0, 0, 50), new Vector3(2.5f, 2.5f, 1), -10);
            AddParallax(island, new Vector2(1f, 1f));
        }
        else
        {
            // Fallback: procedural sky + 3 parallax mountain layers
            SpawnSprite("Sky", bgRoot.transform, skySpr, new Vector3(0, 0, 50), new Vector3(40, 25, 1), -10);
            var farLayer = SpawnSprite("FarMountains", bgRoot.transform, farMountainSpr, new Vector3(0, -1.5f, 40), new Vector3(20, 5, 1), -8);
            AddParallax(farLayer, new Vector2(0.1f, 0.05f));
            var midLayer = SpawnSprite("MidMountains", bgRoot.transform, midMountainSpr, new Vector3(0, -2.2f, 30), new Vector3(20, 4, 1), -6);
            AddParallax(midLayer, new Vector2(0.3f, 0.1f));
            var nearLayer = SpawnSprite("NearHills", bgRoot.transform, nearHillSpr, new Vector3(0, -2.8f, 20), new Vector3(20, 3, 1), -4);
            AddParallax(nearLayer, new Vector2(0.55f, 0.15f));
        }

        // === World root ===
        var worldRoot = new GameObject("World");

        // Main ground (long flat platform)
        SpawnGround("Ground_Main", worldRoot.transform, groundSpr,
            center: new Vector2(0, -3.5f), size: new Vector2(60, 1.5f), layer: GROUND_LAYER, oneWay: false);

        // Side ground extensions
        SpawnGround("Ground_Left", worldRoot.transform, groundSpr,
            center: new Vector2(-32, -2.5f), size: new Vector2(6, 1.5f), layer: GROUND_LAYER, oneWay: false);
        SpawnGround("Ground_Right", worldRoot.transform, groundSpr,
            center: new Vector2(32, -2.5f), size: new Vector2(6, 1.5f), layer: GROUND_LAYER, oneWay: false);

        // Elevated one-way platforms
        SpawnGround("Platform_1", worldRoot.transform, platformSpr,
            center: new Vector2(-12, 0.5f), size: new Vector2(8, 0.5f), layer: GROUND_LAYER, oneWay: true);
        SpawnGround("Platform_2", worldRoot.transform, platformSpr,
            center: new Vector2(8, 2.0f), size: new Vector2(8, 0.5f), layer: GROUND_LAYER, oneWay: true);
        SpawnGround("Platform_3", worldRoot.transform, platformSpr,
            center: new Vector2(-4, 4.0f), size: new Vector2(10, 0.5f), layer: GROUND_LAYER, oneWay: true);
        SpawnGround("Platform_4", worldRoot.transform, platformSpr,
            center: new Vector2(-18, 5.5f), size: new Vector2(6, 0.5f), layer: GROUND_LAYER, oneWay: true);
        SpawnGround("Platform_5", worldRoot.transform, platformSpr,
            center: new Vector2(15, 5.0f), size: new Vector2(6, 0.5f), layer: GROUND_LAYER, oneWay: true);

        // Ladders (vertical climbable triggers)
        SpawnLadder("Ladder_1", worldRoot.transform, ladderSpr, center: new Vector2(-10, -1.3f), size: new Vector2(1f, 3.8f));
        SpawnLadder("Ladder_2", worldRoot.transform, ladderSpr, center: new Vector2(6, -0.5f), size: new Vector2(1f, 5.2f));
        SpawnLadder("Ladder_3", worldRoot.transform, ladderSpr, center: new Vector2(-3, 2.3f), size: new Vector2(1f, 3.6f));
        SpawnLadder("Ladder_4", worldRoot.transform, ladderSpr, center: new Vector2(-17, 2.8f), size: new Vector2(1f, 5.4f));

        // Portal to Forgotten Woods (right edge of map)
        SpawnPortal("Portal_ForgottenWoods", worldRoot.transform,
            position: new Vector2(28f, -2.0f), size: new Vector2(1.4f, 2.6f),
            targetScene: "ForgottenWoodsScene");

        // === Player prefab (multi-part body for animation) ===
        var playerPrefab2 = new GameObject("PlayerPrefab");
        playerPrefab2.transform.position = new Vector3(0, 0f, 0);
        BuildPlayerVisual(playerPrefab2, localParts, out var pBody, out var pLArm, out var pRArm, out var pLLeg, out var pRLeg);
        var pBox = playerPrefab2.AddComponent<BoxCollider2D>();
        pBox.size = new Vector2(0.40f, 0.84f);
        pBox.offset = new Vector2(0, 0.02f);
        var pRb = playerPrefab2.AddComponent<Rigidbody2D>();
        pRb.gravityScale = 3f;
        pRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        pRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        pRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var pCtrl = playerPrefab2.AddComponent<Astrion.Game.PlayerController2D>();
        var pAnim = playerPrefab2.AddComponent<Astrion.Game.PlayerAnimator2D>();
        var groundCheckGo = new GameObject("GroundCheck");
        groundCheckGo.transform.SetParent(playerPrefab2.transform, false);
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

        // === Remote player prefab (also multi-part, no controller/animator — pos synced from server) ===
        var remotePrefab = new GameObject("RemotePlayerPrefab");
        remotePrefab.transform.position = new Vector3(100, 100, 0);
        BuildPlayerVisual(remotePrefab, remoteParts, out _, out _, out _, out _, out _);

        // === NPC: Polaris (수도사 풍, opening story-giver) ===
        var polarisParts = MakePlayerParts(
            shirt: new Color(0.32f, 0.24f, 0.16f),  // friar brown robe
            hair:  new Color(0.72f, 0.68f, 0.62f),  // graying
            pants: new Color(0.30f, 0.22f, 0.14f)); // matching robe
        var polaris = new GameObject("NPC_Polaris");
        polaris.transform.position = new Vector3(4f, -2.5f, 0);
        BuildPlayerVisual(polaris, polarisParts, out _, out _, out _, out _, out _);
        var polarisCol = polaris.AddComponent<BoxCollider2D>();
        polarisCol.size = new Vector2(2.4f, 2.2f);
        polarisCol.offset = Vector2.zero;
        polarisCol.isTrigger = true;
        var npc = polaris.AddComponent<Astrion.Game.NPC2D>();
        var npcSo = new SerializedObject(npc);
        npcSo.FindProperty("npcName").stringValue = "폴라리스";

        var stages = npcSo.FindProperty("questStages");
        stages.arraySize = 2;

        // Stage 1: 흩어진 별의 조각
        SetQuestStage(stages.GetArrayElementAtIndex(0),
            id: "star_fragments", title: "흩어진 별의 조각", target: 5,
            intro: new[] {
                "오, 드디어 깨어났구나... 별빛이 다시 흐르는 게 느껴진다.",
                "여기는 '바람의 등대섬'. 천 년 전 대별(大星)이 추락하며 산산조각 난 세계의 한 조각이지.",
                "그날 별의 파편을 받은 자들이 천체의 힘을 다루는 '별의 후예'가 되었다. 너도 그중 하나야.",
                "오랜 세월이 흘러 그 힘은 잊혔지만... 별빛은 새로운 후예를 골랐지. 바로 너다.",
                "이 섬 곳곳에 별의 조각이 흩어져 있다. 다섯 개를 모아 와다오 — 네 잠든 힘을 깨우는 첫 걸음이 될 게야.",
                "위로, 더 위로 올라가 보거라. 행운을 빈다.",
            },
            reminder: new[] { "별의 조각은 아직 다 모이지 않았구나. 섬 곳곳, 높은 곳을 살펴보거라." },
            completion: new[] {
                "오... 가져왔구나. 별빛이 다시 흐르는 게 느껴진다.",
                "이 조각들이 너의 잠든 힘을 일깨워줄 게다.",
                "이제 너는 진정한 별의 후예다.",
            });

        // Stage 2: 별의 힘 깨우기
        SetQuestStage(stages.GetArrayElementAtIndex(1),
            id: "awaken_power", title: "별의 힘 깨우기", target: 3,
            intro: new[] {
                "조각들이 모이니 너의 손에 별빛이 머무는 게 보이는구나...",
                "별의 후예가 가장 먼저 익히는 건 '별빛 투사체' — 멀리 있는 적을 향해 빛 한 조각을 던지는 것이다.",
                "[Q] 키를 누르면 손에 모인 별빛이 앞으로 뻗어 나갈 게야.",
                "이 섬에는 옛 기사단이 남긴 훈련용 표적 셋이 있다. 그 셋을 부숴 보거라 — 손에 익을 때까지.",
            },
            reminder: new[] { "아직 별빛이 너에게 익숙지 않은 모양이군. 표적 셋을 마저 부숴 보거라." },
            completion: new[] {
                "훌륭하다. 너의 별빛이 손에 익기 시작했구나.",
                "이 힘으로 닥쳐올 위협을 막을 수 있을 것이야.",
                "다음 단계는... 다른 하늘섬으로 가는 길을 여는 것이다.",
                "이 섬의 동쪽 절벽으로 가 보거라. 잊혀진 숲으로 통하는 포탈이 보일 게야.",
                "그 숲 너머에는 '여명의 성채' — 별의 후예들이 모이는 곳이 있다. 카시오를 만나거라.",
            });

        SetStringArray(npcSo, "idleLines", new[] {
            "별빛이 너의 길을 비추기를.",
            "동쪽 절벽의 포탈을 잊지 말거라. 카시오가 너를 기다리고 있을 것이야.",
        });
        npcSo.ApplyModifiedPropertiesWithoutUndo();

        // === StarBolt prefab template (off-screen, used by PlayerController2D.Instantiate) ===
        var boltTex = MakeStarBoltTex(32);
        var boltSpr = TexToSprite(boltTex);
        var starBoltPrefab = new GameObject("StarBoltPrefab");
        starBoltPrefab.transform.position = new Vector3(200f, 200f, 0f); // far off-screen
        starBoltPrefab.SetActive(false); // dormant template
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

        // Wire star bolt to player controller
        var pCtrlSo = new SerializedObject(pCtrl);
        pCtrlSo.FindProperty("starBoltPrefab").objectReferenceValue = starBoltPrefab;
        pCtrlSo.ApplyModifiedPropertiesWithoutUndo();

        // === Target dummies (3 around the map for star bolt training) ===
        var dummyTex = MakeTargetDummyTex(48, 96);
        var dummySpr = TexToSprite(dummyTex);
        Vector2[] dummyPositions = {
            new Vector2(-22f, -2.7f), // far left ground
            new Vector2(8f, 2.7f),    // Platform_2
            new Vector2(-18f, 6.2f),  // Platform_4 (top)
        };
        for (int i = 0; i < dummyPositions.Length; i++)
        {
            var dummy = new GameObject($"TargetDummy_{i + 1}");
            dummy.transform.position = new Vector3(dummyPositions[i].x, dummyPositions[i].y, 0);
            var dVisual = new GameObject("Visual");
            dVisual.transform.SetParent(dummy.transform, false);
            var dsr = dVisual.AddComponent<SpriteRenderer>();
            dsr.sprite = dummySpr;
            dsr.sortingOrder = 7;
            var dCol = dummy.AddComponent<BoxCollider2D>();
            dCol.size = new Vector2(0.48f, 0.96f);
            dCol.isTrigger = true;
            var td = dummy.AddComponent<Astrion.Game.TargetDummy2D>();
            var tdSo = new SerializedObject(td);
            tdSo.FindProperty("dummyId").stringValue = $"dummy_{i + 1}";
            tdSo.FindProperty("questId").stringValue = "awaken_power";
            tdSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // === Quest system ===
        var questSysGo = new GameObject("QuestSystem");
        questSysGo.AddComponent<Astrion.Game.QuestSystem>();

        // === Inventory system ===
        var invSysGo = new GameObject("InventorySystem");
        invSysGo.AddComponent<Astrion.Game.InventorySystem>();

        // === World item pickups (scattered around the map) ===
        var pickupSpawns = new (string pickupId, string itemId, int qty, Vector2 pos, Color color, string letter)[] {
            ("pickup_bread1",    "bread",    1, new Vector2(-14f, -2.6f), new Color(0.78f, 0.55f, 0.28f), "빵"),
            ("pickup_bread2",    "bread",    1, new Vector2(13f, -2.6f),  new Color(0.78f, 0.55f, 0.28f), "빵"),
            ("pickup_elixir1",   "elixir",   1, new Vector2(-4f, 4.7f),   new Color(0.30f, 0.55f, 0.92f), "약"),
            ("pickup_stardust1", "stardust", 3, new Vector2(-12f, 1.2f),  new Color(0.95f, 0.78f, 0.30f), "★"),
            ("pickup_stardust2", "stardust", 5, new Vector2(15f, 5.7f),   new Color(0.95f, 0.78f, 0.30f), "★"),
            ("pickup_dagger1",   "dagger",   1, new Vector2(-22f, 6.2f),  new Color(0.55f, 0.45f, 0.35f), "검"),
        };
        var itemBgTex = MakeRoundRectTex(48, 48, 8, Color.white);
        var itemBgSpr = TexToSprite(itemBgTex);
        foreach (var p in pickupSpawns)
        {
            var go = new GameObject($"Pickup_{p.pickupId}");
            go.transform.position = new Vector3(p.pos.x, p.pos.y, 0);
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.30f;
            col.isTrigger = true;
            var pk = go.AddComponent<Astrion.Game.WorldItemPickup2D>();
            var pkSo = new SerializedObject(pk);
            pkSo.FindProperty("pickupId").stringValue = p.pickupId;
            pkSo.FindProperty("itemId").stringValue = p.itemId;
            pkSo.FindProperty("quantity").intValue = p.qty;
            pkSo.ApplyModifiedPropertiesWithoutUndo();

            // Visual: colored rounded square + letter
            var bg = new GameObject("Bg");
            bg.transform.SetParent(go.transform, false);
            bg.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            var bsr = bg.AddComponent<SpriteRenderer>();
            bsr.sprite = itemBgSpr;
            bsr.color = p.color;
            bsr.sortingOrder = 8;

            // World-space text via TextMesh (legacy, simple)
            var letterGo = new GameObject("Letter");
            letterGo.transform.SetParent(go.transform, false);
            letterGo.transform.localScale = new Vector3(0.04f, 0.04f, 1f);
            var tm = letterGo.AddComponent<TextMesh>();
            tm.text = p.letter;
            tm.fontSize = 36;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.10f, 0.07f, 0.04f);
            var tmr = letterGo.GetComponent<MeshRenderer>();
            tmr.sortingOrder = 9;
        }

        // === Star fragments (5 collectibles on platforms) ===
        var fragSpr = TexToSprite(MakeStarFragmentTex(32));
        Vector2[] fragPositions = {
            new Vector2(-12, 1.4f),   // Platform_1
            new Vector2(8, 2.9f),     // Platform_2
            new Vector2(-4, 4.9f),    // Platform_3
            new Vector2(-18, 6.4f),   // Platform_4
            new Vector2(15, 5.9f),    // Platform_5
        };
        for (int i = 0; i < fragPositions.Length; i++)
        {
            var frag = new GameObject($"StarFragment_{i + 1}");
            frag.transform.position = new Vector3(fragPositions[i].x, fragPositions[i].y, 0);
            var fc = frag.AddComponent<CircleCollider2D>();
            fc.radius = 0.35f;
            fc.isTrigger = true;
            var sf = frag.AddComponent<Astrion.Game.StarFragment2D>();
            var sfSo = new SerializedObject(sf);
            sfSo.FindProperty("fragmentId").stringValue = $"frag_{i + 1}";
            sfSo.FindProperty("questId").stringValue = "star_fragments";
            sfSo.ApplyModifiedPropertiesWithoutUndo();
            // Visual child (rotates separately from physics body)
            var fragVis = new GameObject("Visual");
            fragVis.transform.SetParent(frag.transform, false);
            var fsr = fragVis.AddComponent<SpriteRenderer>();
            fsr.sprite = fragSpr;
            fsr.sortingOrder = 8;
        }

        // === Orthographic Camera ===
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 6.5f;
            mainCam.transform.position = new Vector3(0, 0, -10);
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.62f, 0.58f, 0.55f); // muted medieval haze fallback
            mainCam.farClipPlane = 100f;
            mainCam.gameObject.AddComponent<Astrion.Game.Camera2D>();
        }

        // === GameManager ===
        var gameManagerGo = new GameObject("GameManager");
        var gm = gameManagerGo.AddComponent<Astrion.Game.GameManager>();
        // Note: GameManager.playerPrefab field is intentionally left null;
        // the scene-time PlayerPrefab object is the local player.
        var gmSo = new SerializedObject(gm);
        gmSo.FindProperty("remotePlayerPrefab").objectReferenceValue = remotePrefab;
        gmSo.FindProperty("starBoltPrefab").objectReferenceValue = starBoltPrefab;
        gmSo.ApplyModifiedPropertiesWithoutUndo();

        // ========== GAME HUD ==========
        CreateGameHUD(playerPrefab2, 0f);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainScene.unity");
        Debug.Log("[Astrion] MainScene created and saved (2D).");
    }

    private static void CreateForgottenWoodsScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var lights = Object.FindObjectsOfType<Light>();
        foreach (var l in lights) if (l.type == LightType.Directional) Object.DestroyImmediate(l.gameObject);

        const int GROUND_LAYER = 8;

        var skySpr = TexToSprite(MakeForestSkyTex(512, 256));
        var farMountainSpr = TexToSprite(Make2DMountainTex(1024, 256, new Color(0.22f, 0.20f, 0.30f), 0.6f));
        var midMountainSpr = TexToSprite(Make2DMountainTex(1024, 256, new Color(0.18f, 0.25f, 0.20f), 0.7f));
        var nearHillSpr   = TexToSprite(Make2DMountainTex(1024, 256, new Color(0.18f, 0.28f, 0.18f), 0.85f));
        var groundSpr = TexToSprite(Make2DGroundTex(256, 64));
        var platformSpr = TexToSprite(Make2DPlatformTex(256, 32));
        var ladderSpr = TexToSprite(Make2DLadderTex(64, 256));
        var localParts = MakePlayerParts(
            shirt: new Color(0.30f, 0.48f, 0.22f),
            hair:  new Color(0.32f, 0.22f, 0.12f),
            pants: new Color(0.38f, 0.26f, 0.16f));
        var remoteParts = MakePlayerParts(
            shirt: new Color(0.62f, 0.16f, 0.16f),
            hair:  new Color(0.32f, 0.22f, 0.12f),
            pants: new Color(0.38f, 0.26f, 0.16f));
        var monsterSpr = TexToSprite(MakeMonsterTex(48, 36));

        // Background
        var bgRoot = new GameObject("Background");
        SpawnSprite("Sky", bgRoot.transform, skySpr, new Vector3(0, 0, 50), new Vector3(40, 25, 1), -10);
        var farLayer = SpawnSprite("FarMountains", bgRoot.transform, farMountainSpr, new Vector3(0, -1.5f, 40), new Vector3(20, 5, 1), -8);
        AddParallax(farLayer, new Vector2(0.1f, 0.05f));
        var midLayer = SpawnSprite("MidMountains", bgRoot.transform, midMountainSpr, new Vector3(0, -2.2f, 30), new Vector3(20, 4, 1), -6);
        AddParallax(midLayer, new Vector2(0.3f, 0.1f));
        var nearLayer = SpawnSprite("NearHills", bgRoot.transform, nearHillSpr, new Vector3(0, -2.8f, 20), new Vector3(20, 3, 1), -4);
        AddParallax(nearLayer, new Vector2(0.55f, 0.15f));

        // World
        var worldRoot = new GameObject("World");
        SpawnGround("Ground_Main", worldRoot.transform, groundSpr, new Vector2(0, -3.5f), new Vector2(50, 1.5f), GROUND_LAYER, false);
        SpawnGround("Platform_1", worldRoot.transform, platformSpr, new Vector2(-6, 1f), new Vector2(8, 0.5f), GROUND_LAYER, true);
        SpawnGround("Platform_2", worldRoot.transform, platformSpr, new Vector2(10, 2.5f), new Vector2(8, 0.5f), GROUND_LAYER, true);
        SpawnGround("Platform_3", worldRoot.transform, platformSpr, new Vector2(0, 5f), new Vector2(10, 0.5f), GROUND_LAYER, true);
        SpawnLadder("Ladder_1", worldRoot.transform, ladderSpr, new Vector2(-4, -0.5f), new Vector2(1f, 3.6f));
        SpawnLadder("Ladder_2", worldRoot.transform, ladderSpr, new Vector2(8, 0.8f), new Vector2(1f, 3.4f));

        // Return portal at left edge
        SpawnPortal("Portal_BackToMain", worldRoot.transform,
            position: new Vector2(-22f, -2.0f), size: new Vector2(1.4f, 2.6f),
            targetScene: "MainScene");

        // Portal to Citadel of Dawn (right edge, past Shadow Hulk)
        SpawnPortal("Portal_CitadelOfDawn", worldRoot.transform,
            position: new Vector2(24f, -2.0f), size: new Vector2(1.4f, 2.6f),
            targetScene: "CitadelOfDawnScene");

        // Player prefab spawn at left
        var playerPrefab2 = new GameObject("PlayerPrefab");
        playerPrefab2.transform.position = new Vector3(-18f, 0f, 0);
        BuildPlayerVisual(playerPrefab2, localParts, out var pBody, out var pLArm, out var pRArm, out var pLLeg, out var pRLeg);
        var pBox = playerPrefab2.AddComponent<BoxCollider2D>();
        pBox.size = new Vector2(0.40f, 0.84f);
        pBox.offset = new Vector2(0, 0.02f);
        var pRb = playerPrefab2.AddComponent<Rigidbody2D>();
        pRb.gravityScale = 3f;
        pRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        pRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        pRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var pCtrl = playerPrefab2.AddComponent<Astrion.Game.PlayerController2D>();
        var pAnim = playerPrefab2.AddComponent<Astrion.Game.PlayerAnimator2D>();
        var groundCheckGo = new GameObject("GroundCheck");
        groundCheckGo.transform.SetParent(playerPrefab2.transform, false);
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

        // Remote player prefab
        var remotePrefab = new GameObject("RemotePlayerPrefab");
        remotePrefab.transform.position = new Vector3(100, 100, 0);
        BuildPlayerVisual(remotePrefab, remoteParts, out _, out _, out _, out _, out _);

        // StarBolt prefab template
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
            mainCam.transform.position = new Vector3(-18, 0, -10);
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.22f, 0.20f, 0.30f);
            mainCam.farClipPlane = 100f;
            mainCam.gameObject.AddComponent<Astrion.Game.Camera2D>();
        }

        // GameManager
        var gameManagerGo = new GameObject("GameManager");
        var gm = gameManagerGo.AddComponent<Astrion.Game.GameManager>();
        var gmSo = new SerializedObject(gm);
        gmSo.FindProperty("remotePlayerPrefab").objectReferenceValue = remotePrefab;
        gmSo.FindProperty("starBoltPrefab").objectReferenceValue = starBoltPrefab;
        gmSo.ApplyModifiedPropertiesWithoutUndo();

        // QuestSystem + InventorySystem (DDOL singletons — dedupe if pre-existing)
        var questSysGo = new GameObject("QuestSystem");
        questSysGo.AddComponent<Astrion.Game.QuestSystem>();
        var invSysGo = new GameObject("InventorySystem");
        invSysGo.AddComponent<Astrion.Game.InventorySystem>();

        // Monsters: now server-authoritative — spawned by MonsterNetworkManager via MONSTER_SPAWN packets.

        // HUD
        CreateGameHUD(playerPrefab2, 0f);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ForgottenWoodsScene.unity");
        Debug.Log("[Astrion] ForgottenWoodsScene created and saved (2D).");
    }

    private static void CreateCitadelOfDawnScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var lights = Object.FindObjectsOfType<Light>();
        foreach (var l in lights) if (l.type == LightType.Directional) Object.DestroyImmediate(l.gameObject);

        const int GROUND_LAYER = 8;

        // Dawn-tinted sky
        var skySpr = TexToSprite(Make2DSkyTex(512, 256));
        var farMountainSpr = TexToSprite(Make2DMountainTex(1024, 256, new Color(0.45f, 0.32f, 0.42f), 0.6f));
        var midMountainSpr = TexToSprite(Make2DMountainTex(1024, 256, new Color(0.55f, 0.38f, 0.38f), 0.7f));
        var nearWallSpr   = TexToSprite(Make2DMountainTex(1024, 256, new Color(0.42f, 0.32f, 0.28f), 0.85f));
        var groundSpr = TexToSprite(Make2DGroundTex(256, 64));
        var platformSpr = TexToSprite(Make2DPlatformTex(256, 32));
        var localParts = MakePlayerParts(
            shirt: new Color(0.30f, 0.48f, 0.22f),
            hair:  new Color(0.32f, 0.22f, 0.12f),
            pants: new Color(0.38f, 0.26f, 0.16f));
        var remoteParts = MakePlayerParts(
            shirt: new Color(0.62f, 0.16f, 0.16f),
            hair:  new Color(0.32f, 0.22f, 0.12f),
            pants: new Color(0.38f, 0.26f, 0.16f));

        // Background
        var bgRoot = new GameObject("Background");
        SpawnSprite("Sky", bgRoot.transform, skySpr, new Vector3(0, 0, 50), new Vector3(40, 25, 1), -10);
        var farLayer = SpawnSprite("FarMountains", bgRoot.transform, farMountainSpr, new Vector3(0, -1.5f, 40), new Vector3(20, 5, 1), -8);
        AddParallax(farLayer, new Vector2(0.1f, 0.05f));
        var midLayer = SpawnSprite("MidWalls", bgRoot.transform, midMountainSpr, new Vector3(0, -2.2f, 30), new Vector3(20, 4, 1), -6);
        AddParallax(midLayer, new Vector2(0.3f, 0.1f));
        var nearLayer = SpawnSprite("NearWalls", bgRoot.transform, nearWallSpr, new Vector3(0, -2.8f, 20), new Vector3(20, 3, 1), -4);
        AddParallax(nearLayer, new Vector2(0.55f, 0.15f));

        // World
        var worldRoot = new GameObject("World");
        SpawnGround("Ground_Main", worldRoot.transform, groundSpr, new Vector2(0, -3.5f), new Vector2(50, 1.5f), GROUND_LAYER, false);
        SpawnGround("Plaza_Step", worldRoot.transform, platformSpr, new Vector2(0, -1.2f), new Vector2(10, 0.5f), GROUND_LAYER, true);
        SpawnGround("Watch_Platform", worldRoot.transform, platformSpr, new Vector2(-10, 1.5f), new Vector2(7, 0.5f), GROUND_LAYER, true);
        SpawnGround("Tower_Top", worldRoot.transform, platformSpr, new Vector2(12, 2.5f), new Vector2(7, 0.5f), GROUND_LAYER, true);

        // Return portal to Forgotten Woods (left edge)
        SpawnPortal("Portal_BackToForgottenWoods", worldRoot.transform,
            position: new Vector2(-22f, -2.0f), size: new Vector2(1.4f, 2.6f),
            targetScene: "ForgottenWoodsScene");

        // === NPC: Cassio (Act II 인트로 — 별의 후예 검사) ===
        var cassioParts = MakePlayerParts(
            shirt: new Color(0.18f, 0.32f, 0.52f),  // 푸른 갑주
            hair:  new Color(0.18f, 0.14f, 0.10f),
            pants: new Color(0.22f, 0.20f, 0.26f));
        var cassio = new GameObject("NPC_Cassio");
        cassio.transform.position = new Vector3(2f, -2.5f, 0);
        BuildPlayerVisual(cassio, cassioParts, out _, out _, out _, out _, out _);
        var cassioCol = cassio.AddComponent<BoxCollider2D>();
        cassioCol.size = new Vector2(2.4f, 2.2f);
        cassioCol.offset = Vector2.zero;
        cassioCol.isTrigger = true;
        var cassioNpc = cassio.AddComponent<Astrion.Game.NPC2D>();
        var cassioSo = new SerializedObject(cassioNpc);
        cassioSo.FindProperty("npcName").stringValue = "카시오";
        SetStringArray(cassioSo, "idleLines", new[] {
            "오... 새로운 별의 후예구나. 잊혀진 숲을 건너 여기까지 오다니, 보통이 아니야.",
            "여기는 '여명의 성채' — 별의 후예들이 모여 힘을 갈고닦는 곳이지.",
            "최근 식자(蝕者)의 그림자가 이 변두리까지 닿고 있어. 폴라리스 노인이 너를 보낸 이유가 그것일 게야.",
            "쉬고, 둘러보고, 다음 길을 준비하거라. 곧 너의 별빛이 필요할 때가 올 것이다.",
        });
        cassioSo.ApplyModifiedPropertiesWithoutUndo();

        // Player prefab spawn at left
        var playerPrefab2 = new GameObject("PlayerPrefab");
        playerPrefab2.transform.position = new Vector3(-18f, 0f, 0);
        BuildPlayerVisual(playerPrefab2, localParts, out var pBody, out var pLArm, out var pRArm, out var pLLeg, out var pRLeg);
        var pBox = playerPrefab2.AddComponent<BoxCollider2D>();
        pBox.size = new Vector2(0.40f, 0.84f);
        pBox.offset = new Vector2(0, 0.02f);
        var pRb = playerPrefab2.AddComponent<Rigidbody2D>();
        pRb.gravityScale = 3f;
        pRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        pRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        pRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var pCtrl = playerPrefab2.AddComponent<Astrion.Game.PlayerController2D>();
        var pAnim = playerPrefab2.AddComponent<Astrion.Game.PlayerAnimator2D>();
        var groundCheckGo = new GameObject("GroundCheck");
        groundCheckGo.transform.SetParent(playerPrefab2.transform, false);
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

        // Remote player prefab
        var remotePrefab = new GameObject("RemotePlayerPrefab");
        remotePrefab.transform.position = new Vector3(100, 100, 0);
        BuildPlayerVisual(remotePrefab, remoteParts, out _, out _, out _, out _, out _);

        // StarBolt prefab template
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
            mainCam.transform.position = new Vector3(-18, 0, -10);
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.42f, 0.28f, 0.32f);
            mainCam.farClipPlane = 100f;
            mainCam.gameObject.AddComponent<Astrion.Game.Camera2D>();
        }

        // GameManager
        var gameManagerGo = new GameObject("GameManager");
        var gm = gameManagerGo.AddComponent<Astrion.Game.GameManager>();
        var gmSo = new SerializedObject(gm);
        gmSo.FindProperty("remotePlayerPrefab").objectReferenceValue = remotePrefab;
        gmSo.FindProperty("starBoltPrefab").objectReferenceValue = starBoltPrefab;
        gmSo.ApplyModifiedPropertiesWithoutUndo();

        // QuestSystem + InventorySystem (DDOL singletons — dedupe if pre-existing)
        var questSysGo = new GameObject("QuestSystem");
        questSysGo.AddComponent<Astrion.Game.QuestSystem>();
        var invSysGo = new GameObject("InventorySystem");
        invSysGo.AddComponent<Astrion.Game.InventorySystem>();

        // HUD
        CreateGameHUD(playerPrefab2, 0f);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/CitadelOfDawnScene.unity");
        Debug.Log("[Astrion] CitadelOfDawnScene created and saved (2D).");
    }

    // ---- 2D helper spawners ----
    private static GameObject SpawnSprite(string name, Transform parent, Sprite sprite, Vector3 pos, Vector3 scale, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(1, 1);
        return go;
    }

    private static void AddParallax(GameObject layer, Vector2 factor)
    {
        var p = layer.AddComponent<Astrion.Game.Parallax2D>();
        var so = new SerializedObject(p);
        so.FindProperty("parallaxFactor").vector2Value = factor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject SpawnSpriteSimple(string name, Transform parent, Sprite sprite, Vector3 pos, Vector3 scale, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        sr.drawMode = SpriteDrawMode.Simple;
        return go;
    }

    private static Sprite LoadSpriteAsset(string assetPath)
    {
        if (!System.IO.File.Exists(assetPath)) return null;
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static void SpawnGround(string name, Transform parent, Sprite sprite, Vector2 center, Vector2 size, int layer, bool oneWay)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, 0);
        go.layer = layer;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.sortingOrder = 5;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        if (oneWay) go.AddComponent<Astrion.Game.OneWayPlatform2D>();
    }

    private static GameObject SpawnPortal(string name, Transform parent, Vector2 position, Vector2 size, string targetScene)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(position.x, position.y, 0);
        // Visual
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = TexToSprite(MakePortalTex(64, 128));
        sr.sortingOrder = 7;
        visual.transform.localScale = new Vector3(size.x, size.y / 1.28f, 1f);
        // Collider
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.isTrigger = true;
        var portal = go.AddComponent<Astrion.Game.Portal2D>();
        var so = new SerializedObject(portal);
        so.FindProperty("targetScene").stringValue = targetScene;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    private static void SpawnLadder(string name, Transform parent, Sprite sprite, Vector2 center, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.sortingOrder = 4;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.isTrigger = true;
        go.AddComponent<Astrion.Game.Ladder2D>();
    }

    // ---- 2D placeholder texture generators ----
    private static Texture2D Make2DSkyTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        // Medieval morning haze: muted blue sky → warm horizon
        Color top = new Color(0.55f, 0.60f, 0.70f);
        Color bot = new Color(0.92f, 0.80f, 0.62f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float t = (float)y / (h - 1);
                tex.SetPixel(x, y, Color.Lerp(bot, top, t));
            }
        tex.Apply(); return tex;
    }

    private static Texture2D Make2DMountainTex(int w, int h, Color color, float fillRatio)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        // jagged mountain silhouette via noise
        for (int x = 0; x < w; x++)
        {
            float n = Mathf.PerlinNoise(x * 0.012f, 0f);
            float n2 = Mathf.PerlinNoise(x * 0.05f + 100, 50f) * 0.3f;
            int peak = (int)((n + n2) * h * fillRatio);
            for (int y = 0; y < h; y++)
            {
                if (y < peak)
                {
                    float shade = 0.85f + (y / (float)peak) * 0.25f;
                    tex.SetPixel(x, y, new Color(color.r * shade, color.g * shade, color.b * shade, 1f));
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
        }
        tex.Apply(); return tex;
    }

    private static Texture2D Make2DGroundTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        int grassTop = h - 8;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                float n2 = Mathf.PerlinNoise(x * 0.25f + 50, y * 0.25f + 50) * 0.3f;
                Color c;
                if (y >= grassTop)
                    // Muted moss/grass over old earth
                    c = Color.Lerp(new Color(0.28f, 0.38f, 0.20f), new Color(0.42f, 0.52f, 0.25f), n);
                else
                    // Rich packed dirt (peat brown)
                    c = Color.Lerp(new Color(0.32f, 0.22f, 0.12f), new Color(0.48f, 0.34f, 0.20f), n + n2);
                tex.SetPixel(x, y, c);
            }
        tex.Apply(); return tex;
    }

    private static Texture2D Make2DPlatformTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        // Medieval oak plank: light top edge → mid oak grain → shadowed underside
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float grain = Mathf.PerlinNoise(x * 0.04f, y * 0.55f); // horizontal wood grain
                Color baseC;
                if (y >= h - 4)
                    baseC = Color.Lerp(new Color(0.68f, 0.52f, 0.30f), new Color(0.80f, 0.62f, 0.36f), grain);  // top highlight
                else if (y >= h - 14)
                    baseC = Color.Lerp(new Color(0.48f, 0.32f, 0.18f), new Color(0.62f, 0.42f, 0.22f), grain);  // oak body
                else
                    baseC = Color.Lerp(new Color(0.28f, 0.18f, 0.10f), new Color(0.38f, 0.25f, 0.14f), grain);  // shadowed underside
                // Iron banding on left/right edges
                if ((x < 4 || x >= w - 4) && y < h - 4)
                    baseC = Color.Lerp(baseC, new Color(0.24f, 0.22f, 0.20f), 0.65f);
                tex.SetPixel(x, y, baseC);
            }
        tex.Apply(); return tex;
    }

    private static Texture2D Make2DLadderTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color rail = new Color(0.58f, 0.38f, 0.20f);
        Color rung = new Color(0.72f, 0.50f, 0.28f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool isLeftRail = x >= 4 && x <= 12;
                bool isRightRail = x >= w - 13 && x <= w - 5;
                bool isRung = (y % 24) < 6 && x >= 4 && x <= w - 5;
                if (isRung) tex.SetPixel(x, y, rung);
                else if (isLeftRail || isRightRail) tex.SetPixel(x, y, rail);
                else tex.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        tex.Apply(); return tex;
    }

    // === Multi-part body sprite helpers ===
    private struct PlayerParts
    {
        public Sprite head, body, arm, leg;
    }

    private static PlayerParts MakePlayerParts(Color shirt)
        => MakePlayerParts(shirt, new Color(0.20f, 0.15f, 0.10f), new Color(0.20f, 0.22f, 0.30f));

    private static PlayerParts MakePlayerParts(Color shirt, Color hair, Color pants)
    {
        Color skin = new Color(1f, 0.85f, 0.72f);
        Color outline = new Color(0.05f, 0.04f, 0.06f);

        return new PlayerParts
        {
            head = TexToSpriteWithPivot(MakeHeadTex(28, 28, skin, hair, outline), new Vector2(0.5f, 0.5f)),
            body = TexToSpriteWithPivot(MakeRectOutlineTex(26, 30, shirt, outline), new Vector2(0.5f, 0.5f)),
            arm  = TexToSpriteWithPivot(MakeRectOutlineTex(8, 26, shirt, outline), new Vector2(0.5f, 1f)),
            leg  = TexToSpriteWithPivot(MakeRectOutlineTex(10, 28, pants, outline), new Vector2(0.5f, 1f)),
        };
    }

    private static Texture2D MakeRectOutlineTex(int w, int h, Color fill, Color outline)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool isEdge = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                tex.SetPixel(x, y, isEdge ? outline : fill);
            }
        tex.Apply(); return tex;
    }

    private static Texture2D MakeHeadTex(int w, int h, Color skin, Color hair, Color outline)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        int hairLine = h * 5 / 8; // hair on top 3/8
        int eyeY = h * 7 / 16;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool isEdge = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                Color c;
                if (isEdge) c = outline;
                else if (y >= hairLine) c = hair;
                else c = skin;
                // Simple dot eyes
                if (y == eyeY && (x == w / 2 - 4 || x == w / 2 + 3)) c = outline;
                tex.SetPixel(x, y, c);
            }
        tex.Apply(); return tex;
    }

    private static Sprite TexToSpriteWithPivot(Texture2D tex, Vector2 pivot)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 100, 0, SpriteMeshType.FullRect);
    }

    private static Texture2D MakeStarFragmentTex(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color goldBright = new Color(1f, 0.95f, 0.60f);
        Color gold = new Color(1f, 0.85f, 0.25f);
        Color outline = new Color(0.50f, 0.32f, 0.05f);
        Color clear = new Color(0, 0, 0, 0);
        float cx = size * 0.5f, cy = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / cx;
                float dy = (y - cy) / cy;
                // 4-point sparkle: |dx|+|dy| diamond, with pinched corners
                float diamond = Mathf.Abs(dx) + Mathf.Abs(dy);
                Color c;
                if (diamond < 0.35f) c = goldBright;
                else if (diamond < 0.78f) c = gold;
                else if (diamond < 0.92f) c = outline;
                else c = clear;
                tex.SetPixel(x, y, c);
            }
        tex.Apply(); return tex;
    }

    private static void SetStringArray(SerializedObject so, string propName, string[] values)
    {
        var prop = so.FindProperty(propName);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    private static void SetStringArrayProp(SerializedProperty prop, string[] values)
    {
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    private static void SetQuestStage(SerializedProperty stage, string id, string title, int target,
        string[] intro, string[] reminder, string[] completion)
    {
        stage.FindPropertyRelative("questId").stringValue = id;
        stage.FindPropertyRelative("questTitle").stringValue = title;
        stage.FindPropertyRelative("questTarget").intValue = target;
        SetStringArrayProp(stage.FindPropertyRelative("introLines"), intro);
        SetStringArrayProp(stage.FindPropertyRelative("reminderLines"), reminder);
        SetStringArrayProp(stage.FindPropertyRelative("completionLines"), completion);
    }

    private static Texture2D MakeStarBoltTex(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color core = new Color(1f, 0.98f, 0.75f);
        Color halo = new Color(1f, 0.82f, 0.30f);
        Color outline = new Color(0.55f, 0.35f, 0.05f);
        float cx = size * 0.5f, cy = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / cx;
                float dy = (y - cy) / cy;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                Color c;
                if (r < 0.35f) c = core;
                else if (r < 0.78f) c = halo;
                else if (r < 0.92f) c = outline;
                else c = new Color(0, 0, 0, 0);
                tex.SetPixel(x, y, c);
            }
        tex.Apply(); return tex;
    }

    private static Texture2D MakeWoodGradTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color top = new Color(0.353f, 0.251f, 0.157f, 1f); // #5a4028
        Color bot = new Color(0.102f, 0.063f, 0.031f, 1f); // #1a1008
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float t = 1f - (float)y / (h - 1);
                Color c = Color.Lerp(top, bot, t);
                float n = Mathf.PerlinNoise(x * 0.05f, y * 0.3f) * 0.06f - 0.03f;
                c = new Color(Mathf.Clamp01(c.r + n), Mathf.Clamp01(c.g + n), Mathf.Clamp01(c.b + n), 1f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply(); return tex;
    }

    private static Texture2D MakeParchmentTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color top = new Color(0.910f, 0.831f, 0.627f, 1f); // #e8d4a0
        Color bot = new Color(0.784f, 0.659f, 0.408f, 1f); // #c8a868
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float t = 1f - (float)y / (h - 1);
                Color c = Color.Lerp(top, bot, t);
                float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.05f - 0.025f;
                c = new Color(Mathf.Clamp01(c.r + n), Mathf.Clamp01(c.g + n), Mathf.Clamp01(c.b + n), 1f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply(); return tex;
    }

    private static Texture2D MakePortalTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color core = new Color(0.85f, 0.65f, 1f);
        Color mid = new Color(0.55f, 0.30f, 0.85f);
        Color edge = new Color(0.20f, 0.10f, 0.40f);
        Color clear = new Color(0, 0, 0, 0);
        float cx = w * 0.5f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx) / (w * 0.5f);
                float swirl = Mathf.Sin(y * 0.25f + dx * 4f) * 0.12f;
                float fade = 1f - Mathf.Pow(dx, 3f);
                if (fade <= 0) { tex.SetPixel(x, y, clear); continue; }
                Color c = dx < 0.35f ? Color.Lerp(core, mid, dx / 0.35f + swirl)
                                      : Color.Lerp(mid, edge, (dx - 0.35f) / 0.65f);
                tex.SetPixel(x, y, new Color(c.r, c.g, c.b, fade));
            }
        tex.Apply(); return tex;
    }

    private static Texture2D MakeForestSkyTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color top = new Color(0.18f, 0.16f, 0.28f);  // deep dusk
        Color bot = new Color(0.42f, 0.35f, 0.30f);  // warm horizon
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float t = (float)y / (h - 1);
                tex.SetPixel(x, y, Color.Lerp(bot, top, t));
            }
        tex.Apply(); return tex;
    }

    private static Texture2D MakeMonsterTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color body = new Color(0.30f, 0.62f, 0.38f);     // mossy green
        Color bodyDark = new Color(0.18f, 0.42f, 0.25f);
        Color outline = new Color(0.05f, 0.10f, 0.06f);
        Color eyeWhite = new Color(0.92f, 0.88f, 0.78f);
        Color eyeBlack = new Color(0.05f, 0.04f, 0.02f);
        Color clear = new Color(0, 0, 0, 0);
        float cx = w * 0.5f;
        float cy = h * 0.45f;
        float rx = w * 0.46f;
        float ry = h * 0.48f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = clear;
                float dx = (x - cx) / rx;
                float dy = (y - cy) / ry;
                float d = dx * dx + dy * dy;
                if (d <= 1.0f && y >= h * 0.15f)
                {
                    float shade = 1f - dy * 0.6f - Mathf.Abs(dx) * 0.2f;
                    c = Color.Lerp(bodyDark, body, Mathf.Clamp01(shade));
                }
                // Eyes (two black dots with white)
                int eyeY = (int)(h * 0.55f);
                int eyeL = (int)(w * 0.32f), eyeR = (int)(w * 0.62f);
                if ((Mathf.Abs(x - eyeL) < 4 && Mathf.Abs(y - eyeY) < 3) ||
                    (Mathf.Abs(x - eyeR) < 4 && Mathf.Abs(y - eyeY) < 3))
                    c = eyeWhite;
                if ((Mathf.Abs(x - eyeL) < 2 && Mathf.Abs(y - eyeY) < 2) ||
                    (Mathf.Abs(x - eyeR) < 2 && Mathf.Abs(y - eyeY) < 2))
                    c = eyeBlack;
                tex.SetPixel(x, y, c);
            }
        // Outline pass
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (tex.GetPixel(x, y).a > 0)
                {
                    bool edge = false;
                    if (x > 0 && tex.GetPixel(x - 1, y).a == 0) edge = true;
                    if (y > 0 && tex.GetPixel(x, y - 1).a == 0) edge = true;
                    if (edge) tex.SetPixel(x, y, outline);
                }
            }
        tex.Apply(); return tex;
    }

    private static Texture2D MakeTargetDummyTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color wood = new Color(0.55f, 0.38f, 0.20f);
        Color woodDark = new Color(0.38f, 0.25f, 0.12f);
        Color iron = new Color(0.30f, 0.27f, 0.24f);
        Color straw = new Color(0.85f, 0.72f, 0.32f);
        Color outline = new Color(0.10f, 0.07f, 0.04f);
        Color clear = new Color(0, 0, 0, 0);
        int strawTop = (int)(h * 0.95f);
        int strawBot = (int)(h * 0.55f);
        int postW = w / 3;
        int postX0 = (w - postW) / 2;
        int postX1 = postX0 + postW;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = clear;
                if (y < strawBot)
                {
                    // Wooden post bottom
                    if (x >= postX0 && x < postX1)
                    {
                        float grain = Mathf.PerlinNoise(x * 0.4f, y * 0.05f);
                        c = Color.Lerp(woodDark, wood, grain);
                    }
                }
                else if (y >= strawBot && y < strawTop)
                {
                    // Straw target body (round)
                    int cx = w / 2, cy = (strawTop + strawBot) / 2;
                    float ry = (strawTop - strawBot) * 0.5f;
                    float rx = w * 0.42f;
                    float dx = (x - cx) / rx, dy = (y - cy) / ry;
                    if (dx * dx + dy * dy <= 1.0f)
                    {
                        float ang = Mathf.Atan2(dy, dx);
                        bool ring = (Mathf.FloorToInt(((dx*dx + dy*dy)) * 5) % 2) == 0;
                        c = ring ? straw : woodDark;
                    }
                }
                else
                {
                    // Iron cap (top)
                    if (x >= postX0 - 2 && x < postX1 + 2)
                        c = iron;
                }

                // Outline pass: if pixel is non-transparent and a neighbor is transparent, darken
                if (c.a > 0)
                {
                    bool edge = false;
                    if (x > 0 && tex.GetPixel(x - 1, y).a == 0) edge = true;
                    if (y > 0 && tex.GetPixel(x, y - 1).a == 0) edge = true;
                    if (edge) c = outline;
                }
                tex.SetPixel(x, y, c);
            }
        tex.Apply(); return tex;
    }

    private static void BuildPlayerVisual(GameObject root, PlayerParts parts,
        out Transform body, out Transform leftArm, out Transform rightArm, out Transform leftLeg, out Transform rightLeg)
    {
        var container = new GameObject("SpriteContainer");
        container.transform.SetParent(root.transform, false);

        // Body (torso)
        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(container.transform, false);
        bodyGo.transform.localPosition = new Vector3(0, 0.05f, 0);
        var bSR = bodyGo.AddComponent<SpriteRenderer>();
        bSR.sprite = parts.body;
        bSR.sortingOrder = 11;
        body = bodyGo.transform;

        // Head (above body)
        var headGo = new GameObject("Head");
        headGo.transform.SetParent(container.transform, false);
        headGo.transform.localPosition = new Vector3(0, 0.32f, 0);
        var hSR = headGo.AddComponent<SpriteRenderer>();
        hSR.sprite = parts.head;
        hSR.sortingOrder = 13;

        // Right arm (front, in front of body)
        var raGo = new GameObject("RightArm");
        raGo.transform.SetParent(container.transform, false);
        raGo.transform.localPosition = new Vector3(0.12f, 0.20f, 0);
        var raSR = raGo.AddComponent<SpriteRenderer>();
        raSR.sprite = parts.arm;
        raSR.sortingOrder = 12;
        rightArm = raGo.transform;

        // Left arm (back, behind body)
        var laGo = new GameObject("LeftArm");
        laGo.transform.SetParent(container.transform, false);
        laGo.transform.localPosition = new Vector3(-0.12f, 0.20f, 0);
        var laSR = laGo.AddComponent<SpriteRenderer>();
        laSR.sprite = parts.arm;
        laSR.sortingOrder = 10;
        leftArm = laGo.transform;

        // Right leg
        var rlGo = new GameObject("RightLeg");
        rlGo.transform.SetParent(container.transform, false);
        rlGo.transform.localPosition = new Vector3(0.06f, -0.08f, 0);
        var rlSR = rlGo.AddComponent<SpriteRenderer>();
        rlSR.sprite = parts.leg;
        rlSR.sortingOrder = 10;
        rightLeg = rlGo.transform;

        // Left leg
        var llGo = new GameObject("LeftLeg");
        llGo.transform.SetParent(container.transform, false);
        llGo.transform.localPosition = new Vector3(-0.06f, -0.08f, 0);
        var llSR = llGo.AddComponent<SpriteRenderer>();
        llSR.sprite = parts.leg;
        llSR.sortingOrder = 9;
        leftLeg = llGo.transform;
    }

    // ---- Procedural texture helpers ----
    private static Texture2D MakeCircleTex(int size, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float a = Mathf.Clamp01(1f - Mathf.Pow(d, 8));
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
            }
        tex.Apply(); return tex;
    }

    private static Texture2D MakeRoundRectTex(int w, int h, int radius, Color color)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(radius - x, x - (w - 1 - radius)));
                float dy = Mathf.Max(0, Mathf.Max(radius - y, y - (h - 1 - radius)));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - Mathf.Max(0, d - radius + 1));
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
            }
        tex.Apply(); return tex;
    }

    private static Texture2D MakeGradientBarTex(int w, int h, Color left, Color right)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float t = (float)x / (w - 1);
                float vBright = 1f + (y > h * 0.5f ? 0.15f : 0f);
                Color c = Color.Lerp(left, right, t);
                c = new Color(
                    Mathf.Clamp01(c.r * vBright),
                    Mathf.Clamp01(c.g * vBright),
                    Mathf.Clamp01(c.b * vBright), c.a);
                tex.SetPixel(x, y, c);
            }
        tex.Apply(); return tex;
    }

    private static Texture2D MakeRingTex(int size, float innerRadius, float outerRadius, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float a = 0;
                if (d >= innerRadius && d <= outerRadius)
                    a = 1f - Mathf.Abs(d - (innerRadius + outerRadius) * 0.5f) / ((outerRadius - innerRadius) * 0.5f);
                a = Mathf.Clamp01(a);
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
            }
        tex.Apply(); return tex;
    }

    // WoW-style stone/leather textured background
    private static Texture2D MakeStoneTex(int w, int h, Color baseColor, Color borderColor, int borderWidth)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool isBorder = x < borderWidth || x >= w - borderWidth || y < borderWidth || y >= h - borderWidth;
                if (isBorder)
                {
                    // Gold/dark border with slight 3D bevel
                    float bevelT = 0f;
                    if (y >= h - borderWidth) bevelT = 0.3f; // bottom darker
                    if (x < borderWidth || y < borderWidth) bevelT = -0.15f; // top/left lighter
                    Color bc = new Color(
                        Mathf.Clamp01(borderColor.r + bevelT),
                        Mathf.Clamp01(borderColor.g + bevelT),
                        Mathf.Clamp01(borderColor.b + bevelT),
                        borderColor.a);
                    tex.SetPixel(x, y, bc);
                }
                else
                {
                    // Stone noise
                    float noise = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.06f - 0.03f;
                    float noise2 = Mathf.PerlinNoise(x * 0.2f + 50, y * 0.2f + 50) * 0.03f;
                    Color c = new Color(
                        Mathf.Clamp01(baseColor.r + noise + noise2),
                        Mathf.Clamp01(baseColor.g + noise + noise2),
                        Mathf.Clamp01(baseColor.b + noise + noise2),
                        baseColor.a);
                    tex.SetPixel(x, y, c);
                }
            }
        tex.Apply(); return tex;
    }

    // WoW-style action slot with gold border and inner shadow
    private static Texture2D MakeWowSlotTex(int size, Color fill, Color border, int borderW)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool isOuter = x < borderW || x >= size - borderW || y < borderW || y >= size - borderW;
                if (isOuter)
                {
                    // Bevel: top-left lighter, bottom-right darker
                    float bevel = 0f;
                    if (y >= size - borderW || x >= size - borderW) bevel = -0.12f;
                    if (y < borderW || x < borderW) bevel = 0.08f;
                    Color bc = new Color(
                        Mathf.Clamp01(border.r + bevel),
                        Mathf.Clamp01(border.g + bevel),
                        Mathf.Clamp01(border.b + bevel),
                        border.a);
                    tex.SetPixel(x, y, bc);
                }
                else
                {
                    // Inner shadow near edges
                    int ix = x - borderW, iy = y - borderW;
                    int iw = size - borderW * 2;
                    float shadowX = Mathf.Min(ix, iw - ix) / (float)iw;
                    float shadowY = Mathf.Min(iy, iw - iy) / (float)iw;
                    float shadow = Mathf.Clamp01(Mathf.Min(shadowX, shadowY) * 6f);
                    Color c = Color.Lerp(new Color(0, 0, 0, fill.a), fill, shadow);
                    tex.SetPixel(x, y, c);
                }
            }
        tex.Apply(); return tex;
    }

    // Diablo-style sphere orb: radial gradient (bright center, dark rim) with circular alpha
    private static Texture2D MakeOrbTex(int size, Color innerColor, Color outerColor)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float alpha = Mathf.Clamp01(1f - Mathf.Pow(d, 6));
                // Liquid sphere shading: bright at upper-left, fading to dark at rim
                float dx = (x - c * 0.6f) / c;
                float dy = (c * 1.4f - y) / c;
                float lightDist = Mathf.Sqrt(dx * dx + dy * dy);
                float shade = Mathf.Clamp01(1f - lightDist * 0.55f);
                float rimDarken = Mathf.Pow(d, 2.5f);
                Color col = Color.Lerp(innerColor, outerColor, rimDarken);
                col = new Color(
                    Mathf.Clamp01(col.r * (0.7f + shade * 0.6f)),
                    Mathf.Clamp01(col.g * (0.7f + shade * 0.6f)),
                    Mathf.Clamp01(col.b * (0.7f + shade * 0.6f)),
                    col.a * alpha);
                tex.SetPixel(x, y, col);
            }
        tex.Apply(); return tex;
    }

    // Iron ring frame around orb (thick weathered metal border)
    private static Texture2D MakeOrbFrameTex(int size, Color ironDark, Color ironLight)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = size * 0.5f;
        float outer = 0.99f, inner = 0.78f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                if (d < inner || d > outer) { tex.SetPixel(x, y, new Color(0, 0, 0, 0)); continue; }
                float t = (d - inner) / (outer - inner);
                // bevel: outer rim darker, mid lighter, inner rim darker
                float bevel = 1f - Mathf.Abs(t - 0.45f) * 1.6f;
                bevel = Mathf.Clamp01(bevel);
                Color metal = Color.Lerp(ironDark, ironLight, bevel);
                // Position-based highlight (top-left brighter)
                float angle = Mathf.Atan2(y - c, x - c);
                float hi = Mathf.Cos(angle - Mathf.PI * 0.75f) * 0.18f;
                metal = new Color(
                    Mathf.Clamp01(metal.r + hi),
                    Mathf.Clamp01(metal.g + hi),
                    Mathf.Clamp01(metal.b + hi),
                    1f);
                tex.SetPixel(x, y, metal);
            }
        tex.Apply(); return tex;
    }

    private static Sprite TexToSprite(Texture2D tex)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
    }

    private static void CreateGameHUD(GameObject player, float spawnY)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Medieval HUD palette (dark leather + iron + parchment + heraldic)
        Color panelBg = new Color(0.10f, 0.08f, 0.06f, 0.92f);   // dark leather
        Color textWhite = new Color(0.94f, 0.88f, 0.74f);         // aged parchment
        Color textMuted = new Color(0.62f, 0.55f, 0.42f);         // sepia
        Color slotBg = new Color(0.13f, 0.10f, 0.07f, 0.95f);     // tanned leather

        Color hpRed1 = new Color(0.55f, 0.08f, 0.10f);            // heraldic gules dark
        Color hpRed2 = new Color(0.85f, 0.20f, 0.18f);            // gules bright
        Color mpBlue1 = new Color(0.16f, 0.26f, 0.52f);           // azure dark
        Color mpBlue2 = new Color(0.38f, 0.52f, 0.78f);           // azure bright
        Color expGold1 = new Color(0.55f, 0.40f, 0.10f);          // tarnished gold
        Color expGold2 = new Color(0.92f, 0.72f, 0.25f);          // gold leaf

        var hpGrad = TexToSprite(MakeGradientBarTex(256, 16, hpRed1, hpRed2));
        var mpGrad = TexToSprite(MakeGradientBarTex(256, 16, mpBlue1, mpBlue2));
        var expGrad = TexToSprite(MakeGradientBarTex(256, 12, expGold1, expGold2));
        var panelSpr = TexToSprite(MakeRoundRectTex(256, 64, 10, panelBg));
        var slotSpr = TexToSprite(MakeRoundRectTex(64, 64, 6, slotBg));
        var barBgSpr = TexToSprite(MakeRoundRectTex(256, 18, 7, new Color(0.02f, 0.03f, 0.05f, 0.95f)));
        var circleSpr = TexToSprite(MakeCircleTex(128, Color.white));

        // Canvas
        var canvasGo = new GameObject("GameHUD_Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var hudComp = canvasGo.AddComponent<Astrion.UI.GameHUD>();
        var root = canvasGo.GetComponent<RectTransform>();

        // ========== TOP-LEFT: Character info panel ==========
        var charPanel = HUD_CreateRT("CharPanel", root);
        charPanel.anchorMin = charPanel.anchorMax = new Vector2(0, 1);
        charPanel.pivot = new Vector2(0, 1);
        charPanel.anchoredPosition = new Vector2(16, -16);
        charPanel.sizeDelta = new Vector2(200, 58);
        var cpBg = charPanel.gameObject.AddComponent<Image>();
        cpBg.sprite = panelSpr; cpBg.type = Image.Type.Sliced;

        // Level badge (gold square, MapleStory style)
        var lvlBadge = HUD_CreateRT("LvlBadge", charPanel);
        lvlBadge.anchorMin = lvlBadge.anchorMax = new Vector2(0, 0.5f);
        lvlBadge.anchoredPosition = new Vector2(32, 0);
        lvlBadge.sizeDelta = new Vector2(44, 44);
        var lvlBg = lvlBadge.gameObject.AddComponent<Image>();
        lvlBg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 8, new Color(0.95f, 0.72f, 0.22f, 1f)));
        var lvlNumRT = HUD_CreateRT("Num", lvlBadge);
        lvlNumRT.anchorMin = Vector2.zero; lvlNumRT.anchorMax = Vector2.one;
        lvlNumRT.offsetMin = lvlNumRT.offsetMax = Vector2.zero;
        var lvlNumText = lvlNumRT.gameObject.AddComponent<Text>();
        lvlNumText.font = font; lvlNumText.fontSize = 22; lvlNumText.fontStyle = FontStyle.Bold;
        lvlNumText.color = new Color(0.18f, 0.10f, 0.04f);
        lvlNumText.alignment = TextAnchor.MiddleCenter; lvlNumText.text = "1";

        // Char name
        var nameRT = HUD_CreateRT("CharName", charPanel);
        nameRT.anchorMin = new Vector2(0, 0.55f); nameRT.anchorMax = new Vector2(1, 1);
        nameRT.offsetMin = new Vector2(64, 0); nameRT.offsetMax = new Vector2(-8, -4);
        var nameText = nameRT.gameObject.AddComponent<Text>();
        nameText.font = font; nameText.fontSize = 15; nameText.fontStyle = FontStyle.Bold;
        nameText.color = textWhite;
        nameText.text = "Adventurer"; nameText.alignment = TextAnchor.LowerLeft;

        // Char level/class
        var levelRT = HUD_CreateRT("CharLevel", charPanel);
        levelRT.anchorMin = new Vector2(0, 0); levelRT.anchorMax = new Vector2(1, 0.55f);
        levelRT.offsetMin = new Vector2(64, 4); levelRT.offsetMax = new Vector2(-8, 0);
        var levelText = levelRT.gameObject.AddComponent<Text>();
        levelText.font = font; levelText.fontSize = 11;
        levelText.color = textMuted;
        levelText.text = "Lv.1 Warrior"; levelText.alignment = TextAnchor.UpperLeft;

        // ========== TOP-LEFT: Map name + Minimap (under CharPanel) ==========
        var minimapPanel = HUD_CreateRT("MinimapPanel", root);
        minimapPanel.anchorMin = minimapPanel.anchorMax = new Vector2(0, 1);
        minimapPanel.pivot = new Vector2(0, 1);
        minimapPanel.anchoredPosition = new Vector2(16, -100);
        minimapPanel.sizeDelta = new Vector2(200, 140);
        var minimapBg = minimapPanel.gameObject.AddComponent<Image>();
        minimapBg.sprite = panelSpr; minimapBg.type = Image.Type.Sliced;

        // Map name header
        var mapNameRT = HUD_CreateRT("MapName", minimapPanel);
        mapNameRT.anchorMin = new Vector2(0, 1); mapNameRT.anchorMax = new Vector2(1, 1);
        mapNameRT.pivot = new Vector2(0.5f, 1);
        mapNameRT.anchoredPosition = new Vector2(0, -4);
        mapNameRT.sizeDelta = new Vector2(-8, 24);
        var mapNameTextC = mapNameRT.gameObject.AddComponent<Text>();
        mapNameTextC.font = font; mapNameTextC.fontSize = 13; mapNameTextC.fontStyle = FontStyle.Bold;
        mapNameTextC.color = new Color(1f, 0.85f, 0.40f); // gold
        mapNameTextC.alignment = TextAnchor.MiddleCenter;
        mapNameTextC.text = "—";

        // Thin gold separator under map name
        var mapSep = HUD_CreateRT("Sep", minimapPanel);
        mapSep.anchorMin = new Vector2(0, 1); mapSep.anchorMax = new Vector2(1, 1);
        mapSep.pivot = new Vector2(0.5f, 1);
        mapSep.anchoredPosition = new Vector2(0, -30);
        mapSep.sizeDelta = new Vector2(-12, 1);
        mapSep.gameObject.AddComponent<Image>().color = new Color(0.55f, 0.40f, 0.18f, 0.6f);

        // Minimap placeholder (dark inner frame)
        var mmInner = HUD_CreateRT("MinimapArea", minimapPanel);
        mmInner.anchorMin = new Vector2(0, 0); mmInner.anchorMax = new Vector2(1, 1);
        mmInner.offsetMin = new Vector2(8, 8); mmInner.offsetMax = new Vector2(-8, -34);
        var mmInnerImg = mmInner.gameObject.AddComponent<Image>();
        mmInnerImg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 4, new Color(0.03f, 0.02f, 0.01f, 0.95f)));
        mmInnerImg.type = Image.Type.Sliced;

        // Placeholder label inside minimap area
        var mmLabel = HUD_CreateRT("Label", mmInner);
        mmLabel.anchorMin = Vector2.zero; mmLabel.anchorMax = Vector2.one;
        mmLabel.offsetMin = mmLabel.offsetMax = Vector2.zero;
        var mmLabelT = mmLabel.gameObject.AddComponent<Text>();
        mmLabelT.font = font; mmLabelT.fontSize = 10;
        mmLabelT.color = new Color(0.50f, 0.42f, 0.30f, 0.7f);
        mmLabelT.alignment = TextAnchor.MiddleCenter;
        mmLabelT.text = "MINIMAP";

        // ========== TOP-RIGHT: Coords + FPS ==========
        var coordsRT = HUD_CreateRT("CoordsPanel", root);
        coordsRT.anchorMin = coordsRT.anchorMax = new Vector2(1, 1);
        coordsRT.pivot = new Vector2(1, 1);
        coordsRT.anchoredPosition = new Vector2(-16, -16);
        coordsRT.sizeDelta = new Vector2(150, 30);
        var coordsBg = coordsRT.gameObject.AddComponent<Image>();
        coordsBg.sprite = panelSpr; coordsBg.type = Image.Type.Sliced;
        var coordsTextRT = HUD_CreateRT("Text", coordsRT);
        coordsTextRT.anchorMin = Vector2.zero; coordsTextRT.anchorMax = Vector2.one;
        coordsTextRT.offsetMin = coordsTextRT.offsetMax = Vector2.zero;
        var coordsText = coordsTextRT.gameObject.AddComponent<Text>();
        coordsText.font = font; coordsText.fontSize = 12;
        coordsText.color = textWhite; coordsText.alignment = TextAnchor.MiddleCenter;
        coordsText.text = "X: 0.0  Y: 0.0";

        var fpsRT = HUD_CreateRT("FPSCounter", root);
        fpsRT.anchorMin = fpsRT.anchorMax = new Vector2(1, 1);
        fpsRT.pivot = new Vector2(1, 1);
        fpsRT.anchoredPosition = new Vector2(-16, -52);
        fpsRT.sizeDelta = new Vector2(120, 22);
        var fpsText = fpsRT.gameObject.AddComponent<Text>();
        fpsText.font = font; fpsText.fontSize = 12;
        fpsText.color = new Color(0.7f, 0.85f, 0.45f);
        fpsText.alignment = TextAnchor.MiddleRight;
        fpsText.text = "60 FPS";

        // ========== BOTTOM-CENTER: Action bar (HP/MP/EXP + hotbar) ==========
        var actionRoot = HUD_CreateRT("ActionRoot", root);
        actionRoot.anchorMin = actionRoot.anchorMax = new Vector2(0.5f, 0);
        actionRoot.pivot = new Vector2(0.5f, 0);
        actionRoot.anchoredPosition = new Vector2(0, 12);
        actionRoot.sizeDelta = new Vector2(680, 138);

        var arBg = actionRoot.gameObject.AddComponent<Image>();
        arBg.sprite = panelSpr; arBg.type = Image.Type.Sliced;

        // Bars row (HP | MP | EXP)
        Image hpFill = CreateMapleBar(actionRoot, "HPBar", new Vector2(14, 100), new Vector2(212, 22),
            hpGrad, barBgSpr, font, "100/100", out Text hpBarText);
        Image mpFill = CreateMapleBar(actionRoot, "MPBar", new Vector2(234, 100), new Vector2(212, 22),
            mpGrad, barBgSpr, font, "50/50", out Text mpBarText);
        Image expFill = CreateMapleBar(actionRoot, "EXPBar", new Vector2(454, 100), new Vector2(212, 22),
            expGrad, barBgSpr, font, "35.0%", out Text expBarText);

        // Hotbar slots (5 — match HotbarSystem.SLOT_COUNT)
        float slotSize = 72f;
        float slotGap = 8f;
        float totalW = slotSize * 5 + slotGap * 4;
        float startX = (actionRoot.sizeDelta.x - totalW) * 0.5f;
        for (int i = 0; i < 5; i++)
        {
            var slot = HUD_CreateRT($"Slot_{i}", actionRoot);
            slot.anchorMin = slot.anchorMax = new Vector2(0, 0);
            slot.pivot = new Vector2(0, 0);
            slot.anchoredPosition = new Vector2(startX + i * (slotSize + slotGap), 10);
            slot.sizeDelta = new Vector2(slotSize, slotSize);
            var slotImg = slot.gameObject.AddComponent<Image>();
            slotImg.sprite = slotSpr; slotImg.type = Image.Type.Sliced;

            // Skill icon (filled by HotbarHUD; hidden when slot is empty)
            var iconRT = HUD_CreateRT("SkillIcon", slot);
            iconRT.anchorMin = new Vector2(0, 0); iconRT.anchorMax = new Vector2(1, 1);
            iconRT.offsetMin = new Vector2(6, 6); iconRT.offsetMax = new Vector2(-6, -6);
            var iconImg = iconRT.gameObject.AddComponent<Image>();
            iconImg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 6, Color.white));
            iconImg.color = new Color(1f, 0.85f, 0.30f, 1f);
            iconRT.gameObject.SetActive(false);
            var letterRT = HUD_CreateRT("Letter", iconRT);
            letterRT.anchorMin = Vector2.zero; letterRT.anchorMax = Vector2.one;
            letterRT.offsetMin = letterRT.offsetMax = Vector2.zero;
            var letterT = letterRT.gameObject.AddComponent<Text>();
            letterT.font = font; letterT.fontSize = 30; letterT.fontStyle = FontStyle.Bold;
            letterT.color = new Color(0.10f, 0.07f, 0.04f);
            letterT.alignment = TextAnchor.MiddleCenter;
            letterT.text = "";

            // Slot number (top-left)
            var num = HUD_CreateRT("Num", slot);
            num.anchorMin = num.anchorMax = new Vector2(0, 1);
            num.pivot = new Vector2(0, 1);
            num.anchoredPosition = new Vector2(6, -4);
            num.sizeDelta = new Vector2(22, 20);
            var numT = num.gameObject.AddComponent<Text>();
            numT.font = font; numT.fontSize = 13; numT.fontStyle = FontStyle.Bold;
            numT.color = new Color(1f, 0.88f, 0.45f);
            numT.alignment = TextAnchor.UpperLeft;
            numT.text = (i + 1).ToString();
        }

        // HotbarHUD: binds hotbar state to slot visuals
        var hotbarHud = canvasGo.AddComponent<Astrion.UI.HotbarHUD>();
        var hotbarSo = new UnityEditor.SerializedObject(hotbarHud);
        hotbarSo.FindProperty("actionRoot").objectReferenceValue = actionRoot;
        hotbarSo.ApplyModifiedPropertiesWithoutUndo();

        // ========== BOTTOM-LEFT: Chat panel ==========
        var chatPanel = HUD_CreateRT("ChatPanel", root);
        chatPanel.anchorMin = chatPanel.anchorMax = new Vector2(0, 0);
        chatPanel.pivot = new Vector2(0, 0);
        chatPanel.anchoredPosition = new Vector2(16, 160);
        chatPanel.sizeDelta = new Vector2(360, 150);
        var cBg = chatPanel.gameObject.AddComponent<Image>();
        cBg.sprite = panelSpr; cBg.type = Image.Type.Sliced;
        cBg.color = new Color(1, 1, 1, 0.85f);

        var msgRT = HUD_CreateRT("Messages", chatPanel);
        msgRT.anchorMin = new Vector2(0, 0.28f); msgRT.anchorMax = new Vector2(1, 1);
        msgRT.offsetMin = new Vector2(10, 4); msgRT.offsetMax = new Vector2(-10, -8);
        var msgT = msgRT.gameObject.AddComponent<Text>();
        msgT.font = font; msgT.fontSize = 12;
        msgT.color = new Color(0.92f, 0.94f, 0.97f, 0.95f);
        msgT.alignment = TextAnchor.LowerLeft; msgT.supportRichText = true;
        msgT.text = "<color=#80a8ff>[System]</color> Welcome to Astrion!";

        var inputBarRT = HUD_CreateRT("InputBar", chatPanel);
        inputBarRT.anchorMin = new Vector2(0, 0); inputBarRT.anchorMax = new Vector2(1, 0.28f);
        inputBarRT.offsetMin = new Vector2(8, 6); inputBarRT.offsetMax = new Vector2(-8, -2);
        var inputBg = inputBarRT.gameObject.AddComponent<Image>();
        inputBg.sprite = TexToSprite(MakeRoundRectTex(256, 32, 6, new Color(0.04f, 0.06f, 0.10f, 0.95f)));
        var inputField = inputBarRT.gameObject.AddComponent<InputField>();
        var inputTextRT = HUD_CreateRT("Text", inputBarRT);
        inputTextRT.anchorMin = Vector2.zero; inputTextRT.anchorMax = Vector2.one;
        inputTextRT.offsetMin = new Vector2(8, 4); inputTextRT.offsetMax = new Vector2(-8, -4);
        var inputText = inputTextRT.gameObject.AddComponent<Text>();
        inputText.font = font; inputText.fontSize = 12;
        inputText.color = textWhite; inputText.alignment = TextAnchor.MiddleLeft;
        var placeholderRT = HUD_CreateRT("Placeholder", inputBarRT);
        placeholderRT.anchorMin = Vector2.zero; placeholderRT.anchorMax = Vector2.one;
        placeholderRT.offsetMin = new Vector2(8, 4); placeholderRT.offsetMax = new Vector2(-8, -4);
        var placeholderText = placeholderRT.gameObject.AddComponent<Text>();
        placeholderText.font = font; placeholderText.fontSize = 12;
        placeholderText.color = new Color(0.5f, 0.55f, 0.62f, 0.6f);
        placeholderText.alignment = TextAnchor.MiddleLeft;
        placeholderText.text = "Press [Enter] to chat...";
        inputField.textComponent = inputText;
        inputField.placeholder = placeholderText;

        // ========== MOBILE: Joystick + Jump button ==========
        var joyArea = HUD_CreateRT("JoystickArea", root);
        joyArea.anchorMin = joyArea.anchorMax = new Vector2(0, 0);
        joyArea.pivot = new Vector2(0, 0);
        joyArea.anchoredPosition = new Vector2(80, 80);
        joyArea.sizeDelta = new Vector2(200, 200);
        var joyBgImg = joyArea.gameObject.AddComponent<Image>();
        joyBgImg.sprite = TexToSprite(MakeRingTex(256, 0.0f, 0.95f, new Color(1, 1, 1, 0.10f)));
        var joyHandle = HUD_CreateRT("JoystickHandle", joyArea);
        joyHandle.anchorMin = joyHandle.anchorMax = new Vector2(0.5f, 0.5f);
        joyHandle.anchoredPosition = Vector2.zero;
        joyHandle.sizeDelta = new Vector2(90, 90);
        var joyHandleImg = joyHandle.gameObject.AddComponent<Image>();
        joyHandleImg.sprite = TexToSprite(MakeCircleTex(128, new Color(1, 1, 1, 0.6f)));

        joyArea.gameObject.AddComponent<Astrion.Game.Joystick>();
        joyArea.gameObject.AddComponent<Astrion.Game.JoystickInitializer>();

        var jumpBtn = HUD_CreateRT("MobileJumpBtn", root);
        jumpBtn.anchorMin = jumpBtn.anchorMax = new Vector2(1, 0);
        jumpBtn.pivot = new Vector2(1, 0);
        jumpBtn.anchoredPosition = new Vector2(-80, 100);
        jumpBtn.sizeDelta = new Vector2(130, 130);
        var jumpBtnImg = jumpBtn.gameObject.AddComponent<Image>();
        jumpBtnImg.sprite = TexToSprite(MakeCircleTex(128, new Color(0.95f, 0.50f, 0.20f, 0.90f)));
        var jumpLabel = HUD_CreateRT("Label", jumpBtn);
        jumpLabel.anchorMin = Vector2.zero; jumpLabel.anchorMax = Vector2.one;
        jumpLabel.offsetMin = jumpLabel.offsetMax = Vector2.zero;
        var jumpLabelText = jumpLabel.gameObject.AddComponent<Text>();
        jumpLabelText.font = font; jumpLabelText.fontSize = 22; jumpLabelText.fontStyle = FontStyle.Bold;
        jumpLabelText.color = textWhite; jumpLabelText.alignment = TextAnchor.MiddleCenter;
        jumpLabelText.text = "JUMP";

        // ========== QUEST TRACKER (top-right, under FPS) ==========
        var questPanel = HUD_CreateRT("QuestTrackerPanel", root);
        questPanel.anchorMin = questPanel.anchorMax = new Vector2(1, 1);
        questPanel.pivot = new Vector2(1, 1);
        questPanel.anchoredPosition = new Vector2(-16, -84);
        questPanel.sizeDelta = new Vector2(260, 56);
        var qpBg = questPanel.gameObject.AddComponent<Image>();
        qpBg.sprite = panelSpr; qpBg.type = Image.Type.Sliced;

        var qTitleRT = HUD_CreateRT("Title", questPanel);
        qTitleRT.anchorMin = new Vector2(0, 0.5f); qTitleRT.anchorMax = new Vector2(1, 1);
        qTitleRT.offsetMin = new Vector2(12, 0); qTitleRT.offsetMax = new Vector2(-12, -6);
        var qTitleText = qTitleRT.gameObject.AddComponent<Text>();
        qTitleText.font = font; qTitleText.fontSize = 13; qTitleText.fontStyle = FontStyle.Bold;
        qTitleText.color = new Color(1f, 0.85f, 0.45f);
        qTitleText.alignment = TextAnchor.LowerLeft;
        qTitleText.text = "흩어진 별의 조각";

        var qProgRT = HUD_CreateRT("Progress", questPanel);
        qProgRT.anchorMin = new Vector2(0, 0); qProgRT.anchorMax = new Vector2(1, 0.5f);
        qProgRT.offsetMin = new Vector2(12, 6); qProgRT.offsetMax = new Vector2(-12, 0);
        var qProgText = qProgRT.gameObject.AddComponent<Text>();
        qProgText.font = font; qProgText.fontSize = 12;
        qProgText.color = new Color(0.92f, 0.88f, 0.55f);
        qProgText.alignment = TextAnchor.UpperLeft;
        qProgText.text = "0 / 5";

        var qTracker = canvasGo.AddComponent<Astrion.UI.QuestTrackerUI>();
        var qTrSo = new UnityEditor.SerializedObject(qTracker);
        qTrSo.FindProperty("panel").objectReferenceValue = questPanel.gameObject;
        qTrSo.FindProperty("titleText").objectReferenceValue = qTitleText;
        qTrSo.FindProperty("progressText").objectReferenceValue = qProgText;
        qTrSo.ApplyModifiedPropertiesWithoutUndo();
        questPanel.gameObject.SetActive(false); // hidden until quest accepted

        // ========== CHARACTER INFO PANEL (ESC) — wood + gold + parchment ==========
        // Design tokens
        Color tokWood       = new Color(0.227f, 0.157f, 0.094f, 0.98f);
        Color tokWoodDark   = new Color(0.102f, 0.063f, 0.031f, 0.98f);
        Color tokWoodLite   = new Color(0.353f, 0.251f, 0.157f, 1f);
        Color tokParchment  = new Color(0.910f, 0.831f, 0.627f, 1f);
        Color tokParchDark  = new Color(0.784f, 0.659f, 0.408f, 1f);
        Color tokParchShade = new Color(0.478f, 0.345f, 0.157f, 1f);
        Color tokGold       = new Color(0.941f, 0.847f, 0.471f, 1f);
        Color tokGoldDark   = new Color(0.659f, 0.541f, 0.227f, 1f);
        Color tokGoldBright = new Color(1.000f, 0.961f, 0.784f, 1f);
        Color tokInk        = new Color(0.102f, 0.055f, 0.016f, 1f);
        Color tokHp         = new Color(0.910f, 0.314f, 0.282f, 1f);
        Color tokHpDark     = new Color(0.659f, 0.157f, 0.125f, 1f);
        Color tokMp         = new Color(0.282f, 0.627f, 0.910f, 1f);
        Color tokMpDark     = new Color(0.157f, 0.376f, 0.659f, 1f);
        Color tokExp        = new Color(0.941f, 0.784f, 0.282f, 1f);
        Color tokExpDark    = new Color(0.659f, 0.439f, 0.094f, 1f);

        var woodPanelSpr     = TexToSprite(MakeWoodGradTex(256, 128));
        var parchmentSpr     = TexToSprite(MakeParchmentTex(256, 128));
        var goldBorderSpr    = TexToSprite(MakeRoundRectTex(256, 256, 6, tokGold));
        var slotDarkSpr      = TexToSprite(MakeRoundRectTex(64, 64, 4, new Color(0.16f, 0.10f, 0.04f, 1f)));
        var slotEmptyWoodSpr = TexToSprite(MakeRoundRectTex(64, 64, 4, tokWoodLite));

        var infoPanel = HUD_CreateRT("CharacterInfoPanel", root);
        infoPanel.anchorMin = infoPanel.anchorMax = new Vector2(0.5f, 0.5f);
        infoPanel.pivot = new Vector2(0.5f, 0.5f);
        infoPanel.anchoredPosition = new Vector2(0, 0);
        infoPanel.sizeDelta = new Vector2(620, 760);

        // Outer gold border (layered)
        var infoGoldBg = HUD_CreateRT("GoldBorder", infoPanel);
        infoGoldBg.anchorMin = Vector2.zero; infoGoldBg.anchorMax = Vector2.one;
        infoGoldBg.offsetMin = new Vector2(-4, -4); infoGoldBg.offsetMax = new Vector2(4, 4);
        var infoGoldBgImg = infoGoldBg.gameObject.AddComponent<Image>();
        infoGoldBgImg.sprite = goldBorderSpr; infoGoldBgImg.type = Image.Type.Sliced;
        infoGoldBgImg.color = new Color(0.941f, 0.847f, 0.471f, 0.67f);

        // Wood body
        var infoBodyBg = HUD_CreateRT("Body", infoPanel);
        infoBodyBg.anchorMin = Vector2.zero; infoBodyBg.anchorMax = Vector2.one;
        infoBodyBg.offsetMin = infoBodyBg.offsetMax = Vector2.zero;
        var infoBodyImg = infoBodyBg.gameObject.AddComponent<Image>();
        infoBodyImg.sprite = woodPanelSpr; infoBodyImg.type = Image.Type.Sliced;

        // Header (wood-lite gradient)
        var infoHdr = HUD_CreateRT("Header", infoPanel);
        infoHdr.anchorMin = new Vector2(0, 1); infoHdr.anchorMax = new Vector2(1, 1);
        infoHdr.pivot = new Vector2(0.5f, 1);
        infoHdr.anchoredPosition = new Vector2(0, 0);
        infoHdr.sizeDelta = new Vector2(0, 44);
        var infoHdrBg = infoHdr.gameObject.AddComponent<Image>();
        infoHdrBg.sprite = TexToSprite(MakeRoundRectTex(256, 64, 6, tokWoodLite));
        infoHdrBg.type = Image.Type.Sliced;
        var infoTitle = HUD_CreateRT("Title", infoHdr);
        infoTitle.anchorMin = new Vector2(0, 0); infoTitle.anchorMax = new Vector2(1, 1);
        infoTitle.offsetMin = new Vector2(20, 0); infoTitle.offsetMax = new Vector2(-50, 0);
        var infoTitleText = infoTitle.gameObject.AddComponent<Text>();
        infoTitleText.font = font; infoTitleText.fontSize = 16; infoTitleText.fontStyle = FontStyle.Bold;
        infoTitleText.color = tokGold;
        infoTitleText.alignment = TextAnchor.MiddleLeft;
        infoTitleText.text = "❖ 캐릭터 정보   C H A R A C T E R";

        // Gold border under header
        var infoHdrLine = HUD_CreateRT("HdrLine", infoPanel);
        infoHdrLine.anchorMin = new Vector2(0, 1); infoHdrLine.anchorMax = new Vector2(1, 1);
        infoHdrLine.pivot = new Vector2(0.5f, 1);
        infoHdrLine.anchoredPosition = new Vector2(0, -44);
        infoHdrLine.sizeDelta = new Vector2(0, 2);
        infoHdrLine.gameObject.AddComponent<Image>().color = tokGoldDark;

        // Close X button
        var infoCloseBtn = HUD_CreateRT("Close", infoHdr);
        infoCloseBtn.anchorMin = infoCloseBtn.anchorMax = new Vector2(1, 0.5f);
        infoCloseBtn.pivot = new Vector2(1, 0.5f);
        infoCloseBtn.anchoredPosition = new Vector2(-10, 0);
        infoCloseBtn.sizeDelta = new Vector2(28, 28);
        var infoCloseImg = infoCloseBtn.gameObject.AddComponent<Image>();
        infoCloseImg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 4, new Color(0.91f, 0.31f, 0.28f, 0.98f)));
        var infoCloseBtnComp = infoCloseBtn.gameObject.AddComponent<Button>();
        var infoCloseX = HUD_CreateRT("X", infoCloseBtn);
        infoCloseX.anchorMin = Vector2.zero; infoCloseX.anchorMax = Vector2.one;
        infoCloseX.offsetMin = infoCloseX.offsetMax = Vector2.zero;
        var infoCloseXT = infoCloseX.gameObject.AddComponent<Text>();
        infoCloseXT.font = font; infoCloseXT.fontSize = 18; infoCloseXT.fontStyle = FontStyle.Bold;
        infoCloseXT.color = tokGoldBright;
        infoCloseXT.alignment = TextAnchor.MiddleCenter;
        infoCloseXT.text = "×";

        // === Equipment columns (left + right) ===
        string[] leftSlots  = { "helmet", "face", "eye", "earring", "cape", "shield", "emblem" };
        string[] leftLabels = { "투구", "얼굴", "눈", "귀", "망토", "방패", "엠블럼" };
        Astrion.Game.ItemDatabase.Rarity[] leftRars = {
            Astrion.Game.ItemDatabase.Rarity.Epic, Astrion.Game.ItemDatabase.Rarity.Common, Astrion.Game.ItemDatabase.Rarity.Rare,
            Astrion.Game.ItemDatabase.Rarity.Rare, Astrion.Game.ItemDatabase.Rarity.Epic, Astrion.Game.ItemDatabase.Rarity.Epic,
            Astrion.Game.ItemDatabase.Rarity.Uncommon,
        };
        int[] leftEnh = { 10, 0, 9, 10, 10, 10, 7 };

        string[] rightSlots  = { "weapon", "armor", "pants", "glove", "shoes", "ring", "pendant" };
        string[] rightLabels = { "무기", "갑옷", "각반", "장갑", "신발", "반지", "펜던트" };
        Astrion.Game.ItemDatabase.Rarity[] rightRars = {
            Astrion.Game.ItemDatabase.Rarity.Legend, Astrion.Game.ItemDatabase.Rarity.Epic, Astrion.Game.ItemDatabase.Rarity.Rare,
            Astrion.Game.ItemDatabase.Rarity.Rare, Astrion.Game.ItemDatabase.Rarity.Rare, Astrion.Game.ItemDatabase.Rarity.Epic,
            Astrion.Game.ItemDatabase.Rarity.Legend,
        };
        int[] rightEnh = { 11, 10, 10, 10, 9, 10, 11 };

        System.Action<RectTransform, string, string, Astrion.Game.ItemDatabase.Rarity, int, bool, int> AddEquipSlot =
            (parent, slotName, koLabel, rar, enh, isEmpty, idx) =>
        {
            var s = HUD_CreateRT($"Equip_{slotName}", parent);
            s.anchorMin = s.anchorMax = new Vector2(0, 1);
            s.pivot = new Vector2(0, 1);
            s.anchoredPosition = new Vector2(0, -idx * 56f);
            s.sizeDelta = new Vector2(50, 50);
            // Gold bevel outer
            var bevel = HUD_CreateRT("Bevel", s);
            bevel.anchorMin = Vector2.zero; bevel.anchorMax = Vector2.one;
            bevel.offsetMin = bevel.offsetMax = Vector2.zero;
            var bevelImg = bevel.gameObject.AddComponent<Image>();
            bevelImg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 5, tokGold));
            // Rarity border
            var rarBorder = HUD_CreateRT("RarityBorder", s);
            rarBorder.anchorMin = Vector2.zero; rarBorder.anchorMax = Vector2.one;
            rarBorder.offsetMin = new Vector2(2, 2); rarBorder.offsetMax = new Vector2(-2, -2);
            var rarImg = rarBorder.gameObject.AddComponent<Image>();
            rarImg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 4,
                isEmpty ? new Color(0.55f, 0.42f, 0.25f, 0.5f) : Astrion.Game.ItemDatabase.RarityColor(rar)));
            // Inner
            var inner = HUD_CreateRT("Inner", s);
            inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
            inner.offsetMin = new Vector2(4, 4); inner.offsetMax = new Vector2(-4, -4);
            var innerImg = inner.gameObject.AddComponent<Image>();
            innerImg.sprite = isEmpty ? slotEmptyWoodSpr : slotDarkSpr;
            innerImg.type = Image.Type.Sliced;
            // Icon letter
            var iconRT = HUD_CreateRT("Icon", inner);
            iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;
            var iconT = iconRT.gameObject.AddComponent<Text>();
            iconT.font = font; iconT.fontSize = 17; iconT.fontStyle = FontStyle.Bold;
            iconT.color = isEmpty ? new Color(0.5f, 0.42f, 0.28f, 0.6f) : Astrion.Game.ItemDatabase.RarityColor(rar);
            iconT.alignment = TextAnchor.MiddleCenter;
            iconT.text = isEmpty ? "" : koLabel;
            // Enhance +N (top-right)
            if (enh > 0 && !isEmpty)
            {
                var enhRT = HUD_CreateRT("Enh", s);
                enhRT.anchorMin = enhRT.anchorMax = new Vector2(1, 1);
                enhRT.pivot = new Vector2(1, 1);
                enhRT.anchoredPosition = new Vector2(-3, -2);
                enhRT.sizeDelta = new Vector2(24, 14);
                var enhT = enhRT.gameObject.AddComponent<Text>();
                enhT.font = font; enhT.fontSize = 10; enhT.fontStyle = FontStyle.Bold;
                enhT.color = tokGoldBright;
                enhT.alignment = TextAnchor.UpperRight;
                enhT.text = $"+{enh}";
                var sh = enhRT.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0, 0, 0, 0.95f);
                sh.effectDistance = new Vector2(1, -1);
            }
        };

        // Left column container
        var leftCol = HUD_CreateRT("LeftColumn", infoPanel);
        leftCol.anchorMin = new Vector2(0, 1); leftCol.anchorMax = new Vector2(0, 1);
        leftCol.pivot = new Vector2(0, 1);
        leftCol.anchoredPosition = new Vector2(20, -60);
        leftCol.sizeDelta = new Vector2(50, 400);
        for (int i = 0; i < 7; i++)
            AddEquipSlot(leftCol, leftSlots[i], leftLabels[i], leftRars[i], leftEnh[i], leftEnh[i] == 0, i);

        // Right column container
        var rightCol = HUD_CreateRT("RightColumn", infoPanel);
        rightCol.anchorMin = new Vector2(1, 1); rightCol.anchorMax = new Vector2(1, 1);
        rightCol.pivot = new Vector2(1, 1);
        rightCol.anchoredPosition = new Vector2(-20, -60);
        rightCol.sizeDelta = new Vector2(50, 400);
        for (int i = 0; i < 7; i++)
        {
            var off = HUD_CreateRT($"Slot_Right_{i}", rightCol);
            off.anchorMin = off.anchorMax = new Vector2(0, 1);
            off.pivot = new Vector2(0, 1);
            off.anchoredPosition = new Vector2(0, -i * 56f);
            off.sizeDelta = new Vector2(50, 50);
            AddEquipSlot(off, rightSlots[i], rightLabels[i], rightRars[i], rightEnh[i], false, 0);
        }

        // === Center: Name plaque + character preview + bars ===
        var centerCol = HUD_CreateRT("CenterColumn", infoPanel);
        centerCol.anchorMin = new Vector2(0.5f, 1); centerCol.anchorMax = new Vector2(0.5f, 1);
        centerCol.pivot = new Vector2(0.5f, 1);
        centerCol.anchoredPosition = new Vector2(0, -56);
        centerCol.sizeDelta = new Vector2(380, 430);

        // Name plaque (parchment)
        var namePlate = HUD_CreateRT("NamePlaque", centerCol);
        namePlate.anchorMin = new Vector2(0.5f, 1); namePlate.anchorMax = new Vector2(0.5f, 1);
        namePlate.pivot = new Vector2(0.5f, 1);
        namePlate.anchoredPosition = new Vector2(0, 0);
        namePlate.sizeDelta = new Vector2(280, 58);
        var npBg = namePlate.gameObject.AddComponent<Image>();
        npBg.sprite = parchmentSpr; npBg.type = Image.Type.Sliced;
        var npName = HUD_CreateRT("Name", namePlate);
        npName.anchorMin = new Vector2(0, 0.5f); npName.anchorMax = new Vector2(1, 1);
        npName.offsetMin = new Vector2(12, 0); npName.offsetMax = new Vector2(-12, -4);
        var npNameText = npName.gameObject.AddComponent<Text>();
        npNameText.font = font; npNameText.fontSize = 20; npNameText.fontStyle = FontStyle.Bold;
        npNameText.color = tokInk;
        npNameText.alignment = TextAnchor.MiddleCenter;
        npNameText.text = "Aldric";
        var npSub = HUD_CreateRT("Sub", namePlate);
        npSub.anchorMin = new Vector2(0, 0); npSub.anchorMax = new Vector2(1, 0.5f);
        npSub.offsetMin = new Vector2(12, 4); npSub.offsetMax = new Vector2(-12, 0);
        var npSubText = npSub.gameObject.AddComponent<Text>();
        npSubText.font = font; npSubText.fontSize = 11; npSubText.fontStyle = FontStyle.Bold;
        npSubText.color = tokParchShade;
        npSubText.alignment = TextAnchor.MiddleCenter;
        npSubText.text = "Lv.42  ·  용  기  사";

        // Character preview frame
        var prevFrame = HUD_CreateRT("PreviewFrame", centerCol);
        prevFrame.anchorMin = new Vector2(0.5f, 1); prevFrame.anchorMax = new Vector2(0.5f, 1);
        prevFrame.pivot = new Vector2(0.5f, 1);
        prevFrame.anchoredPosition = new Vector2(0, -68);
        prevFrame.sizeDelta = new Vector2(240, 260);
        var pfBg = prevFrame.gameObject.AddComponent<Image>();
        pfBg.sprite = woodPanelSpr; pfBg.type = Image.Type.Sliced;
        // Guild banner
        var guild = HUD_CreateRT("Guild", prevFrame);
        guild.anchorMin = new Vector2(0.5f, 1); guild.anchorMax = new Vector2(0.5f, 1);
        guild.pivot = new Vector2(0.5f, 1);
        guild.anchoredPosition = new Vector2(0, -8);
        guild.sizeDelta = new Vector2(180, 24);
        var guildBg = guild.gameObject.AddComponent<Image>();
        guildBg.sprite = TexToSprite(MakeRoundRectTex(128, 32, 8, new Color(0.10f, 0.06f, 0.03f, 0.92f)));
        var guildT = HUD_CreateRT("Text", guild);
        guildT.anchorMin = Vector2.zero; guildT.anchorMax = Vector2.one;
        guildT.offsetMin = guildT.offsetMax = Vector2.zero;
        var guildTT = guildT.gameObject.AddComponent<Text>();
        guildTT.font = font; guildTT.fontSize = 10; guildTT.fontStyle = FontStyle.Bold;
        guildTT.color = tokGold;
        guildTT.alignment = TextAnchor.MiddleCenter;
        guildTT.text = "⚜  ORDER OF DAWN  ⚜";
        // Chibi placeholder
        var chibi = HUD_CreateRT("Chibi", prevFrame);
        chibi.anchorMin = new Vector2(0.5f, 0.5f); chibi.anchorMax = new Vector2(0.5f, 0.5f);
        chibi.pivot = new Vector2(0.5f, 0.5f);
        chibi.anchoredPosition = new Vector2(0, -10);
        chibi.sizeDelta = new Vector2(120, 160);
        var chibiBg = chibi.gameObject.AddComponent<Image>();
        chibiBg.sprite = TexToSprite(MakeRoundRectTex(64, 96, 8, new Color(0.16f, 0.10f, 0.04f, 0.6f)));
        var chibiLbl = HUD_CreateRT("Label", chibi);
        chibiLbl.anchorMin = Vector2.zero; chibiLbl.anchorMax = Vector2.one;
        chibiLbl.offsetMin = chibiLbl.offsetMax = Vector2.zero;
        var chibiLT = chibiLbl.gameObject.AddComponent<Text>();
        chibiLT.font = font; chibiLT.fontSize = 11;
        chibiLT.color = tokParchDark;
        chibiLT.alignment = TextAnchor.MiddleCenter;
        chibiLT.text = "[Character Preview]";

        // HP/MP/EXP bars panel
        var barsPanel = HUD_CreateRT("Bars", centerCol);
        barsPanel.anchorMin = new Vector2(0.5f, 1); barsPanel.anchorMax = new Vector2(0.5f, 1);
        barsPanel.pivot = new Vector2(0.5f, 1);
        barsPanel.anchoredPosition = new Vector2(0, -336);
        barsPanel.sizeDelta = new Vector2(280, 92);
        var bpBg = barsPanel.gameObject.AddComponent<Image>();
        bpBg.sprite = parchmentSpr; bpBg.type = Image.Type.Sliced;

        System.Action<RectTransform, string, string, Color, Color, float, int> AddInfoBar =
            (parent, label, valTxt, fillColor, fillDark, ratio, yOff) =>
        {
            var bar = HUD_CreateRT($"InfoBar_{label}", parent);
            bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1);
            bar.pivot = new Vector2(0.5f, 1);
            bar.anchoredPosition = new Vector2(0, -yOff);
            bar.sizeDelta = new Vector2(-16, 26);
            // Label row
            var lblRT = HUD_CreateRT("Label", bar);
            lblRT.anchorMin = new Vector2(0, 0.5f); lblRT.anchorMax = new Vector2(1, 1);
            lblRT.offsetMin = new Vector2(2, 0); lblRT.offsetMax = new Vector2(-2, 0);
            var lblT = lblRT.gameObject.AddComponent<Text>();
            lblT.font = font; lblT.fontSize = 9; lblT.fontStyle = FontStyle.Bold;
            lblT.color = tokInk;
            lblT.alignment = TextAnchor.MiddleLeft;
            lblT.text = label;
            var valRT = HUD_CreateRT("Val", bar);
            valRT.anchorMin = new Vector2(0, 0.5f); valRT.anchorMax = new Vector2(1, 1);
            valRT.offsetMin = new Vector2(2, 0); valRT.offsetMax = new Vector2(-2, 0);
            var valT = valRT.gameObject.AddComponent<Text>();
            valT.font = font; valT.fontSize = 9;
            valT.color = tokParchShade;
            valT.alignment = TextAnchor.MiddleRight;
            valT.text = valTxt;
            // Bar track
            var track = HUD_CreateRT("Track", bar);
            track.anchorMin = new Vector2(0, 0); track.anchorMax = new Vector2(1, 0.5f);
            track.offsetMin = new Vector2(2, 1); track.offsetMax = new Vector2(-2, -1);
            track.gameObject.AddComponent<Image>().color = new Color(0.227f, 0.157f, 0.094f, 1f);
            var fillRT = HUD_CreateRT("Fill", track);
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = new Vector2(ratio, 1);
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            var fillImg = fillRT.gameObject.AddComponent<Image>();
            fillImg.sprite = TexToSprite(MakeGradientBarTex(256, 16, fillDark, fillColor));
        };
        AddInfoBar(barsPanel, "HP", "4,824 / 6,700", tokHp, tokHpDark, 4824f / 6700f, 4);
        AddInfoBar(barsPanel, "MP", "1,278 / 2,840", tokMp, tokMpDark, 1278f / 2840f, 34);
        AddInfoBar(barsPanel, "EXP", "62.4 %", tokExp, tokExpDark, 0.624f, 64);

        // === Stats panel (bottom) ===
        var statsPanel = HUD_CreateRT("StatsPanel", infoPanel);
        statsPanel.anchorMin = new Vector2(0, 0); statsPanel.anchorMax = new Vector2(1, 0);
        statsPanel.pivot = new Vector2(0.5f, 0);
        statsPanel.anchoredPosition = new Vector2(0, 16);
        statsPanel.sizeDelta = new Vector2(-32, 240);
        var spBg = statsPanel.gameObject.AddComponent<Image>();
        spBg.sprite = parchmentSpr; spBg.type = Image.Type.Sliced;

        // Left side: primary stats
        string[] primaryStats = { "STR", "DEX", "INT", "LUK" };
        int[] primaryValues = { 148, 86, 42, 58 };
        int[] primaryBonus = { 22, 12, 4, 8 };
        for (int i = 0; i < primaryStats.Length; i++)
        {
            var row = HUD_CreateRT($"Stat_{primaryStats[i]}", statsPanel);
            row.anchorMin = new Vector2(0, 1); row.anchorMax = new Vector2(0.5f, 1);
            row.pivot = new Vector2(0, 1);
            row.anchoredPosition = new Vector2(12, -16 - i * 28f);
            row.sizeDelta = new Vector2(-14, 24);

            var name = HUD_CreateRT("Name", row);
            name.anchorMin = new Vector2(0, 0); name.anchorMax = new Vector2(0.28f, 1);
            name.offsetMin = name.offsetMax = Vector2.zero;
            var nameT = name.gameObject.AddComponent<Text>();
            nameT.font = font; nameT.fontSize = 13; nameT.fontStyle = FontStyle.Bold;
            nameT.color = tokInk;
            nameT.alignment = TextAnchor.MiddleLeft;
            nameT.text = primaryStats[i];

            var val = HUD_CreateRT("Val", row);
            val.anchorMin = new Vector2(0.28f, 0); val.anchorMax = new Vector2(0.7f, 1);
            val.offsetMin = val.offsetMax = Vector2.zero;
            var valT = val.gameObject.AddComponent<Text>();
            valT.font = font; valT.fontSize = 13; valT.fontStyle = FontStyle.Bold;
            valT.color = tokInk;
            valT.alignment = TextAnchor.MiddleLeft;
            valT.text = $"{primaryValues[i]}";

            var bonus = HUD_CreateRT("Bonus", row);
            bonus.anchorMin = new Vector2(0.42f, 0); bonus.anchorMax = new Vector2(0.78f, 1);
            bonus.offsetMin = bonus.offsetMax = Vector2.zero;
            var bonusT = bonus.gameObject.AddComponent<Text>();
            bonusT.font = font; bonusT.fontSize = 11;
            bonusT.color = new Color(0.35f, 0.78f, 0.31f);
            bonusT.alignment = TextAnchor.MiddleLeft;
            bonusT.text = $"(+{primaryBonus[i]})";

            // + button
            var plus = HUD_CreateRT("Plus", row);
            plus.anchorMin = new Vector2(0.85f, 0.5f); plus.anchorMax = new Vector2(0.85f, 0.5f);
            plus.pivot = new Vector2(0.5f, 0.5f);
            plus.anchoredPosition = new Vector2(0, 0);
            plus.sizeDelta = new Vector2(20, 20);
            var plusImg = plus.gameObject.AddComponent<Image>();
            plusImg.sprite = TexToSprite(MakeCircleTex(64, tokGold));
            var plusT = HUD_CreateRT("T", plus);
            plusT.anchorMin = Vector2.zero; plusT.anchorMax = Vector2.one;
            plusT.offsetMin = plusT.offsetMax = Vector2.zero;
            var plusTT = plusT.gameObject.AddComponent<Text>();
            plusTT.font = font; plusTT.fontSize = 14; plusTT.fontStyle = FontStyle.Bold;
            plusTT.color = tokInk;
            plusTT.alignment = TextAnchor.MiddleCenter;
            plusTT.text = "+";
            plus.gameObject.AddComponent<Button>();
        }

        // Remaining points
        var remPts = HUD_CreateRT("RemPoints", statsPanel);
        remPts.anchorMin = new Vector2(0, 1); remPts.anchorMax = new Vector2(0.5f, 1);
        remPts.pivot = new Vector2(0, 1);
        remPts.anchoredPosition = new Vector2(12, -130);
        remPts.sizeDelta = new Vector2(-14, 20);
        var remT = remPts.gameObject.AddComponent<Text>();
        remT.font = font; remT.fontSize = 11; remT.fontStyle = FontStyle.Bold;
        remT.color = new Color(0.91f, 0.31f, 0.28f);
        remT.alignment = TextAnchor.MiddleLeft;
        remT.text = "남은 포인트 : 5";

        // Right side: derived stats
        string[] derivedStats = {
            "공격력", "마법력", "방어력", "명중률", "회피율", "크리티컬", "이동속도", "공격속도"
        };
        string[] derivedValues = {
            "2,148 ~ 2,420", "584 ~ 712", "1,842   +18%", "94 %",
            "32 %", "28 %", "+12 %", "+8 %",
        };
        for (int i = 0; i < derivedStats.Length; i++)
        {
            var row = HUD_CreateRT($"Derived_{i}", statsPanel);
            row.anchorMin = new Vector2(0.5f, 1); row.anchorMax = new Vector2(1, 1);
            row.pivot = new Vector2(0, 1);
            row.anchoredPosition = new Vector2(0, -16 - i * 26f);
            row.sizeDelta = new Vector2(-14, 22);
            var n = HUD_CreateRT("Name", row);
            n.anchorMin = new Vector2(0, 0); n.anchorMax = new Vector2(0.4f, 1);
            n.offsetMin = n.offsetMax = Vector2.zero;
            var nT = n.gameObject.AddComponent<Text>();
            nT.font = font; nT.fontSize = 11;
            nT.color = tokParchShade;
            nT.alignment = TextAnchor.MiddleLeft;
            nT.text = derivedStats[i];
            var v = HUD_CreateRT("Val", row);
            v.anchorMin = new Vector2(0.4f, 0); v.anchorMax = new Vector2(1, 1);
            v.offsetMin = new Vector2(0, 0); v.offsetMax = new Vector2(-6, 0);
            var vT = v.gameObject.AddComponent<Text>();
            vT.font = font; vT.fontSize = 11; vT.fontStyle = FontStyle.Bold;
            vT.color = tokInk;
            vT.alignment = TextAnchor.MiddleRight;
            vT.text = derivedValues[i];
        }

        var ciComp = canvasGo.AddComponent<Astrion.UI.CharacterInfoUI>();
        var ciSo = new UnityEditor.SerializedObject(ciComp);
        ciSo.FindProperty("panel").objectReferenceValue = infoPanel.gameObject;
        ciSo.FindProperty("closeButton").objectReferenceValue = infoCloseBtnComp;
        ciSo.FindProperty("nameText").objectReferenceValue = npNameText;
        ciSo.FindProperty("levelText").objectReferenceValue = npSubText;
        ciSo.ApplyModifiedPropertiesWithoutUndo();
        infoPanel.gameObject.SetActive(false);

        // ========== INVENTORY PANEL (toggle: I) — 5 tabs + 6×6 grid + tooltip ==========
        var invPanel = HUD_CreateRT("InventoryPanel", root);
        invPanel.anchorMin = invPanel.anchorMax = new Vector2(0.5f, 0.5f);
        invPanel.pivot = new Vector2(0.5f, 0.5f);
        invPanel.anchoredPosition = new Vector2(0, 0);
        invPanel.sizeDelta = new Vector2(440, 720);

        var invGold = HUD_CreateRT("GoldBorder", invPanel);
        invGold.anchorMin = Vector2.zero; invGold.anchorMax = Vector2.one;
        invGold.offsetMin = new Vector2(-4, -4); invGold.offsetMax = new Vector2(4, 4);
        var invGoldImg = invGold.gameObject.AddComponent<Image>();
        invGoldImg.sprite = goldBorderSpr; invGoldImg.type = Image.Type.Sliced;
        invGoldImg.color = new Color(0.941f, 0.847f, 0.471f, 0.67f);

        var invBody = HUD_CreateRT("Body", invPanel);
        invBody.anchorMin = Vector2.zero; invBody.anchorMax = Vector2.one;
        invBody.offsetMin = invBody.offsetMax = Vector2.zero;
        var invBodyImg = invBody.gameObject.AddComponent<Image>();
        invBodyImg.sprite = woodPanelSpr; invBodyImg.type = Image.Type.Sliced;

        // Header
        var invHdr2 = HUD_CreateRT("Header", invPanel);
        invHdr2.anchorMin = new Vector2(0, 1); invHdr2.anchorMax = new Vector2(1, 1);
        invHdr2.pivot = new Vector2(0.5f, 1);
        invHdr2.anchoredPosition = new Vector2(0, 0);
        invHdr2.sizeDelta = new Vector2(0, 44);
        var invHdr2Bg = invHdr2.gameObject.AddComponent<Image>();
        invHdr2Bg.sprite = TexToSprite(MakeRoundRectTex(256, 64, 6, tokWoodLite));
        invHdr2Bg.type = Image.Type.Sliced;
        var invTitle = HUD_CreateRT("Title", invHdr2);
        invTitle.anchorMin = new Vector2(0, 0); invTitle.anchorMax = new Vector2(1, 1);
        invTitle.offsetMin = new Vector2(20, 0); invTitle.offsetMax = new Vector2(-50, 0);
        var invTitleT = invTitle.gameObject.AddComponent<Text>();
        invTitleT.font = font; invTitleT.fontSize = 16; invTitleT.fontStyle = FontStyle.Bold;
        invTitleT.color = tokGold;
        invTitleT.alignment = TextAnchor.MiddleLeft;
        invTitleT.text = "❖ 인벤토리   I N V E N T O R Y";

        var invHdrLine = HUD_CreateRT("HdrLine", invPanel);
        invHdrLine.anchorMin = new Vector2(0, 1); invHdrLine.anchorMax = new Vector2(1, 1);
        invHdrLine.pivot = new Vector2(0.5f, 1);
        invHdrLine.anchoredPosition = new Vector2(0, -44);
        invHdrLine.sizeDelta = new Vector2(0, 2);
        invHdrLine.gameObject.AddComponent<Image>().color = tokGoldDark;

        // Close X
        var invCloseBtn2 = HUD_CreateRT("Close", invHdr2);
        invCloseBtn2.anchorMin = invCloseBtn2.anchorMax = new Vector2(1, 0.5f);
        invCloseBtn2.pivot = new Vector2(1, 0.5f);
        invCloseBtn2.anchoredPosition = new Vector2(-10, 0);
        invCloseBtn2.sizeDelta = new Vector2(28, 28);
        var invCloseImg = invCloseBtn2.gameObject.AddComponent<Image>();
        invCloseImg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 4, new Color(0.91f, 0.31f, 0.28f, 0.98f)));
        var invCloseB = invCloseBtn2.gameObject.AddComponent<Button>();
        var invCloseX2 = HUD_CreateRT("X", invCloseBtn2);
        invCloseX2.anchorMin = Vector2.zero; invCloseX2.anchorMax = Vector2.one;
        invCloseX2.offsetMin = invCloseX2.offsetMax = Vector2.zero;
        var invCloseXT = invCloseX2.gameObject.AddComponent<Text>();
        invCloseXT.font = font; invCloseXT.fontSize = 18; invCloseXT.fontStyle = FontStyle.Bold;
        invCloseXT.color = tokGoldBright;
        invCloseXT.alignment = TextAnchor.MiddleCenter;
        invCloseXT.text = "×";

        // Tabs (5)
        string[] tabLabels = { "장비", "소비", "기타", "설치", "캐쉬" };
        var tabsRow = HUD_CreateRT("Tabs", invPanel);
        tabsRow.anchorMin = new Vector2(0, 1); tabsRow.anchorMax = new Vector2(1, 1);
        tabsRow.pivot = new Vector2(0.5f, 1);
        tabsRow.anchoredPosition = new Vector2(0, -52);
        tabsRow.sizeDelta = new Vector2(-32, 32);
        float tabW = (440 - 32) / 5f;
        for (int i = 0; i < 5; i++)
        {
            var tab = HUD_CreateRT($"Tab_{i}", tabsRow);
            tab.anchorMin = tab.anchorMax = new Vector2(0, 0);
            tab.pivot = new Vector2(0, 0);
            tab.anchoredPosition = new Vector2(i * tabW, 0);
            tab.sizeDelta = new Vector2(tabW - 4, 32);
            bool active = i == 0;
            var tabBg = tab.gameObject.AddComponent<Image>();
            tabBg.sprite = active ? parchmentSpr : TexToSprite(MakeRoundRectTex(64, 32, 4, tokWoodLite));
            tabBg.type = Image.Type.Sliced;
            var tabT = HUD_CreateRT("Label", tab);
            tabT.anchorMin = Vector2.zero; tabT.anchorMax = Vector2.one;
            tabT.offsetMin = tabT.offsetMax = Vector2.zero;
            var tabTT = tabT.gameObject.AddComponent<Text>();
            tabTT.font = font; tabTT.fontSize = 13; tabTT.fontStyle = FontStyle.Bold;
            tabTT.color = active ? tokInk : tokParchDark;
            tabTT.alignment = TextAnchor.MiddleCenter;
            tabTT.text = tabLabels[i];
            tab.gameObject.AddComponent<Button>();
        }

        // 6x6 Slots grid (parchment panel)
        var gridPanel = HUD_CreateRT("GridPanel", invPanel);
        gridPanel.anchorMin = new Vector2(0, 1); gridPanel.anchorMax = new Vector2(1, 1);
        gridPanel.pivot = new Vector2(0.5f, 1);
        gridPanel.anchoredPosition = new Vector2(0, -88);
        gridPanel.sizeDelta = new Vector2(-32, 344);
        var gpBg = gridPanel.gameObject.AddComponent<Image>();
        gpBg.sprite = parchmentSpr; gpBg.type = Image.Type.Sliced;

        var slotsRoot2 = HUD_CreateRT("SlotsRoot", gridPanel);
        slotsRoot2.anchorMin = Vector2.zero; slotsRoot2.anchorMax = Vector2.one;
        slotsRoot2.offsetMin = new Vector2(10, 10); slotsRoot2.offsetMax = new Vector2(-10, -10);

        const int INV_COLS = 6, INV_ROWS = 6;
        float invSlotSize = 50f;
        float invSlotGap = 4f;
        for (int r = 0; r < INV_ROWS; r++)
            for (int c = 0; c < INV_COLS; c++)
            {
                int idx = r * INV_COLS + c;
                var slot = HUD_CreateRT($"Slot_{idx}", slotsRoot2);
                slot.anchorMin = slot.anchorMax = new Vector2(0, 1);
                slot.pivot = new Vector2(0, 1);
                slot.anchoredPosition = new Vector2(c * (invSlotSize + invSlotGap),
                                                    -r * (invSlotSize + invSlotGap));
                slot.sizeDelta = new Vector2(invSlotSize, invSlotSize);
                var slotBevel = slot.gameObject.AddComponent<Image>();
                slotBevel.sprite = TexToSprite(MakeRoundRectTex(64, 64, 4, tokGold));
                var sInner = HUD_CreateRT("Bg", slot);
                sInner.anchorMin = Vector2.zero; sInner.anchorMax = Vector2.one;
                sInner.offsetMin = new Vector2(2, 2); sInner.offsetMax = new Vector2(-2, -2);
                var sInnerImg = sInner.gameObject.AddComponent<Image>();
                sInnerImg.sprite = slotEmptyWoodSpr;
                sInnerImg.type = Image.Type.Sliced;

                // Icon (initially hidden; InventoryUI fills it)
                var iconRT = HUD_CreateRT("Icon", slot);
                iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
                iconRT.offsetMin = new Vector2(4, 4); iconRT.offsetMax = new Vector2(-4, -4);
                var iconImg = iconRT.gameObject.AddComponent<Image>();
                iconImg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 4, Color.white));
                iconImg.color = Color.white;
                iconRT.gameObject.SetActive(false);

                var letterRT = HUD_CreateRT("Letter", iconRT);
                letterRT.anchorMin = Vector2.zero; letterRT.anchorMax = Vector2.one;
                letterRT.offsetMin = letterRT.offsetMax = Vector2.zero;
                var letterT = letterRT.gameObject.AddComponent<Text>();
                letterT.font = font; letterT.fontSize = 20; letterT.fontStyle = FontStyle.Bold;
                letterT.color = tokInk;
                letterT.alignment = TextAnchor.MiddleCenter;

                var qtyRT = HUD_CreateRT("Qty", slot);
                qtyRT.anchorMin = qtyRT.anchorMax = new Vector2(1, 0);
                qtyRT.pivot = new Vector2(1, 0);
                qtyRT.anchoredPosition = new Vector2(-3, 2);
                qtyRT.sizeDelta = new Vector2(36, 16);
                var qtyT = qtyRT.gameObject.AddComponent<Text>();
                qtyT.font = font; qtyT.fontSize = 11; qtyT.fontStyle = FontStyle.Bold;
                qtyT.color = tokParchment;
                qtyT.alignment = TextAnchor.LowerRight;
                var qtySh = qtyRT.gameObject.AddComponent<Shadow>();
                qtySh.effectColor = new Color(0, 0, 0, 0.95f);
                qtySh.effectDistance = new Vector2(1, -1);
            }

        // Selected item tooltip
        var ttip = HUD_CreateRT("Tooltip", invPanel);
        ttip.anchorMin = new Vector2(0, 1); ttip.anchorMax = new Vector2(1, 1);
        ttip.pivot = new Vector2(0.5f, 1);
        ttip.anchoredPosition = new Vector2(0, -442);
        ttip.sizeDelta = new Vector2(-32, 122);
        var ttipBg = ttip.gameObject.AddComponent<Image>();
        ttipBg.sprite = TexToSprite(MakeRoundRectTex(128, 64, 6, new Color(0.10f, 0.06f, 0.03f, 0.95f)));
        ttipBg.type = Image.Type.Sliced;
        var ttipName = HUD_CreateRT("Name", ttip);
        ttipName.anchorMin = new Vector2(0, 1); ttipName.anchorMax = new Vector2(1, 1);
        ttipName.offsetMin = new Vector2(14, -28); ttipName.offsetMax = new Vector2(-14, -4);
        var ttipNameT = ttipName.gameObject.AddComponent<Text>();
        ttipNameT.font = font; ttipNameT.fontSize = 14; ttipNameT.fontStyle = FontStyle.Bold;
        ttipNameT.color = new Color(0.94f, 0.66f, 0.19f); // legend gold
        ttipNameT.alignment = TextAnchor.MiddleLeft;
        ttipNameT.text = "⚔  여명의 검   +11";
        var ttipTier = HUD_CreateRT("Tier", ttip);
        ttipTier.anchorMin = new Vector2(0, 1); ttipTier.anchorMax = new Vector2(1, 1);
        ttipTier.offsetMin = new Vector2(14, -44); ttipTier.offsetMax = new Vector2(-14, -28);
        var ttipTierT = ttipTier.gameObject.AddComponent<Text>();
        ttipTierT.font = font; ttipTierT.fontSize = 9;
        ttipTierT.color = tokParchDark;
        ttipTierT.alignment = TextAnchor.MiddleLeft;
        ttipTierT.text = "LEGENDARY  ·  무기  ·  Lv.45";
        var ttipStats = HUD_CreateRT("Stats", ttip);
        ttipStats.anchorMin = new Vector2(0, 0); ttipStats.anchorMax = new Vector2(1, 1);
        ttipStats.offsetMin = new Vector2(14, 26); ttipStats.offsetMax = new Vector2(-14, -50);
        var ttipStatsT = ttipStats.gameObject.AddComponent<Text>();
        ttipStatsT.font = font; ttipStatsT.fontSize = 11;
        ttipStatsT.color = tokParchment;
        ttipStatsT.alignment = TextAnchor.UpperLeft;
        ttipStatsT.text = "공격력  2,420\nSTR  +42\n크리티컬  +12%   공격속도  +8%";
        var ttipFlavor = HUD_CreateRT("Flavor", ttip);
        ttipFlavor.anchorMin = new Vector2(0, 0); ttipFlavor.anchorMax = new Vector2(1, 0);
        ttipFlavor.offsetMin = new Vector2(14, 4); ttipFlavor.offsetMax = new Vector2(-14, 24);
        var ttipFlavorT = ttipFlavor.gameObject.AddComponent<Text>();
        ttipFlavorT.font = font; ttipFlavorT.fontSize = 10; ttipFlavorT.fontStyle = FontStyle.Italic;
        ttipFlavorT.color = new Color(0.78f, 0.38f, 0.91f); // epic+ flavor
        ttipFlavorT.alignment = TextAnchor.MiddleLeft;
        ttipFlavorT.text = "\"여명의 빛이 깃든 검...\"";

        // Footer (gold + slot count)
        var footer = HUD_CreateRT("Footer", invPanel);
        footer.anchorMin = new Vector2(0, 0); footer.anchorMax = new Vector2(1, 0);
        footer.pivot = new Vector2(0.5f, 0);
        footer.anchoredPosition = new Vector2(0, 60);
        footer.sizeDelta = new Vector2(-32, 32);
        var footerBg = footer.gameObject.AddComponent<Image>();
        footerBg.sprite = TexToSprite(MakeRoundRectTex(128, 32, 4, tokWoodLite));
        footerBg.type = Image.Type.Sliced;
        var goldLbl = HUD_CreateRT("Gold", footer);
        goldLbl.anchorMin = new Vector2(0, 0); goldLbl.anchorMax = new Vector2(0.5f, 1);
        goldLbl.offsetMin = new Vector2(14, 0); goldLbl.offsetMax = new Vector2(-4, 0);
        var goldT = goldLbl.gameObject.AddComponent<Text>();
        goldT.font = font; goldT.fontSize = 14; goldT.fontStyle = FontStyle.Bold;
        goldT.color = tokGold;
        goldT.alignment = TextAnchor.MiddleLeft;
        goldT.text = "◉  28,442 G";
        var slotsLbl = HUD_CreateRT("SlotCount", footer);
        slotsLbl.anchorMin = new Vector2(0.5f, 0); slotsLbl.anchorMax = new Vector2(1, 1);
        slotsLbl.offsetMin = new Vector2(4, 0); slotsLbl.offsetMax = new Vector2(-14, 0);
        var slotsT = slotsLbl.gameObject.AddComponent<Text>();
        slotsT.font = font; slotsT.fontSize = 11;
        slotsT.color = tokParchDark;
        slotsT.alignment = TextAnchor.MiddleRight;
        slotsT.text = "0 / 36  슬롯";

        // Action buttons (정렬 / 분할 / 버리기)
        string[] actLabels = { "⇅  정렬", "⊟  분할", "✕  버리기" };
        for (int i = 0; i < 3; i++)
        {
            var btn = HUD_CreateRT($"Act_{i}", invPanel);
            btn.anchorMin = new Vector2(0, 0); btn.anchorMax = new Vector2(0, 0);
            btn.pivot = new Vector2(0, 0);
            float bw = (440 - 32 - 12) / 3f;
            btn.anchoredPosition = new Vector2(16 + i * (bw + 6), 18);
            btn.sizeDelta = new Vector2(bw, 32);
            var bImg = btn.gameObject.AddComponent<Image>();
            bImg.sprite = parchmentSpr; bImg.type = Image.Type.Sliced;
            var lblRT2 = HUD_CreateRT("Label", btn);
            lblRT2.anchorMin = Vector2.zero; lblRT2.anchorMax = Vector2.one;
            lblRT2.offsetMin = lblRT2.offsetMax = Vector2.zero;
            var lblT2 = lblRT2.gameObject.AddComponent<Text>();
            lblT2.font = font; lblT2.fontSize = 11; lblT2.fontStyle = FontStyle.Bold;
            lblT2.color = tokInk;
            lblT2.alignment = TextAnchor.MiddleCenter;
            lblT2.text = actLabels[i];
            btn.gameObject.AddComponent<Button>();
        }

        var invUI = canvasGo.AddComponent<Astrion.UI.InventoryUI>();
        var invUiSo = new UnityEditor.SerializedObject(invUI);
        invUiSo.FindProperty("panel").objectReferenceValue = invPanel.gameObject;
        invUiSo.FindProperty("slotsRoot").objectReferenceValue = slotsRoot2;
        invUiSo.FindProperty("closeButton").objectReferenceValue = invCloseB;
        invUiSo.ApplyModifiedPropertiesWithoutUndo();
        invPanel.gameObject.SetActive(false);

        // ========== SKILL WINDOW (K key) ==========
        var skillPanel = HUD_CreateRT("SkillWindow", root);
        skillPanel.anchorMin = skillPanel.anchorMax = new Vector2(0.5f, 0.5f);
        skillPanel.pivot = new Vector2(0.5f, 0.5f);
        skillPanel.anchoredPosition = Vector2.zero;
        skillPanel.sizeDelta = new Vector2(480, 430);
        var skillBgImg = skillPanel.gameObject.AddComponent<Image>();
        skillBgImg.sprite = panelSpr; skillBgImg.type = Image.Type.Sliced;

        // Header
        var skillHdrRT = HUD_CreateRT("Header", skillPanel);
        skillHdrRT.anchorMin = new Vector2(0, 1); skillHdrRT.anchorMax = new Vector2(1, 1);
        skillHdrRT.pivot = new Vector2(0.5f, 1);
        skillHdrRT.anchoredPosition = new Vector2(0, -10);
        skillHdrRT.sizeDelta = new Vector2(-20, 32);
        var skillHdrText = skillHdrRT.gameObject.AddComponent<Text>();
        skillHdrText.font = font; skillHdrText.fontSize = 18; skillHdrText.fontStyle = FontStyle.Bold;
        skillHdrText.color = new Color(1f, 0.85f, 0.40f);
        skillHdrText.alignment = TextAnchor.MiddleLeft;
        skillHdrText.text = "스킬   S K I L L S";

        // Close X
        var skillCloseRT = HUD_CreateRT("Close", skillPanel);
        skillCloseRT.anchorMin = skillCloseRT.anchorMax = new Vector2(1, 1);
        skillCloseRT.pivot = new Vector2(1, 1);
        skillCloseRT.anchoredPosition = new Vector2(-8, -8);
        skillCloseRT.sizeDelta = new Vector2(28, 28);
        skillCloseRT.gameObject.AddComponent<Image>().sprite =
            TexToSprite(MakeRoundRectTex(64, 64, 6, new Color(0.20f, 0.05f, 0.05f, 0.95f)));
        var skillCloseB = skillCloseRT.gameObject.AddComponent<Button>();
        var skillXRT = HUD_CreateRT("X", skillCloseRT);
        skillXRT.anchorMin = Vector2.zero; skillXRT.anchorMax = Vector2.one;
        skillXRT.offsetMin = skillXRT.offsetMax = Vector2.zero;
        var skillXT = skillXRT.gameObject.AddComponent<Text>();
        skillXT.font = font; skillXT.fontSize = 16; skillXT.fontStyle = FontStyle.Bold;
        skillXT.color = new Color(1f, 0.85f, 0.75f);
        skillXT.alignment = TextAnchor.MiddleCenter;
        skillXT.text = "×";

        // Rows container
        var skillRowsRoot = HUD_CreateRT("Rows", skillPanel);
        skillRowsRoot.anchorMin = new Vector2(0, 0); skillRowsRoot.anchorMax = new Vector2(1, 1);
        skillRowsRoot.offsetMin = new Vector2(12, 40); skillRowsRoot.offsetMax = new Vector2(-12, -48);

        // 3 skill rows
        (string id, string name, string letter, Color color, string desc)[] skillRows = {
            ("starbolt",     "별빛 투사체",  "★", new Color(1f,    0.85f, 0.30f), "정면 별빛 발사 / 자동 호밍 / Lv당 데미지 +5"),
            ("meteor",       "유성 낙하",    "☄", new Color(0.95f, 0.45f, 0.20f), "범위 별 폭발 (구현 예정)"),
            ("stellar_heal", "별빛 회복",    "♥", new Color(0.55f, 1f,    0.55f), "HP 회복 / Lv당 회복량 +10"),
        };
        for (int i = 0; i < skillRows.Length; i++)
        {
            var s = skillRows[i];
            var row = HUD_CreateRT($"Row_{i}", skillRowsRoot);
            row.anchorMin = new Vector2(0, 1); row.anchorMax = new Vector2(1, 1);
            row.pivot = new Vector2(0.5f, 1);
            row.anchoredPosition = new Vector2(0, -i * 100);
            row.sizeDelta = new Vector2(0, 90);
            var rowBg = row.gameObject.AddComponent<Image>();
            rowBg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 6, new Color(0.07f, 0.05f, 0.04f, 0.85f)));
            rowBg.type = Image.Type.Sliced;

            // Icon
            var icon = HUD_CreateRT("Icon", row);
            icon.anchorMin = new Vector2(0, 0.5f); icon.anchorMax = new Vector2(0, 0.5f);
            icon.pivot = new Vector2(0, 0.5f);
            icon.anchoredPosition = new Vector2(10, 0);
            icon.sizeDelta = new Vector2(68, 68);
            var iconBg = icon.gameObject.AddComponent<Image>();
            iconBg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 6, s.color));
            var iconLbl = HUD_CreateRT("Letter", icon);
            iconLbl.anchorMin = Vector2.zero; iconLbl.anchorMax = Vector2.one;
            iconLbl.offsetMin = iconLbl.offsetMax = Vector2.zero;
            var iconLblT = iconLbl.gameObject.AddComponent<Text>();
            iconLblT.font = font; iconLblT.fontSize = 32; iconLblT.fontStyle = FontStyle.Bold;
            iconLblT.color = new Color(0.10f, 0.07f, 0.04f);
            iconLblT.alignment = TextAnchor.MiddleCenter;
            iconLblT.text = s.letter;

            // Name
            var sNameRT = HUD_CreateRT("Name", row);
            sNameRT.anchorMin = new Vector2(0, 0.55f); sNameRT.anchorMax = new Vector2(0.65f, 1);
            sNameRT.offsetMin = new Vector2(90, 0); sNameRT.offsetMax = new Vector2(0, -6);
            var sNameT = sNameRT.gameObject.AddComponent<Text>();
            sNameT.font = font; sNameT.fontSize = 15; sNameT.fontStyle = FontStyle.Bold;
            sNameT.color = new Color(1f, 0.85f, 0.40f);
            sNameT.alignment = TextAnchor.LowerLeft;
            sNameT.text = s.name;

            // Desc
            var descRT = HUD_CreateRT("Desc", row);
            descRT.anchorMin = new Vector2(0, 0.15f); descRT.anchorMax = new Vector2(0.65f, 0.55f);
            descRT.offsetMin = new Vector2(90, 0); descRT.offsetMax = new Vector2(0, 0);
            var descT = descRT.gameObject.AddComponent<Text>();
            descT.font = font; descT.fontSize = 11;
            descT.color = new Color(0.78f, 0.72f, 0.60f);
            descT.alignment = TextAnchor.UpperLeft;
            descT.text = s.desc;

            // Requirement (bottom-left of row)
            var reqRT = HUD_CreateRT("Requirement", row);
            reqRT.anchorMin = new Vector2(0, 0); reqRT.anchorMax = new Vector2(0.65f, 0.15f);
            reqRT.offsetMin = new Vector2(90, 4); reqRT.offsetMax = new Vector2(0, 0);
            var reqT = reqRT.gameObject.AddComponent<Text>();
            reqT.font = font; reqT.fontSize = 10;
            reqT.color = new Color(0.55f, 0.48f, 0.34f);
            reqT.alignment = TextAnchor.MiddleLeft;
            reqT.text = "—";

            // Level (right side, large)
            var levelRT2 = HUD_CreateRT("Level", row);
            levelRT2.anchorMin = new Vector2(0.65f, 0.45f); levelRT2.anchorMax = new Vector2(0.85f, 1);
            levelRT2.offsetMin = levelRT2.offsetMax = Vector2.zero;
            var levelT2 = levelRT2.gameObject.AddComponent<Text>();
            levelT2.font = font; levelT2.fontSize = 16; levelT2.fontStyle = FontStyle.Bold;
            levelT2.color = new Color(0.94f, 0.85f, 0.47f);
            levelT2.alignment = TextAnchor.MiddleCenter;
            levelT2.text = "Lv.0/5";

            // Plus button (top-right, smaller)
            var plusRT = HUD_CreateRT("Plus", row);
            plusRT.anchorMin = new Vector2(0.85f, 0.55f); plusRT.anchorMax = new Vector2(1, 1);
            plusRT.offsetMin = new Vector2(4, 4); plusRT.offsetMax = new Vector2(-10, -4);
            var plusImg = plusRT.gameObject.AddComponent<Image>();
            plusImg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 8, new Color(0.85f, 0.65f, 0.22f, 1f)));
            var plusBtn2 = plusRT.gameObject.AddComponent<Button>();
            var plusT2 = HUD_CreateRT("T", plusRT);
            plusT2.anchorMin = Vector2.zero; plusT2.anchorMax = Vector2.one;
            plusT2.offsetMin = plusT2.offsetMax = Vector2.zero;
            var plusTT = plusT2.gameObject.AddComponent<Text>();
            plusTT.font = font; plusTT.fontSize = 16; plusTT.fontStyle = FontStyle.Bold;
            plusTT.color = new Color(0.12f, 0.08f, 0.04f);
            plusTT.alignment = TextAnchor.MiddleCenter;
            plusTT.text = "+";

            // Hotbar slot buttons (1~5) — bottom-right of row
            var hotRoot = HUD_CreateRT("HotRoot", row);
            hotRoot.anchorMin = new Vector2(0.62f, 0); hotRoot.anchorMax = new Vector2(1, 0.50f);
            hotRoot.offsetMin = new Vector2(4, 4); hotRoot.offsetMax = new Vector2(-4, 0);
            var hotLbl = HUD_CreateRT("Label", hotRoot);
            hotLbl.anchorMin = new Vector2(0, 0); hotLbl.anchorMax = new Vector2(0.30f, 1);
            hotLbl.offsetMin = hotLbl.offsetMax = Vector2.zero;
            var hotLblT = hotLbl.gameObject.AddComponent<Text>();
            hotLblT.font = font; hotLblT.fontSize = 11;
            hotLblT.color = new Color(0.78f, 0.72f, 0.60f);
            hotLblT.alignment = TextAnchor.MiddleRight;
            hotLblT.text = "단축키 ";

            float slotStart = 0.30f;
            float slotEnd = 1.0f;
            float slotSpan = (slotEnd - slotStart) / 5f;
            for (int hi = 0; hi < 5; hi++)
            {
                var hotRT = HUD_CreateRT($"Hot_{hi}", hotRoot);
                hotRT.anchorMin = new Vector2(slotStart + slotSpan * hi, 0);
                hotRT.anchorMax = new Vector2(slotStart + slotSpan * (hi + 1), 1);
                hotRT.offsetMin = new Vector2(2, 1); hotRT.offsetMax = new Vector2(-2, -1);
                var hotImg = hotRT.gameObject.AddComponent<Image>();
                hotImg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 6, Color.white));
                hotImg.color = new Color(0.30f, 0.24f, 0.16f, 1f);
                var hotBtn = hotRT.gameObject.AddComponent<Button>();
                var hotKeyT = HUD_CreateRT("Key", hotRT);
                hotKeyT.anchorMin = Vector2.zero; hotKeyT.anchorMax = Vector2.one;
                hotKeyT.offsetMin = hotKeyT.offsetMax = Vector2.zero;
                var hotKeyText = hotKeyT.gameObject.AddComponent<Text>();
                hotKeyText.font = font; hotKeyText.fontSize = 14; hotKeyText.fontStyle = FontStyle.Bold;
                hotKeyText.color = new Color(0.92f, 0.86f, 0.72f);
                hotKeyText.alignment = TextAnchor.MiddleCenter;
                hotKeyText.text = (hi + 1).ToString();
            }
        }

        // Footer (skill points)
        var spFooter = HUD_CreateRT("Footer", skillPanel);
        spFooter.anchorMin = new Vector2(0, 0); spFooter.anchorMax = new Vector2(1, 0);
        spFooter.pivot = new Vector2(0.5f, 0);
        spFooter.anchoredPosition = new Vector2(0, 8);
        spFooter.sizeDelta = new Vector2(-20, 28);
        var spT = spFooter.gameObject.AddComponent<Text>();
        spT.font = font; spT.fontSize = 13; spT.fontStyle = FontStyle.Bold;
        spT.color = new Color(0.91f, 0.31f, 0.28f);
        spT.alignment = TextAnchor.MiddleCenter;
        spT.text = "남은 스킬 포인트 : 0";

        var skillUI = canvasGo.AddComponent<Astrion.UI.SkillWindowUI>();
        var skillSo = new UnityEditor.SerializedObject(skillUI);
        skillSo.FindProperty("panel").objectReferenceValue = skillPanel.gameObject;
        skillSo.FindProperty("rowsRoot").objectReferenceValue = skillRowsRoot;
        skillSo.FindProperty("spText").objectReferenceValue = spT;
        skillSo.FindProperty("closeButton").objectReferenceValue = skillCloseB;
        skillSo.ApplyModifiedPropertiesWithoutUndo();
        skillPanel.gameObject.SetActive(false);

        // ========== DIALOGUE UI ==========
        var dlg = canvasGo.AddComponent<Astrion.UI.DialogueUI>();

        // Interaction hint (top-center, "[E] NPC와 대화")
        var hintPanel = HUD_CreateRT("DialogueHint", root);
        hintPanel.anchorMin = hintPanel.anchorMax = new Vector2(0.5f, 1);
        hintPanel.pivot = new Vector2(0.5f, 1);
        hintPanel.anchoredPosition = new Vector2(0, -90);
        hintPanel.sizeDelta = new Vector2(320, 42);
        var hpBg = hintPanel.gameObject.AddComponent<Image>();
        hpBg.sprite = panelSpr; hpBg.type = Image.Type.Sliced;
        var hintTextRT = HUD_CreateRT("Text", hintPanel);
        hintTextRT.anchorMin = Vector2.zero; hintTextRT.anchorMax = Vector2.one;
        hintTextRT.offsetMin = hintTextRT.offsetMax = Vector2.zero;
        var hintTextC = hintTextRT.gameObject.AddComponent<Text>();
        hintTextC.font = font; hintTextC.fontSize = 14; hintTextC.fontStyle = FontStyle.Bold;
        hintTextC.color = new Color(1f, 0.88f, 0.45f);
        hintTextC.alignment = TextAnchor.MiddleCenter;
        hintTextC.text = "[E]  NPC와 대화";

        // Dialog panel (centered above the action bar)
        var dialogPanel = HUD_CreateRT("DialoguePanel", root);
        dialogPanel.anchorMin = dialogPanel.anchorMax = new Vector2(0.5f, 0);
        dialogPanel.pivot = new Vector2(0.5f, 0);
        dialogPanel.anchoredPosition = new Vector2(0, 170);
        dialogPanel.sizeDelta = new Vector2(900, 190);
        var dpBg = dialogPanel.gameObject.AddComponent<Image>();
        dpBg.sprite = panelSpr; dpBg.type = Image.Type.Sliced;

        var speakerRT = HUD_CreateRT("Speaker", dialogPanel);
        speakerRT.anchorMin = new Vector2(0, 1); speakerRT.anchorMax = new Vector2(0, 1);
        speakerRT.pivot = new Vector2(0, 1);
        speakerRT.anchoredPosition = new Vector2(24, -12);
        speakerRT.sizeDelta = new Vector2(360, 30);
        var speakerTextC = speakerRT.gameObject.AddComponent<Text>();
        speakerTextC.font = font; speakerTextC.fontSize = 18; speakerTextC.fontStyle = FontStyle.Bold;
        speakerTextC.color = new Color(1f, 0.85f, 0.40f);
        speakerTextC.alignment = TextAnchor.MiddleLeft;
        speakerTextC.text = "폴라리스";

        var contentRT = HUD_CreateRT("Content", dialogPanel);
        contentRT.anchorMin = new Vector2(0, 0); contentRT.anchorMax = new Vector2(1, 1);
        contentRT.offsetMin = new Vector2(28, 36); contentRT.offsetMax = new Vector2(-28, -46);
        var contentTextC = contentRT.gameObject.AddComponent<Text>();
        contentTextC.font = font; contentTextC.fontSize = 16;
        contentTextC.color = textWhite;
        contentTextC.alignment = TextAnchor.UpperLeft;
        contentTextC.supportRichText = true;
        contentTextC.horizontalOverflow = HorizontalWrapMode.Wrap;
        contentTextC.text = "...";

        var contRT = HUD_CreateRT("Continue", dialogPanel);
        contRT.anchorMin = contRT.anchorMax = new Vector2(1, 0);
        contRT.pivot = new Vector2(1, 0);
        contRT.anchoredPosition = new Vector2(-20, 12);
        contRT.sizeDelta = new Vector2(180, 22);
        var contTextC = contRT.gameObject.AddComponent<Text>();
        contTextC.font = font; contTextC.fontSize = 12;
        contTextC.color = new Color(0.75f, 0.85f, 0.50f);
        contTextC.alignment = TextAnchor.MiddleRight;
        contTextC.text = "[Space]  계속";

        // Wire HUD references
        var so = new UnityEditor.SerializedObject(hudComp);
        so.FindProperty("hpFill").objectReferenceValue = hpFill;
        so.FindProperty("mpFill").objectReferenceValue = mpFill;
        so.FindProperty("expFill").objectReferenceValue = expFill;
        so.FindProperty("hpText").objectReferenceValue = hpBarText;
        so.FindProperty("mpText").objectReferenceValue = mpBarText;
        so.FindProperty("expText").objectReferenceValue = expBarText;
        so.FindProperty("charNameText").objectReferenceValue = nameText;
        so.FindProperty("charLevelText").objectReferenceValue = levelText;
        so.FindProperty("coordsText").objectReferenceValue = coordsText;
        so.FindProperty("mapNameText").objectReferenceValue = mapNameTextC;
        so.ApplyModifiedPropertiesWithoutUndo();

        var dlgSo = new UnityEditor.SerializedObject(dlg);
        dlgSo.FindProperty("hintPanel").objectReferenceValue = hintPanel.gameObject;
        dlgSo.FindProperty("hintText").objectReferenceValue = hintTextC;
        dlgSo.FindProperty("dialogPanel").objectReferenceValue = dialogPanel.gameObject;
        dlgSo.FindProperty("speakerText").objectReferenceValue = speakerTextC;
        dlgSo.FindProperty("contentText").objectReferenceValue = contentTextC;
        dlgSo.FindProperty("continuePrompt").objectReferenceValue = contTextC;
        dlgSo.ApplyModifiedPropertiesWithoutUndo();

        // ========== TOAST UI (top-right notifications) ==========
        var toastStack = HUD_CreateRT("ToastStack", root);
        toastStack.anchorMin = toastStack.anchorMax = new Vector2(1, 1);
        toastStack.pivot = new Vector2(1, 1);
        toastStack.anchoredPosition = Vector2.zero;
        toastStack.sizeDelta = new Vector2(380, 220);

        // Toast template (hidden) — ToastUI clones this when emitting
        var toastTpl = HUD_CreateRT("ToastTemplate", toastStack);
        toastTpl.anchorMin = toastTpl.anchorMax = new Vector2(1, 1);
        toastTpl.pivot = new Vector2(1, 1);
        toastTpl.anchoredPosition = new Vector2(-12, -200);
        toastTpl.sizeDelta = new Vector2(360, 32);
        var toastBg = toastTpl.gameObject.AddComponent<Image>();
        toastBg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 6, new Color(0.08f, 0.06f, 0.04f, 0.92f)));
        toastBg.type = Image.Type.Sliced;
        toastTpl.gameObject.AddComponent<CanvasGroup>();
        var toastTextRT = HUD_CreateRT("Text", toastTpl);
        toastTextRT.anchorMin = Vector2.zero; toastTextRT.anchorMax = Vector2.one;
        toastTextRT.offsetMin = new Vector2(12, 0); toastTextRT.offsetMax = new Vector2(-12, 0);
        var toastTextC = toastTextRT.gameObject.AddComponent<Text>();
        toastTextC.font = font; toastTextC.fontSize = 13; toastTextC.fontStyle = FontStyle.Bold;
        toastTextC.color = new Color(0.92f, 0.86f, 0.72f);
        toastTextC.alignment = TextAnchor.MiddleLeft;
        toastTextC.text = "";
        toastTpl.gameObject.SetActive(false);

        var toastUI = canvasGo.AddComponent<Astrion.UI.ToastUI>();
        var toastSo = new UnityEditor.SerializedObject(toastUI);
        toastSo.FindProperty("stackRoot").objectReferenceValue = toastStack;
        toastSo.FindProperty("toastTemplate").objectReferenceValue = toastTpl.gameObject;
        toastSo.ApplyModifiedPropertiesWithoutUndo();

        // ========== SYSTEM MENU (ESC) ==========
        var sysPanel = HUD_CreateRT("SystemMenu", root);
        sysPanel.anchorMin = sysPanel.anchorMax = new Vector2(0.5f, 0.5f);
        sysPanel.pivot = new Vector2(0.5f, 0.5f);
        sysPanel.anchoredPosition = Vector2.zero;
        sysPanel.sizeDelta = new Vector2(360, 280);
        var sysBg = sysPanel.gameObject.AddComponent<Image>();
        sysBg.sprite = panelSpr; sysBg.type = Image.Type.Sliced;

        var sysHdr = HUD_CreateRT("Header", sysPanel);
        sysHdr.anchorMin = new Vector2(0, 1); sysHdr.anchorMax = new Vector2(1, 1);
        sysHdr.pivot = new Vector2(0.5f, 1);
        sysHdr.anchoredPosition = new Vector2(0, -12);
        sysHdr.sizeDelta = new Vector2(-20, 32);
        var sysHdrT = sysHdr.gameObject.AddComponent<Text>();
        sysHdrT.font = font; sysHdrT.fontSize = 18; sysHdrT.fontStyle = FontStyle.Bold;
        sysHdrT.color = new Color(1f, 0.85f, 0.40f);
        sysHdrT.alignment = TextAnchor.MiddleCenter;
        sysHdrT.text = "시스템";

        // Buttons
        Button MakeMenuBtn(string nm, string label, float yPos, Color tint) {
            var bRT = HUD_CreateRT(nm, sysPanel);
            bRT.anchorMin = new Vector2(0.5f, 1); bRT.anchorMax = new Vector2(0.5f, 1);
            bRT.pivot = new Vector2(0.5f, 1);
            bRT.anchoredPosition = new Vector2(0, yPos);
            bRT.sizeDelta = new Vector2(280, 48);
            var bImg = bRT.gameObject.AddComponent<Image>();
            bImg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 8, tint));
            bImg.type = Image.Type.Sliced;
            var btn = bRT.gameObject.AddComponent<Button>();
            var lblRT = HUD_CreateRT("Label", bRT);
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
            var lblT = lblRT.gameObject.AddComponent<Text>();
            lblT.font = font; lblT.fontSize = 15; lblT.fontStyle = FontStyle.Bold;
            lblT.color = new Color(0.96f, 0.90f, 0.78f);
            lblT.alignment = TextAnchor.MiddleCenter;
            lblT.text = label;
            return btn;
        }
        var sysCharBtn = MakeMenuBtn("CharSelectBtn", "캐릭터 선택으로", -56, new Color(0.42f, 0.28f, 0.15f, 1f));
        var sysCloseBtn = MakeMenuBtn("CloseBtn", "계속하기  (ESC)", -112, new Color(0.30f, 0.24f, 0.16f, 1f));
        var sysQuitBtn = MakeMenuBtn("QuitBtn", "게임 종료", -176, new Color(0.55f, 0.18f, 0.18f, 1f));

        var sysUI = canvasGo.AddComponent<Astrion.UI.SystemMenuUI>();
        var sysSo = new UnityEditor.SerializedObject(sysUI);
        sysSo.FindProperty("panel").objectReferenceValue = sysPanel.gameObject;
        sysSo.FindProperty("charSelectButton").objectReferenceValue = sysCharBtn;
        sysSo.FindProperty("quitButton").objectReferenceValue = sysQuitBtn;
        sysSo.FindProperty("closeButton").objectReferenceValue = sysCloseBtn;
        sysSo.ApplyModifiedPropertiesWithoutUndo();
        sysPanel.gameObject.SetActive(false);

        // Top-right menu (☰) button — opens system menu
        var menuBtnRT = HUD_CreateRT("MenuBtn", root);
        menuBtnRT.anchorMin = menuBtnRT.anchorMax = new Vector2(1, 1);
        menuBtnRT.pivot = new Vector2(1, 1);
        menuBtnRT.anchoredPosition = new Vector2(-12, -12);
        menuBtnRT.sizeDelta = new Vector2(40, 40);
        var menuBtnImg = menuBtnRT.gameObject.AddComponent<Image>();
        menuBtnImg.sprite = TexToSprite(MakeRoundRectTex(64, 64, 8, new Color(0.10f, 0.08f, 0.06f, 0.92f)));
        menuBtnImg.type = Image.Type.Sliced;
        var menuBtn = menuBtnRT.gameObject.AddComponent<Button>();
        menuBtn.onClick.AddListener(() => sysUI.Toggle());
        var menuTRT = HUD_CreateRT("Icon", menuBtnRT);
        menuTRT.anchorMin = Vector2.zero; menuTRT.anchorMax = Vector2.one;
        menuTRT.offsetMin = menuTRT.offsetMax = Vector2.zero;
        var menuT = menuTRT.gameObject.AddComponent<Text>();
        menuT.font = font; menuT.fontSize = 22; menuT.fontStyle = FontStyle.Bold;
        menuT.color = new Color(0.92f, 0.86f, 0.72f);
        menuT.alignment = TextAnchor.MiddleCenter;
        menuT.text = "☰";
    }

    private static Image CreateMapleBar(RectTransform parent, string name, Vector2 pos, Vector2 size,
        Sprite fillSpr, Sprite bgSpr, Font font, string defaultText, out Text valText)
    {
        var bar = HUD_CreateRT(name, parent);
        bar.anchorMin = bar.anchorMax = new Vector2(0, 0);
        bar.pivot = new Vector2(0, 0);
        bar.anchoredPosition = pos;
        bar.sizeDelta = size;

        var bg = bar.gameObject.AddComponent<Image>();
        bg.sprite = bgSpr; bg.type = Image.Type.Sliced;

        var fillRT = HUD_CreateRT("Fill", bar);
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(2, 2); fillRT.offsetMax = new Vector2(-2, -2);
        var fill = fillRT.gameObject.AddComponent<Image>();
        fill.sprite = fillSpr;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 1f;

        var shineRT = HUD_CreateRT("Shine", bar);
        shineRT.anchorMin = new Vector2(0, 0.5f); shineRT.anchorMax = Vector2.one;
        shineRT.offsetMin = new Vector2(3, 0); shineRT.offsetMax = new Vector2(-3, -1);
        shineRT.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.20f);

        var valRT = HUD_CreateRT("Text", bar);
        valRT.anchorMin = Vector2.zero; valRT.anchorMax = Vector2.one;
        valRT.offsetMin = new Vector2(6, 0); valRT.offsetMax = new Vector2(-6, 0);
        valText = valRT.gameObject.AddComponent<Text>();
        valText.font = font; valText.fontSize = 11; valText.fontStyle = FontStyle.Bold;
        valText.color = new Color(1, 1, 1, 0.95f);
        valText.alignment = TextAnchor.MiddleCenter;
        valText.text = defaultText;

        return fill;
    }

    private static RectTransform HUD_CreateRT(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static RectTransform HUD_CreateModernBar(RectTransform parent, string name,
        Texture2D gradientTex, Sprite circleSpr,
        Vector2 offsetMin, Vector2 offsetMax, float yMin, float yMax, Font font, string defaultText)
    {
        var bar = HUD_CreateRT(name, parent);
        bar.anchorMin = new Vector2(0, yMin);
        bar.anchorMax = new Vector2(1, yMax);
        bar.offsetMin = offsetMin;
        bar.offsetMax = offsetMax;

        // Dark track background
        var barBg = bar.gameObject.AddComponent<Image>();
        barBg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

        // Gradient fill
        var fill = HUD_CreateRT("Fill", bar);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = new Vector2(1, 1);
        fill.offsetMax = new Vector2(-1, -1);
        var fillImg = fill.gameObject.AddComponent<Image>();
        fillImg.sprite = TexToSprite(gradientTex);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;

        // Shine overlay (top half brighter)
        var shine = HUD_CreateRT("Shine", bar);
        shine.anchorMin = new Vector2(0, 0.5f);
        shine.anchorMax = Vector2.one;
        shine.offsetMin = new Vector2(1, 0);
        shine.offsetMax = new Vector2(-1, -1);
        shine.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.08f);

        // Value text
        var txt = HUD_CreateRT("Text", bar);
        txt.anchorMin = Vector2.zero;
        txt.anchorMax = Vector2.one;
        txt.offsetMin = new Vector2(6, 0);
        txt.offsetMax = new Vector2(-6, 0);
        var t = txt.gameObject.AddComponent<Text>();
        t.font = font; t.fontSize = 11;
        t.color = new Color(1, 1, 1, 0.85f);
        t.alignment = TextAnchor.MiddleRight;
        t.text = defaultText;

        return bar;
    }

    private static void HUD_CreateModernActionBtn(RectTransform parent, string icon,
        Texture2D bgTex, Sprite circleSpr, Font font, Vector2 anchor, int size, int fontSize, Color glowColor)
    {
        // Glow behind button
        var glow = HUD_CreateRT(icon + "_Glow", parent);
        glow.anchorMin = glow.anchorMax = anchor;
        glow.sizeDelta = new Vector2(size + 20, size + 20);
        var glowImg = glow.gameObject.AddComponent<Image>();
        glowImg.sprite = circleSpr;
        glowImg.color = glowColor;

        // Button
        var btn = HUD_CreateRT(icon + "_Btn", parent);
        btn.anchorMin = btn.anchorMax = anchor;
        btn.sizeDelta = new Vector2(size, size);
        var btnImg = btn.gameObject.AddComponent<Image>();
        btnImg.sprite = TexToSprite(bgTex);
        btn.gameObject.AddComponent<Button>();

        // Inner ring
        var ring = HUD_CreateRT("Ring", btn);
        ring.anchorMin = Vector2.zero; ring.anchorMax = Vector2.one;
        ring.offsetMin = new Vector2(-1, -1);
        ring.offsetMax = new Vector2(1, 1);
        var ringTex2 = MakeRingTex(64, 0.88f, 0.98f, new Color(1, 1, 1, 0.25f));
        ring.gameObject.AddComponent<Image>().sprite = TexToSprite(ringTex2);

        // Icon text
        var txt = HUD_CreateRT("Icon", btn);
        txt.anchorMin = Vector2.zero; txt.anchorMax = Vector2.one;
        txt.offsetMin = txt.offsetMax = Vector2.zero;
        var t = txt.gameObject.AddComponent<Text>();
        t.font = font; t.fontSize = fontSize; t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = icon;
    }

    // ===== HELPERS =====

    private static GameObject CreateUIElement(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform CreateUIElement(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
    }

    private static void SetRect(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void SetRect(GameObject go, float x, float y, float w, float h)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
    }

    private static void SetRectInPanel(GameObject go, float x, float y, float w, float h)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
    }

    private static void StretchFull(GameObject go)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject CreateFieldWithIcon(string name, Transform parent, string icon, string placeholder, float yPos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var bgImage = go.AddComponent<Image>();
        bgImage.color = FieldBg;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = FieldBorder;
        outline.effectDistance = new Vector2(1, 1);

        var inputField = go.AddComponent<InputField>();

        // Icon label
        var iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(go.transform, false);
        var iconText = iconGo.AddComponent<Text>();
        iconText.text = icon;
        iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iconText.fontSize = 18;
        iconText.color = AccentGold;
        iconText.alignment = TextAnchor.MiddleCenter;
        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0);
        iconRect.anchorMax = new Vector2(0, 1);
        iconRect.anchoredPosition = new Vector2(22, 0);
        iconRect.sizeDelta = new Vector2(22, 0);

        // Text
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 16;
        text.color = TextLight;
        text.supportRichText = false;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(48, 5);
        textRect.offsetMax = new Vector2(-15, -5);

        // Placeholder
        var phGo = new GameObject("Placeholder", typeof(RectTransform));
        phGo.transform.SetParent(go.transform, false);
        var phText = phGo.AddComponent<Text>();
        phText.text = placeholder;
        phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        phText.fontSize = 16;
        phText.fontStyle = FontStyle.Normal;
        phText.color = new Color(0.55f, 0.65f, 0.6f, 0.6f);
        var phRect = phGo.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = new Vector2(48, 5);
        phRect.offsetMax = new Vector2(-15, -5);

        inputField.textComponent = text;
        inputField.placeholder = phText;

        SetRectInPanel(go, 0, yPos, 600, 65);
        return go;
    }

    private static void CreateSocialButton(string name, Transform parent, string label, Color bgColor, float xPos)
    {
        var go = CreateUIElement(name, parent);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        go.AddComponent<Button>();
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.72f, 0.40f, 0.25f);
        outline.effectDistance = new Vector2(1, 1);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(xPos, 0);
        rect.sizeDelta = new Vector2(50, 50);

        var textGo = CreateUIElement("Label", go.transform);
        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        StretchFull(textGo);
    }

    private static void CreateCornerDeco(Transform parent, Vector2 anchor, Vector2 offset)
    {
        var go = CreateUIElement("CornerDeco", parent);
        var img = go.AddComponent<Image>();
        img.color = AccentGold;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(6, 6);
    }
}
