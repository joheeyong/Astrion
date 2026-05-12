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

        // Remove default 3D directional light (2D doesn't need it)
        var lights = Object.FindObjectsOfType<Light>();
        foreach (var l in lights) if (l.type == LightType.Directional) Object.DestroyImmediate(l.gameObject);

        const int GROUND_LAYER = 8;

        // === 2D Sprites (procedurally generated placeholders) ===
        var skySpr = TexToSprite(Make2DSkyTex(512, 256));
        var farMountainSpr = TexToSprite(Make2DMountainTex(1024, 256, new Color(0.45f, 0.42f, 0.55f), 0.6f));
        var midMountainSpr = TexToSprite(Make2DMountainTex(1024, 256, new Color(0.32f, 0.45f, 0.40f), 0.7f));
        var nearHillSpr = TexToSprite(Make2DMountainTex(1024, 256, new Color(0.20f, 0.42f, 0.28f), 0.85f));
        var groundSpr = TexToSprite(Make2DGroundTex(256, 64));
        var platformSpr = TexToSprite(Make2DPlatformTex(256, 32));
        var ladderSpr = TexToSprite(Make2DLadderTex(64, 256));
        var playerSpr = TexToSprite(Make2DPlayerTex(64, 96));
        var remotePlayerSpr = TexToSprite(Make2DPlayerTex(64, 96, true));

        // === Background root ===
        var bgRoot = new GameObject("Background");

        // Sky (huge tiled background, no parallax)
        var sky = SpawnSprite("Sky", bgRoot.transform, skySpr, new Vector3(0, 0, 50), new Vector3(40, 25, 1), -10);

        // Parallax layers (3 mountain layers)
        var farLayer = SpawnSprite("FarMountains", bgRoot.transform, farMountainSpr, new Vector3(0, -1.5f, 40), new Vector3(20, 5, 1), -8);
        AddParallax(farLayer, new Vector2(0.1f, 0.05f));

        var midLayer = SpawnSprite("MidMountains", bgRoot.transform, midMountainSpr, new Vector3(0, -2.2f, 30), new Vector3(20, 4, 1), -6);
        AddParallax(midLayer, new Vector2(0.3f, 0.1f));

        var nearLayer = SpawnSprite("NearHills", bgRoot.transform, nearHillSpr, new Vector3(0, -2.8f, 20), new Vector3(20, 3, 1), -4);
        AddParallax(nearLayer, new Vector2(0.55f, 0.15f));

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

        // === Player prefab (placeholder chibi sprite) ===
        var playerPrefab2 = new GameObject("PlayerPrefab");
        playerPrefab2.transform.position = new Vector3(0, 0f, 0);
        var pSprite = new GameObject("Sprite");
        pSprite.transform.SetParent(playerPrefab2.transform, false);
        var psr = pSprite.AddComponent<SpriteRenderer>();
        psr.sprite = playerSpr;
        psr.sortingOrder = 10;
        var pBox = playerPrefab2.AddComponent<BoxCollider2D>();
        pBox.size = new Vector2(0.55f, 0.92f);
        pBox.offset = new Vector2(0, 0);
        var pRb = playerPrefab2.AddComponent<Rigidbody2D>();
        pRb.gravityScale = 3f;
        pRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        pRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        pRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var pCtrl = playerPrefab2.AddComponent<Astrion.Game.PlayerController2D>();
        // groundCheck transform at feet
        var groundCheckGo = new GameObject("GroundCheck");
        groundCheckGo.transform.SetParent(playerPrefab2.transform, false);
        groundCheckGo.transform.localPosition = new Vector3(0, -0.48f, 0);
        // Wire groundCheck and layer via serialized fields
        var pSo = new SerializedObject(pCtrl);
        pSo.FindProperty("groundCheck").objectReferenceValue = groundCheckGo.transform;
        pSo.FindProperty("groundMask").intValue = 1 << GROUND_LAYER;
        pSo.ApplyModifiedPropertiesWithoutUndo();

        // === Remote player prefab ===
        var remotePrefab = new GameObject("RemotePlayerPrefab");
        remotePrefab.transform.position = new Vector3(100, 100, 0); // off-screen
        var rSprite = new GameObject("Sprite");
        rSprite.transform.SetParent(remotePrefab.transform, false);
        var rsr = rSprite.AddComponent<SpriteRenderer>();
        rsr.sprite = remotePlayerSpr;
        rsr.sortingOrder = 10;

        // === Orthographic Camera ===
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.orthographic = true;
            mainCam.orthographicSize = 6.5f;
            mainCam.transform.position = new Vector3(0, 0, -10);
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.55f, 0.78f, 0.92f); // sky blue fallback
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
        gmSo.ApplyModifiedPropertiesWithoutUndo();

        // ========== GAME HUD ==========
        CreateGameHUD(playerPrefab2, 0f);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainScene.unity");
        Debug.Log("[Astrion] MainScene created and saved (2D).");
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
        Color top = new Color(0.40f, 0.65f, 0.90f);
        Color bot = new Color(0.78f, 0.88f, 0.95f);
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
        int grassTop = h - 10;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                Color c;
                if (y >= grassTop)
                    c = Color.Lerp(new Color(0.22f, 0.55f, 0.18f), new Color(0.35f, 0.70f, 0.25f), n);
                else
                    c = Color.Lerp(new Color(0.35f, 0.25f, 0.15f), new Color(0.50f, 0.35f, 0.20f), n);
                tex.SetPixel(x, y, c);
            }
        tex.Apply(); return tex;
    }

    private static Texture2D Make2DPlatformTex(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        // wood plank top + dark underside
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.05f, y * 0.3f);
                Color baseC;
                if (y >= h - 6) baseC = Color.Lerp(new Color(0.30f, 0.65f, 0.22f), new Color(0.42f, 0.75f, 0.28f), n);
                else if (y >= h - 14) baseC = Color.Lerp(new Color(0.52f, 0.35f, 0.20f), new Color(0.68f, 0.48f, 0.25f), n);
                else baseC = Color.Lerp(new Color(0.32f, 0.22f, 0.12f), new Color(0.45f, 0.30f, 0.18f), n);
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

    private static Texture2D Make2DPlayerTex(int w, int h, bool remote = false)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color shirt = remote ? new Color(0.30f, 0.45f, 0.85f) : new Color(0.95f, 0.45f, 0.20f);
        Color pants = new Color(0.20f, 0.22f, 0.30f);
        Color skin = new Color(1f, 0.85f, 0.72f);
        Color hair = new Color(0.20f, 0.15f, 0.10f);
        Color outline = new Color(0.05f, 0.04f, 0.06f);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // Center column bounds (chibi body)
                int cx = w / 2;
                int dx = x - cx;
                // Head (top 1/3): rounded
                int headTop = h - 4;
                int headBot = h * 2 / 3;
                int torsoTop = headBot;
                int torsoBot = h / 3;
                int legBot = 2;

                Color c = new Color(0, 0, 0, 0);
                if (y >= headBot && y <= headTop)
                {
                    int hr = (int)((1 - Mathf.Abs((y - (headTop + headBot) * 0.5f)) / ((headTop - headBot) * 0.5f)) * 14);
                    if (Mathf.Abs(dx) <= hr) c = skin;
                    if (Mathf.Abs(dx) <= hr && y > headBot + (headTop - headBot) * 0.7f) c = hair;
                }
                else if (y >= torsoBot && y < torsoTop)
                {
                    if (Mathf.Abs(dx) <= 14) c = shirt;
                }
                else if (y >= legBot && y < torsoBot)
                {
                    bool leftLeg = dx >= -12 && dx <= -2;
                    bool rightLeg = dx >= 2 && dx <= 12;
                    if (leftLeg || rightLeg) c = pants;
                }

                if (c.a > 0)
                {
                    // Outline
                    bool isEdge = false;
                    if (x > 0 && tex.GetPixel(x - 1, y).a == 0) isEdge = true;
                    if (y > 0 && tex.GetPixel(x, y - 1).a == 0) isEdge = true;
                    if (isEdge) c = outline;
                }
                tex.SetPixel(x, y, c);
            }
        tex.Apply(); return tex;
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

        // MapleStory-style cheerful palette
        Color panelBg = new Color(0.05f, 0.08f, 0.15f, 0.92f);
        Color textWhite = new Color(1f, 0.98f, 0.92f);
        Color textMuted = new Color(0.70f, 0.75f, 0.82f);
        Color slotBg = new Color(0.08f, 0.10f, 0.16f, 0.95f);

        Color hpRed1 = new Color(0.85f, 0.18f, 0.22f);
        Color hpRed2 = new Color(1.0f, 0.40f, 0.45f);
        Color mpBlue1 = new Color(0.18f, 0.45f, 0.90f);
        Color mpBlue2 = new Color(0.45f, 0.72f, 1.0f);
        Color expGold1 = new Color(0.85f, 0.68f, 0.20f);
        Color expGold2 = new Color(1.0f, 0.85f, 0.40f);

        var hpGrad = TexToSprite(MakeGradientBarTex(256, 16, hpRed1, hpRed2));
        var mpGrad = TexToSprite(MakeGradientBarTex(256, 16, mpBlue1, mpBlue2));
        var expGrad = TexToSprite(MakeGradientBarTex(256, 12, expGold1, expGold2));
        var panelSpr = TexToSprite(MakeRoundRectTex(256, 64, 10, panelBg));
        var slotSpr = TexToSprite(MakeRoundRectTex(64, 64, 6, slotBg));
        var barBgSpr = TexToSprite(MakeRoundRectTex(256, 18, 7, new Color(0.02f, 0.03f, 0.05f, 0.95f)));

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

        // Hotbar slots
        float slotSize = 58f;
        float slotGap = 6f;
        float totalW = slotSize * 10 + slotGap * 9;
        float startX = (actionRoot.sizeDelta.x - totalW) * 0.5f;
        for (int i = 0; i < 10; i++)
        {
            var slot = HUD_CreateRT($"Slot_{i}", actionRoot);
            slot.anchorMin = slot.anchorMax = new Vector2(0, 0);
            slot.pivot = new Vector2(0, 0);
            slot.anchoredPosition = new Vector2(startX + i * (slotSize + slotGap), 14);
            slot.sizeDelta = new Vector2(slotSize, slotSize);
            var slotImg = slot.gameObject.AddComponent<Image>();
            slotImg.sprite = slotSpr; slotImg.type = Image.Type.Sliced;

            var num = HUD_CreateRT("Num", slot);
            num.anchorMin = num.anchorMax = new Vector2(0, 1);
            num.pivot = new Vector2(0, 1);
            num.anchoredPosition = new Vector2(5, -3);
            num.sizeDelta = new Vector2(20, 18);
            var numT = num.gameObject.AddComponent<Text>();
            numT.font = font; numT.fontSize = 12; numT.fontStyle = FontStyle.Bold;
            numT.color = new Color(1f, 0.88f, 0.45f);
            numT.alignment = TextAnchor.UpperLeft;
            numT.text = ((i + 1) % 10).ToString();
        }

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
        so.ApplyModifiedPropertiesWithoutUndo();
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
