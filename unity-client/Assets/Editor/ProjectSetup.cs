using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class ProjectSetup
{
    // Diablo-style color palette (gothic blood + iron)
    private static readonly Color AccentGold = new Color(0.78f, 0.18f, 0.10f, 1f);       // blood red (was gold accent)
    private static readonly Color AccentGoldDim = new Color(0.78f, 0.18f, 0.10f, 0.25f);
    private static readonly Color AccentGreen = new Color(0.55f, 0.10f, 0.08f, 1f);       // deep crimson (was emerald accent)
    private static readonly Color PanelBg = new Color(0.04f, 0.03f, 0.02f, 0.92f);        // near-black charcoal
    private static readonly Color PanelInner = new Color(0.07f, 0.05f, 0.04f, 0.7f);      // inner stone
    private static readonly Color FieldBg = new Color(0.06f, 0.04f, 0.03f, 0.95f);        // dark parchment
    private static readonly Color FieldBorder = new Color(0.40f, 0.20f, 0.10f, 0.55f);    // rusted iron border
    private static readonly Color TextLight = new Color(0.88f, 0.83f, 0.72f, 1f);         // bone/parchment white
    private static readonly Color TextMuted = new Color(0.50f, 0.42f, 0.34f, 1f);         // weathered stone
    private static readonly Color BtnColor = new Color(0.45f, 0.08f, 0.06f, 1f);          // dried blood button

    [MenuItem("Astrion/Setup Project")]
    public static void Setup()
    {
        SetupBuildSettings();
        CreateLoginScene();
        CreateCharacterSelectScene();
        CreateCharacterCreateScene();
        CreateMainScene();
        Debug.Log("[Astrion] Project setup complete!");
    }

    [MenuItem("Astrion/Build Android (Debug)")]
    public static void BuildAndroid()
    {
        var scenes = new[] { "Assets/Scenes/LoginScene.unity", "Assets/Scenes/CharacterSelectScene.unity", "Assets/Scenes/CharacterCreateScene.unity", "Assets/Scenes/MainScene.unity" };
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
        var scenes = new[] { "Assets/Scenes/LoginScene.unity", "Assets/Scenes/CharacterSelectScene.unity", "Assets/Scenes/CharacterCreateScene.unity", "Assets/Scenes/MainScene.unity" };
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
        var scenes = new[] { "Assets/Scenes/LoginScene.unity", "Assets/Scenes/CharacterSelectScene.unity", "Assets/Scenes/CharacterCreateScene.unity", "Assets/Scenes/MainScene.unity" };
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
        var scenes = new[] { "Assets/Scenes/LoginScene.unity", "Assets/Scenes/CharacterSelectScene.unity", "Assets/Scenes/CharacterCreateScene.unity", "Assets/Scenes/MainScene.unity" };
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

        // ===== Wire CharacterSelectUI =====
        var csUIGo = new GameObject("CharacterSelectUI");
        var csUI = csUIGo.AddComponent<Astrion.UI.CharacterSelectUI>();
        var so = new SerializedObject(csUI);

        so.FindProperty("slotContainer").objectReferenceValue = slotContainer.transform;
        so.FindProperty("enterButton").objectReferenceValue = enterBtn;
        so.FindProperty("createButton").objectReferenceValue = createBtn;
        so.FindProperty("selectedInfoName").objectReferenceValue = infoNameText;
        so.FindProperty("selectedInfoDetail").objectReferenceValue = infoDetailText;

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

        // === Procedural Terrain (500x500) ===
        var terrainData = new TerrainData();
        terrainData.heightmapResolution = 513;
        terrainData.size = new Vector3(500, 60, 500); // 500x500 field, max height 60

        // Generate rolling hills with Perlin noise
        int res = terrainData.heightmapResolution;
        float[,] heights = new float[res, res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = (float)x / res;
                float ny = (float)y / res;

                // Large gentle hills
                float h = Mathf.PerlinNoise(nx * 3f, ny * 3f) * 0.15f;
                // Medium bumps
                h += Mathf.PerlinNoise(nx * 8f + 100, ny * 8f + 100) * 0.06f;
                // Small detail
                h += Mathf.PerlinNoise(nx * 20f + 200, ny * 20f + 200) * 0.02f;

                // Flatten the center area (player spawn)
                float cx = nx - 0.5f;
                float cy = ny - 0.5f;
                float distFromCenter = Mathf.Sqrt(cx * cx + cy * cy) * 2f;
                float flattenFactor = Mathf.Clamp01(distFromCenter * 2f);
                h *= Mathf.Lerp(0.3f, 1f, flattenFactor);

                // Base height so terrain isn't at y=0
                h += 0.05f;

                heights[y, x] = h;
            }
        }
        terrainData.SetHeights(0, 0, heights);

        // Create terrain layers (grass + dirt)
        var grassTex = new Texture2D(64, 64);
        var dirtTex = new Texture2D(64, 64);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.15f, y * 0.15f);
                grassTex.SetPixel(x, y, Color.Lerp(
                    new Color(0.22f, 0.45f, 0.12f),
                    new Color(0.30f, 0.55f, 0.18f), n));
                dirtTex.SetPixel(x, y, Color.Lerp(
                    new Color(0.35f, 0.25f, 0.15f),
                    new Color(0.45f, 0.32f, 0.20f), n));
            }
        }
        grassTex.Apply();
        dirtTex.Apply();

        var grassLayer = new TerrainLayer { diffuseTexture = grassTex, tileSize = new Vector2(10, 10) };
        var dirtLayer = new TerrainLayer { diffuseTexture = dirtTex, tileSize = new Vector2(8, 8) };
        terrainData.terrainLayers = new[] { grassLayer, dirtLayer };

        // Paint dirt on steeper slopes
        float[,,] alphamaps = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, 2];
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                float normX = (float)x / terrainData.alphamapWidth;
                float normY = (float)y / terrainData.alphamapHeight;
                float steepness = terrainData.GetSteepness(normX, normY) / 90f;
                float dirtWeight = Mathf.Clamp01(steepness * 3f);
                alphamaps[y, x, 0] = 1f - dirtWeight;
                alphamaps[y, x, 1] = dirtWeight;
            }
        }
        terrainData.SetAlphamaps(0, 0, alphamaps);

        // Instantiate terrain at origin offset so center is at (0,0,0)
        var terrainGo = Terrain.CreateTerrainGameObject(terrainData);
        terrainGo.name = "GameTerrain";
        terrainGo.transform.position = new Vector3(-250, 0, -250);
        var terrainComp = terrainGo.GetComponent<Terrain>();
        terrainComp.materialTemplate = new Material(Shader.Find("Nature/Terrain/Standard"));

        // === Scatter rocks from asset ===
        string[] rockPaths = {
            "Assets/Mountain Terrain rocks and tree/Prefab/rock_set_01.prefab",
            "Assets/Mountain Terrain rocks and tree/Prefab/rock_set_02.prefab",
            "Assets/Mountain Terrain rocks and tree/Prefab/rock_set_03.prefab",
            "Assets/Mountain Terrain rocks and tree/Prefab/rock_set_04.prefab"
        };
        var rng = new System.Random(42);
        for (int i = 0; i < 25; i++)
        {
            var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rockPaths[rng.Next(rockPaths.Length)]);
            if (rockPrefab == null) continue;
            var rock = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab);
            float rx = (float)(rng.NextDouble() * 400 - 200);
            float rz = (float)(rng.NextDouble() * 400 - 200);
            // Skip center spawn area
            if (Mathf.Abs(rx) < 30 && Mathf.Abs(rz) < 30) rx += 50;
            float ry = terrainComp.SampleHeight(new Vector3(rx, 0, rz));
            rock.name = $"Rock_{i}";
            rock.transform.position = new Vector3(rx, ry, rz);
            float scale = 0.8f + (float)rng.NextDouble() * 1.5f;
            rock.transform.localScale = Vector3.one * scale;
            rock.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0);
        }

        // === Scatter trees from asset ===
        var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Mountain Terrain rocks and tree/Prefab/tree_01.prefab");
        if (treePrefab != null)
        {
            for (int i = 0; i < 40; i++)
            {
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab);
                float tx = (float)(rng.NextDouble() * 400 - 200);
                float tz = (float)(rng.NextDouble() * 400 - 200);
                if (Mathf.Abs(tx) < 20 && Mathf.Abs(tz) < 20) tx += 40;
                float ty = terrainComp.SampleHeight(new Vector3(tx, 0, tz));
                tree.name = $"Tree_{i}";
                tree.transform.position = new Vector3(tx, ty, tz);
                float scale = 0.7f + (float)rng.NextDouble() * 0.8f;
                tree.transform.localScale = Vector3.one * scale;
                tree.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0);
            }
        }

        // === Skybox ===
        var skyboxMats = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Mountain Terrain rocks and tree/SkyBox" });
        if (skyboxMats.Length > 0)
        {
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(skyboxMats[0]));
            if (skyMat != null)
                RenderSettings.skybox = skyMat;
        }

        // === Lighting ===
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.6f, 0.75f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.5f, 0.4f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.18f, 0.15f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.65f, 0.75f, 0.85f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 80f;
        RenderSettings.fogEndDistance = 350f;

        // === Directional Light (sun) ===
        var lights = Object.FindObjectsOfType<Light>();
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.transform.rotation = Quaternion.Euler(45, -30, 0);
                light.color = new Color(1f, 0.95f, 0.85f);
                light.intensity = 1.2f;
                light.shadows = LightShadows.Soft;
            }
        }

        // === Player (spawn at terrain center) ===
        float spawnY = terrainComp.SampleHeight(Vector3.zero) + 2f;
        var playerPrefab2 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerPrefab2.name = "PlayerPrefab";
        playerPrefab2.AddComponent<Astrion.Game.SimplePlayerController>();
        var cc = playerPrefab2.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 1f, 0);
        cc.skinWidth = 0.08f;
        // Remove default Capsule collider (CharacterController handles collision)
        var defaultCollider = playerPrefab2.GetComponent<CapsuleCollider>();
        if (defaultCollider != null) Object.DestroyImmediate(defaultCollider);
        playerPrefab2.transform.position = new Vector3(0, spawnY + 1f, 0);

        // Remote player template
        var remotePrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        remotePrefab.name = "RemotePlayerPrefab";
        var remoteMat = new Material(Shader.Find("Standard"));
        remoteMat.color = Color.blue;
        remotePrefab.GetComponent<Renderer>().material = remoteMat;
        remotePrefab.transform.position = new Vector3(100, 100, 100);

        // Diablo-style top-down camera
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.gameObject.AddComponent<Astrion.Game.DiabloCamera>();
            // Match DiabloCamera defaults (pitch 55, distance 12, lookHeight 1.2) so first frame looks right
            float pitch = 55f;
            float distance = 12f;
            Quaternion rot = Quaternion.Euler(pitch, 0f, 0f);
            Vector3 offset = rot * new Vector3(0f, 0f, -distance);
            Vector3 lookPoint = new Vector3(0f, spawnY + 1.2f, 0f);
            mainCam.transform.position = lookPoint + offset;
            mainCam.transform.rotation = rot;
            mainCam.farClipPlane = 500f;
        }

        // GameManager
        var gameManagerGo = new GameObject("GameManager");
        gameManagerGo.AddComponent<Astrion.Game.GameManager>();

        // ========== GAME HUD ==========
        CreateGameHUD(playerPrefab2, spawnY);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainScene.unity");
        Debug.Log("[Astrion] MainScene created and saved.");
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

        // Diablo gothic palette (blood + iron + bone)
        Color panelDark = new Color(0.04f, 0.03f, 0.02f, 0.92f);
        Color panelMid = new Color(0.07f, 0.05f, 0.04f, 0.85f);
        Color borderLight = new Color(0.38f, 0.20f, 0.10f, 0.55f); // rust iron
        Color accentCyan = new Color(0.85f, 0.18f, 0.10f);          // blood red (was cyan)
        Color accentBlue = new Color(0.55f, 0.10f, 0.08f);          // deep crimson
        Color textWhite = new Color(0.88f, 0.83f, 0.72f);           // bone/parchment
        Color textGray = new Color(0.50f, 0.42f, 0.34f);            // weathered stone
        Color hpColor1 = new Color(0.55f, 0.05f, 0.04f);            // dark blood
        Color hpColor2 = new Color(0.92f, 0.20f, 0.10f);            // bright blood
        Color mpColor1 = new Color(0.10f, 0.08f, 0.40f);            // deep arcane
        Color mpColor2 = new Color(0.30f, 0.25f, 0.85f);            // bright arcane
        Color xpColor1 = new Color(0.45f, 0.32f, 0.10f);            // dim brass
        Color xpColor2 = new Color(0.78f, 0.58f, 0.18f);            // worn brass
        Color ironDark = new Color(0.16f, 0.14f, 0.12f);
        Color ironLight = new Color(0.42f, 0.38f, 0.34f);

        // Pre-generate textures
        var circleWhite = MakeCircleTex(128, Color.white);
        var circleSpr = TexToSprite(circleWhite);
        var panelTex = MakeRoundRectTex(256, 64, 12, panelDark);
        var panelSpr = TexToSprite(panelTex);
        var hpGradient = MakeGradientBarTex(256, 16, hpColor1, hpColor2);
        var mpGradient = MakeGradientBarTex(256, 16, mpColor1, mpColor2);
        var expGradient = MakeGradientBarTex(256, 8, xpColor1, xpColor2);
        var ringTex = MakeRingTex(128, 0.85f, 0.98f, accentCyan);
        var ringSpr = TexToSprite(ringTex);
        var thinRingSpr = TexToSprite(MakeRingTex(128, 0.90f, 0.98f, new Color(0.5f, 0.55f, 0.65f, 0.4f)));
        var joystickBgTex = MakeRingTex(256, 0.0f, 0.95f, new Color(1f, 1f, 1f, 0.08f));
        var joystickOuterTex = MakeRingTex(256, 0.85f, 0.98f, new Color(1f, 1f, 1f, 0.2f));
        var slotTex = MakeRoundRectTex(64, 64, 6, new Color(0.10f, 0.11f, 0.15f, 0.9f));
        var slotSpr = TexToSprite(slotTex);
        var slotBorderTex = MakeRoundRectTex(64, 64, 6, borderLight);
        var slotBorderSpr = TexToSprite(slotBorderTex);

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

        // ========== TOP-LEFT: Compact Player Frame (Diablo: portrait + name only; HP/MP moved to orbs) ==========
        var charPanel = HUD_CreateRT("CharPanel", root);
        charPanel.anchorMin = charPanel.anchorMax = new Vector2(0, 1);
        charPanel.pivot = new Vector2(0, 1);
        charPanel.anchoredPosition = new Vector2(16, -16);
        charPanel.sizeDelta = new Vector2(220, 64);

        // Panel background (clean dark glass)
        var charPanelBg = charPanel.gameObject.AddComponent<Image>();
        charPanelBg.sprite = panelSpr;
        charPanelBg.type = Image.Type.Sliced;
        charPanelBg.color = Color.white;

        // Portrait (clean circle)
        var portrait = HUD_CreateRT("Portrait", charPanel);
        portrait.anchorMin = portrait.anchorMax = new Vector2(0, 0.5f);
        portrait.anchoredPosition = new Vector2(38, 0);
        portrait.sizeDelta = new Vector2(50, 50);
        var portraitImg = portrait.gameObject.AddComponent<Image>();
        portraitImg.sprite = circleSpr;
        portraitImg.color = new Color(0.14f, 0.16f, 0.22f);

        // Portrait ring (thin cyan glow)
        var portraitRing = HUD_CreateRT("Ring", portrait);
        portraitRing.anchorMin = Vector2.zero; portraitRing.anchorMax = Vector2.one;
        portraitRing.offsetMin = new Vector2(-3, -3); portraitRing.offsetMax = new Vector2(3, 3);
        portraitRing.gameObject.AddComponent<Image>().sprite = ringSpr;

        // Class letter in portrait
        var classIcon = HUD_CreateRT("ClassIcon", portrait);
        classIcon.anchorMin = Vector2.zero; classIcon.anchorMax = Vector2.one;
        classIcon.offsetMin = classIcon.offsetMax = Vector2.zero;
        var ciText = classIcon.gameObject.AddComponent<Text>();
        ciText.font = font; ciText.fontSize = 22; ciText.fontStyle = FontStyle.Bold;
        ciText.color = accentCyan; ciText.alignment = TextAnchor.MiddleCenter;
        ciText.text = "W"; // Warrior

        // Level badge
        var lvlBadge = HUD_CreateRT("LvlBadge", portrait);
        lvlBadge.anchorMin = lvlBadge.anchorMax = new Vector2(1, 0);
        lvlBadge.anchoredPosition = new Vector2(4, -2);
        lvlBadge.sizeDelta = new Vector2(22, 22);
        lvlBadge.gameObject.AddComponent<Image>().sprite = circleSpr;
        lvlBadge.GetComponent<Image>().color = panelDark;
        var lvlRing = HUD_CreateRT("Ring", lvlBadge);
        lvlRing.anchorMin = Vector2.zero; lvlRing.anchorMax = Vector2.one;
        lvlRing.offsetMin = new Vector2(-1, -1); lvlRing.offsetMax = new Vector2(1, 1);
        lvlRing.gameObject.AddComponent<Image>().sprite = thinRingSpr;
        var lvlText = HUD_CreateRT("LvlText", lvlBadge);
        lvlText.anchorMin = Vector2.zero; lvlText.anchorMax = Vector2.one;
        lvlText.offsetMin = lvlText.offsetMax = Vector2.zero;
        var lvlT = lvlText.gameObject.AddComponent<Text>();
        lvlT.font = font; lvlT.fontSize = 11; lvlT.fontStyle = FontStyle.Bold;
        lvlT.color = textWhite; lvlT.alignment = TextAnchor.MiddleCenter;
        lvlT.text = "1";

        // Name text (full vertical area now since HP/MP moved to orbs)
        var nameRT = HUD_CreateRT("CharName", charPanel);
        nameRT.anchorMin = new Vector2(0, 0.5f); nameRT.anchorMax = new Vector2(1, 1);
        nameRT.offsetMin = new Vector2(72, 0); nameRT.offsetMax = new Vector2(-10, -4);
        var nameText = nameRT.gameObject.AddComponent<Text>();
        nameText.font = font; nameText.fontSize = 14; nameText.fontStyle = FontStyle.Bold;
        nameText.color = textWhite;
        nameText.text = "Character"; nameText.alignment = TextAnchor.LowerLeft;

        // Level/class subtext
        var levelRT = HUD_CreateRT("CharLevel", charPanel);
        levelRT.anchorMin = new Vector2(0, 0); levelRT.anchorMax = new Vector2(1, 0.5f);
        levelRT.offsetMin = new Vector2(72, 4); levelRT.offsetMax = new Vector2(-10, 0);
        var levelText = levelRT.gameObject.AddComponent<Text>();
        levelText.font = font; levelText.fontSize = 11;
        levelText.color = textGray;
        levelText.text = "Lv.1 Warrior"; levelText.alignment = TextAnchor.UpperLeft;

        // ========== BOTTOM-LEFT: HP Orb (Diablo signature) ==========
        var hpOrbTex = MakeOrbTex(256, hpColor2, hpColor1);
        var mpOrbTex = MakeOrbTex(256, mpColor2, mpColor1);
        var orbFrameTex = MakeOrbFrameTex(256, ironDark, ironLight);
        var orbFrameSpr = TexToSprite(orbFrameTex);

        const float ORB_SIZE = 160f;
        const float ORB_MARGIN = 24f;

        var hpOrb = HUD_CreateRT("HPOrb", root);
        hpOrb.anchorMin = hpOrb.anchorMax = new Vector2(0, 0);
        hpOrb.pivot = new Vector2(0, 0);
        hpOrb.anchoredPosition = new Vector2(ORB_MARGIN, ORB_MARGIN);
        hpOrb.sizeDelta = new Vector2(ORB_SIZE, ORB_SIZE);

        // Dark socket behind the orb (inset shadow)
        var hpSocket = HUD_CreateRT("Socket", hpOrb);
        hpSocket.anchorMin = Vector2.zero; hpSocket.anchorMax = Vector2.one;
        hpSocket.offsetMin = new Vector2(8, 8); hpSocket.offsetMax = new Vector2(-8, -8);
        var hpSocketImg = hpSocket.gameObject.AddComponent<Image>();
        hpSocketImg.sprite = circleSpr;
        hpSocketImg.color = new Color(0.02f, 0.01f, 0.01f, 0.95f);

        // Liquid fill (vertical fill from bottom — looks like blood rising)
        var hpFillRT = HUD_CreateRT("Fill", hpOrb);
        hpFillRT.anchorMin = Vector2.zero; hpFillRT.anchorMax = Vector2.one;
        hpFillRT.offsetMin = new Vector2(8, 8); hpFillRT.offsetMax = new Vector2(-8, -8);
        var hpFill = hpFillRT.gameObject.AddComponent<Image>();
        hpFill.sprite = TexToSprite(hpOrbTex);
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Vertical;
        hpFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        hpFill.fillAmount = 1f;

        // Iron frame ring (drawn on top)
        var hpFrame = HUD_CreateRT("Frame", hpOrb);
        hpFrame.anchorMin = Vector2.zero; hpFrame.anchorMax = Vector2.one;
        hpFrame.offsetMin = hpFrame.offsetMax = Vector2.zero;
        hpFrame.gameObject.AddComponent<Image>().sprite = orbFrameSpr;

        // HP text overlay (centered on orb)
        var hpTextRT = HUD_CreateRT("Text", hpOrb);
        hpTextRT.anchorMin = Vector2.zero; hpTextRT.anchorMax = Vector2.one;
        hpTextRT.offsetMin = hpTextRT.offsetMax = Vector2.zero;
        var hpBarText = hpTextRT.gameObject.AddComponent<Text>();
        hpBarText.font = font; hpBarText.fontSize = 16; hpBarText.fontStyle = FontStyle.Bold;
        hpBarText.color = new Color(1f, 0.95f, 0.85f, 0.95f);
        hpBarText.alignment = TextAnchor.MiddleCenter;
        hpBarText.text = "100/100";
        var hpShadow = hpTextRT.gameObject.AddComponent<Shadow>();
        hpShadow.effectColor = new Color(0, 0, 0, 0.95f);
        hpShadow.effectDistance = new Vector2(1, -1);

        // ========== BOTTOM-RIGHT: MP Orb ==========
        var mpOrb = HUD_CreateRT("MPOrb", root);
        mpOrb.anchorMin = mpOrb.anchorMax = new Vector2(1, 0);
        mpOrb.pivot = new Vector2(1, 0);
        mpOrb.anchoredPosition = new Vector2(-ORB_MARGIN, ORB_MARGIN);
        mpOrb.sizeDelta = new Vector2(ORB_SIZE, ORB_SIZE);

        var mpSocket = HUD_CreateRT("Socket", mpOrb);
        mpSocket.anchorMin = Vector2.zero; mpSocket.anchorMax = Vector2.one;
        mpSocket.offsetMin = new Vector2(8, 8); mpSocket.offsetMax = new Vector2(-8, -8);
        var mpSocketImg = mpSocket.gameObject.AddComponent<Image>();
        mpSocketImg.sprite = circleSpr;
        mpSocketImg.color = new Color(0.02f, 0.01f, 0.01f, 0.95f);

        var mpFillRT = HUD_CreateRT("Fill", mpOrb);
        mpFillRT.anchorMin = Vector2.zero; mpFillRT.anchorMax = Vector2.one;
        mpFillRT.offsetMin = new Vector2(8, 8); mpFillRT.offsetMax = new Vector2(-8, -8);
        var mpFill = mpFillRT.gameObject.AddComponent<Image>();
        mpFill.sprite = TexToSprite(mpOrbTex);
        mpFill.type = Image.Type.Filled;
        mpFill.fillMethod = Image.FillMethod.Vertical;
        mpFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        mpFill.fillAmount = 1f;

        var mpFrame = HUD_CreateRT("Frame", mpOrb);
        mpFrame.anchorMin = Vector2.zero; mpFrame.anchorMax = Vector2.one;
        mpFrame.offsetMin = mpFrame.offsetMax = Vector2.zero;
        mpFrame.gameObject.AddComponent<Image>().sprite = orbFrameSpr;

        var mpTextRT = HUD_CreateRT("Text", mpOrb);
        mpTextRT.anchorMin = Vector2.zero; mpTextRT.anchorMax = Vector2.one;
        mpTextRT.offsetMin = mpTextRT.offsetMax = Vector2.zero;
        var mpBarText = mpTextRT.gameObject.AddComponent<Text>();
        mpBarText.font = font; mpBarText.fontSize = 16; mpBarText.fontStyle = FontStyle.Bold;
        mpBarText.color = new Color(0.85f, 0.92f, 1f, 0.95f);
        mpBarText.alignment = TextAnchor.MiddleCenter;
        mpBarText.text = "50/50";
        var mpShadow = mpTextRT.gameObject.AddComponent<Shadow>();
        mpShadow.effectColor = new Color(0, 0, 0, 0.95f);
        mpShadow.effectDistance = new Vector2(1, -1);

        // ========== TOP: XP Bar (sleek, under top edge) ==========
        var expBar = HUD_CreateRT("ExpBar", root);
        expBar.anchorMin = new Vector2(0, 1); expBar.anchorMax = new Vector2(1, 1);
        expBar.pivot = new Vector2(0.5f, 1);
        expBar.anchoredPosition = Vector2.zero;
        expBar.sizeDelta = new Vector2(0, 3);
        expBar.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 0.7f);

        var expFill = HUD_CreateRT("ExpFill", expBar);
        expFill.anchorMin = Vector2.zero;
        expFill.anchorMax = new Vector2(0.35f, 1);
        expFill.offsetMin = expFill.offsetMax = Vector2.zero;
        var expFillImg = expFill.gameObject.AddComponent<Image>();
        expFillImg.sprite = TexToSprite(expGradient);

        // ========== TOP-RIGHT: Clean Minimap ==========
        var minimapFrame = HUD_CreateRT("MinimapFrame", root);
        minimapFrame.anchorMin = minimapFrame.anchorMax = new Vector2(1, 1);
        minimapFrame.pivot = new Vector2(1, 1);
        minimapFrame.anchoredPosition = new Vector2(-16, -16);
        minimapFrame.sizeDelta = new Vector2(170, 170);

        // Minimap background
        var mmBg = minimapFrame.gameObject.AddComponent<Image>();
        mmBg.sprite = circleSpr;
        mmBg.color = new Color(0.06f, 0.07f, 0.10f);

        // Minimap render
        var minimapInner = HUD_CreateRT("MinimapInner", minimapFrame);
        minimapInner.anchorMin = Vector2.zero; minimapInner.anchorMax = Vector2.one;
        minimapInner.offsetMin = new Vector2(8, 8); minimapInner.offsetMax = new Vector2(-8, -8);
        var minimapRawImg = minimapInner.gameObject.AddComponent<RawImage>();
        minimapInner.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;

        // Outer ring (subtle)
        var mmRing = HUD_CreateRT("Ring", minimapFrame);
        mmRing.anchorMin = Vector2.zero; mmRing.anchorMax = Vector2.one;
        mmRing.offsetMin = new Vector2(-2, -2); mmRing.offsetMax = new Vector2(2, 2);
        mmRing.gameObject.AddComponent<Image>().sprite = TexToSprite(MakeRingTex(256, 0.88f, 0.98f, new Color(0.35f, 0.40f, 0.50f, 0.5f)));

        // Inner glow ring
        var mmGlow = HUD_CreateRT("Glow", minimapFrame);
        mmGlow.anchorMin = Vector2.zero; mmGlow.anchorMax = Vector2.one;
        mmGlow.offsetMin = new Vector2(3, 3); mmGlow.offsetMax = new Vector2(-3, -3);
        mmGlow.gameObject.AddComponent<Image>().sprite = TexToSprite(MakeRingTex(256, 0.92f, 0.99f, new Color(0.3f, 0.85f, 0.95f, 0.15f)));

        // Zone name (clean text above minimap)
        var zoneNameRT = HUD_CreateRT("ZoneName", minimapFrame);
        zoneNameRT.anchorMin = new Vector2(0, 1); zoneNameRT.anchorMax = new Vector2(1, 1);
        zoneNameRT.pivot = new Vector2(0.5f, 0);
        zoneNameRT.anchoredPosition = new Vector2(0, 6);
        zoneNameRT.sizeDelta = new Vector2(180, 18);
        var zoneText = zoneNameRT.gameObject.AddComponent<Text>();
        zoneText.font = font; zoneText.fontSize = 12;
        zoneText.color = textWhite;
        zoneText.alignment = TextAnchor.MiddleCenter;
        zoneText.text = "Astrion Fields";

        // Compass
        string[] dirs = {"N","E","S","W"};
        Vector2[] dirPos = { new Vector2(0,0.95f), new Vector2(0.95f,0), new Vector2(0,-0.95f), new Vector2(-0.95f,0) };
        for (int i = 0; i < 4; i++)
        {
            var drt = HUD_CreateRT(dirs[i], minimapFrame);
            drt.anchorMin = drt.anchorMax = new Vector2(0.5f + dirPos[i].x*0.5f, 0.5f + dirPos[i].y*0.5f);
            drt.sizeDelta = new Vector2(18, 14);
            var dt = drt.gameObject.AddComponent<Text>();
            dt.font = font; dt.fontSize = 10; dt.fontStyle = FontStyle.Bold;
            dt.color = i == 0 ? accentCyan : new Color(0.5f, 0.55f, 0.65f, 0.5f);
            dt.alignment = TextAnchor.MiddleCenter; dt.text = dirs[i];
        }

        // Minimap camera
        var minimapCamGo = new GameObject("MinimapCamera");
        var minimapCam = minimapCamGo.AddComponent<Camera>();
        minimapCam.orthographic = true;
        minimapCam.orthographicSize = 60;
        minimapCam.transform.position = new Vector3(0, spawnY + 80, 0);
        minimapCam.transform.rotation = Quaternion.Euler(90, 0, 0);
        minimapCam.clearFlags = CameraClearFlags.SolidColor;
        minimapCam.backgroundColor = new Color(0.10f, 0.12f, 0.10f);
        minimapCam.cullingMask = 1;
        minimapCam.depth = -2;
        minimapCam.farClipPlane = 200f;
        var rt = new RenderTexture(256, 256, 16);
        minimapCam.targetTexture = rt;
        minimapRawImg.texture = rt;

        // Player dot
        var dotRT = HUD_CreateRT("PlayerDot", minimapFrame);
        dotRT.anchorMin = dotRT.anchorMax = new Vector2(0.5f, 0.5f);
        dotRT.sizeDelta = new Vector2(10, 10);
        dotRT.gameObject.AddComponent<Image>().sprite = circleSpr;
        dotRT.GetComponent<Image>().color = accentCyan;

        // Coordinates
        var coordsRT = HUD_CreateRT("Coords", minimapFrame);
        coordsRT.anchorMin = new Vector2(0, 0); coordsRT.anchorMax = new Vector2(1, 0);
        coordsRT.pivot = new Vector2(0.5f, 1);
        coordsRT.anchoredPosition = new Vector2(0, -6);
        coordsRT.sizeDelta = new Vector2(0, 16);
        var coordsText = coordsRT.gameObject.AddComponent<Text>();
        coordsText.font = font; coordsText.fontSize = 11;
        coordsText.color = textGray;
        coordsText.alignment = TextAnchor.MiddleCenter;
        coordsText.text = "0, 0";

        // ========== BOTTOM-LEFT: Modern Joystick ==========
        var joyBgRT = HUD_CreateRT("JoystickArea", root);
        joyBgRT.anchorMin = joyBgRT.anchorMax = new Vector2(0, 0);
        joyBgRT.pivot = new Vector2(0, 0);
        joyBgRT.anchoredPosition = new Vector2(25, 40);
        joyBgRT.sizeDelta = new Vector2(240, 240);

        // Outer ring
        var joyOuter = joyBgRT.gameObject.AddComponent<Image>();
        joyOuter.sprite = TexToSprite(joystickOuterTex);
        joyOuter.color = Color.white;

        // Inner fill
        var joyInner = HUD_CreateRT("JoyInnerBg", joyBgRT);
        joyInner.anchorMin = Vector2.zero; joyInner.anchorMax = Vector2.one;
        joyInner.offsetMin = joyInner.offsetMax = Vector2.zero;
        joyInner.gameObject.AddComponent<Image>().sprite = TexToSprite(joystickBgTex);

        // Handle
        var joyHandleRT = HUD_CreateRT("JoystickHandle", joyBgRT);
        joyHandleRT.anchorMin = joyHandleRT.anchorMax = new Vector2(0.5f, 0.5f);
        joyHandleRT.sizeDelta = new Vector2(85, 85);
        var handleImg = joyHandleRT.gameObject.AddComponent<Image>();
        handleImg.sprite = circleSpr;
        handleImg.color = new Color(0.9f, 0.88f, 0.82f, 0.3f);

        // Handle inner dot
        var joyDot = HUD_CreateRT("HandleDot", joyHandleRT);
        joyDot.anchorMin = joyDot.anchorMax = new Vector2(0.5f, 0.5f);
        joyDot.sizeDelta = new Vector2(30, 30);
        joyDot.gameObject.AddComponent<Image>().sprite = circleSpr;
        joyDot.GetComponent<Image>().color = new Color(1, 1, 1, 0.25f);

        canvasGo.AddComponent<Astrion.Game.Joystick>();
        canvasGo.AddComponent<Astrion.Game.JoystickInitializer>();

        // ========== BOTTOM-RIGHT: Action Buttons (arc layout) ==========
        var actionRT = HUD_CreateRT("ActionArea", root);
        actionRT.anchorMin = actionRT.anchorMax = new Vector2(1, 0);
        actionRT.pivot = new Vector2(1, 0);
        actionRT.anchoredPosition = new Vector2(-20, 35);
        actionRT.sizeDelta = new Vector2(300, 300);

        // Main attack button (large circle)
        var atkCircleTex = MakeCircleTex(128, new Color(0.9f, 0.2f, 0.15f, 0.85f));
        HUD_CreateModernActionBtn(actionRT, "\u2694", atkCircleTex, circleSpr, font,
            new Vector2(0.7f, 0.32f), 110, 30, new Color(1f, 0.3f, 0.2f, 0.15f));

        // Skill buttons in arc
        var s1Tex = MakeCircleTex(128, new Color(0.15f, 0.4f, 0.9f, 0.75f));
        var s2Tex = MakeCircleTex(128, new Color(0.55f, 0.15f, 0.85f, 0.75f));
        var s3Tex = MakeCircleTex(128, new Color(0.1f, 0.7f, 0.35f, 0.75f));
        var s4Tex = MakeCircleTex(128, new Color(0.85f, 0.55f, 0.1f, 0.75f));

        HUD_CreateModernActionBtn(actionRT, "\u2604", s1Tex, circleSpr, font,
            new Vector2(0.18f, 0.15f), 78, 22, new Color(0.3f, 0.5f, 1f, 0.12f));
        HUD_CreateModernActionBtn(actionRT, "\u2726", s2Tex, circleSpr, font,
            new Vector2(0.08f, 0.55f), 78, 22, new Color(0.6f, 0.3f, 0.9f, 0.12f));
        HUD_CreateModernActionBtn(actionRT, "\u2748", s3Tex, circleSpr, font,
            new Vector2(0.38f, 0.78f), 78, 22, new Color(0.2f, 0.8f, 0.4f, 0.12f));
        HUD_CreateModernActionBtn(actionRT, "\u2600", s4Tex, circleSpr, font,
            new Vector2(0.72f, 0.78f), 68, 20, new Color(0.9f, 0.7f, 0.2f, 0.12f));

        // ========== RIGHT SIDE: Quick menu icons (vertical, mobile only) ==========
        var mobileMenuGroup = HUD_CreateRT("MobileMenu", root);
        mobileMenuGroup.anchorMin = Vector2.zero; mobileMenuGroup.anchorMax = Vector2.one;
        mobileMenuGroup.offsetMin = mobileMenuGroup.offsetMax = Vector2.zero;

        string[] menuIcons = { "\u2692", "\u2605", "\u2302", "\u2709" }; // tools, star, house, mail
        string[] menuTips = { "BAG", "SKILL", "MENU", "CHAT" };
        for (int i = 0; i < menuIcons.Length; i++)
        {
            var mBtn = HUD_CreateRT(menuTips[i], mobileMenuGroup);
            mBtn.anchorMin = mBtn.anchorMax = new Vector2(1, 1);
            mBtn.pivot = new Vector2(1, 0.5f);
            mBtn.anchoredPosition = new Vector2(-18, -200 - i * 52);
            mBtn.sizeDelta = new Vector2(44, 44);

            var mBg = mBtn.gameObject.AddComponent<Image>();
            mBg.sprite = circleSpr;
            mBg.color = new Color(0.08f, 0.1f, 0.14f, 0.75f);
            mBtn.gameObject.AddComponent<Button>();

            // Ring
            var mRing = HUD_CreateRT("Ring", mBtn);
            mRing.anchorMin = Vector2.zero; mRing.anchorMax = Vector2.one;
            mRing.offsetMin = new Vector2(-2, -2);
            mRing.offsetMax = new Vector2(2, 2);
            var mRingImg = mRing.gameObject.AddComponent<Image>();
            mRingImg.sprite = ringSpr;
            mRingImg.color = new Color(1, 1, 1, 0.4f);

            var mTxt = HUD_CreateRT("Icon", mBtn);
            mTxt.anchorMin = Vector2.zero; mTxt.anchorMax = Vector2.one;
            mTxt.offsetMin = mTxt.offsetMax = Vector2.zero;
            var mt = mTxt.gameObject.AddComponent<Text>();
            mt.font = font; mt.fontSize = 20;
            mt.color = new Color(0.85f, 0.82f, 0.75f);
            mt.alignment = TextAnchor.MiddleCenter;
            mt.text = menuIcons[i];
        }

        // ========== BOTTOM-CENTER: Chat bar (minimal) ==========
        var chatBar = HUD_CreateRT("ChatBar", root);
        chatBar.anchorMin = new Vector2(0, 0);
        chatBar.anchorMax = new Vector2(0, 0);
        chatBar.pivot = new Vector2(0, 0);
        chatBar.anchoredPosition = new Vector2(25, 300);
        chatBar.sizeDelta = new Vector2(300, 30);

        var chatBg = chatBar.gameObject.AddComponent<Image>();
        chatBg.sprite = panelSpr;
        chatBg.type = Image.Type.Sliced;
        chatBg.color = new Color(0.05f, 0.06f, 0.1f, 0.5f);

        var chatTxt = HUD_CreateRT("ChatText", chatBar);
        chatTxt.anchorMin = Vector2.zero; chatTxt.anchorMax = Vector2.one;
        chatTxt.offsetMin = new Vector2(10, 0);
        chatTxt.offsetMax = new Vector2(-10, 0);
        var ct = chatTxt.gameObject.AddComponent<Text>();
        ct.font = font; ct.fontSize = 13;
        ct.color = new Color(0.6f, 0.6f, 0.55f, 0.7f);
        ct.alignment = TextAnchor.MiddleLeft;
        ct.text = "Tap to chat...";

        // ========== DESKTOP: Clean Action Bar (bottom-center) ==========
        var hotbar = HUD_CreateRT("DesktopHotbar", root);
        hotbar.anchorMin = new Vector2(0.5f, 0);
        hotbar.anchorMax = new Vector2(0.5f, 0);
        hotbar.pivot = new Vector2(0.5f, 0);
        hotbar.anchoredPosition = new Vector2(0, 10);
        hotbar.sizeDelta = new Vector2(620, 58);
        hotbar.gameObject.SetActive(false);

        // Clean dark panel background
        var hotbarBg = hotbar.gameObject.AddComponent<Image>();
        hotbarBg.sprite = panelSpr;
        hotbarBg.type = Image.Type.Sliced;
        hotbarBg.color = Color.white;

        // Top accent line (subtle cyan glow)
        var hotbarLine = HUD_CreateRT("TopLine", hotbar);
        hotbarLine.anchorMin = new Vector2(0.02f, 1); hotbarLine.anchorMax = new Vector2(0.98f, 1);
        hotbarLine.sizeDelta = new Vector2(0, 1);
        hotbarLine.anchoredPosition = Vector2.zero;
        hotbarLine.gameObject.AddComponent<Image>().color = new Color(0.3f, 0.85f, 0.95f, 0.2f);

        // 12 clean action slots
        string[] hotkeys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };
        Color[] slotAccents = {
            new Color(0.9f, 0.25f, 0.2f, 0.6f),   // red
            new Color(0.25f, 0.5f, 0.95f, 0.6f),   // blue
            new Color(0.6f, 0.25f, 0.9f, 0.6f),    // purple
            new Color(0.2f, 0.8f, 0.4f, 0.6f),     // green
            new Color(0.9f, 0.6f, 0.15f, 0.5f),    // orange
            new Color(0.85f, 0.3f, 0.55f, 0.4f),   // pink
            Color.clear, Color.clear, Color.clear,
            Color.clear, Color.clear, Color.clear,
        };
        string[] slotLabels = { "ATK", "ICE", "ARC", "HEL", "FIR", "SHD", "", "", "", "", "", "" };

        float slotSize = 46f;
        float slotGap = 3f;
        float totalWidth = 12 * slotSize + 11 * slotGap;
        float startX = -totalWidth * 0.5f + slotSize * 0.5f;

        for (int i = 0; i < 12; i++)
        {
            var slot = HUD_CreateRT($"Slot_{i}", hotbar);
            slot.anchorMin = slot.anchorMax = new Vector2(0.5f, 0.5f);
            slot.anchoredPosition = new Vector2(startX + i * (slotSize + slotGap), 0);
            slot.sizeDelta = new Vector2(slotSize, slotSize);

            // Clean slot background
            slot.gameObject.AddComponent<Image>().sprite = slotSpr;
            slot.gameObject.AddComponent<Button>();

            // Subtle border
            var slotBorder = HUD_CreateRT("Border", slot);
            slotBorder.anchorMin = Vector2.zero; slotBorder.anchorMax = Vector2.one;
            slotBorder.offsetMin = new Vector2(-1, -1); slotBorder.offsetMax = new Vector2(1, 1);
            slotBorder.gameObject.AddComponent<Image>().sprite = slotBorderSpr;

            // Color accent bar at bottom of filled slots
            if (slotAccents[i].a > 0)
            {
                var accentBar = HUD_CreateRT("Accent", slot);
                accentBar.anchorMin = new Vector2(0.15f, 0); accentBar.anchorMax = new Vector2(0.85f, 0);
                accentBar.anchoredPosition = new Vector2(0, 2);
                accentBar.sizeDelta = new Vector2(0, 2);
                accentBar.gameObject.AddComponent<Image>().color = slotAccents[i];
            }

            // Highlight overlay
            var slotHL = HUD_CreateRT("Highlight", slot);
            slotHL.anchorMin = Vector2.zero; slotHL.anchorMax = Vector2.one;
            slotHL.offsetMin = new Vector2(2, 2); slotHL.offsetMax = new Vector2(-2, -2);
            slotHL.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0);

            // Skill label (clean text instead of ugly unicode)
            var slotIcon = HUD_CreateRT("Icon", slot);
            slotIcon.anchorMin = Vector2.zero; slotIcon.anchorMax = Vector2.one;
            slotIcon.offsetMin = new Vector2(2, 8); slotIcon.offsetMax = new Vector2(-2, -2);
            var iconT = slotIcon.gameObject.AddComponent<Text>();
            iconT.font = font; iconT.fontSize = slotLabels[i].Length > 0 ? 11 : 10;
            iconT.fontStyle = FontStyle.Bold;
            iconT.color = slotLabels[i].Length > 0 ? textWhite : new Color(0.35f, 0.38f, 0.45f, 0.4f);
            iconT.alignment = TextAnchor.MiddleCenter;
            iconT.text = slotLabels[i].Length > 0 ? slotLabels[i] : "";

            // Hotkey number (top-right, small clean text)
            var keyLabel = HUD_CreateRT("Key", slot);
            keyLabel.anchorMin = new Vector2(1, 1); keyLabel.anchorMax = new Vector2(1, 1);
            keyLabel.pivot = new Vector2(1, 1);
            keyLabel.anchoredPosition = new Vector2(-3, -2);
            keyLabel.sizeDelta = new Vector2(14, 12);
            var keyT = keyLabel.gameObject.AddComponent<Text>();
            keyT.font = font; keyT.fontSize = 9;
            keyT.color = textGray;
            keyT.alignment = TextAnchor.UpperRight;
            keyT.text = hotkeys[i];

            // Cooldown overlay
            var cdOverlay = HUD_CreateRT("Cooldown", slot);
            cdOverlay.anchorMin = Vector2.zero; cdOverlay.anchorMax = Vector2.one;
            cdOverlay.offsetMin = new Vector2(2, 2); cdOverlay.offsetMax = new Vector2(-2, -2);
            var cdImg = cdOverlay.gameObject.AddComponent<Image>();
            cdImg.color = new Color(0, 0, 0, 0);
            cdImg.type = Image.Type.Filled;
            cdImg.fillMethod = Image.FillMethod.Radial360;
            cdImg.fillOrigin = 2;
            cdImg.fillClockwise = false;
        }

        // ========== DESKTOP: Clean Chat (bottom-left) ==========
        var deskChat = HUD_CreateRT("DesktopChat", root);
        deskChat.anchorMin = deskChat.anchorMax = new Vector2(0, 0);
        deskChat.pivot = new Vector2(0, 0);
        deskChat.anchoredPosition = new Vector2(10, 10);
        deskChat.sizeDelta = new Vector2(380, 200);
        deskChat.gameObject.SetActive(false);

        // Chat background
        var deskChatBg = deskChat.gameObject.AddComponent<Image>();
        deskChatBg.sprite = panelSpr;
        deskChatBg.type = Image.Type.Sliced;
        deskChatBg.color = new Color(1, 1, 1, 0.85f);

        // Chat tabs (top)
        string[] tabNames = { "All", "Party", "Guild", "Whisper" };
        Color[] tabTextColors = { accentCyan, new Color(0.4f, 0.7f, 1f), new Color(0.4f, 0.9f, 0.5f), new Color(0.9f, 0.5f, 0.9f) };
        for (int i = 0; i < tabNames.Length; i++)
        {
            var tab = HUD_CreateRT(tabNames[i], deskChat);
            tab.anchorMin = new Vector2(0, 1); tab.anchorMax = new Vector2(0, 1);
            tab.pivot = new Vector2(0, 1);
            tab.anchoredPosition = new Vector2(6 + i * 80, -4);
            tab.sizeDelta = new Vector2(74, 22);
            tab.gameObject.AddComponent<Image>().sprite = TexToSprite(MakeRoundRectTex(74, 22, 4, i == 0 ? new Color(0.15f, 0.18f, 0.25f, 0.9f) : new Color(0.08f, 0.09f, 0.12f, 0.6f)));
            tab.gameObject.AddComponent<Button>();
            var tabTxt = HUD_CreateRT("T", tab);
            tabTxt.anchorMin = Vector2.zero; tabTxt.anchorMax = Vector2.one;
            tabTxt.offsetMin = tabTxt.offsetMax = Vector2.zero;
            var tt = tabTxt.gameObject.AddComponent<Text>();
            tt.font = font; tt.fontSize = 11;
            tt.color = i == 0 ? tabTextColors[i] : textGray;
            tt.alignment = TextAnchor.MiddleCenter;
            tt.text = tabNames[i];
        }

        // Chat messages
        var chatMsgArea = HUD_CreateRT("Messages", deskChat);
        chatMsgArea.anchorMin = Vector2.zero; chatMsgArea.anchorMax = Vector2.one;
        chatMsgArea.offsetMin = new Vector2(10, 36);
        chatMsgArea.offsetMax = new Vector2(-10, -30);
        var msgText = chatMsgArea.gameObject.AddComponent<Text>();
        msgText.font = font; msgText.fontSize = 12;
        msgText.color = new Color(0.85f, 0.87f, 0.92f, 0.95f);
        msgText.alignment = TextAnchor.LowerLeft;
        msgText.verticalOverflow = VerticalWrapMode.Truncate;
        msgText.supportRichText = true;
        msgText.text = "<color=#5cd9e8>[System]</color> Welcome to Astrion!\n<color=#7abaff>[World]</color> Player1: Looking for party\n<color=#6dd94a>[Guild]</color> Event starts in 5 min\n<color=#5cd9e8>[System]</color> Press Enter to chat";

        // Chat input bar
        var chatInput = HUD_CreateRT("InputBar", deskChat);
        chatInput.anchorMin = new Vector2(0, 0); chatInput.anchorMax = new Vector2(1, 0);
        chatInput.pivot = new Vector2(0.5f, 0);
        chatInput.anchoredPosition = new Vector2(0, 4);
        chatInput.sizeDelta = new Vector2(-12, 28);
        chatInput.gameObject.AddComponent<Image>().sprite = TexToSprite(MakeRoundRectTex(256, 28, 6, new Color(0.04f, 0.05f, 0.07f, 0.95f)));

        var inputTextGo = HUD_CreateRT("InputText", chatInput);
        inputTextGo.anchorMin = Vector2.zero; inputTextGo.anchorMax = Vector2.one;
        inputTextGo.offsetMin = new Vector2(10, 2); inputTextGo.offsetMax = new Vector2(-10, -2);
        var inputText = inputTextGo.gameObject.AddComponent<Text>();
        inputText.font = font; inputText.fontSize = 12;
        inputText.color = textWhite;
        inputText.supportRichText = false;

        var phGo = HUD_CreateRT("Placeholder", chatInput);
        phGo.anchorMin = Vector2.zero; phGo.anchorMax = Vector2.one;
        phGo.offsetMin = new Vector2(10, 2); phGo.offsetMax = new Vector2(-10, -2);
        var phT = phGo.gameObject.AddComponent<Text>();
        phT.font = font; phT.fontSize = 12;
        phT.color = new Color(0.4f, 0.42f, 0.48f, 0.5f);
        phT.alignment = TextAnchor.MiddleLeft;
        phT.text = "Press Enter to chat...";

        var chatIF = chatInput.gameObject.AddComponent<InputField>();
        chatIF.textComponent = inputText;
        chatIF.placeholder = phT;

        // ========== DESKTOP: Clean Menu Bar (bottom-right) ==========
        var deskMenu = HUD_CreateRT("DesktopMenu", root);
        deskMenu.anchorMin = deskMenu.anchorMax = new Vector2(1, 0);
        deskMenu.pivot = new Vector2(1, 0);
        deskMenu.anchoredPosition = new Vector2(-10, 10);
        deskMenu.sizeDelta = new Vector2(300, 48);
        deskMenu.gameObject.SetActive(false);

        var deskMenuBg = deskMenu.gameObject.AddComponent<Image>();
        deskMenuBg.sprite = panelSpr;
        deskMenuBg.type = Image.Type.Sliced;
        deskMenuBg.color = Color.white;

        string[] dMenuLabels = { "BAG", "SKILL", "MAP", "SOCIAL", "QUEST", "MENU" };
        string[] dMenuKeys =   { "B",   "K",     "M",   "O",      "L",     "ESC" };
        for (int i = 0; i < dMenuLabels.Length; i++)
        {
            var mBtn = HUD_CreateRT(dMenuLabels[i], deskMenu);
            mBtn.anchorMin = mBtn.anchorMax = new Vector2(0, 0.5f);
            mBtn.anchoredPosition = new Vector2(14 + i * 48, 0);
            mBtn.sizeDelta = new Vector2(42, 36);
            mBtn.gameObject.AddComponent<Image>().sprite = slotSpr;
            mBtn.gameObject.AddComponent<Button>();

            // Label text
            var mLabel = HUD_CreateRT("Label", mBtn);
            mLabel.anchorMin = Vector2.zero; mLabel.anchorMax = Vector2.one;
            mLabel.offsetMin = new Vector2(0, 6); mLabel.offsetMax = Vector2.zero;
            var mlT = mLabel.gameObject.AddComponent<Text>();
            mlT.font = font; mlT.fontSize = 8; mlT.fontStyle = FontStyle.Bold;
            mlT.color = textGray;
            mlT.alignment = TextAnchor.MiddleCenter;
            mlT.text = dMenuLabels[i];

            // Key hint
            var mKey = HUD_CreateRT("Key", mBtn);
            mKey.anchorMin = new Vector2(0.5f, 0); mKey.anchorMax = new Vector2(0.5f, 0);
            mKey.anchoredPosition = new Vector2(0, 5);
            mKey.sizeDelta = new Vector2(30, 11);
            var mkT = mKey.gameObject.AddComponent<Text>();
            mkT.font = font; mkT.fontSize = 8;
            mkT.color = new Color(0.35f, 0.55f, 0.7f, 0.6f);
            mkT.alignment = TextAnchor.MiddleCenter;
            mkT.text = dMenuKeys[i];
        }

        // ========== DESKTOP: Target Frame (below player frame) ==========
        var targetPanel = HUD_CreateRT("TargetPanel", root);
        targetPanel.anchorMin = targetPanel.anchorMax = new Vector2(0, 1);
        targetPanel.pivot = new Vector2(0, 1);
        targetPanel.anchoredPosition = new Vector2(16, -96);
        targetPanel.sizeDelta = new Vector2(240, 50);
        targetPanel.gameObject.SetActive(false);

        var tpBg = targetPanel.gameObject.AddComponent<Image>();
        tpBg.sprite = panelSpr;
        tpBg.type = Image.Type.Sliced;
        tpBg.color = Color.white;

        // Target portrait
        var tPortrait = HUD_CreateRT("TPortrait", targetPanel);
        tPortrait.anchorMin = tPortrait.anchorMax = new Vector2(0, 0.5f);
        tPortrait.anchoredPosition = new Vector2(28, 0);
        tPortrait.sizeDelta = new Vector2(34, 34);
        tPortrait.gameObject.AddComponent<Image>().sprite = circleSpr;
        tPortrait.GetComponent<Image>().color = new Color(0.18f, 0.10f, 0.10f);
        var tpRing = HUD_CreateRT("Ring", tPortrait);
        tpRing.anchorMin = Vector2.zero; tpRing.anchorMax = Vector2.one;
        tpRing.offsetMin = new Vector2(-2, -2); tpRing.offsetMax = new Vector2(2, 2);
        tpRing.gameObject.AddComponent<Image>().sprite = TexToSprite(MakeRingTex(64, 0.82f, 0.98f, new Color(0.9f, 0.3f, 0.25f, 0.8f)));

        // Target name
        var tName = HUD_CreateRT("TargetName", targetPanel);
        tName.anchorMin = new Vector2(0, 0.58f); tName.anchorMax = new Vector2(1, 1);
        tName.offsetMin = new Vector2(52, 0); tName.offsetMax = new Vector2(-8, -4);
        var tnText = tName.gameObject.AddComponent<Text>();
        tnText.font = font; tnText.fontSize = 12; tnText.fontStyle = FontStyle.Bold;
        tnText.color = new Color(0.95f, 0.4f, 0.35f);
        tnText.alignment = TextAnchor.MiddleLeft;
        tnText.text = "Target";

        // Target level
        var tLevel = HUD_CreateRT("TargetLevel", targetPanel);
        tLevel.anchorMin = new Vector2(0, 0.58f); tLevel.anchorMax = new Vector2(1, 1);
        tLevel.offsetMin = new Vector2(52, 0); tLevel.offsetMax = new Vector2(-8, -4);
        var tlText = tLevel.gameObject.AddComponent<Text>();
        tlText.font = font; tlText.fontSize = 10;
        tlText.color = textGray;
        tlText.alignment = TextAnchor.MiddleRight;
        tlText.text = "Lv.?";

        // Target HP bar
        var tHPBar = HUD_CreateRT("HPBar", targetPanel);
        tHPBar.anchorMin = new Vector2(0, 0.15f); tHPBar.anchorMax = new Vector2(1, 0.50f);
        tHPBar.offsetMin = new Vector2(52, 0); tHPBar.offsetMax = new Vector2(-8, 0);
        tHPBar.gameObject.AddComponent<Image>().sprite = TexToSprite(MakeRoundRectTex(256, 16, 4, new Color(0.04f, 0.05f, 0.07f, 0.95f)));
        var tHPFill = HUD_CreateRT("Fill", tHPBar);
        tHPFill.anchorMin = Vector2.zero; tHPFill.anchorMax = Vector2.one;
        tHPFill.offsetMin = new Vector2(1, 1); tHPFill.offsetMax = new Vector2(-1, -1);
        var tHPImg = tHPFill.gameObject.AddComponent<Image>();
        tHPImg.sprite = TexToSprite(MakeGradientBarTex(256, 16, new Color(0.9f, 0.25f, 0.2f), new Color(1f, 0.4f, 0.3f)));
        tHPImg.type = Image.Type.Filled;
        tHPImg.fillMethod = Image.FillMethod.Horizontal;

        // ========== DESKTOP: Buff Bar (top-right, below minimap) ==========
        var buffBar = HUD_CreateRT("BuffBar", root);
        buffBar.anchorMin = buffBar.anchorMax = new Vector2(1, 1);
        buffBar.pivot = new Vector2(1, 1);
        buffBar.anchoredPosition = new Vector2(-16, -196);
        buffBar.sizeDelta = new Vector2(200, 30);
        buffBar.gameObject.SetActive(false);

        for (int i = 0; i < 4; i++)
        {
            var buffSlot = HUD_CreateRT($"Buff_{i}", buffBar);
            buffSlot.anchorMin = buffSlot.anchorMax = new Vector2(1, 0.5f);
            buffSlot.anchoredPosition = new Vector2(-15 - i * 34, 0);
            buffSlot.sizeDelta = new Vector2(28, 28);
            buffSlot.gameObject.AddComponent<Image>().sprite = slotSpr;

            // Accent color at bottom
            Color[] buffAccents = { hpColor1, mpColor1, xpColor1, accentCyan };
            var bAccent = HUD_CreateRT("Accent", buffSlot);
            bAccent.anchorMin = new Vector2(0.1f, 0); bAccent.anchorMax = new Vector2(0.9f, 0);
            bAccent.anchoredPosition = new Vector2(0, 1); bAccent.sizeDelta = new Vector2(0, 2);
            bAccent.gameObject.AddComponent<Image>().color = new Color(buffAccents[i].r, buffAccents[i].g, buffAccents[i].b, 0.6f);

            // Duration
            var bDur = HUD_CreateRT("Dur", buffSlot);
            bDur.anchorMin = Vector2.zero; bDur.anchorMax = Vector2.one;
            bDur.offsetMin = bDur.offsetMax = Vector2.zero;
            var bdT = bDur.gameObject.AddComponent<Text>();
            bdT.font = font; bdT.fontSize = 10; bdT.fontStyle = FontStyle.Bold;
            bdT.color = textWhite;
            bdT.alignment = TextAnchor.MiddleCenter;
            bdT.text = $"{30 - i * 8}";
        }

        // ========== DESKTOP: FPS Counter ==========
        var fpsCounter = HUD_CreateRT("FPSCounter", root);
        fpsCounter.anchorMin = fpsCounter.anchorMax = new Vector2(1, 1);
        fpsCounter.pivot = new Vector2(1, 1);
        fpsCounter.anchoredPosition = new Vector2(-200, -16);
        fpsCounter.sizeDelta = new Vector2(70, 16);
        fpsCounter.gameObject.SetActive(false);
        var fpsText = fpsCounter.gameObject.AddComponent<Text>();
        fpsText.font = font; fpsText.fontSize = 11;
        fpsText.color = new Color(0.4f, 0.85f, 0.5f, 0.6f);
        fpsText.alignment = TextAnchor.MiddleRight;
        fpsText.text = "60 FPS";

        // ========== DESKTOP: Quest Tracker (right side) ==========
        var questTracker = HUD_CreateRT("QuestTracker", root);
        questTracker.anchorMin = questTracker.anchorMax = new Vector2(1, 1);
        questTracker.pivot = new Vector2(1, 1);
        questTracker.anchoredPosition = new Vector2(-10, -235);
        questTracker.sizeDelta = new Vector2(230, 150);
        questTracker.gameObject.SetActive(false);

        var qtBg = questTracker.gameObject.AddComponent<Image>();
        qtBg.sprite = panelSpr;
        qtBg.type = Image.Type.Sliced;
        qtBg.color = new Color(1, 1, 1, 0.7f);

        // Quest header
        var qtHeader = HUD_CreateRT("QHeader", questTracker);
        qtHeader.anchorMin = new Vector2(0, 1); qtHeader.anchorMax = new Vector2(1, 1);
        qtHeader.pivot = new Vector2(0.5f, 1);
        qtHeader.anchoredPosition = Vector2.zero;
        qtHeader.sizeDelta = new Vector2(0, 22);
        var qhT = qtHeader.gameObject.AddComponent<Text>();
        qhT.font = font; qhT.fontSize = 11; qhT.fontStyle = FontStyle.Bold;
        qhT.color = accentCyan;
        qhT.alignment = TextAnchor.MiddleCenter;
        qhT.text = "OBJECTIVES";

        // Accent line under header
        var qtLine = HUD_CreateRT("Line", questTracker);
        qtLine.anchorMin = new Vector2(0.1f, 1); qtLine.anchorMax = new Vector2(0.9f, 1);
        qtLine.anchoredPosition = new Vector2(0, -23);
        qtLine.sizeDelta = new Vector2(0, 1);
        qtLine.gameObject.AddComponent<Image>().color = new Color(0.3f, 0.85f, 0.95f, 0.15f);

        // Quest entries
        var qtContent = HUD_CreateRT("Content", questTracker);
        qtContent.anchorMin = Vector2.zero; qtContent.anchorMax = Vector2.one;
        qtContent.offsetMin = new Vector2(12, 8); qtContent.offsetMax = new Vector2(-12, -28);
        var qcT = qtContent.gameObject.AddComponent<Text>();
        qcT.font = font; qcT.fontSize = 11;
        qcT.color = new Color(0.8f, 0.82f, 0.88f, 0.9f);
        qcT.alignment = TextAnchor.UpperLeft;
        qcT.supportRichText = true;
        qcT.text = "<color=#5cd9e8>The Awakening</color>\n  Explore the world  <color=#555f70>0/3</color>\n  Defeat monsters  <color=#555f70>0/5</color>\n\n<color=#5cd9e8>A New Beginning</color>\n  Talk to the Elder";

        // Wire up HUD references
        var so = new UnityEditor.SerializedObject(hudComp);
        so.FindProperty("hpFill").objectReferenceValue = hpFill;
        so.FindProperty("mpFill").objectReferenceValue = mpFill;
        so.FindProperty("hpText").objectReferenceValue = hpBarText;
        so.FindProperty("mpText").objectReferenceValue = mpBarText;
        so.FindProperty("charNameText").objectReferenceValue = nameText;
        so.FindProperty("charLevelText").objectReferenceValue = levelText;
        so.FindProperty("coordsText").objectReferenceValue = coordsText;
        so.FindProperty("minimapImage").objectReferenceValue = minimapRawImg;
        so.ApplyModifiedPropertiesWithoutUndo();

        hudComp.SetMinimapCamera(minimapCam);
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
