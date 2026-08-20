using UnityEngine;
using UnityEngine.UI;

public class JarvisHudBootstrap : MonoBehaviour
{
    [SerializeField] private bool buildOnStart = true;

    private void Start()
    {
        if (buildOnStart)
        {
            BuildHud();
        }
    }

    [ContextMenu("Build JARVIS HUD")]
    public void BuildHud()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        var root = EnsureRect("JARVIS_HUD", canvas.transform, new Vector2(420f, 420f), new Vector2(0f, 160f));
        var glow = CreateImage("Glow_Back", root, MakeRadialSprite(256, new Color(0.05f, 0.65f, 1f, 1f), 0.96f), new Vector2(380f, 380f), new Color(0.45f, 0.85f, 1f, 0.45f));
        var ringOuter = CreateImage("Ring_Outer", root, MakeRingSprite(256, 0.72f, 0.79f), new Vector2(330f, 330f), new Color(0.12f, 0.70f, 1f, 0.85f));
        var ringMid = CreateImage("Ring_Mid", root, MakeRingSprite(256, 0.58f, 0.64f), new Vector2(285f, 285f), new Color(0.45f, 0.88f, 1f, 0.8f));
        var ringInner = CreateImage("Ring_Inner", root, MakeRingSprite(256, 0.44f, 0.49f), new Vector2(235f, 235f), new Color(0.70f, 0.95f, 1f, 0.75f));
        var core = CreateImage("Core", root, MakeRadialSprite(256, new Color(0.62f, 0.98f, 1f, 1f), 0.62f), new Vector2(180f, 180f), Color.white);

        var particles = CreateAmbientParticles(root);
        var controller = root.gameObject.GetComponent<JarvisHudController>();
        if (controller == null)
        {
            controller = root.gameObject.AddComponent<JarvisHudController>();
        }

        controller.Configure(core.rectTransform, ringOuter.rectTransform, ringMid.rectTransform, ringInner.rectTransform, glow, particles);
    }

    private static RectTransform EnsureRect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var child = parent.Find(name);
        GameObject go;
        if (child == null)
        {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
        }
        else
        {
            go = child.gameObject;
        }

        var rect = go.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = go.AddComponent<RectTransform>();
        }
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;

        var rect = image.rectTransform;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        return image;
    }

    private static ParticleSystem CreateAmbientParticles(RectTransform parent)
    {
        var existing = parent.Find("Particles");
        if (existing != null)
        {
            var psExisting = existing.GetComponent<ParticleSystem>();
            if (psExisting != null)
            {
                return psExisting;
            }
        }

        var go = new GameObject("Particles");
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = 2.2f;
        main.startSpeed = 8f;
        main.startSize = 0.03f;
        main.maxParticles = 28;

        var emission = ps.emission;
        emission.rateOverTime = 6f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = 1.25f;
        shape.donutRadius = 0.18f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(0.52f, 0.92f, 1f), 0f), new GradientColorKey(new Color(0.12f, 0.72f, 1f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.2f), new GradientAlphaKey(0.9f, 0.75f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        return ps;
    }

    private static Sprite MakeRadialSprite(int size, Color color, float radiusRatio)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var center = (size - 1) * 0.5f;
        var radius = center * Mathf.Clamp(radiusRatio, 0.1f, 1f);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                var t = Mathf.Clamp01(d / radius);
                var c = color;
                c.a = 1f - Mathf.SmoothStep(0.75f, 1f, t);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite MakeRingSprite(int size, float innerRatio, float outerRatio)
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
                var c = new Color(1f, 1f, 1f, 0f);
                if (onRing)
                {
                    var edge = Mathf.Min(Mathf.Abs(r - innerRatio), Mathf.Abs(outerRatio - r));
                    c.a = Mathf.Clamp01(edge * 44f);
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

}
