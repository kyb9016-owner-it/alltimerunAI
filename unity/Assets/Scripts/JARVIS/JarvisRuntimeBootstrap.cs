using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace AllTimeRunAI.Jarvis
{
    public static class JarvisRuntimeBootstrap
    {
        public static void EnsureFigmaHud(Transform hudRoot)
        {
            if (hudRoot == null)
            {
                return;
            }

            if (hudRoot.Find("FigmaTopPanel") == null)
            {
                BuildFigmaTopPanel(hudRoot);
            }

            if (hudRoot.Find("FigmaContentRoot") == null || hudRoot.Find("FigmaTabBar") == null)
            {
                BuildFigmaTabLayout(hudRoot);
            }
        }

        public static void BuildIfMissing(JarvisUIController controller)
        {
            if (controller == null)
            {
                return;
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 1f;
            }

            EnsureEventSystem();

            var naming = CreatePanel(canvas.transform, "NamingPanel", new Color(0.03f, 0.06f, 0.11f, 0.96f));
            var hudRoot = CreatePanel(canvas.transform, "HUDRoot", new Color(0f, 0f, 0f, 0f));

            CreateText(naming.transform, "NameTitleText", "AI 이름을 정해주세요", 56, new Vector2(0, 280), new Vector2(920, 120));
            var input = CreateInputField(naming.transform, "NameInput", new Vector2(0, 120), new Vector2(820, 110));
            input.text = "JARVIS";
            var nameVoice = CreateButton(naming.transform, "NameVoiceButton", "누르고 이름 말하기", new Vector2(0, -20), new Vector2(520, 96));
            var confirm = CreateButton(naming.transform, "ConfirmNameButton", "확인", new Vector2(0, -140), new Vector2(420, 98));
            var hint = CreateText(naming.transform, "NameHintText", "이름 입력 또는 음성 입력 후 확인", 30, new Vector2(0, -260), new Vector2(920, 90));

            BuildFigmaTopPanel(hudRoot.transform);
            var coreRoot = CreateCore(hudRoot.transform);
            BuildFigmaTabLayout(hudRoot.transform);
            var aiName = CreateText(hudRoot.transform, "AINameText", "JARVIS", 48, new Vector2(0, 520), new Vector2(900, 100));
            var status = CreateText(hudRoot.transform, "StatusText", "상태: Idle", 30, new Vector2(0, 680), new Vector2(980, 80));
            var recognized = CreateText(hudRoot.transform, "RecognizedText", "음성 텍스트", 34, new Vector2(0, -360), new Vector2(980, 120));
            var response = CreateText(hudRoot.transform, "ResponseText", "한국어 응답", 30, new Vector2(0, -470), new Vector2(980, 130));
            var error = CreateText(hudRoot.transform, "ErrorText", "", 28, new Vector2(0, -580), new Vector2(980, 80));
            error.color = new Color(1f, 0.46f, 0.46f, 1f);
            var ptt = CreateButton(hudRoot.transform, "PushToTalkButton", "누르고 말하기", new Vector2(0, -620), new Vector2(520, 98));

            var coreGlow = coreRoot.GetComponent<CoreGlow>();
            var ringRotators = coreRoot.GetComponentsInChildren<RingRotator>(true);
            var particleAmbient = coreRoot.GetComponentInChildren<ParticleAmbient>(true);
            var waveform = coreRoot.GetComponentInChildren<WaveformBars>(true);

            var bridge = controller.gameObject.GetComponent<JarvisVoiceBridge>();
            if (bridge == null)
            {
                bridge = controller.gameObject.AddComponent<JarvisVoiceBridge>();
            }

            controller.ConfigureRuntime(
                naming, input, confirm, nameVoice, hint,
                hudRoot, aiName, recognized, response, status, error, ptt,
                coreGlow, ringRotators, particleAmbient, waveform, bridge
            );
        }

        private static void EnsureEventSystem()
        {
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            }

            var inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                if (es.GetComponent(inputSystemModuleType) == null)
                {
                    es.gameObject.AddComponent(inputSystemModuleType);
                }
                var legacy = es.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                {
                    Object.Destroy(legacy);
                }
                return;
            }

            if (es.GetComponent<BaseInputModule>() == null)
            {
                es.gameObject.AddComponent<StandaloneInputModule>();
            }
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private static void BuildFigmaTopPanel(Transform parent)
        {
            var topPanel = new GameObject("FigmaTopPanel", typeof(RectTransform), typeof(Image));
            topPanel.transform.SetParent(parent, false);
            var topRt = topPanel.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0.5f, 1f);
            topRt.anchorMax = new Vector2(0.5f, 1f);
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.sizeDelta = new Vector2(980f, 224f);
            topRt.anchoredPosition = new Vector2(0f, -20f);

            var topBg = topPanel.GetComponent<Image>();
            topBg.color = new Color(0.03f, 0.11f, 0.19f, 0.72f);

            var coinCard = CreateCard(topPanel.transform, "CoinCard", new Vector2(-235f, -114f), new Vector2(430f, 164f), new Color(0.04f, 0.18f, 0.30f, 0.85f));
            CreateText(coinCard.transform, "CoinTitle", "코인", 26, new Vector2(0f, 46f), new Vector2(390f, 52f));
            CreateText(coinCard.transform, "CoinValueText", "0", 46, new Vector2(0f, -2f), new Vector2(390f, 70f));
            CreateText(coinCard.transform, "IncomeValueText", "+0/s", 24, new Vector2(0f, -58f), new Vector2(390f, 46f));

            var levelCard = CreateCard(topPanel.transform, "LevelCard", new Vector2(235f, -114f), new Vector2(430f, 164f), new Color(0.05f, 0.19f, 0.34f, 0.85f));
            CreateText(levelCard.transform, "LevelTitle", "레벨", 26, new Vector2(0f, 46f), new Vector2(390f, 52f));
            CreateText(levelCard.transform, "LevelValueText", "1", 46, new Vector2(0f, -2f), new Vector2(390f, 70f));
            CreateText(levelCard.transform, "ExpValueText", "EXP 0 / 20", 24, new Vector2(0f, -58f), new Vector2(390f, 46f));
        }

        private static void BuildFigmaTabLayout(Transform parent)
        {
            var contentRoot = new GameObject("FigmaContentRoot", typeof(RectTransform));
            contentRoot.transform.SetParent(parent, false);
            var crt = contentRoot.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(980f, 560f);
            crt.anchoredPosition = new Vector2(0f, -300f);

            var homeContent = CreateCard(contentRoot.transform, "FigmaHomeContent", Vector2.zero, new Vector2(980f, 560f), new Color(0f, 0f, 0f, 0f));
            homeContent.GetComponent<Image>().raycastTarget = false;
            var homeTapArea = CreateButton(homeContent.transform, "TapAreaButton", "AI를 탭해서 코인을 획득", new Vector2(0f, 150f), new Vector2(760f, 154f));
            TintButton(homeTapArea, new Color(0.12f, 0.40f, 0.68f, 0.34f));
            CreateText(homeContent.transform, "ComboText", "", 32, new Vector2(0f, 46f), new Vector2(880f, 56f));
            CreateText(homeContent.transform, "TotalTapText", "총 탭 횟수: 0", 26, new Vector2(0f, -8f), new Vector2(880f, 52f));
            CreateText(homeContent.transform, "TapHintText", "연속 탭 시 보너스 배율이 올라갑니다", 24, new Vector2(0f, -62f), new Vector2(900f, 48f));

            var shopContent = CreateCard(contentRoot.transform, "FigmaShopContent", Vector2.zero, new Vector2(980f, 560f), new Color(0.03f, 0.12f, 0.21f, 0.64f));
            CreateText(shopContent.transform, "ShopTitle", "상점", 36, new Vector2(0f, 244f), new Vector2(900f, 64f));
            CreateShopUpgradeCard(shopContent.transform, "ShopTapUpgradeCard", new Vector2(0f, 132f));
            CreateShopUpgradeCard(shopContent.transform, "ShopAutoUpgradeCard", new Vector2(0f, 28f));
            CreateShopUpgradeCard(shopContent.transform, "ShopMultiplierUpgradeCard", new Vector2(0f, -76f));
            CreateShopUpgradeCard(shopContent.transform, "ShopIntelligenceUpgradeCard", new Vector2(0f, -180f));
            shopContent.SetActive(false);

            var missionContent = CreateCard(contentRoot.transform, "FigmaMissionContent", Vector2.zero, new Vector2(980f, 560f), new Color(0.03f, 0.12f, 0.21f, 0.64f));
            CreateText(missionContent.transform, "MissionTitle", "일일 미션", 36, new Vector2(0f, 244f), new Vector2(900f, 64f));
            CreateMissionCard(missionContent.transform, "MissionTap100Card", new Vector2(0f, 132f));
            CreateMissionCard(missionContent.transform, "MissionCoins500Card", new Vector2(0f, 28f));
            CreateMissionCard(missionContent.transform, "MissionLevel5Card", new Vector2(0f, -76f));
            CreateMissionCard(missionContent.transform, "MissionUpgrade3Card", new Vector2(0f, -180f));
            missionContent.SetActive(false);

            var tabBar = CreateCard(parent, "FigmaTabBar", Vector2.zero, new Vector2(980f, 122f), new Color(0.03f, 0.11f, 0.19f, 0.92f));
            var tabRt = tabBar.GetComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0.5f, 0f);
            tabRt.anchorMax = new Vector2(0.5f, 0f);
            tabRt.pivot = new Vector2(0.5f, 0f);
            tabRt.anchoredPosition = new Vector2(0f, 28f);
            var homeBtn = CreateButton(tabBar.transform, "TabHomeButton", "홈", new Vector2(-280f, 0f), new Vector2(220f, 84f));
            var shopBtn = CreateButton(tabBar.transform, "TabShopButton", "상점", new Vector2(0f, 0f), new Vector2(220f, 84f));
            var missionBtn = CreateButton(tabBar.transform, "TabMissionButton", "미션", new Vector2(280f, 0f), new Vector2(220f, 84f));
            TintButton(homeBtn, new Color(0.08f, 0.53f, 0.93f, 1f));
            TintButton(shopBtn, new Color(0.13f, 0.24f, 0.35f, 1f));
            TintButton(missionBtn, new Color(0.13f, 0.24f, 0.35f, 1f));
        }

        private static void CreateShopUpgradeCard(Transform parent, string name, Vector2 anchoredPos)
        {
            var card = CreateCard(parent, name, anchoredPos, new Vector2(900f, 88f), new Color(0.06f, 0.20f, 0.33f, 0.84f));
            CreateText(card.transform, "TitleText", "업그레이드", 26, new Vector2(-250f, 0f), new Vector2(430f, 56f));
            CreateText(card.transform, "LevelText", "Lv.0", 22, new Vector2(10f, 0f), new Vector2(120f, 50f));
            CreateText(card.transform, "CostText", "0 코인", 22, new Vector2(160f, 0f), new Vector2(140f, 50f));
            var button = CreateButton(card.transform, "BuyButton", "구매", new Vector2(330f, 0f), new Vector2(160f, 62f));
            TintButton(button, new Color(0.06f, 0.56f, 1f, 0.95f));
            var label = button.transform.Find("Label");
            if (label != null)
            {
                var text = label.GetComponent<Text>();
                if (text != null)
                {
                    text.fontSize = 24;
                }
            }
        }

        private static void CreateMissionCard(Transform parent, string name, Vector2 anchoredPos)
        {
            var card = CreateCard(parent, name, anchoredPos, new Vector2(900f, 88f), new Color(0.06f, 0.20f, 0.33f, 0.84f));
            CreateText(card.transform, "TitleText", "미션", 24, new Vector2(-220f, 0f), new Vector2(470f, 56f));
            CreateText(card.transform, "ProgressText", "0/1", 22, new Vector2(170f, 0f), new Vector2(150f, 50f));
            var button = CreateButton(card.transform, "ClaimButton", "수령", new Vector2(330f, 0f), new Vector2(160f, 62f));
            TintButton(button, new Color(0.10f, 0.66f, 0.44f, 0.96f));
            var label = button.transform.Find("Label");
            if (label != null)
            {
                var text = label.GetComponent<Text>();
                if (text != null)
                {
                    text.fontSize = 24;
                }
            }
        }

        private static GameObject CreateCore(Transform parent)
        {
            var root = new GameObject("JARVIS_Core", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rtRoot = root.GetComponent<RectTransform>();
            rtRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rtRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rtRoot.sizeDelta = new Vector2(600, 600);
            rtRoot.anchoredPosition = new Vector2(0, 120);

            var glowA = CreateImage(root.transform, "CoreGlowA", new Vector2(340, 340), new Color(0.16f, 0.78f, 1f, 0.32f));
            var glowB = CreateImage(root.transform, "CoreGlowB", new Vector2(430, 430), new Color(0.33f, 0.90f, 1f, 0.18f));
            var ring1 = CreateImage(root.transform, "Ring1", new Vector2(360, 360), new Color(0.26f, 0.84f, 1f, 0.76f));
            var ring2 = CreateImage(root.transform, "Ring2", new Vector2(420, 420), new Color(0.45f, 0.90f, 1f, 0.56f));
            var ring3 = CreateImage(root.transform, "Ring3", new Vector2(500, 500), new Color(0.55f, 0.95f, 1f, 0.34f));
            var core = CreateImage(root.transform, "CoreMain", new Vector2(220, 220), new Color(0.68f, 0.95f, 1f, 0.96f));

            ring1.gameObject.AddComponent<RingRotator>();
            ring2.gameObject.AddComponent<RingRotator>().SetSpeedMultiplier(1.2f);
            ring3.gameObject.AddComponent<RingRotator>().SetSpeedMultiplier(1.45f);

            var barsRoot = new GameObject("WaveBarsRoot", typeof(RectTransform));
            barsRoot.transform.SetParent(root.transform, false);
            var barsRt = barsRoot.GetComponent<RectTransform>();
            barsRt.anchorMin = new Vector2(0.5f, 0.5f);
            barsRt.anchorMax = new Vector2(0.5f, 0.5f);
            barsRt.sizeDelta = new Vector2(560, 560);
            var barPrefab = CreateImage(barsRoot.transform, "BarPrefab", new Vector2(6, 14), new Color(0.58f, 0.9f, 1f, 0.8f));
            barPrefab.gameObject.SetActive(false);
            var waveform = barsRoot.AddComponent<WaveformBars>();
            SetField(waveform, "barsRoot", barsRt);
            SetField(waveform, "barPrefab", barPrefab);

            var psObj = new GameObject("AmbientParticles");
            psObj.transform.SetParent(root.transform, false);
            var ps = psObj.AddComponent<ParticleSystem>();
            ConfigureParticle(ps);
            var pa = psObj.AddComponent<ParticleAmbient>();
            SetField(pa, "particleSystemRef", ps);

            var cg = root.AddComponent<CoreGlow>();
            SetField(cg, "coreTransform", core.rectTransform);
            SetField(cg, "glowLayers", new Graphic[] { glowA, glowB });
            return root;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, Vector2 pos, Vector2 rect)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = rect;
            rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = value;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.78f, 0.93f, 1f, 1f);
            return t;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 rect)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = rect;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.06f, 0.56f, 1f, 1f);
            var txt = CreateText(go.transform, "Label", label, 34, Vector2.zero, rect);
            txt.color = Color.white;
            return go.GetComponent<Button>();
        }

        private static GameObject CreateCard(Transform parent, string name, Vector2 pos, Vector2 rect, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = rect;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = color;
            return go;
        }

        private static InputField CreateInputField(Transform parent, string name, Vector2 pos, Vector2 rect)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = rect;
            rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = new Color(0.09f, 0.15f, 0.25f, 0.95f);

            var text = CreateText(go.transform, "Text", "", 34, Vector2.zero, new Vector2(rect.x - 40, rect.y - 20));
            var placeholder = CreateText(go.transform, "Placeholder", "AI 이름", 30, Vector2.zero, new Vector2(rect.x - 40, rect.y - 20));
            placeholder.color = new Color(0.6f, 0.75f, 0.9f, 0.6f);

            var input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        private static void ConfigureParticle(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 2.2f;
            main.startSpeed = 8f;
            main.startSize = 0.03f;
            main.maxParticles = 28;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.rateOverTime = 5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Donut;
            shape.radius = 1.25f;
            shape.donutRadius = 0.18f;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var f = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (f != null)
            {
                f.SetValue(target, value);
            }
        }

        private static void TintButton(Button button, Color color)
        {
            if (button == null)
            {
                return;
            }
            var img = button.GetComponent<Image>();
            if (img != null)
            {
                img.color = color;
            }
        }
    }
}
