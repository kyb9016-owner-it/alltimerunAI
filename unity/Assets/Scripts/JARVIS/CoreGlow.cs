using UnityEngine;
using UnityEngine.UI;

namespace AllTimeRunAI.Jarvis
{
    public class CoreGlow : MonoBehaviour
    {
        [SerializeField] private RectTransform coreTransform;
        [SerializeField] private Graphic[] glowLayers;
        [SerializeField] private float basePulseSpeed = 1.6f;
        [SerializeField] private float basePulseAmount = 0.05f;
        [SerializeField] private float boostDuration = 0.5f;

        private float timeValue;
        private float boostTimer;
        private float externalBoost;
        private float externalSpeedBonus;
        private Vector3 baseScale = Vector3.one;

        private void Awake()
        {
            if (coreTransform != null)
            {
                baseScale = coreTransform.localScale;
            }
        }

        private void Update()
        {
            var dt = Time.unscaledDeltaTime;
            if (boostTimer > 0f)
            {
                boostTimer = Mathf.Max(0f, boostTimer - dt);
            }

            timeValue += dt;
            var tapBoost = boostTimer > 0f ? boostTimer / boostDuration : 0f;
            var totalBoost = Mathf.Clamp01(Mathf.Max(tapBoost, externalBoost));
            var speed = basePulseSpeed + externalSpeedBonus + totalBoost * 2.2f;
            var pulse = 1f + Mathf.Sin(timeValue * speed) * (basePulseAmount + totalBoost * 0.045f);

            if (coreTransform != null)
            {
                coreTransform.localScale = baseScale * pulse;
            }

            for (var i = 0; i < glowLayers.Length; i++)
            {
                var g = glowLayers[i];
                if (g == null) continue;
                var c = g.color;
                c.a = Mathf.Lerp(0.26f, 0.9f, totalBoost) + Mathf.Sin(timeValue * (1.8f + i * 0.25f)) * 0.03f;
                c.a = Mathf.Clamp01(c.a);
                g.color = c;
            }
        }

        public void TriggerTapBoost()
        {
            boostTimer = boostDuration;
        }

        public void SetExternalBoost(float boost01, float speedBonus)
        {
            externalBoost = Mathf.Clamp01(boost01);
            externalSpeedBonus = Mathf.Max(0f, speedBonus);
        }
    }
}
