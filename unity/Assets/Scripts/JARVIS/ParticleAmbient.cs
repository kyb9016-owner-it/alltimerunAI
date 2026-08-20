using UnityEngine;

namespace AllTimeRunAI.Jarvis
{
    public class ParticleAmbient : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particleSystemRef;
        [SerializeField] private float idleRate = 5f;
        [SerializeField] private float activeRate = 14f;
        [SerializeField] private float smooth = 8f;

        private float intensity;
        private float currentRate;

        private void Awake()
        {
            currentRate = idleRate;
            ApplyRate(currentRate);
        }

        private void Update()
        {
            var target = Mathf.Lerp(idleRate, activeRate, intensity);
            currentRate = Mathf.Lerp(currentRate, target, Time.unscaledDeltaTime * smooth);
            ApplyRate(currentRate);
        }

        public void SetIntensity(float value)
        {
            intensity = Mathf.Clamp01(value);
        }

        private void ApplyRate(float rate)
        {
            if (particleSystemRef == null)
            {
                return;
            }
            var emission = particleSystemRef.emission;
            emission.rateOverTime = rate;
        }
    }
}
