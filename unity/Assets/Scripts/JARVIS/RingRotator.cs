using UnityEngine;

namespace AllTimeRunAI.Jarvis
{
    public class RingRotator : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private float degreesPerSecond = 22f;
        [SerializeField] private bool clockwise = true;

        private float speedMultiplier = 1f;

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            var dir = clockwise ? -1f : 1f;
            var delta = dir * degreesPerSecond * speedMultiplier * Time.unscaledDeltaTime;
            target.Rotate(0f, 0f, delta);
        }

        public void SetSpeedMultiplier(float value)
        {
            speedMultiplier = Mathf.Clamp(value, 0.2f, 3f);
        }
    }
}
