using UnityEngine;
using UnityEngine.UI;

namespace AllTimeRunAI.Jarvis
{
    public class WaveformBars : MonoBehaviour
    {
        [SerializeField] private RectTransform barsRoot;
        [SerializeField] private Image barPrefab;
        [SerializeField] private int barCount = 24;
        [SerializeField] private float orbitRadius = 180f;
        [SerializeField] private float minHeight = 10f;
        [SerializeField] private float maxHeight = 52f;
        [SerializeField] private float barWidth = 6f;
        [SerializeField] private float spinSpeed = 18f;
        [SerializeField] private Color barColor = new Color(0.58f, 0.9f, 1f, 0.8f);

        private RectTransform[] bars;
        private float[] seeds;
        private float intensity;
        private float angleOffset;

        private void Awake()
        {
            BuildBars();
        }

        private void Update()
        {
            if (bars == null || bars.Length == 0)
            {
                return;
            }

            angleOffset += Time.unscaledDeltaTime * spinSpeed;
            var t = Time.unscaledTime;
            var amp = Mathf.Lerp(0.35f, 1f, intensity);

            for (var i = 0; i < bars.Length; i++)
            {
                var rt = bars[i];
                var baseAngle = (360f / bars.Length) * i + angleOffset;
                var rad = baseAngle * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                var pos = dir * orbitRadius;

                rt.anchoredPosition = pos;
                rt.localRotation = Quaternion.Euler(0f, 0f, baseAngle + 90f);

                var wave = Mathf.PerlinNoise(seeds[i], t * 1.6f);
                var h = Mathf.Lerp(minHeight, maxHeight, wave * amp);
                rt.sizeDelta = new Vector2(barWidth, h);
            }
        }

        public void SetIntensity(float value)
        {
            intensity = Mathf.Clamp01(value);
        }

        private void BuildBars()
        {
            if (barsRoot == null || barPrefab == null)
            {
                return;
            }

            bars = new RectTransform[barCount];
            seeds = new float[barCount];

            for (var i = 0; i < barCount; i++)
            {
                var img = Instantiate(barPrefab, barsRoot);
                img.gameObject.SetActive(true);
                img.gameObject.name = "WaveBar_" + i;
                img.raycastTarget = false;
                img.color = barColor;
                var rt = img.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(barWidth, minHeight);

                bars[i] = rt;
                seeds[i] = 0.173f * (i + 1);
            }

            // Keep template hidden after runtime instantiation.
            barPrefab.gameObject.SetActive(false);
        }
    }
}
