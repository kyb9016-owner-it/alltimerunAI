using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace AllTimeRunAI.Jarvis
{
    public class JarvisUIController : MonoBehaviour
    {
        public enum VoiceState
        {
            Idle,
            Listening,
            Processing,
            Error
        }

        [Header("First Screen")]
        [SerializeField] private GameObject namingPanel;
        [SerializeField] private InputField nameInputField;
        [SerializeField] private Button confirmNameButton;
        [SerializeField] private Button nameVoiceButton;
        [SerializeField] private Text nameHintText;

        [Header("Main HUD")]
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private Text aiNameText;
        [SerializeField] private Text recognizedText;
        [SerializeField] private Text responseText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text errorText;
        [SerializeField] private Button pushToTalkButton;

        [Header("Figma HUD Bridge")]
        [SerializeField] private Text coinValueText;
        [SerializeField] private Text incomeValueText;
        [SerializeField] private Text levelValueText;
        [SerializeField] private Text expValueText;
        [SerializeField] private Button tabHomeButton;
        [SerializeField] private Button tabShopButton;
        [SerializeField] private Button tabMissionButton;
        [SerializeField] private GameObject figmaHomeContent;
        [SerializeField] private GameObject figmaShopContent;
        [SerializeField] private GameObject figmaMissionContent;
        [SerializeField] private Button tapAreaButton;
        [SerializeField] private Text comboText;
        [SerializeField] private Text totalTapText;

        [Header("Visual Reactors")]
        [SerializeField] private CoreGlow coreGlow;
        [SerializeField] private RingRotator[] ringRotators;
        [SerializeField] private ParticleAmbient particleAmbient;
        [SerializeField] private WaveformBars waveformBars;

        [Header("Voice Bridge (Optional)")]
        [Tooltip("Optional component with StartListening()/StopListening() methods.")]
        [SerializeField] private MonoBehaviour voiceBridge;

        [Header("Game Bridge (Optional)")]
        [Tooltip("Optional target (e.g., AiPetGamePrototype) that receives command actions.")]
        [SerializeField] private MonoBehaviour gameBridge;

        [Header("Defaults")]
        [SerializeField] private string defaultAiName = "JARVIS";
        [SerializeField] private string playerPrefsAiNameKey = "jarvis_ai_name";
        [SerializeField] private bool requireNamingAsLogin = true;

        private VoiceState currentState = VoiceState.Idle;
        private enum HudTab
        {
            Home,
            Shop,
            Mission
        }

        private HudTab activeTab = HudTab.Home;
        private string aiName = "JARVIS";
        private Coroutine mockRoutine;
        private bool loginCompleted;
        private int figmaCoins = 40;
        private int figmaLevel = 1;
        private int figmaExp;
        private int totalTaps;
        private int tapStreak;
        private float lastTapTime = -99f;
        private float autoIncomeTimer;
        private int purchasedUpgradeCount;

        private sealed class UpgradeState
        {
            public string id;
            public string title;
            public int level;
            public int baseCost;
            public int effect;
            public Button buyButton;
            public Text titleText;
            public Text levelText;
            public Text costText;
        }

        private sealed class MissionState
        {
            public string id;
            public string title;
            public int progress;
            public int target;
            public int reward;
            public bool completed;
            public Button claimButton;
            public Text titleText;
            public Text progressText;
        }

        private readonly List<UpgradeState> upgrades = new List<UpgradeState>();
        private readonly List<MissionState> missions = new List<MissionState>();

        private int TapPower => Mathf.Max(1, FindUpgradeEffect("tap"));
        private int AutoIncomePerSecond => Mathf.Max(0, FindUpgradeEffect("auto"));
        private int ExpMultiplier => Mathf.Max(1, FindUpgradeEffect("multiplier"));

        private void Awake()
        {
            // Always require naming as first-login screen for this project flow.
            requireNamingAsLogin = true;

            if (namingPanel == null || hudRoot == null || pushToTalkButton == null)
            {
                JarvisRuntimeBootstrap.BuildIfMissing(this);
            }
            JarvisRuntimeBootstrap.EnsureFigmaHud(hudRoot != null ? hudRoot.transform : null);
            EnsureNameVoiceButtonExists();
            InitializeFigmaHudBindings();
            InitializeFigmaGameData();

            aiName = PlayerPrefs.GetString(playerPrefsAiNameKey, string.Empty);
            if (string.IsNullOrWhiteSpace(aiName))
            {
                aiName = defaultAiName;
            }

            if (confirmNameButton != null)
            {
                confirmNameButton.onClick.AddListener(ConfirmAiName);
            }
            if (nameVoiceButton != null)
            {
                var namePtt = nameVoiceButton.GetComponent<PushToTalkProxy>();
                if (namePtt == null)
                {
                    namePtt = nameVoiceButton.gameObject.AddComponent<PushToTalkProxy>();
                }
                namePtt.Initialize(this, true);
            }

            if (pushToTalkButton != null)
            {
                var ptt = pushToTalkButton.GetComponent<PushToTalkProxy>();
                if (ptt == null)
                {
                    ptt = pushToTalkButton.gameObject.AddComponent<PushToTalkProxy>();
                }
                ptt.Initialize(this, false);
            }

            var hasSavedName = PlayerPrefs.HasKey(playerPrefsAiNameKey);
            var showNaming = requireNamingAsLogin || !hasSavedName;
            loginCompleted = !showNaming;
            if (namingPanel != null) namingPanel.SetActive(showNaming);
            if (hudRoot != null) hudRoot.SetActive(!showNaming);
            EnsureNamingModalTop();
            OrganizeStatusLayout();

            if (!showNaming)
            {
                ApplyAiName(aiName);
                SetState(VoiceState.Idle, "대기 중");
                RefreshFigmaHud();
            }
            else
            {
                if (nameInputField != null)
                {
                    SetInputFieldTextSafe(nameInputField, aiName);
                }
                if (nameHintText != null)
                {
                    nameHintText.text = hasSavedName
                        ? "이전 이름: " + aiName + " (새 이름으로 변경 가능)"
                        : "AI 이름을 정해주세요";
                }
            }
        }

        private void ConfirmAiName()
        {
            var typed = nameInputField != null ? nameInputField.text : string.Empty;
            if (string.IsNullOrWhiteSpace(typed))
            {
                if (nameHintText != null)
                {
                    nameHintText.text = "이름을 입력해주세요";
                }
                return;
            }

            ApplyAiName(typed.Trim());
            PlayerPrefs.SetString(playerPrefsAiNameKey, aiName);
            PlayerPrefs.Save();

            loginCompleted = true;
            if (namingPanel != null) namingPanel.SetActive(false);
            if (hudRoot != null) hudRoot.SetActive(true);
            SetState(VoiceState.Idle, "준비 완료");
            RefreshFigmaHud();
        }

        private void Update()
        {
            if (!loginCompleted)
            {
                return;
            }

            autoIncomeTimer += Time.unscaledDeltaTime;
            while (autoIncomeTimer >= 1f)
            {
                autoIncomeTimer -= 1f;
                if (AutoIncomePerSecond > 0)
                {
                    AddCoins(AutoIncomePerSecond);
                    AdvanceMission("coins500", AutoIncomePerSecond);
                }
            }
        }

        private void LateUpdate()
        {
            // Hard gate for login flow: keep naming modal visible and top-most until confirmed.
            if (requireNamingAsLogin && !loginCompleted)
            {
                if (namingPanel != null && !namingPanel.activeSelf)
                {
                    namingPanel.SetActive(true);
                }
                if (hudRoot != null && hudRoot.activeSelf)
                {
                    hudRoot.SetActive(false);
                }
                EnsureNamingModalTop();
            }
            else
            {
                UpdateTabVisibility();
                ClampNameInputCaretSafe();
            }
        }

        private void ApplyAiName(string newName)
        {
            aiName = newName;
            if (aiNameText != null)
            {
                aiNameText.text = aiName;
            }
        }

        public void OnPushToTalkDown()
        {
            SetState(VoiceState.Listening, "듣는 중...");
            if (recognizedText != null) recognizedText.text = string.Empty;
            if (errorText != null) errorText.text = string.Empty;
            coreGlow?.TriggerTapBoost();

            if (voiceBridge != null)
            {
                voiceBridge.SendMessage("BeginPushToTalk", SendMessageOptions.DontRequireReceiver);
                voiceBridge.SendMessage("StartListening", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                if (mockRoutine != null)
                {
                    StopCoroutine(mockRoutine);
                    mockRoutine = null;
                }
            }
        }

        public void OnPushToTalkUp()
        {
            SetState(VoiceState.Processing, "처리 중...");
            if (voiceBridge != null)
            {
                voiceBridge.SendMessage("EndPushToTalk", SendMessageOptions.DontRequireReceiver);
                voiceBridge.SendMessage("StopListening", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                mockRoutine = StartCoroutine(MockProcessRoutine());
            }
        }

        public void OnNameVoiceDown()
        {
            SetState(VoiceState.Listening, "이름 음성 입력 중...");
            if (nameHintText != null)
            {
                nameHintText.text = "이름을 말해주세요...";
            }
            coreGlow?.TriggerTapBoost();

            if (voiceBridge != null)
            {
                voiceBridge.SendMessage("BeginPushToTalk", SendMessageOptions.DontRequireReceiver);
                voiceBridge.SendMessage("StartListening", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                if (mockRoutine != null)
                {
                    StopCoroutine(mockRoutine);
                    mockRoutine = null;
                }
            }
        }

        public void OnNameVoiceUp()
        {
            SetState(VoiceState.Processing, "이름 처리 중...");
            if (voiceBridge != null)
            {
                voiceBridge.SendMessage("EndPushToTalk", SendMessageOptions.DontRequireReceiver);
                voiceBridge.SendMessage("StopListening", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                if (mockRoutine != null)
                {
                    StopCoroutine(mockRoutine);
                }
                mockRoutine = StartCoroutine(MockNameProcessRoutine());
            }
        }

        // Native/bridge callback
        public void OnVoiceFinalText(string text)
        {
            var finalText = text ?? string.Empty;
            if (requireNamingAsLogin && !loginCompleted)
            {
                var nameCandidate = ExtractNameCandidate(finalText);
                if (!string.IsNullOrWhiteSpace(nameCandidate) && nameInputField != null)
                {
                    SetInputFieldTextSafe(nameInputField, nameCandidate);
                    if (nameHintText != null)
                    {
                        nameHintText.text = "인식된 이름: " + nameCandidate + " (확인 버튼을 눌러주세요)";
                    }
                }
                SetState(VoiceState.Idle, "이름 인식 완료");
                return;
            }

            if (recognizedText != null) recognizedText.text = finalText;
            if (responseText != null) responseText.text = BuildKoreanResponse(finalText);
            ExecuteGameCommand(finalText);
            AccumulateFigmaProgress(finalText);
            SetState(VoiceState.Idle, "완료");
        }

        // Native/bridge callback
        public void OnVoicePartialText(string text)
        {
            var partial = text ?? string.Empty;
            if (requireNamingAsLogin && !loginCompleted)
            {
                if (nameHintText != null)
                {
                    nameHintText.text = "이름 인식 중: " + partial;
                }
                return;
            }

            if (recognizedText != null) recognizedText.text = partial;
        }

        // Native/bridge callback ("Listening|msg")
        public void OnVoiceState(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return;
            }

            var split = payload.Split('|');
            var stateToken = split.Length > 0 ? split[0] : string.Empty;
            var message = split.Length > 1 ? split[1] : string.Empty;
            if (!Enum.TryParse(stateToken, true, out VoiceState parsed))
            {
                parsed = VoiceState.Error;
                message = "음성 상태 파싱 실패";
            }
            SetState(parsed, message);
        }

        public void OnVoiceError(string message)
        {
            SetState(VoiceState.Error, string.IsNullOrEmpty(message) ? "음성 인식 오류" : message);
        }

        private void SetState(VoiceState state, string message)
        {
            currentState = state;
            if (statusText != null)
            {
                statusText.text = "상태: " + currentState + "  " + message;
            }
            if (errorText != null)
            {
                errorText.text = currentState == VoiceState.Error ? message : string.Empty;
            }

            // Visual reaction
            var boost = currentState == VoiceState.Listening ? 1f : 0f;
            if (currentState == VoiceState.Error)
            {
                boost = 0.15f;
            }
            coreGlow?.SetExternalBoost(boost, currentState == VoiceState.Listening ? 0.5f : 0.2f);
            particleAmbient?.SetIntensity(boost);
            waveformBars?.SetIntensity(boost);
            if (ringRotators != null)
            {
                var speedMul = currentState == VoiceState.Listening ? 1.45f : 1f;
                for (var i = 0; i < ringRotators.Length; i++)
                {
                    if (ringRotators[i] != null)
                    {
                        ringRotators[i].SetSpeedMultiplier(speedMul);
                    }
                }
            }
        }

        private static string BuildKoreanResponse(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return "입력된 음성이 없어요. 다시 말씀해 주세요.";
            }

            var p = prompt.ToLowerInvariant();
            if (p.Contains("start") || p.Contains("run") || p.Contains("시작"))
            {
                return "시뮬레이션 시작 명령으로 이해했어요. 즉시 실행할게요.";
            }
            if (p.Contains("stop") || p.Contains("정지"))
            {
                return "정지 명령을 확인했어요. 현재 작업을 멈출게요.";
            }
            if (p.Contains("shop") || p.Contains("상점"))
            {
                return "상점 화면으로 이동할게요. 필요한 아이템을 선택해 주세요.";
            }

            // Default rule: answer in Korean even if prompt is English.
            return "요청을 한국어로 처리했어요: " + prompt;
        }

        private void ExecuteGameCommand(string prompt)
        {
            if (gameBridge == null || string.IsNullOrWhiteSpace(prompt))
            {
                return;
            }

            var p = prompt.ToLowerInvariant();
            if (p.Contains("start") || p.Contains("run") || p.Contains("시작"))
            {
                gameBridge.SendMessage("StartSimulationFromExternal", SendMessageOptions.DontRequireReceiver);
                return;
            }
            if (p.Contains("stop") || p.Contains("정지"))
            {
                gameBridge.SendMessage("StopSimulationFromExternal", SendMessageOptions.DontRequireReceiver);
                return;
            }
            if (p.Contains("shop") || p.Contains("상점"))
            {
                gameBridge.SendMessage("OpenShopFromExternal", SendMessageOptions.DontRequireReceiver);
                return;
            }
            if (p.Contains("home") || p.Contains("메인") || p.Contains("홈"))
            {
                gameBridge.SendMessage("GoHomeFromExternal", SendMessageOptions.DontRequireReceiver);
            }
        }

        public void ConfigureRuntime(
            GameObject namingPanelRef,
            InputField nameInputRef,
            Button confirmNameRef,
            Button nameVoiceButtonRef,
            Text nameHintRef,
            GameObject hudRootRef,
            Text aiNameRef,
            Text recognizedRef,
            Text responseRef,
            Text statusRef,
            Text errorRef,
            Button pushToTalkRef,
            CoreGlow coreGlowRef,
            RingRotator[] ringRotatorRefs,
            ParticleAmbient particleAmbientRef,
            WaveformBars waveformBarsRef,
            MonoBehaviour voiceBridgeRef
        )
        {
            namingPanel = namingPanelRef;
            nameInputField = nameInputRef;
            confirmNameButton = confirmNameRef;
            nameVoiceButton = nameVoiceButtonRef;
            nameHintText = nameHintRef;
            hudRoot = hudRootRef;
            aiNameText = aiNameRef;
            recognizedText = recognizedRef;
            responseText = responseRef;
            statusText = statusRef;
            errorText = errorRef;
            pushToTalkButton = pushToTalkRef;
            coreGlow = coreGlowRef;
            ringRotators = ringRotatorRefs;
            particleAmbient = particleAmbientRef;
            waveformBars = waveformBarsRef;
            voiceBridge = voiceBridgeRef;
            if (gameBridge == null)
            {
                var game = FindFirstObjectByType<AiPetGamePrototype>(FindObjectsInactive.Exclude);
                if (game != null)
                {
                    gameBridge = game;
                }
            }
        }

        private IEnumerator MockProcessRoutine()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            var mock = "start simulation";
            OnVoiceFinalText(mock);
        }

        private IEnumerator MockNameProcessRoutine()
        {
            yield return new WaitForSecondsRealtime(0.35f);
            OnVoiceFinalText("내 이름은 자비스");
        }

        private static string ExtractNameCandidate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var text = raw.Trim();
            text = text.Replace("내 이름은", string.Empty);
            text = text.Replace("이름은", string.Empty);
            text = text.Replace("이름", string.Empty);
            text = text.Replace("아이 이름", string.Empty);
            text = RemoveIgnoreCase(text, "name is");
            text = RemoveIgnoreCase(text, "my name is");
            text = RemoveIgnoreCase(text, "ai 이름");
            text = RemoveIgnoreCase(text, "jarvis name");
            text = text.Replace(":", " ");
            text = text.Trim(' ', '.', ',', '!', '?', '"', '\'');

            if (text.Length > 12)
            {
                text = text.Substring(0, 12).Trim();
            }
            return text;
        }

        private static string RemoveIgnoreCase(string source, string token)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(token))
            {
                return source;
            }

            var idx = source.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            while (idx >= 0)
            {
                source = source.Remove(idx, token.Length);
                idx = source.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            }
            return source;
        }

        private void EnsureNamingModalTop()
        {
            if (namingPanel == null)
            {
                return;
            }

            namingPanel.transform.SetAsLastSibling();
            var panelImage = namingPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.03f, 0.06f, 0.11f, 0.98f);
            }
        }

        private void EnsureNameVoiceButtonExists()
        {
            if (nameVoiceButton != null || namingPanel == null)
            {
                return;
            }

            var existing = namingPanel.transform.Find("NameVoiceButton");
            if (existing != null)
            {
                nameVoiceButton = existing.GetComponent<Button>();
                return;
            }

            var go = new GameObject("NameVoiceButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(namingPanel.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(520f, 96f);
            rt.anchoredPosition = new Vector2(0f, -20f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.10f, 0.56f, 0.95f, 1f);
            nameVoiceButton = go.GetComponent<Button>();

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 0.5f);
            lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(500f, 88f);
            lrt.anchoredPosition = Vector2.zero;

            var label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = "누르고 이름 말하기";
            label.fontSize = 32;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        private void OrganizeStatusLayout()
        {
            if (hudRoot == null || statusText == null || recognizedText == null || responseText == null || errorText == null)
            {
                return;
            }

            var rootRt = hudRoot.GetComponent<RectTransform>();
            if (rootRt == null)
            {
                return;
            }

            var panel = statusText.transform.parent != null && statusText.transform.parent.name == "StatusPanel"
                ? statusText.transform.parent.gameObject
                : null;

            if (panel == null)
            {
                panel = new GameObject("StatusPanel", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(hudRoot.transform, false);
            }

            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0f);
            panelRt.anchorMax = new Vector2(0.5f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.sizeDelta = new Vector2(980f, 210f);
            panelRt.anchoredPosition = new Vector2(0f, 160f);

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.02f, 0.08f, 0.16f, 0.72f);
            panelImage.raycastTarget = false;

            LayoutStatusText(statusText, panel.transform, new Vector2(0f, 184f), new Vector2(920f, 40f), 22);
            LayoutStatusText(recognizedText, panel.transform, new Vector2(0f, 142f), new Vector2(920f, 44f), 24);
            LayoutStatusText(responseText, panel.transform, new Vector2(0f, 96f), new Vector2(920f, 46f), 22);
            LayoutStatusText(errorText, panel.transform, new Vector2(0f, 52f), new Vector2(920f, 42f), 20);
            errorText.color = new Color(1f, 0.46f, 0.46f, 1f);

            if (pushToTalkButton != null)
            {
                var pttRect = pushToTalkButton.GetComponent<RectTransform>();
                if (pttRect != null)
                {
                    pttRect.anchorMin = new Vector2(0.5f, 0f);
                    pttRect.anchorMax = new Vector2(0.5f, 0f);
                    pttRect.pivot = new Vector2(0.5f, 0f);
                    pttRect.sizeDelta = new Vector2(520f, 92f);
                    pttRect.anchoredPosition = new Vector2(0f, 378f);
                }
            }
        }

        private void InitializeFigmaHudBindings()
        {
            if (hudRoot == null)
            {
                return;
            }

            if (coinValueText == null) coinValueText = FindTextInHud("CoinValueText");
            if (incomeValueText == null) incomeValueText = FindTextInHud("IncomeValueText");
            if (levelValueText == null) levelValueText = FindTextInHud("LevelValueText");
            if (expValueText == null) expValueText = FindTextInHud("ExpValueText");

            if (tabHomeButton == null) tabHomeButton = FindButtonInHud("TabHomeButton");
            if (tabShopButton == null) tabShopButton = FindButtonInHud("TabShopButton");
            if (tabMissionButton == null) tabMissionButton = FindButtonInHud("TabMissionButton");

            if (figmaHomeContent == null) figmaHomeContent = FindInHud("FigmaHomeContent");
            if (figmaShopContent == null) figmaShopContent = FindInHud("FigmaShopContent");
            if (figmaMissionContent == null) figmaMissionContent = FindInHud("FigmaMissionContent");
            if (tapAreaButton == null) tapAreaButton = FindButtonInHud("TapAreaButton");
            if (comboText == null) comboText = FindTextInHud("ComboText");
            if (totalTapText == null) totalTapText = FindTextInHud("TotalTapText");

            if (tabHomeButton != null)
            {
                tabHomeButton.onClick.RemoveListener(OnTabHome);
                tabHomeButton.onClick.AddListener(OnTabHome);
            }
            if (tabShopButton != null)
            {
                tabShopButton.onClick.RemoveListener(OnTabShop);
                tabShopButton.onClick.AddListener(OnTabShop);
            }
            if (tabMissionButton != null)
            {
                tabMissionButton.onClick.RemoveListener(OnTabMission);
                tabMissionButton.onClick.AddListener(OnTabMission);
            }
            if (tapAreaButton != null)
            {
                tapAreaButton.onClick.RemoveListener(OnTapArea);
                tapAreaButton.onClick.AddListener(OnTapArea);
            }

            BindUpgradeCard("ShopTapUpgradeCard", "tap");
            BindUpgradeCard("ShopAutoUpgradeCard", "auto");
            BindUpgradeCard("ShopMultiplierUpgradeCard", "multiplier");
            BindUpgradeCard("ShopIntelligenceUpgradeCard", "intelligence");
            BindMissionCard("MissionTap100Card", "tap100");
            BindMissionCard("MissionCoins500Card", "coins500");
            BindMissionCard("MissionLevel5Card", "level5");
            BindMissionCard("MissionUpgrade3Card", "upgrade3");
            UpdateTabVisibility();
        }

        private void InitializeFigmaGameData()
        {
            if (upgrades.Count == 0)
            {
                upgrades.Add(new UpgradeState { id = "tap", title = "탭 파워", level = 1, baseCost = 10, effect = 1 });
                upgrades.Add(new UpgradeState { id = "auto", title = "자동 수입", level = 0, baseCost = 50, effect = 0 });
                upgrades.Add(new UpgradeState { id = "multiplier", title = "경험치 배율", level = 1, baseCost = 100, effect = 1 });
                upgrades.Add(new UpgradeState { id = "intelligence", title = "지능 향상", level = 0, baseCost = 200, effect = 0 });
            }

            if (missions.Count == 0)
            {
                missions.Add(new MissionState { id = "tap100", title = "AI를 100번 탭하기", target = 100, reward = 50 });
                missions.Add(new MissionState { id = "coins500", title = "코인 500개 모으기", target = 500, reward = 100 });
                missions.Add(new MissionState { id = "level5", title = "레벨 5 달성하기", target = 5, reward = 200 });
                missions.Add(new MissionState { id = "upgrade3", title = "업그레이드 3번 구매하기", target = 3, reward = 150 });
            }
        }

        private void BindUpgradeCard(string cardName, string upgradeId)
        {
            var upgrade = upgrades.Find(x => x.id == upgradeId);
            var card = FindInHud(cardName);
            if (upgrade == null || card == null)
            {
                return;
            }

            upgrade.titleText = FindChildText(card.transform, "TitleText");
            upgrade.levelText = FindChildText(card.transform, "LevelText");
            upgrade.costText = FindChildText(card.transform, "CostText");
            upgrade.buyButton = FindChildButton(card.transform, "BuyButton");
            if (upgrade.buyButton != null)
            {
                upgrade.buyButton.onClick.RemoveAllListeners();
                upgrade.buyButton.onClick.AddListener(() => HandlePurchaseUpgrade(upgrade.id));
            }
        }

        private void BindMissionCard(string cardName, string missionId)
        {
            var mission = missions.Find(x => x.id == missionId);
            var card = FindInHud(cardName);
            if (mission == null || card == null)
            {
                return;
            }

            mission.titleText = FindChildText(card.transform, "TitleText");
            mission.progressText = FindChildText(card.transform, "ProgressText");
            mission.claimButton = FindChildButton(card.transform, "ClaimButton");
            if (mission.claimButton != null)
            {
                mission.claimButton.onClick.RemoveAllListeners();
                mission.claimButton.onClick.AddListener(() => HandleClaimMission(mission.id));
            }
        }

        private void OnTabHome()
        {
            activeTab = HudTab.Home;
            UpdateTabVisibility();
        }

        private void OnTabShop()
        {
            activeTab = HudTab.Shop;
            UpdateTabVisibility();
        }

        private void OnTabMission()
        {
            activeTab = HudTab.Mission;
            UpdateTabVisibility();
        }

        private void UpdateTabVisibility()
        {
            if (requireNamingAsLogin && !loginCompleted)
            {
                return;
            }

            var home = activeTab == HudTab.Home;
            if (figmaHomeContent != null) figmaHomeContent.SetActive(home);
            if (figmaShopContent != null) figmaShopContent.SetActive(activeTab == HudTab.Shop);
            if (figmaMissionContent != null) figmaMissionContent.SetActive(activeTab == HudTab.Mission);
            if (pushToTalkButton != null) pushToTalkButton.gameObject.SetActive(home);
            SetButtonTint(tabHomeButton, home);
            SetButtonTint(tabShopButton, activeTab == HudTab.Shop);
            SetButtonTint(tabMissionButton, activeTab == HudTab.Mission);
        }

        private void AccumulateFigmaProgress(string text)
        {
            if (requireNamingAsLogin && !loginCompleted)
            {
                return;
            }

            var addCoin = Mathf.Max(1, (text ?? string.Empty).Length / 6);
            AddCoins(addCoin);
            AddExp(2);
        }

        private void RefreshFigmaHud()
        {
            if (coinValueText != null) coinValueText.text = figmaCoins.ToString();
            if (incomeValueText != null) incomeValueText.text = "+" + AutoIncomePerSecond + "/s";
            if (levelValueText != null) levelValueText.text = figmaLevel.ToString();
            if (expValueText != null) expValueText.text = "EXP " + figmaExp + " / " + ExpToNextLevel();
            if (comboText != null) comboText.text = tapStreak > 1 ? "콤보 x" + tapStreak : string.Empty;
            if (totalTapText != null) totalTapText.text = "총 탭 횟수: " + totalTaps;
            RefreshUpgradeCards();
            RefreshMissionCards();
        }

        private void OnTapArea()
        {
            if (!loginCompleted)
            {
                return;
            }

            var now = Time.unscaledTime;
            tapStreak = now - lastTapTime < 0.5f ? tapStreak + 1 : 1;
            lastTapTime = now;
            totalTaps += 1;

            var earnedCoins = TapPower * tapStreak;
            AddCoins(earnedCoins);
            AddExp(ExpMultiplier);
            AdvanceMission("tap100", 1);
            AdvanceMission("coins500", earnedCoins);

            coreGlow?.TriggerTapBoost();
            SetState(VoiceState.Idle, "탭으로 " + earnedCoins + " 코인 획득");
        }

        private void HandlePurchaseUpgrade(string upgradeId)
        {
            var upgrade = upgrades.Find(x => x.id == upgradeId);
            if (upgrade == null)
            {
                return;
            }

            var cost = GetUpgradeCost(upgrade);
            if (figmaCoins < cost)
            {
                SetState(VoiceState.Error, "코인이 부족합니다");
                return;
            }

            figmaCoins -= cost;
            upgrade.level += 1;
            switch (upgrade.id)
            {
                case "tap":
                    upgrade.effect = upgrade.level;
                    break;
                case "auto":
                    upgrade.effect = upgrade.level * 2;
                    break;
                case "multiplier":
                    upgrade.effect = upgrade.level;
                    break;
                case "intelligence":
                    upgrade.effect = upgrade.level;
                    break;
            }

            purchasedUpgradeCount += 1;
            AdvanceMission("upgrade3", 1);
            SetState(VoiceState.Idle, upgrade.title + " 업그레이드 완료");
            RefreshFigmaHud();
        }

        private void HandleClaimMission(string missionId)
        {
            var mission = missions.Find(x => x.id == missionId);
            if (mission == null || mission.completed || mission.progress < mission.target)
            {
                return;
            }

            mission.completed = true;
            AddCoins(mission.reward);
            SetState(VoiceState.Idle, "미션 보상 " + mission.reward + " 코인 획득");
        }

        private void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }
            figmaCoins += amount;
            RefreshFigmaHud();
        }

        private void AddExp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            figmaExp += amount;
            while (figmaExp >= ExpToNextLevel())
            {
                figmaExp -= ExpToNextLevel();
                figmaLevel += 1;
            }
            RefreshFigmaHud();
        }

        private int ExpToNextLevel()
        {
            return Mathf.Max(50, figmaLevel * 50);
        }

        private void AdvanceMission(string missionId, int delta)
        {
            var mission = missions.Find(x => x.id == missionId);
            if (mission == null || mission.completed || delta <= 0)
            {
                return;
            }
            mission.progress = Mathf.Min(mission.target, mission.progress + delta);
            RefreshFigmaHud();
        }

        private void RefreshUpgradeCards()
        {
            for (var i = 0; i < upgrades.Count; i++)
            {
                var u = upgrades[i];
                if (u.titleText != null) u.titleText.text = u.title;
                if (u.levelText != null) u.levelText.text = "Lv." + u.level;
                if (u.costText != null) u.costText.text = GetUpgradeCost(u) + " 코인";
                if (u.buyButton != null) u.buyButton.interactable = figmaCoins >= GetUpgradeCost(u);
            }
        }

        private void RefreshMissionCards()
        {
            SyncMissionProgressDerivedFromState();
            for (var i = 0; i < missions.Count; i++)
            {
                var m = missions[i];
                if (m.titleText != null) m.titleText.text = m.title + " (+" + m.reward + ")";
                if (m.progressText != null) m.progressText.text = m.completed ? "완료" : m.progress + "/" + m.target;
                if (m.claimButton != null)
                {
                    m.claimButton.interactable = !m.completed && m.progress >= m.target;
                    var label = m.claimButton.transform.Find("Label");
                    if (label != null)
                    {
                        var labelText = label.GetComponent<Text>();
                        if (labelText != null)
                        {
                            labelText.text = m.completed ? "완료" : "수령";
                        }
                    }
                }
            }
        }

        private void SyncMissionProgressDerivedFromState()
        {
            var coinMission = missions.Find(x => x.id == "coins500");
            if (coinMission != null && !coinMission.completed)
            {
                coinMission.progress = Mathf.Max(coinMission.progress, Mathf.Min(coinMission.target, figmaCoins));
            }

            var levelMission = missions.Find(x => x.id == "level5");
            if (levelMission != null && !levelMission.completed)
            {
                levelMission.progress = Mathf.Max(levelMission.progress, Mathf.Min(levelMission.target, figmaLevel));
            }

            var upgradeMission = missions.Find(x => x.id == "upgrade3");
            if (upgradeMission != null && !upgradeMission.completed)
            {
                upgradeMission.progress = Mathf.Max(upgradeMission.progress, Mathf.Min(upgradeMission.target, purchasedUpgradeCount));
            }
        }

        private int FindUpgradeEffect(string id)
        {
            var upgrade = upgrades.Find(x => x.id == id);
            return upgrade != null ? upgrade.effect : 0;
        }

        private static int GetUpgradeCost(UpgradeState upgrade)
        {
            return Mathf.FloorToInt(upgrade.baseCost * Mathf.Pow(1.5f, upgrade.level));
        }

        private Text FindTextInHud(string name)
        {
            var go = FindInHud(name);
            return go != null ? go.GetComponent<Text>() : null;
        }

        private Button FindButtonInHud(string name)
        {
            var go = FindInHud(name);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private static Text FindChildText(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }
            var child = parent.Find(childName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static Button FindChildButton(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }
            var child = parent.Find(childName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private GameObject FindInHud(string name)
        {
            if (hudRoot == null)
            {
                return null;
            }
            var t = hudRoot.transform.Find(name);
            if (t != null)
            {
                return t.gameObject;
            }
            t = FindDeepChild(hudRoot.transform, name);
            return t != null ? t.gameObject : null;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
                var nested = FindDeepChild(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }
            return null;
        }

        private static void SetButtonTint(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }
            var img = button.GetComponent<Image>();
            if (img != null)
            {
                img.color = active
                    ? new Color(0.08f, 0.53f, 0.93f, 1f)
                    : new Color(0.13f, 0.24f, 0.35f, 1f);
            }
        }

        private static void SetInputFieldTextSafe(InputField input, string value)
        {
            if (input == null)
            {
                return;
            }

            var text = value ?? string.Empty;
            var wasFocused = input.isFocused;
            if (wasFocused)
            {
                input.DeactivateInputField();
            }

            input.SetTextWithoutNotify(text);
            input.text = text;
            input.ForceLabelUpdate();

            var len = text.Length;
            try
            {
                input.caretPosition = len;
            }
            catch
            {
                // Ignore to keep runtime safe across UGUI variants.
            }

            if (wasFocused)
            {
                input.ActivateInputField();
                input.MoveTextEnd(false);
            }
        }

        private void ClampNameInputCaretSafe()
        {
            if (nameInputField == null || !nameInputField.isFocused)
            {
                return;
            }

            var len = (nameInputField.text ?? string.Empty).Length;
            try
            {
                if (nameInputField.caretPosition > len)
                {
                    nameInputField.caretPosition = len;
                }
            }
            catch
            {
                // Ignore to avoid editor/runtime interruption.
            }
        }

        private static void LayoutStatusText(Text text, Transform parent, Vector2 anchoredPos, Vector2 size, int fontSize)
        {
            if (text == null)
            {
                return;
            }

            text.transform.SetParent(parent, false);
            var rt = text.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = fontSize;
        }

        private sealed class PushToTalkProxy : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler, UnityEngine.EventSystems.IPointerExitHandler
        {
            private JarvisUIController owner;
            private bool forNaming;
            private bool pressed;

            public void Initialize(JarvisUIController controller, bool isForNaming)
            {
                owner = controller;
                forNaming = isForNaming;
            }

            public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
            {
                pressed = true;
                if (forNaming)
                {
                    owner?.OnNameVoiceDown();
                }
                else
                {
                    owner?.OnPushToTalkDown();
                }
            }

            public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
            {
                if (!pressed) return;
                pressed = false;
                if (forNaming)
                {
                    owner?.OnNameVoiceUp();
                }
                else
                {
                    owner?.OnPushToTalkUp();
                }
            }

            public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
            {
                if (!pressed) return;
                pressed = false;
                if (forNaming)
                {
                    owner?.OnNameVoiceUp();
                }
                else
                {
                    owner?.OnPushToTalkUp();
                }
            }
        }
    }
}
