using System.IO;
using AllTimeRunAI.Jarvis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class JarvisScaffoldBuilder
{
    private const string ScenePath = "Assets/Scenes/JARVIS_UI.unity";
    private const string PrefabPath = "Assets/Prefabs/JARVIS/JARVIS_Core.prefab";
    private const string SpriteDir = "Assets/Sprites/JARVIS";
    private const string MaterialDir = "Assets/Materials/JARVIS";

    [MenuItem("Tools/JARVIS/Build Complete HUD")]
    public static void BuildCompleteHud()
    {
        EnsureFolders();
        CreateDefaultSprites();
        CreateDefaultMaterial();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "JARVIS_UI";

        var canvas = CreateCanvas();
        CreateEventSystemIfMissing();
        CreateBackground(canvas.transform);

        var namingPanel = CreateNamingPanel(canvas.transform);
        var hudRoot = CreateHudRoot(canvas.transform);
        hudRoot.SetActive(false);

        var coreRoot = CreateJarvisCore(hudRoot.transform);
        SaveOrUpdatePrefab(coreRoot);

        var uiRefs = CreateHudTextsAndButton(hudRoot.transform);
        var controller = canvas.gameObject.AddComponent<JarvisUIController>();
        var voiceBridge = canvas.gameObject.AddComponent<JarvisVoiceBridge>();
        BindController(controller, voiceBridge, namingPanel, hudRoot, uiRefs, coreRoot);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("JARVIS scaffold complete: " + ScenePath + " / " + PrefabPath);
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/JARVIS");
        EnsureFolder("Assets/Scripts");
        EnsureFolder("Assets/Scripts/JARVIS");
        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialDir);
        EnsureFolder("Assets/Sprites");
        EnsureFolder(SpriteDir);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        var name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && AssetDatabase.IsValidFolder(parent))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;
        return canvas;
    }

    private static void CreateEventSystemIfMissing()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void CreateBackground(Transform parent)
    {
        var bg = CreateUi("Background", parent);
        var img = bg.AddComponent<Image>();
        img.sprite = LoadSprite("bg_gradient");
        img.color = Color.white;
        Stretch(bg.GetComponent<RectTransform>());
    }

    private static GameObject CreateNamingPanel(Transform parent)
    {
        var panel = CreateUi("NamingPanel", parent);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.07f, 0.13f, 0.92f);
        Stretch(panel.GetComponent<RectTransform>());

        CreateText(panel.transform, "NameTitleText", "AI 이름을 정해주세요", 56, new Vector2(0f, 300f), new Vector2(960, 120));
        var input = CreateInputField(panel.transform, "NameInput", new Vector2(0f, 120f), new Vector2(820, 110));
        input.text = "JARVIS";
        CreateButton(panel.transform, "NameVoiceButton", "누르고 이름 말하기", new Vector2(0f, -20f), new Vector2(520, 96));
        CreateButton(panel.transform, "ConfirmNameButton", "확인", new Vector2(0f, -140f), new Vector2(420, 100));
        CreateText(panel.transform, "NameHintText", "이름 입력 또는 음성 입력 후 확인", 30, new Vector2(0f, -260f), new Vector2(960, 90));
        return panel;
    }

    private static GameObject CreateHudRoot(Transform parent)
    {
        var root = CreateUi("HUDRoot", parent);
        Stretch(root.GetComponent<RectTransform>());
        return root;
    }

    private static GameObject CreateJarvisCore(Transform parent)
    {
        var root = CreateUi("JARVIS_Core", parent);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(600, 600);
        rootRt.anchoredPosition = new Vector2(0f, 150f);

        var glowA = CreateImage(root.transform, "CoreGlowA", LoadSprite("core_disc"), new Vector2(340, 340), new Color(0.16f, 0.78f, 1f, 0.32f));
        var glowB = CreateImage(root.transform, "CoreGlowB", LoadSprite("core_disc"), new Vector2(430, 430), new Color(0.33f, 0.90f, 1f, 0.18f));
        var ring1 = CreateImage(root.transform, "Ring1", LoadSprite("ring_thin"), new Vector2(360, 360), new Color(0.26f, 0.84f, 1f, 0.76f));
        var ring2 = CreateImage(root.transform, "Ring2", LoadSprite("ring_thin"), new Vector2(420, 420), new Color(0.45f, 0.90f, 1f, 0.56f));
        var ring3 = CreateImage(root.transform, "Ring3", LoadSprite("ring_thin"), new Vector2(500, 500), new Color(0.55f, 0.95f, 1f, 0.34f));
        var core = CreateImage(root.transform, "CoreMain", LoadSprite("core_disc"), new Vector2(220, 220), new Color(0.68f, 0.95f, 1f, 0.96f));

        var barsRoot = CreateUi("WaveBarsRoot", root.transform);
        barsRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(560, 560);
        barsRoot.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        var barTemplate = CreateImage(barsRoot.transform, "BarPrefab", null, new Vector2(6, 14), new Color(0.58f, 0.9f, 1f, 0.8f));
        barTemplate.gameObject.SetActive(false);

        var particleGo = new GameObject("AmbientParticles");
        particleGo.transform.SetParent(root.transform, false);
        var ps = particleGo.AddComponent<ParticleSystem>();
        ConfigureParticle(ps);
        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        var coreGlow = root.AddComponent<CoreGlow>();
        SetPrivateSerialized(coreGlow, "coreTransform", core.rectTransform);
        SetPrivateSerialized(coreGlow, "glowLayers", new Graphic[] { glowA, glowB });

        var rr1 = ring1.gameObject.AddComponent<RingRotator>();
        SetPrivateSerialized(rr1, "target", ring1.rectTransform);
        SetPrivateSerialized(rr1, "degreesPerSecond", 18f);
        SetPrivateSerialized(rr1, "clockwise", true);

        var rr2 = ring2.gameObject.AddComponent<RingRotator>();
        SetPrivateSerialized(rr2, "target", ring2.rectTransform);
        SetPrivateSerialized(rr2, "degreesPerSecond", 26f);
        SetPrivateSerialized(rr2, "clockwise", false);

        var rr3 = ring3.gameObject.AddComponent<RingRotator>();
        SetPrivateSerialized(rr3, "target", ring3.rectTransform);
        SetPrivateSerialized(rr3, "degreesPerSecond", 34f);
        SetPrivateSerialized(rr3, "clockwise", true);

        var ambient = particleGo.AddComponent<ParticleAmbient>();
        SetPrivateSerialized(ambient, "particleSystemRef", ps);

        var waveform = barsRoot.gameObject.AddComponent<WaveformBars>();
        SetPrivateSerialized(waveform, "barsRoot", barsRoot.GetComponent<RectTransform>());
        SetPrivateSerialized(waveform, "barPrefab", barTemplate);

        return root;
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

    private static (Text aiName, Text recognized, Text response, Text status, Text error, Button ptt, InputField nameInput, Button nameConfirm, Button nameVoice, Text nameHint) CreateHudTextsAndButton(Transform hudRoot)
    {
        var aiName = CreateText(hudRoot, "AINameText", "JARVIS", 52, new Vector2(0f, 780f), new Vector2(900, 110));
        var status = CreateText(hudRoot, "StatusText", "상태: Idle", 30, new Vector2(0f, 680f), new Vector2(980, 80));
        var recognized = CreateText(hudRoot, "RecognizedText", "음성 인식 텍스트", 34, new Vector2(0f, -360f), new Vector2(980, 120));
        var response = CreateText(hudRoot, "ResponseText", "한국어 응답", 30, new Vector2(0f, -470f), new Vector2(980, 130));
        var error = CreateText(hudRoot, "ErrorText", "", 28, new Vector2(0f, -580f), new Vector2(980, 80));
        error.color = new Color(1f, 0.46f, 0.46f, 1f);
        var ptt = CreateButton(hudRoot, "PushToTalkButton", "누르고 말하기", new Vector2(0f, -760f), new Vector2(520, 98));

        var nameInput = Object.FindFirstObjectByType<InputField>();
        var nameConfirm = GameObject.Find("ConfirmNameButton")?.GetComponent<Button>();
        var nameVoice = GameObject.Find("NameVoiceButton")?.GetComponent<Button>();
        var nameHint = GameObject.Find("NameHintText")?.GetComponent<Text>();
        return (aiName, recognized, response, status, error, ptt, nameInput, nameConfirm, nameVoice, nameHint);
    }

    private static void BindController(JarvisUIController controller, JarvisVoiceBridge voiceBridge, GameObject namingPanel, GameObject hudRoot, (Text aiName, Text recognized, Text response, Text status, Text error, Button ptt, InputField nameInput, Button nameConfirm, Button nameVoice, Text nameHint) refsTuple, GameObject jarvisCore)
    {
        var so = new SerializedObject(controller);
        so.FindProperty("namingPanel").objectReferenceValue = namingPanel;
        so.FindProperty("nameInputField").objectReferenceValue = refsTuple.nameInput;
        so.FindProperty("confirmNameButton").objectReferenceValue = refsTuple.nameConfirm;
        so.FindProperty("nameVoiceButton").objectReferenceValue = refsTuple.nameVoice;
        so.FindProperty("nameHintText").objectReferenceValue = refsTuple.nameHint;
        so.FindProperty("hudRoot").objectReferenceValue = hudRoot;
        so.FindProperty("aiNameText").objectReferenceValue = refsTuple.aiName;
        so.FindProperty("recognizedText").objectReferenceValue = refsTuple.recognized;
        so.FindProperty("responseText").objectReferenceValue = refsTuple.response;
        so.FindProperty("statusText").objectReferenceValue = refsTuple.status;
        so.FindProperty("errorText").objectReferenceValue = refsTuple.error;
        so.FindProperty("pushToTalkButton").objectReferenceValue = refsTuple.ptt;
        so.FindProperty("coreGlow").objectReferenceValue = jarvisCore.GetComponent<CoreGlow>();
        so.FindProperty("particleAmbient").objectReferenceValue = jarvisCore.GetComponentInChildren<ParticleAmbient>();
        so.FindProperty("waveformBars").objectReferenceValue = jarvisCore.GetComponentInChildren<WaveformBars>();
        so.FindProperty("voiceBridge").objectReferenceValue = voiceBridge;
        var game = Object.FindFirstObjectByType<AiPetGamePrototype>();
        if (game != null)
        {
            so.FindProperty("gameBridge").objectReferenceValue = game;
        }

        var rings = new[]
        {
            jarvisCore.transform.Find("Ring1")?.GetComponent<RingRotator>(),
            jarvisCore.transform.Find("Ring2")?.GetComponent<RingRotator>(),
            jarvisCore.transform.Find("Ring3")?.GetComponent<RingRotator>()
        };
        var ringProp = so.FindProperty("ringRotators");
        ringProp.arraySize = rings.Length;
        for (var i = 0; i < rings.Length; i++)
        {
            ringProp.GetArrayElementAtIndex(i).objectReferenceValue = rings[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void SetPrivateSerialized(Object target, string fieldName, object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            return;
        }

        switch (prop.propertyType)
        {
            case SerializedPropertyType.ObjectReference:
                prop.objectReferenceValue = value as Object;
                break;
            case SerializedPropertyType.Boolean:
                prop.boolValue = value is bool b && b;
                break;
            case SerializedPropertyType.Float:
                prop.floatValue = value is float f ? f : 0f;
                break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateUi(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 size, Color color)
    {
        var go = CreateUi(name, parent);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return img;
    }

    private static Text CreateText(Transform parent, string name, string value, int fontSize, Vector2 anchoredPos, Vector2 size)
    {
        var go = CreateUi(name, parent);
        var text = go.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.78f, 0.93f, 1f, 1f);
        text.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        return text;
    }

    private static InputField CreateInputField(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        var go = CreateUi(name, parent);
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.09f, 0.15f, 0.25f, 0.95f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var placeholder = CreateText(go.transform, "Placeholder", "AI 이름", 30, Vector2.zero, new Vector2(size.x - 40, size.y - 20));
        placeholder.color = new Color(0.6f, 0.75f, 0.9f, 0.6f);
        var inputText = CreateText(go.transform, "Text", "", 34, Vector2.zero, new Vector2(size.x - 40, size.y - 20));

        var input = go.AddComponent<InputField>();
        input.targetGraphic = bg;
        input.placeholder = placeholder;
        input.textComponent = inputText;
        return input;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
    {
        var go = CreateUi(name, parent);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.06f, 0.56f, 1f, 1f);
        var button = go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        var txt = CreateText(go.transform, "Label", label, 34, Vector2.zero, size);
        txt.color = Color.white;
        return button;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SaveOrUpdatePrefab(GameObject coreRoot)
    {
        var clone = Object.Instantiate(coreRoot);
        clone.name = "JARVIS_Core";
        PrefabUtility.SaveAsPrefabAsset(clone, PrefabPath);
        Object.DestroyImmediate(clone);
    }

    private static void CreateDefaultSprites()
    {
        WriteTexture("core_disc", BuildRadialTexture(256, new Color(0.68f, 0.95f, 1f, 1f), 0.88f));
        WriteTexture("ring_thin", BuildRingTexture(256, 0.72f, 0.78f, new Color(1f, 1f, 1f, 1f)));
        WriteTexture("bg_gradient", BuildVerticalGradient(32, 512, new Color(0.02f, 0.03f, 0.06f), new Color(0.05f, 0.10f, 0.20f)));
    }

    private static void WriteTexture(string name, Texture2D tex)
    {
        var path = $"{SpriteDir}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/{name}.png");
    }

    private static Texture2D BuildRadialTexture(int size, Color color, float radiusRatio)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var c = (size - 1) * 0.5f;
        var radius = c * Mathf.Clamp(radiusRatio, 0.1f, 1f);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - c;
                var dy = y - c;
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                var t = Mathf.Clamp01(d / radius);
                var outColor = color;
                outColor.a = 1f - Mathf.SmoothStep(0.75f, 1f, t);
                tex.SetPixel(x, y, outColor);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D BuildRingTexture(int size, float innerRatio, float outerRatio, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var c = (size - 1) * 0.5f;
        var radius = c;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - c;
                var dy = y - c;
                var r = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                var onRing = r >= innerRatio && r <= outerRatio;
                var outColor = new Color(0f, 0f, 0f, 0f);
                if (onRing)
                {
                    outColor = color;
                    outColor.a = 1f;
                }
                tex.SetPixel(x, y, outColor);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D BuildVerticalGradient(int width, int height, Color bottom, Color top)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
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
        return tex;
    }

    private static void CreateDefaultMaterial()
    {
        var path = $"{MaterialDir}/JARVIS_UI_Default.mat";
        if (File.Exists(path))
        {
            return;
        }
        var shader = Shader.Find("UI/Default");
        var mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, path);
    }
}
