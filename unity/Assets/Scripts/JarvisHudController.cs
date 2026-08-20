using UnityEngine;
using UnityEngine.UI;

public class JarvisHudController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform core;
    [SerializeField] private RectTransform ringOuter;
    [SerializeField] private RectTransform ringMid;
    [SerializeField] private RectTransform ringInner;
    [SerializeField] private Graphic glowBack;
    [SerializeField] private ParticleSystem ambientParticles;

    [Header("Motion")]
    [SerializeField] private float outerSpeed = -18f;
    [SerializeField] private float midSpeed = 28f;
    [SerializeField] private float innerSpeed = -42f;
    [SerializeField] private float pulseAmplitude = 0.04f;
    [SerializeField] private float pulseFrequency = 1.6f;

    [Header("Reactive")]
    [Range(0f, 1f)]
    [SerializeField] private float intensity = 0.35f;

    private float elapsed;
    private Vector3 coreBaseScale = Vector3.one;

    private void Awake()
    {
        if (core != null)
        {
            coreBaseScale = core.localScale;
        }
    }

    private void Update()
    {
        var dt = Time.unscaledDeltaTime;
        elapsed += dt;

        RotateRing(ringOuter, outerSpeed, dt);
        RotateRing(ringMid, midSpeed, dt);
        RotateRing(ringInner, innerSpeed, dt);

        if (core != null)
        {
            var pulse = 1f + Mathf.Sin(elapsed * pulseFrequency) * pulseAmplitude * (0.6f + intensity);
            core.localScale = coreBaseScale * pulse;
        }

        if (glowBack != null)
        {
            var alpha = Mathf.Lerp(0.25f, 0.7f, intensity) + Mathf.Sin(elapsed * 2.2f) * 0.05f;
            var c = glowBack.color;
            c.a = Mathf.Clamp01(alpha);
            glowBack.color = c;
        }

        if (ambientParticles != null)
        {
            var emission = ambientParticles.emission;
            emission.rateOverTime = Mathf.Lerp(3f, 14f, intensity);
        }
    }

    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
    }

    public void Configure(
        RectTransform coreRef,
        RectTransform ringOuterRef,
        RectTransform ringMidRef,
        RectTransform ringInnerRef,
        Graphic glowBackRef,
        ParticleSystem ambientParticlesRef
    )
    {
        core = coreRef;
        ringOuter = ringOuterRef;
        ringMid = ringMidRef;
        ringInner = ringInnerRef;
        glowBack = glowBackRef;
        ambientParticles = ambientParticlesRef;
        if (core != null)
        {
            coreBaseScale = core.localScale;
        }
    }

    private static void RotateRing(RectTransform ring, float speed, float dt)
    {
        if (ring == null)
        {
            return;
        }
        ring.Rotate(0f, 0f, speed * dt);
    }
}
