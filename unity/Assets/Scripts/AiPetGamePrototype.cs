using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.IO;

public class AiPetGamePrototype : MonoBehaviour
{
    private static Font s_defaultFont;

    /// <summary>Font for UI Text. Avoids GetBuiltinResource (Arial deprecated). Use Inspector override, or place a font in Resources.</summary>
    private Font GetDefaultFont()
    {
        if (_uiFontOverride != null) return _uiFontOverride;
        if (s_defaultFont != null) return s_defaultFont;
        s_defaultFont = Resources.Load<Font>("LegacyRuntime")
            ?? Resources.Load<Font>("Fonts/LegacyRuntime")
            ?? Resources.Load<Font>("Font");
        return s_defaultFont;
    }

    [Serializable]
    private class SaveData
    {
        public float energy;
        public float energyMax;
        public float data;
        public float money;
        public int level;
        public float xpInLevel;
        public float intelligenceMult;
        public float optimizationBonus;
        public float eventChanceMultiplier;
        public float stability;
        public int firewallLevel;
        public bool isProtoHuman;
        public int generatorLevel;
        public int batteryLevel;
        public int modelTrainingLevel;
        public int optimizationLevel;
        public int stabilityLevel;
        public int sessionScore;
        public int bestScore;
        public int coins;
        public int retryCount;
        public int lastReward;
        public int itemBatteryCell;
        public int itemDataCache;
        public int itemFirewallPatch;
        public long lastTimestampUtc;
    }

    private enum ViewState
    {
        Home,
        Run,
        Result,
        Shop
    }

    private Canvas _canvas;
    private GameObject _homePanel;
    private GameObject _runPanel;
    private GameObject _resultPanel;
    private GameObject _shopPanel;
    private Text _hudText;
    private Text _stageText;
    private Text _scenarioText;
    private Text _logText;
    private Text _eventPopupText;
    private Text _resultText;
    private Text _evolutionOverlayText;
    private Text _activeStatusText;
    private Text _shopStatusText;
    private Image _worldBackgroundVisual;
    private Image _coreVisual;
    private Image _protoVisual;
    private Image _scanlineOverlay;
    private Image _vignetteOverlay;
    private Image _ringOuterVisual;
    private Image _ringInnerVisual;
    private RectTransform _coreVisualRect;
    private RectTransform _protoVisualRect;
    private RectTransform _ringOuterRect;
    private RectTransform _ringInnerRect;
    private float _visualPulse;
    private float _scanlineScroll;

    [Header("Optional Art Overrides")]
    [SerializeField] private Sprite _coreSpriteOverride;
    [SerializeField] private Sprite _protoSpriteOverride;
    [SerializeField] private Sprite _backgroundSpriteOverride;
    [Tooltip("Assign a font here if UI text is blank (Unity no longer provides Arial via script). Or put a font in Resources as 'LegacyRuntime' or 'Font'.")]
    [SerializeField] private Font _uiFontOverride;

    private static readonly Color HomeBgColor = new Color(0.96f, 0.97f, 0.99f);
    private static readonly Color RunBgColor = new Color(0.95f, 0.96f, 0.98f);
    private static readonly Color ResultBgColor = new Color(0.97f, 0.98f, 1.00f);
    private static readonly Color PrimaryButtonColor = new Color(0.02f, 0.48f, 1.00f);
    private static readonly Color UpgradeButtonColor = new Color(0.88f, 0.93f, 1.00f);
    private static readonly Color UiTextColor = new Color(0.11f, 0.13f, 0.18f);

    // Tick / state
    private const float TickInterval = 1.0f;
    private float _tickTimer;
    private bool _isRunning;
    private bool _isEvolutionSequence;
    private float _evolutionTimer;

    // Core resources
    private float _energy;
    private float _energyMax;
    private float _data;
    private float _money;

    // AI growth
    private int _level;
    private float _xpInLevel;
    private float _intelligenceMult;
    private float _optimizationBonus;
    private float _eventChanceMultiplier;
    private int _sessionScore;
    private int _bestScore;
    private int _coins;
    private int _retryCount;
    private int _lastReward;
    private int _itemBatteryCell;
    private int _itemDataCache;
    private int _itemFirewallPatch;
    private float _collectCooldown;
    private float _riskBoostTimer;
    private bool _riskBoostActive;
    private const float RewardPerScore = 0.2f;
    private const float AutosaveInterval = 30f;
    private const long OfflineMaxSeconds = 8 * 3600;
    private const float OfflineMultiplier = 0.6f;
    private float _autosaveTimer;
    private string _savePath;

    // Constants (MVP tuning from request)
    private float _energyGenPerTick = 2.0f;
    private float _learnEnergyCost = 1.5f;
    private float _baseDataPerTick = 3.0f;
    private float _baseMoneyPerTick = 0.8f;
    private float _baseEventChance = 0.03f;
    private float _stability = 1.0f;
    private int _firewallLevel;
    private bool _isProtoHuman;

    // Upgrade levels and costs
    private int _generatorLevel;
    private int _batteryLevel;
    private int _modelTrainingLevel;
    private int _optimizationLevel;
    private int _stabilityLevel;

    private readonly float[] _generatorCosts = { 30, 80, 160, 300, 500 };
    private readonly float[] _batteryCosts = { 20, 60, 120, 220, 380 };
    private readonly float[] _modelTrainingCosts = { 40, 120, 250, 450, 700 };
    private readonly float[] _optimizationCosts = { 60, 160, 330, 600, 900 };
    private readonly float[] _opsCosts = { 50, 140, 300, 520, 800 };

    // UI button labels for dynamic text
    private readonly List<Text> _upgradeLabelTexts = new List<Text>();
    private readonly Queue<string> _logs = new Queue<string>();

    private void Start()
    {
        _savePath = Path.Combine(Application.persistentDataPath, "mvp_save.json");
        EnsureEventSystem();
        BuildUI();
        ResetRunState();
        LoadState();
        Show(ViewState.Home);
        RefreshUI();
    }

    private void Update()
    {
        _autosaveTimer += Time.deltaTime;
        if (_autosaveTimer >= AutosaveInterval)
        {
            _autosaveTimer = 0f;
            SaveState();
        }

        UpdateWorldVisuals();

        if (!_isRunning)
        {
            return;
        }

        if (_collectCooldown > 0f)
        {
            _collectCooldown = Mathf.Max(0f, _collectCooldown - Time.deltaTime);
        }
        if (_riskBoostActive)
        {
            _riskBoostTimer = Mathf.Max(0f, _riskBoostTimer - Time.deltaTime);
            if (_riskBoostTimer <= 0f)
            {
                _riskBoostActive = false;
                AddLog("Risk Boost ended.");
            }
        }

        if (_isEvolutionSequence)
        {
            _evolutionTimer -= Time.deltaTime;
            var t = 1f - Mathf.Clamp01(_evolutionTimer / 5.5f);
            SetImageAlpha(_coreVisual, 1f - t);
            SetImageAlpha(_protoVisual, t);
            if (_evolutionTimer <= 0f)
            {
                _isEvolutionSequence = false;
                _evolutionOverlayText.gameObject.SetActive(false);
                if (_coreVisual != null)
                {
                    _coreVisual.gameObject.SetActive(false);
                }
                SetImageAlpha(_protoVisual, 1f);
                AddLog("Evolution sequence complete.");
            }
            return;
        }

        _tickTimer += Time.deltaTime;
        while (_tickTimer >= TickInterval)
        {
            _tickTimer -= TickInterval;
            RunSingleTick();
        }
    }

    private void EnsureEventSystem()
    {
        var es = FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            var esObject = new GameObject("EventSystem");
            es = esObject.AddComponent<EventSystem>();
        }

        // Support projects using the new Input System package.
        var inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
        {
            if (es.GetComponent(inputSystemModuleType) == null)
            {
                es.gameObject.AddComponent(inputSystemModuleType);
            }
            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                Destroy(legacy);
            }
            return;
        }

        if (es.GetComponent<BaseInputModule>() == null)
        {
            es.gameObject.AddComponent<StandaloneInputModule>();
        }
    }

    private void BuildUI()
    {
        _canvas = FindFirstObjectByType<Canvas>();
        if (_canvas == null)
        {
            var canvasObject = new GameObject("Canvas");
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        var scaler = _canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = _canvas.gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 1f;

        if (_canvas.GetComponent<GraphicRaycaster>() == null)
        {
            _canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        _homePanel = CreatePanel("HomePanel", HomeBgColor);
        _runPanel = CreatePanel("RunPanel", RunBgColor);
        _resultPanel = CreatePanel("ResultPanel", ResultBgColor);
        _shopPanel = CreatePanel("ShopPanel", new Color(0.94f, 0.96f, 0.99f));

        BuildHomeUI();
        BuildRunUI();
        BuildResultUI();
        BuildShopUI();
    }

    private GameObject CreatePanel(string name, Color color)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(_canvas.transform, false);
        var image = panel.AddComponent<Image>();
        image.color = color;

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    private void BuildHomeUI()
    {
        CreateText(_homePanel, "Title", "AI를 키운 건 나인데", 64, new Vector2(0, 620));
        CreateText(_homePanel, "Subtitle", "Idle Strategy MVP", 28, new Vector2(0, 540));
        _stageText = CreateText(_homePanel, "StageText", "Stage: Core", 34, new Vector2(0, 420));
        _scenarioText = CreateText(_homePanel, "ScenarioText", "", 28, new Vector2(0, 260));
        _scenarioText.GetComponent<RectTransform>().sizeDelta = new Vector2(920, 190);
        CreateButton(_homePanel, "StartButton", "Start Simulation", new Vector2(0, -240), OnStartRun);
        CreateButton(_homePanel, "ShopButton", "Open Shop", new Vector2(0, -360), OnOpenShop);
        CreateButton(_homePanel, "ResetButton", "Reset Run", new Vector2(0, -480), OnResetRun);
    }

    private void BuildRunUI()
    {
        BuildWorldVisuals();
        CreateText(_runPanel, "RunTitle", "AI Simulation", 52, new Vector2(0, 850));
        _hudText = CreateText(_runPanel, "HudText", "", 28, new Vector2(0, 680));
        _hudText.GetComponent<RectTransform>().sizeDelta = new Vector2(920, 200);
        _eventPopupText = CreateText(_runPanel, "EventPopup", "", 30, new Vector2(0, 540));
        _eventPopupText.color = new Color(1.0f, 0.58f, 0.34f);

        CreateButton(_runPanel, "FastForwardButton", "Fast Tick x10", new Vector2(0, -690), OnFastForward10);
        CreateButton(_runPanel, "StopButton", "Stop", new Vector2(0, -810), OnStopRun);
        CreateButton(_runPanel, "CollectButton", "Active Collect", new Vector2(0, -570), OnActiveCollect);
        CreateButton(_runPanel, "RiskBoostButton", "Risk Boost x1.4 (20s)", new Vector2(0, -450), OnRiskBoost);

        _logText = CreateText(_runPanel, "LogText", "", 22, new Vector2(0, -520));
        _logText.alignment = TextAnchor.UpperLeft;
        _logText.GetComponent<RectTransform>().sizeDelta = new Vector2(920, 220);
        _activeStatusText = CreateText(_runPanel, "ActiveStatus", "", 24, new Vector2(0, -330));
        _activeStatusText.GetComponent<RectTransform>().sizeDelta = new Vector2(920, 80);

        var startY = 320f;
        var gapY = 96f;
        CreateUpgradeButton("Generator", new Vector2(-350, startY - gapY * 0), OnUpgradeGenerator);
        CreateUpgradeButton("Battery", new Vector2(0, startY - gapY * 0), OnUpgradeBattery);
        CreateUpgradeButton("ModelTraining", new Vector2(350, startY - gapY * 0), OnUpgradeModelTraining);
        CreateUpgradeButton("Optimization", new Vector2(-350, startY - gapY * 1), OnUpgradeOptimization);
        CreateUpgradeButton("Stability", new Vector2(0, startY - gapY * 1), OnUpgradeStability);
        CreateUpgradeButton("Firewall", new Vector2(350, startY - gapY * 1), OnUpgradeFirewall);

        _evolutionOverlayText = CreateText(_runPanel, "EvolutionOverlay", "", 50, new Vector2(0, 80));
        _evolutionOverlayText.color = new Color(0.03f, 0.42f, 0.92f);
        _evolutionOverlayText.GetComponent<RectTransform>().sizeDelta = new Vector2(980, 220);
        _evolutionOverlayText.gameObject.SetActive(false);
    }

    private void BuildResultUI()
    {
        CreateText(_resultPanel, "ResultTitle", "Session Summary", 58, new Vector2(0, 620));
        _resultText = CreateText(_resultPanel, "ResultText", "", 32, new Vector2(0, 280));
        _resultText.GetComponent<RectTransform>().sizeDelta = new Vector2(980, 520);
        CreateButton(_resultPanel, "ResumeButton", "Resume Run", new Vector2(0, -260), OnResumeRun);
        CreateButton(_resultPanel, "ShopButton", "Open Shop", new Vector2(0, -380), OnOpenShop);
        CreateButton(_resultPanel, "HomeButton", "Home", new Vector2(0, -500), () => Show(ViewState.Home));
    }

    private void BuildShopUI()
    {
        CreateText(_shopPanel, "ShopTitle", "Neon Market", 58, new Vector2(0, 700));
        _shopStatusText = CreateText(_shopPanel, "ShopStatus", "", 28, new Vector2(0, 570));
        _shopStatusText.GetComponent<RectTransform>().sizeDelta = new Vector2(920, 170);

        CreateShopItemCard(
            "BatteryCellCard",
            "Battery Cell",
            "+25 Energy",
            120,
            new Vector2(0, 330),
            CreateBatteryIconSprite(220),
            OnBuyBatteryCell
        );
        CreateShopItemCard(
            "DataCacheCard",
            "Data Cache",
            "+120 Data",
            150,
            new Vector2(0, 160),
            CreateChipIconSprite(220),
            OnBuyDataCache
        );
        CreateShopItemCard(
            "FirewallPatchCard",
            "Firewall Patch",
            "+1 Firewall",
            180,
            new Vector2(0, -10),
            CreateShieldIconSprite(220),
            OnBuyFirewallPatch
        );

        CreateButton(_shopPanel, "BackHomeButton", "Back Home", new Vector2(0, -500), () => Show(ViewState.Home));
    }

    private void CreateShopItemCard(
        string name,
        string title,
        string effect,
        int cost,
        Vector2 pos,
        Sprite icon,
        UnityEngine.Events.UnityAction onBuy
    )
    {
        var card = new GameObject(name);
        card.transform.SetParent(_shopPanel.transform, false);
        var cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.90f, 0.95f, 1.0f, 0.98f);

        var button = card.AddComponent<Button>();
        button.onClick.AddListener(onBuy);

        var cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(900, 150);
        cardRect.anchoredPosition = pos;

        var iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(card.transform, false);
        var iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = icon;
        iconImage.raycastTarget = false;
        var iconRect = iconImage.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(96, 96);
        iconRect.anchoredPosition = new Vector2(-360, 0);

        var titleText = CreateText(card, "Title", title, 30, new Vector2(-140, 26));
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 50);
        titleText.color = new Color(0.10f, 0.18f, 0.30f);

        var effectText = CreateText(card, "Effect", effect, 24, new Vector2(-140, -20));
        effectText.alignment = TextAnchor.MiddleLeft;
        effectText.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 44);
        effectText.color = new Color(0.18f, 0.34f, 0.52f);

        var costText = CreateText(card, "Cost", cost + " C", 28, new Vector2(300, 0));
        costText.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 54);
        costText.color = new Color(0.02f, 0.48f, 1.00f);
    }

    private void BuildWorldVisuals()
    {
        var backgroundSprite = _backgroundSpriteOverride != null
            ? _backgroundSpriteOverride
            : Resources.Load<Sprite>("Art/Backgrounds/lab_bg_day");
        var coreSprite = _coreSpriteOverride != null
            ? _coreSpriteOverride
            : Resources.Load<Sprite>("Art/Characters/core_idle");
        var protoSprite = _protoSpriteOverride != null
            ? _protoSpriteOverride
            : Resources.Load<Sprite>("Art/Characters/protohuman_idle");

        var bg = new GameObject("WorldBackgroundVisual");
        bg.transform.SetParent(_runPanel.transform, false);
        _worldBackgroundVisual = bg.AddComponent<Image>();
        _worldBackgroundVisual.sprite = backgroundSprite != null
            ? backgroundSprite
            : CreateGradientSprite(16, 256, new Color(0.06f, 0.12f, 0.20f), new Color(0.12f, 0.30f, 0.46f));
        _worldBackgroundVisual.color = Color.white;
        _worldBackgroundVisual.raycastTarget = false;
        var bgRect = _worldBackgroundVisual.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(900, 680);
        bgRect.anchoredPosition = new Vector2(0, 110);

        var core = new GameObject("CoreVisual");
        core.transform.SetParent(_runPanel.transform, false);
        _coreVisual = core.AddComponent<Image>();
        _coreVisual.sprite = coreSprite != null
            ? coreSprite
            : CreateCircleSprite(256, new Color(0.55f, 1.0f, 1.0f), new Color(0.02f, 0.25f, 0.58f));
        _coreVisual.color = Color.white;
        _coreVisual.raycastTarget = false;
        _coreVisualRect = _coreVisual.GetComponent<RectTransform>();
        _coreVisualRect.sizeDelta = new Vector2(250, 250);
        _coreVisualRect.anchoredPosition = new Vector2(0, 120);

        var ringOuter = new GameObject("HudRingOuter");
        ringOuter.transform.SetParent(_runPanel.transform, false);
        _ringOuterVisual = ringOuter.AddComponent<Image>();
        _ringOuterVisual.sprite = CreateRingSprite(512, 0.74f, 0.80f, new Color(0.10f, 0.66f, 1.00f));
        _ringOuterVisual.color = new Color(0.10f, 0.66f, 1.00f, 0.85f);
        _ringOuterVisual.raycastTarget = false;
        _ringOuterRect = _ringOuterVisual.GetComponent<RectTransform>();
        _ringOuterRect.sizeDelta = new Vector2(340, 340);
        _ringOuterRect.anchoredPosition = new Vector2(0, 120);

        var ringInner = new GameObject("HudRingInner");
        ringInner.transform.SetParent(_runPanel.transform, false);
        _ringInnerVisual = ringInner.AddComponent<Image>();
        _ringInnerVisual.sprite = CreateRingSprite(512, 0.58f, 0.63f, new Color(0.58f, 0.86f, 1.00f));
        _ringInnerVisual.color = new Color(0.58f, 0.86f, 1.00f, 0.75f);
        _ringInnerVisual.raycastTarget = false;
        _ringInnerRect = _ringInnerVisual.GetComponent<RectTransform>();
        _ringInnerRect.sizeDelta = new Vector2(290, 290);
        _ringInnerRect.anchoredPosition = new Vector2(0, 120);

        var proto = new GameObject("ProtoVisual");
        proto.transform.SetParent(_runPanel.transform, false);
        _protoVisual = proto.AddComponent<Image>();
        _protoVisual.sprite = protoSprite != null
            ? protoSprite
            : CreateDiamondSprite(256, new Color(0.92f, 0.98f, 1.0f), new Color(0.23f, 0.52f, 0.96f));
        _protoVisual.color = Color.white;
        _protoVisual.raycastTarget = false;
        _protoVisualRect = _protoVisual.GetComponent<RectTransform>();
        _protoVisualRect.sizeDelta = new Vector2(280, 320);
        _protoVisualRect.anchoredPosition = new Vector2(0, 120);
        _protoVisual.gameObject.SetActive(false);

        var scan = new GameObject("ScanlineOverlay");
        scan.transform.SetParent(_runPanel.transform, false);
        _scanlineOverlay = scan.AddComponent<Image>();
        _scanlineOverlay.sprite = CreateScanlineSprite(256, 256);
        _scanlineOverlay.color = new Color(0.68f, 0.95f, 1.0f, 0.16f);
        _scanlineOverlay.raycastTarget = false;
        var scanRect = _scanlineOverlay.GetComponent<RectTransform>();
        scanRect.sizeDelta = new Vector2(900, 680);
        scanRect.anchoredPosition = new Vector2(0, 110);

        var vignette = new GameObject("VignetteOverlay");
        vignette.transform.SetParent(_runPanel.transform, false);
        _vignetteOverlay = vignette.AddComponent<Image>();
        _vignetteOverlay.sprite = CreateVignetteSprite(512, 512);
        _vignetteOverlay.color = new Color(0.05f, 0.12f, 0.22f, 0.42f);
        _vignetteOverlay.raycastTarget = false;
        var vignetteRect = _vignetteOverlay.GetComponent<RectTransform>();
        vignetteRect.sizeDelta = new Vector2(980, 760);
        vignetteRect.anchoredPosition = new Vector2(0, 110);
    }

    private Sprite CreateGradientSprite(int width, int height, Color bottom, Color top)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (var y = 0; y < height; y++)
        {
            var t = (float)y / Mathf.Max(1, height - 1);
            var c = Color.Lerp(bottom, top, t);
            for (var x = 0; x < width; x++)
            {
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateCircleSprite(int size, Color inner, Color outer)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var center = (size - 1) * 0.5f;
        var radius = center;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                var t = Mathf.Clamp01(d / radius);
                var alpha = 1f - Mathf.SmoothStep(0.85f, 1f, t);
                var c = Color.Lerp(inner, outer, t);
                c.a = alpha;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateDiamondSprite(int size, Color inner, Color outer)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var center = (size - 1) * 0.5f;
        var radius = center;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = Mathf.Abs(x - center);
                var dy = Mathf.Abs(y - center);
                var m = (dx + dy) / radius;
                var t = Mathf.Clamp01(m);
                var alpha = 1f - Mathf.SmoothStep(0.82f, 1f, t);
                var c = Color.Lerp(inner, outer, t);
                c.a = alpha;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateScanlineSprite(int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        for (var y = 0; y < height; y++)
        {
            var strong = (y % 6 == 0) ? 0.14f : 0.02f;
            for (var x = 0; x < width; x++)
            {
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, strong));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateVignetteSprite(int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var cx = (width - 1) * 0.5f;
        var cy = (height - 1) * 0.5f;
        var maxR = Mathf.Sqrt(cx * cx + cy * cy);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                var t = Mathf.Sqrt(dx * dx + dy * dy) / maxR;
                var alpha = Mathf.SmoothStep(0.0f, 1f, t);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRingSprite(int size, float innerRatio, float outerRatio, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var center = (size - 1) * 0.5f;
        var radius = center;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var r = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                var onRing = r >= innerRatio && r <= outerRatio;
                var c = color;
                if (!onRing)
                {
                    c.a = 0f;
                }
                else
                {
                    var edge = Mathf.Min(Mathf.Abs(r - innerRatio), Mathf.Abs(outerRatio - r));
                    c.a = Mathf.Clamp01(edge * 38f);
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateBatteryIconSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var bodyMinX = (int)(size * 0.22f);
        var bodyMaxX = (int)(size * 0.78f);
        var bodyMinY = (int)(size * 0.28f);
        var bodyMaxY = (int)(size * 0.72f);
        var capMinX = (int)(size * 0.79f);
        var capMaxX = (int)(size * 0.88f);
        var capMinY = (int)(size * 0.41f);
        var capMaxY = (int)(size * 0.59f);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var insideBody = x >= bodyMinX && x <= bodyMaxX && y >= bodyMinY && y <= bodyMaxY;
                var insideCap = x >= capMinX && x <= capMaxX && y >= capMinY && y <= capMaxY;
                var c = new Color(0f, 0f, 0f, 0f);
                if (insideBody || insideCap)
                {
                    c = new Color(0.16f, 0.76f, 1.0f, 1f);
                }
                var fill = x >= bodyMinX + 8 && x <= bodyMinX + (int)((bodyMaxX - bodyMinX) * 0.6f) && y >= bodyMinY + 8 && y <= bodyMaxY - 8;
                if (fill)
                {
                    c = new Color(0.62f, 0.92f, 1.0f, 1f);
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateChipIconSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var min = (int)(size * 0.27f);
        var max = (int)(size * 0.73f);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var inside = x >= min && x <= max && y >= min && y <= max;
                var pin = ((x < min || x > max) && y % 22 < 7 && y > min + 8 && y < max - 8)
                    || ((y < min || y > max) && x % 22 < 7 && x > min + 8 && x < max - 8);
                var c = new Color(0f, 0f, 0f, 0f);
                if (inside)
                {
                    c = new Color(0.14f, 0.56f, 0.98f, 1f);
                }
                if (x > min + 16 && x < max - 16 && y > min + 16 && y < max - 16)
                {
                    c = new Color(0.70f, 0.92f, 1.0f, 1f);
                }
                if (pin)
                {
                    c = new Color(0.22f, 0.72f, 1.0f, 1f);
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateShieldIconSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var cx = (size - 1) * 0.5f;
        var top = size * 0.18f;
        var bottom = size * 0.84f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var nx = Mathf.Abs((x - cx) / (size * 0.28f));
                var ny = (y - top) / (bottom - top);
                var inside = ny >= 0f && ny <= 1f && nx <= (1f - ny * 0.68f);
                var c = new Color(0f, 0f, 0f, 0f);
                if (inside)
                {
                    c = new Color(0.18f, 0.62f, 1.0f, 1f);
                    var core = nx < (0.55f - ny * 0.3f) && ny > 0.18f && ny < 0.78f;
                    if (core)
                    {
                        c = new Color(0.72f, 0.94f, 1.0f, 1f);
                    }
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }
        var c = image.color;
        c.a = Mathf.Clamp01(alpha);
        image.color = c;
    }

    private void UpdateWorldVisuals()
    {
        _visualPulse += Time.deltaTime * (_isRunning ? 2.4f : 1.4f);
        var pulse = 1f + Mathf.Sin(_visualPulse) * 0.04f;
        _scanlineScroll += Time.deltaTime * 18f;

        if (_coreVisualRect != null && _coreVisual != null && _coreVisual.gameObject.activeSelf)
        {
            _coreVisualRect.localScale = new Vector3(pulse, pulse, 1f);
        }
        if (_protoVisualRect != null && _protoVisual != null && _protoVisual.gameObject.activeSelf)
        {
            var protoPulse = 1f + Mathf.Sin(_visualPulse * 1.2f) * 0.05f;
            _protoVisualRect.localScale = new Vector3(protoPulse, protoPulse, 1f);
        }
        if (_ringOuterRect != null)
        {
            _ringOuterRect.localRotation = Quaternion.Euler(0f, 0f, _visualPulse * -22f);
        }
        if (_ringInnerRect != null)
        {
            _ringInnerRect.localRotation = Quaternion.Euler(0f, 0f, _visualPulse * 36f);
        }
        if (_worldBackgroundVisual != null)
        {
            var osc = 0.90f + Mathf.Sin(_visualPulse * 0.6f) * 0.08f;
            _worldBackgroundVisual.color = _isProtoHuman
                ? new Color(0.88f * osc, 0.93f * osc, 1.00f * osc, 1f)
                : new Color(0.82f * osc, 0.95f * osc, 1.00f * osc, 1f);
        }

        if (_scanlineOverlay != null)
        {
            var scanRect = _scanlineOverlay.rectTransform;
            scanRect.anchoredPosition = new Vector2(0f, 110f + Mathf.Repeat(_scanlineScroll, 8f));
            var flicker = 0.07f + Mathf.PerlinNoise(Time.time * 2.4f, 0.21f) * 0.04f;
            _scanlineOverlay.color = new Color(0.68f, 0.95f, 1.0f, flicker);
        }

        if (_vignetteOverlay != null)
        {
            var alpha = _isProtoHuman ? 0.12f : 0.16f;
            _vignetteOverlay.color = new Color(0.18f, 0.28f, 0.42f, alpha);
        }
        if (_ringOuterVisual != null)
        {
            _ringOuterVisual.color = _isProtoHuman
                ? new Color(0.12f, 0.56f, 1.00f, 0.88f)
                : new Color(0.10f, 0.66f, 1.00f, 0.82f);
        }
        if (_ringInnerVisual != null)
        {
            _ringInnerVisual.color = _isProtoHuman
                ? new Color(0.72f, 0.92f, 1.00f, 0.78f)
                : new Color(0.58f, 0.86f, 1.00f, 0.72f);
        }
    }

    private Text CreateText(GameObject parent, string name, string value, int fontSize, Vector2 pos)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent.transform, false);
        var text = textObject.AddComponent<Text>();
        text.text = value;
        var font = GetDefaultFont();
        if (font != null)
            text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = UiTextColor;
        text.raycastTarget = false;

        var rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900, 120);
        rect.anchoredPosition = pos;
        return text;
    }

    private void CreateButton(GameObject parent, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent.transform, false);
        var image = buttonObject.AddComponent<Image>();
        image.color = PrimaryButtonColor;
        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(760, 96);
        rect.anchoredPosition = pos;

        var labelText = CreateText(buttonObject, "Label", label, 34, Vector2.zero);
        labelText.color = Color.white;
    }

    private void CreateUpgradeButton(string upgradeName, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(upgradeName + "Button");
        buttonObject.transform.SetParent(_runPanel.transform, false);
        var image = buttonObject.AddComponent<Image>();
        image.color = UpgradeButtonColor;
        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 82);
        rect.anchoredPosition = pos;

        var labelText = CreateText(buttonObject, "Label", upgradeName, 21, Vector2.zero);
        labelText.color = new Color(0.10f, 0.20f, 0.34f);
        _upgradeLabelTexts.Add(labelText);
    }

    private void ResetRunState()
    {
        _energy = 50f;
        _energyMax = 50f;
        _data = 0f;
        _money = 0f;
        _level = 1;
        _xpInLevel = 0f;
        _intelligenceMult = 1.0f;
        _optimizationBonus = 0f;
        _eventChanceMultiplier = 1f;
        _stability = 1.0f;
        _firewallLevel = 0;
        _isProtoHuman = false;
        if (_coreVisual != null)
        {
            _coreVisual.gameObject.SetActive(true);
            SetImageAlpha(_coreVisual, 1f);
        }
        if (_protoVisual != null)
        {
            _protoVisual.gameObject.SetActive(false);
            SetImageAlpha(_protoVisual, 0f);
        }

        _generatorLevel = 0;
        _batteryLevel = 0;
        _modelTrainingLevel = 0;
        _optimizationLevel = 0;
        _stabilityLevel = 0;
        _sessionScore = 0;
        _bestScore = 0;
        _coins = 0;
        _retryCount = 0;
        _lastReward = 0;
        _itemBatteryCell = 0;
        _itemDataCache = 0;
        _itemFirewallPatch = 0;
        _collectCooldown = 0f;
        _riskBoostTimer = 0f;
        _riskBoostActive = false;

        _tickTimer = 0f;
        _isEvolutionSequence = false;
        _evolutionTimer = 0f;
        _logs.Clear();
        AddLog("Simulation initialized.");
    }

    private void OnStartRun()
    {
        if (_isRunning)
        {
            return;
        }
        _sessionScore = 0;
        _lastReward = 0;
        _isRunning = true;
        Show(ViewState.Run);
        AddLog("Tick loop started.");
        RefreshUI();
    }

    private void OnResumeRun()
    {
        _retryCount += 1;
        _isRunning = true;
        Show(ViewState.Run);
        AddLog("Simulation resumed. Retry " + _retryCount);
        RefreshUI();
    }

    private void OnResetRun()
    {
        _isRunning = false;
        ResetRunState();
        SaveState();
        RefreshUI();
    }

    private void OnStopRun()
    {
        _isRunning = false;
        FinalizeRun();
        _resultText.text =
            "Stage: " + (_isProtoHuman ? "ProtoHuman" : "Core") + "\n"
            + "Level: " + _level + "\n"
            + "Score: " + _sessionScore + "  Best: " + _bestScore + "\n"
            + "Reward: +" + _lastReward + " coins  Total: " + _coins + "\n"
            + "Retry: " + _retryCount + "\n"
            + "Energy: " + _energy.ToString("F1") + " / " + _energyMax.ToString("F1") + "\n"
            + "Data: " + _data.ToString("F1") + "\n"
            + "Money: " + _money.ToString("F1") + "\n"
            + "IntelligenceMult: " + _intelligenceMult.ToString("F2");
        SaveState();
        Show(ViewState.Result);
    }

    private void OnOpenShop()
    {
        Show(ViewState.Shop);
        RefreshUI();
    }

    // External API for voice/UI bridge
    public void StartSimulationFromExternal()
    {
        OnStartRun();
    }

    // External API for voice/UI bridge
    public void StopSimulationFromExternal()
    {
        OnStopRun();
    }

    // External API for voice/UI bridge
    public void OpenShopFromExternal()
    {
        OnOpenShop();
    }

    // External API for voice/UI bridge
    public void GoHomeFromExternal()
    {
        Show(ViewState.Home);
        RefreshUI();
    }

    private void BuyWithCoins(int cost, Action onSuccess, string itemName)
    {
        if (_coins < cost)
        {
            AddLog(itemName + " purchase failed. Need " + cost + "C.");
            return;
        }
        _coins -= cost;
        onSuccess();
        AddLog("Purchased " + itemName + ".");
        SaveState();
        RefreshUI();
    }

    private void OnBuyBatteryCell()
    {
        BuyWithCoins(120, () =>
        {
            _itemBatteryCell += 1;
            _energy = Mathf.Min(_energyMax, _energy + 25f);
        }, "Battery Cell");
    }

    private void OnBuyDataCache()
    {
        BuyWithCoins(150, () =>
        {
            _itemDataCache += 1;
            _data += 120f;
        }, "Data Cache");
    }

    private void OnBuyFirewallPatch()
    {
        BuyWithCoins(180, () =>
        {
            _itemFirewallPatch += 1;
            _firewallLevel = Mathf.Min(5, _firewallLevel + 1);
        }, "Firewall Patch");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveState();
        }
    }

    private void OnApplicationQuit()
    {
        SaveState();
    }

    private void OnFastForward10()
    {
        if (!_isRunning || _isEvolutionSequence)
        {
            return;
        }
        for (var i = 0; i < 10; i++)
        {
            RunSingleTick();
        }
    }

    private void OnActiveCollect()
    {
        if (!_isRunning)
        {
            AddLog("Start simulation first.");
            return;
        }
        if (_collectCooldown > 0f)
        {
            AddLog("Collect cooling down...");
            return;
        }

        _coins += 12;
        _data += 18f;
        _collectCooldown = 10f;
        AddLog("Active Collect: +12C, +18D");
        RefreshUI();
    }

    private void OnRiskBoost()
    {
        if (!_isRunning)
        {
            AddLog("Start simulation first.");
            return;
        }
        if (_riskBoostActive)
        {
            AddLog("Risk Boost already active.");
            return;
        }

        _riskBoostActive = true;
        _riskBoostTimer = 20f;
        AddLog("Risk Boost active: production x1.4, event risk x1.3");
        RefreshUI();
    }

    private void RunSingleTick()
    {
        _energy = Mathf.Min(_energyMax, _energy + _energyGenPerTick);
        var prodMult = _riskBoostActive ? 1.4f : 1.0f;
        var riskMult = _riskBoostActive ? 1.3f : 1.0f;

        // 1) AI process + resource production gate
        if (_energy >= _learnEnergyCost)
        {
            _energy -= _learnEnergyCost;
            var dataGenerated = _baseDataPerTick * _intelligenceMult * (1f + _optimizationBonus) * prodMult;
            var moneyGenerated = _baseMoneyPerTick * _intelligenceMult * prodMult;
            _data += dataGenerated;
            _money += moneyGenerated;
            _xpInLevel += dataGenerated * 0.25f;
            _sessionScore += Mathf.FloorToInt(dataGenerated);
        }

        // 3) Event check
        var pFinal = _baseEventChance * _eventChanceMultiplier * riskMult
            * (1f - 0.25f * Mathf.Clamp01((_stability - 1f) / 3f));
        if (UnityEngine.Random.value < pFinal)
        {
            TriggerEvent();
        }
        else
        {
            _eventPopupText.text = "";
        }

        // 4) Level check
        while (_xpInLevel >= XPToLevel(_level))
        {
            _xpInLevel -= XPToLevel(_level);
            _level += 1;
            _intelligenceMult += 0.04f;
            AddLog("Level up -> Lv." + _level + " (INT +" + 0.04f.ToString("F2") + ")");
        }

        // 5) Evolution check
        if (!_isProtoHuman && _level >= 10 && _data >= 2500f && _money >= 1500f)
        {
            TriggerEvolution();
        }

        RefreshUI();
    }

    private void FinalizeRun()
    {
        _bestScore = Mathf.Max(_bestScore, _sessionScore);
        _lastReward = Mathf.FloorToInt(_sessionScore * RewardPerScore);
        _coins += _lastReward;
    }

    private float XPToLevel(int level)
    {
        return 50f + (level - 1) * 25f;
    }

    private void TriggerEvent()
    {
        var roll = UnityEngine.Random.Range(0, 3);
        if (roll == 0)
        {
            _energy = Mathf.Max(0f, _energy - 20f);
            _eventPopupText.color = new Color(1.0f, 0.52f, 0.34f);
            _eventPopupText.text = "[HEAT] ServerOverheat (-20 Energy)";
            AddLog("ServerOverheat: Energy -20");
        }
        else if (roll == 1)
        {
            var damageMult = Mathf.Clamp01(1f - _firewallLevel * 0.10f);
            var loss = 50f * damageMult;
            _money = Mathf.Max(0f, _money - loss);
            _eventPopupText.color = new Color(1.0f, 0.42f, 0.46f);
            _eventPopupText.text = "[BREACH] SecurityBreach (-" + loss.ToString("F0") + " Money)";
            AddLog("SecurityBreach: Money -" + loss.ToString("F0"));
        }
        else
        {
            _intelligenceMult += 0.05f;
            _eventPopupText.color = new Color(0.56f, 1.0f, 0.92f);
            _eventPopupText.text = "[BOOST] EfficiencyBoost (+0.05 INT)";
            AddLog("EfficiencyBoost: INT +0.05");
        }
    }

    private void TriggerEvolution()
    {
        _isProtoHuman = true;
        _intelligenceMult *= 1.25f;
        _eventChanceMultiplier *= 1.10f;
        if (_protoVisual != null)
        {
            _protoVisual.gameObject.SetActive(true);
            SetImageAlpha(_protoVisual, 0f);
        }
        if (_coreVisual != null)
        {
            _coreVisual.gameObject.SetActive(true);
            SetImageAlpha(_coreVisual, 1f);
        }
        _isEvolutionSequence = true;
        _evolutionTimer = 5.5f;
        _evolutionOverlayText.text = "EVOLUTION: CORE -> PROTOHUMAN";
        _evolutionOverlayText.gameObject.SetActive(true);
        AddLog("Evolution triggered. INT x1.25 / Event chance x1.10");
    }

    private void OnUpgradeGenerator()
    {
        TryBuyUpgrade(_generatorLevel, _generatorCosts, () =>
        {
            _generatorLevel += 1;
            _energyGenPerTick += 0.6f;
            AddLog("Upgrade Generator Lv." + _generatorLevel);
        });
    }

    private void OnUpgradeBattery()
    {
        TryBuyUpgrade(_batteryLevel, _batteryCosts, () =>
        {
            _batteryLevel += 1;
            _energyMax += 10f;
            _energy = Mathf.Min(_energyMax, _energy + 10f);
            AddLog("Upgrade Battery Lv." + _batteryLevel);
        });
    }

    private void OnUpgradeModelTraining()
    {
        TryBuyUpgrade(_modelTrainingLevel, _modelTrainingCosts, () =>
        {
            _modelTrainingLevel += 1;
            _baseDataPerTick += 0.8f;
            AddLog("Upgrade ModelTraining Lv." + _modelTrainingLevel);
        });
    }

    private void OnUpgradeOptimization()
    {
        TryBuyUpgrade(_optimizationLevel, _optimizationCosts, () =>
        {
            _optimizationLevel += 1;
            _optimizationBonus += 0.06f;
            AddLog("Upgrade Optimization Lv." + _optimizationLevel);
        });
    }

    private void OnUpgradeStability()
    {
        TryBuyUpgrade(_stabilityLevel, _opsCosts, () =>
        {
            _stabilityLevel += 1;
            _stability += 0.5f;
            AddLog("Upgrade Stability Lv." + _stabilityLevel);
        });
    }

    private void OnUpgradeFirewall()
    {
        TryBuyUpgrade(_firewallLevel, _opsCosts, () =>
        {
            _firewallLevel += 1;
            AddLog("Upgrade Firewall Lv." + _firewallLevel);
        });
    }

    private void TryBuyUpgrade(int currentLevel, float[] costs, Action onSuccess)
    {
        if (currentLevel >= 5)
        {
            AddLog("Upgrade max level reached.");
            return;
        }
        var cost = costs[currentLevel];
        if (_money < cost)
        {
            AddLog("Not enough money. Need " + cost.ToString("F0"));
            return;
        }
        _money -= cost;
        onSuccess();
        RefreshUI();
    }

    private void AddLog(string message)
    {
        while (_logs.Count >= 8)
        {
            _logs.Dequeue();
        }
        _logs.Enqueue("• " + message);
    }

    private void RefreshUI()
    {
        var scenario = BuildScenarioText();
        _stageText.text = "Stage: " + (_isProtoHuman ? "ProtoHuman" : "Core");
        if (_scenarioText != null)
        {
            _scenarioText.text = scenario;
        }
        _hudText.text =
            "Level " + _level
            + "  XP " + _xpInLevel.ToString("F1") + " / " + XPToLevel(_level).ToString("F0")
            + "\nE " + _energy.ToString("F1") + " / " + _energyMax.ToString("F0")
            + "   D " + _data.ToString("F1")
            + "   M " + _money.ToString("F1")
            + "\nINT x" + _intelligenceMult.ToString("F2")
            + "  OPT +" + (_optimizationBonus * 100f).ToString("F0") + "%"
            + "  STB " + _stability.ToString("F1")
            + "  FW Lv." + _firewallLevel
            + "\nScore " + _sessionScore + "  Coins " + _coins + "  Retry " + _retryCount;
        if (_shopStatusText != null)
        {
            _shopStatusText.text =
                "Coins: " + _coins + "C"
                + "\nOwned  Battery:" + _itemBatteryCell
                + "  DataCache:" + _itemDataCache
                + "  FirewallPatch:" + _itemFirewallPatch
                + "\nScenario: " + scenario;
        }
        if (_activeStatusText != null)
        {
            var collectStatus = _collectCooldown <= 0f
                ? "Collect READY"
                : "Collect CD " + Mathf.CeilToInt(_collectCooldown) + "s";
            var riskStatus = _riskBoostActive
                ? "RiskBoost " + Mathf.CeilToInt(_riskBoostTimer) + "s"
                : "RiskBoost READY";
            _activeStatusText.text = collectStatus + "   |   " + riskStatus;
        }

        _logText.text = string.Join("\n", _logs.ToArray());
        RefreshUpgradeLabels();
    }

    private string BuildScenarioText()
    {
        if (_isProtoHuman)
        {
            return "Chapter 3 - Emergence\nAI has crossed the Core barrier. Stabilize risk and prepare expansion.";
        }
        if (_level >= 6)
        {
            return "Chapter 2 - Acceleration\nPush training throughput while avoiding breach cascades.";
        }
        return "Chapter 1 - Boot Sequence\nCollect data, earn funds, and build a safe learning loop.";
    }

    private void RefreshUpgradeLabels()
    {
        if (_upgradeLabelTexts.Count < 6)
        {
            return;
        }

        _upgradeLabelTexts[0].text = "Generator Lv." + _generatorLevel + CostText(_generatorLevel, _generatorCosts);
        _upgradeLabelTexts[1].text = "Battery Lv." + _batteryLevel + CostText(_batteryLevel, _batteryCosts);
        _upgradeLabelTexts[2].text = "ModelTraining Lv." + _modelTrainingLevel + CostText(_modelTrainingLevel, _modelTrainingCosts);
        _upgradeLabelTexts[3].text = "Optimization Lv." + _optimizationLevel + CostText(_optimizationLevel, _optimizationCosts);
        _upgradeLabelTexts[4].text = "Stability Lv." + _stabilityLevel + CostText(_stabilityLevel, _opsCosts);
        _upgradeLabelTexts[5].text = "Firewall Lv." + _firewallLevel + CostText(_firewallLevel, _opsCosts);
    }

    private string CostText(int level, float[] costs)
    {
        if (level >= 5)
        {
            return " (MAX)";
        }
        return " ($" + costs[level].ToString("F0") + ")";
    }

    private void Show(ViewState state)
    {
        _homePanel.SetActive(state == ViewState.Home);
        _runPanel.SetActive(state == ViewState.Run);
        _resultPanel.SetActive(state == ViewState.Result);
        _shopPanel.SetActive(state == ViewState.Shop);
    }

    private long NowUnixUtc()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private SaveData BuildSaveData()
    {
        return new SaveData
        {
            energy = _energy,
            energyMax = _energyMax,
            data = _data,
            money = _money,
            level = _level,
            xpInLevel = _xpInLevel,
            intelligenceMult = _intelligenceMult,
            optimizationBonus = _optimizationBonus,
            eventChanceMultiplier = _eventChanceMultiplier,
            stability = _stability,
            firewallLevel = _firewallLevel,
            isProtoHuman = _isProtoHuman,
            generatorLevel = _generatorLevel,
            batteryLevel = _batteryLevel,
            modelTrainingLevel = _modelTrainingLevel,
            optimizationLevel = _optimizationLevel,
            stabilityLevel = _stabilityLevel,
            sessionScore = _sessionScore,
            bestScore = _bestScore,
            coins = _coins,
            retryCount = _retryCount,
            lastReward = _lastReward,
            itemBatteryCell = _itemBatteryCell,
            itemDataCache = _itemDataCache,
            itemFirewallPatch = _itemFirewallPatch,
            lastTimestampUtc = NowUnixUtc()
        };
    }

    private void ApplySaveData(SaveData data)
    {
        _energy = data.energy;
        _energyMax = data.energyMax;
        _data = data.data;
        _money = data.money;
        _level = data.level;
        _xpInLevel = data.xpInLevel;
        _intelligenceMult = data.intelligenceMult;
        _optimizationBonus = data.optimizationBonus;
        _eventChanceMultiplier = data.eventChanceMultiplier;
        _stability = data.stability;
        _firewallLevel = data.firewallLevel;
        _isProtoHuman = data.isProtoHuman;
        _generatorLevel = data.generatorLevel;
        _batteryLevel = data.batteryLevel;
        _modelTrainingLevel = data.modelTrainingLevel;
        _optimizationLevel = data.optimizationLevel;
        _stabilityLevel = data.stabilityLevel;
        _sessionScore = data.sessionScore;
        _bestScore = data.bestScore;
        _coins = data.coins;
        _retryCount = data.retryCount;
        _lastReward = data.lastReward;
        _itemBatteryCell = data.itemBatteryCell;
        _itemDataCache = data.itemDataCache;
        _itemFirewallPatch = data.itemFirewallPatch;

        // Rebuild derived tuning values from upgrade levels.
        _energyMax = 50.0f + (_batteryLevel * 10.0f);
        _energy = Mathf.Min(_energy, _energyMax);
        _energyGenPerTick = 2.0f + (_generatorLevel * 0.6f);
        _baseDataPerTick = 3.0f + (_modelTrainingLevel * 0.8f);
        _stability = 1.0f + (_stabilityLevel * 0.5f);
        _optimizationBonus = _optimizationLevel * 0.06f;
        if (_isProtoHuman)
        {
            if (_coreVisual != null)
            {
                _coreVisual.gameObject.SetActive(false);
            }
            if (_protoVisual != null)
            {
                _protoVisual.gameObject.SetActive(true);
                SetImageAlpha(_protoVisual, 1f);
            }
        }
        else
        {
            if (_coreVisual != null)
            {
                _coreVisual.gameObject.SetActive(true);
                SetImageAlpha(_coreVisual, 1f);
            }
            if (_protoVisual != null)
            {
                _protoVisual.gameObject.SetActive(false);
                SetImageAlpha(_protoVisual, 0f);
            }
        }
    }

    private void ApplyOfflineEarnings(long offlineSeconds)
    {
        if (offlineSeconds <= 0)
        {
            return;
        }

        var dataPerSec = _baseDataPerTick * _intelligenceMult * (1f + _optimizationBonus);
        var moneyPerSec = _baseMoneyPerTick * _intelligenceMult;

        var gainData = dataPerSec * offlineSeconds * OfflineMultiplier;
        var gainMoney = moneyPerSec * offlineSeconds * OfflineMultiplier;
        _data += gainData;
        _money += gainMoney;
        _energy = _energyMax;
        AddLog(
            "Offline +" + gainData.ToString("F1") + " Data, +"
            + gainMoney.ToString("F1") + " Money (" + offlineSeconds + "s)"
        );
    }

    private void LoadState()
    {
        if (string.IsNullOrEmpty(_savePath))
        {
            _savePath = Path.Combine(Application.persistentDataPath, "mvp_save.json");
        }
        if (!File.Exists(_savePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_savePath);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                return;
            }

            var now = NowUnixUtc();
            var offlineSeconds = Math.Max(0L, now - data.lastTimestampUtc);
            offlineSeconds = Math.Min(offlineSeconds, OfflineMaxSeconds);

            ApplySaveData(data);
            ApplyOfflineEarnings(offlineSeconds);
            SaveState();
            AddLog("Save loaded.");
        }
        catch (Exception ex)
        {
            AddLog("Load failed: " + ex.Message);
        }
    }

    private void SaveState()
    {
        if (string.IsNullOrEmpty(_savePath))
        {
            _savePath = Path.Combine(Application.persistentDataPath, "mvp_save.json");
        }
        try
        {
            var json = JsonUtility.ToJson(BuildSaveData(), true);
            File.WriteAllText(_savePath, json);
        }
        catch (Exception ex)
        {
            AddLog("Save failed: " + ex.Message);
        }
    }
}
