using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class AiPetGamePrototype : MonoBehaviour
{
    private enum ViewState
    {
        Home,
        Run,
        Result
    }

    private Canvas _canvas;
    private GameObject _homePanel;
    private GameObject _runPanel;
    private GameObject _resultPanel;
    private Text _hudText;
    private Text _stageText;
    private Text _logText;
    private Text _eventPopupText;
    private Text _resultText;
    private Text _evolutionOverlayText;

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
        EnsureEventSystem();
        BuildUI();
        ResetRunState();
        Show(ViewState.Home);
        RefreshUI();
    }

    private void Update()
    {
        if (!_isRunning)
        {
            return;
        }

        if (_isEvolutionSequence)
        {
            _evolutionTimer -= Time.deltaTime;
            if (_evolutionTimer <= 0f)
            {
                _isEvolutionSequence = false;
                _evolutionOverlayText.gameObject.SetActive(false);
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

        if (_canvas.GetComponent<GraphicRaycaster>() == null)
        {
            _canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        _homePanel = CreatePanel("HomePanel", new Color(0.95f, 0.90f, 0.95f));
        _runPanel = CreatePanel("RunPanel", new Color(0.88f, 0.95f, 0.90f));
        _resultPanel = CreatePanel("ResultPanel", new Color(0.90f, 0.92f, 0.98f));

        BuildHomeUI();
        BuildRunUI();
        BuildResultUI();
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
        CreateText(_homePanel, "Title", "AI를 키운 건 나인데", 58, new Vector2(0, 250));
        CreateText(_homePanel, "Subtitle", "Idle Strategy MVP", 30, new Vector2(0, 190));
        _stageText = CreateText(_homePanel, "StageText", "Stage: Core", 30, new Vector2(0, 130));
        CreateButton(_homePanel, "StartButton", "Start Simulation", new Vector2(0, -20), OnStartRun);
        CreateButton(_homePanel, "ResetButton", "Reset Run", new Vector2(0, -120), OnResetRun);
    }

    private void BuildRunUI()
    {
        CreateText(_runPanel, "RunTitle", "Game Tick Running (1s)", 44, new Vector2(0, 300));
        _hudText = CreateText(_runPanel, "HudText", "", 28, new Vector2(0, 220));
        _eventPopupText = CreateText(_runPanel, "EventPopup", "", 28, new Vector2(0, 130));
        _eventPopupText.color = new Color(0.70f, 0.18f, 0.18f);

        CreateButton(_runPanel, "FastForwardButton", "Fast Tick x10", new Vector2(-250, 40), OnFastForward10);
        CreateButton(_runPanel, "StopButton", "Stop", new Vector2(250, 40), OnStopRun);

        _logText = CreateText(_runPanel, "LogText", "", 22, new Vector2(0, -210));
        _logText.alignment = TextAnchor.UpperLeft;
        _logText.GetComponent<RectTransform>().sizeDelta = new Vector2(1050, 240);

        var startY = -30f;
        var gapY = 72f;
        CreateUpgradeButton("Generator", new Vector2(-350, startY - gapY * 0), OnUpgradeGenerator);
        CreateUpgradeButton("Battery", new Vector2(0, startY - gapY * 0), OnUpgradeBattery);
        CreateUpgradeButton("ModelTraining", new Vector2(350, startY - gapY * 0), OnUpgradeModelTraining);
        CreateUpgradeButton("Optimization", new Vector2(-350, startY - gapY * 1), OnUpgradeOptimization);
        CreateUpgradeButton("Stability", new Vector2(0, startY - gapY * 1), OnUpgradeStability);
        CreateUpgradeButton("Firewall", new Vector2(350, startY - gapY * 1), OnUpgradeFirewall);

        _evolutionOverlayText = CreateText(_runPanel, "EvolutionOverlay", "", 46, Vector2.zero);
        _evolutionOverlayText.color = new Color(1.0f, 0.95f, 0.30f);
        _evolutionOverlayText.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 220);
        _evolutionOverlayText.gameObject.SetActive(false);
    }

    private void BuildResultUI()
    {
        CreateText(_resultPanel, "ResultTitle", "Session Summary", 56, new Vector2(0, 260));
        _resultText = CreateText(_resultPanel, "ResultText", "", 32, new Vector2(0, 90));
        _resultText.GetComponent<RectTransform>().sizeDelta = new Vector2(1100, 360);
        CreateButton(_resultPanel, "ResumeButton", "Resume Run", new Vector2(-180, -170), OnResumeRun);
        CreateButton(_resultPanel, "HomeButton", "Home", new Vector2(180, -170), () => Show(ViewState.Home));
    }

    private Text CreateText(GameObject parent, string name, string value, int fontSize, Vector2 pos)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent.transform, false);
        var text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.15f, 0.15f, 0.15f);
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
        image.color = new Color(0.23f, 0.49f, 0.94f);
        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320, 80);
        rect.anchoredPosition = pos;

        var labelText = CreateText(buttonObject, "Label", label, 32, Vector2.zero);
        labelText.color = Color.white;
    }

    private void CreateUpgradeButton(string upgradeName, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(upgradeName + "Button");
        buttonObject.transform.SetParent(_runPanel.transform, false);
        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.45f, 0.80f);
        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320, 64);
        rect.anchoredPosition = pos;

        var labelText = CreateText(buttonObject, "Label", upgradeName, 22, Vector2.zero);
        labelText.color = Color.white;
        _upgradeLabelTexts.Add(labelText);
    }

    private void ResetRunState()
    {
        _energy = 50f;
        _energyMax = 100f;
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

        _generatorLevel = 0;
        _batteryLevel = 0;
        _modelTrainingLevel = 0;
        _optimizationLevel = 0;
        _stabilityLevel = 0;

        _tickTimer = 0f;
        _isEvolutionSequence = false;
        _evolutionTimer = 0f;
        _logs.Clear();
        AddLog("Simulation initialized.");
    }

    private void OnStartRun()
    {
        _isRunning = true;
        Show(ViewState.Run);
        AddLog("Tick loop started.");
        RefreshUI();
    }

    private void OnResumeRun()
    {
        _isRunning = true;
        Show(ViewState.Run);
        AddLog("Simulation resumed.");
        RefreshUI();
    }

    private void OnResetRun()
    {
        _isRunning = false;
        ResetRunState();
        RefreshUI();
    }

    private void OnStopRun()
    {
        _isRunning = false;
        _resultText.text =
            "Stage: " + (_isProtoHuman ? "ProtoHuman" : "Core") + "\n"
            + "Level: " + _level + "\n"
            + "Energy: " + _energy.ToString("F1") + " / " + _energyMax.ToString("F1") + "\n"
            + "Data: " + _data.ToString("F1") + "\n"
            + "Money: " + _money.ToString("F1") + "\n"
            + "IntelligenceMult: " + _intelligenceMult.ToString("F2");
        Show(ViewState.Result);
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

    private void RunSingleTick()
    {
        // 1) AI process + resource production gate
        if (_energy >= _learnEnergyCost)
        {
            _energy -= _learnEnergyCost;
            var dataGenerated = _baseDataPerTick * _intelligenceMult * (1f + _optimizationBonus);
            var moneyGenerated = _baseMoneyPerTick * _intelligenceMult;
            _data += dataGenerated;
            _money += moneyGenerated;
            _xpInLevel += dataGenerated * 0.25f;
        }

        // 2) Passive energy process
        _energy = Mathf.Min(_energyMax, _energy + _energyGenPerTick);

        // 3) Event check
        var pFinal = _baseEventChance * _eventChanceMultiplier
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
            _eventPopupText.text = "EVENT: ServerOverheat (-20 Energy)";
            AddLog("ServerOverheat: Energy -20");
        }
        else if (roll == 1)
        {
            var damageMult = Mathf.Clamp01(1f - _firewallLevel * 0.10f);
            var loss = 50f * damageMult;
            _money = Mathf.Max(0f, _money - loss);
            _eventPopupText.text = "EVENT: SecurityBreach (-" + loss.ToString("F0") + " Money)";
            AddLog("SecurityBreach: Money -" + loss.ToString("F0"));
        }
        else
        {
            _intelligenceMult += 0.05f;
            _eventPopupText.text = "EVENT: EfficiencyBoost (+0.05 INT)";
            AddLog("EfficiencyBoost: INT +0.05");
        }
    }

    private void TriggerEvolution()
    {
        _isProtoHuman = true;
        _intelligenceMult *= 1.25f;
        _eventChanceMultiplier *= 1.10f;
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
        _stageText.text = "Stage: " + (_isProtoHuman ? "ProtoHuman" : "Core");
        _hudText.text =
            "Level " + _level
            + "  XP " + _xpInLevel.ToString("F1") + " / " + XPToLevel(_level).ToString("F0")
            + "\nE " + _energy.ToString("F1") + " / " + _energyMax.ToString("F0")
            + "   D " + _data.ToString("F1")
            + "   M " + _money.ToString("F1")
            + "\nINT x" + _intelligenceMult.ToString("F2")
            + "  OPT +" + (_optimizationBonus * 100f).ToString("F0") + "%"
            + "  STB " + _stability.ToString("F1")
            + "  FW Lv." + _firewallLevel;

        _logText.text = string.Join("\n", _logs.ToArray());
        RefreshUpgradeLabels();
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
    }
}
