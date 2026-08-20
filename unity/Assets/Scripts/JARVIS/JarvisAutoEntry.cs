using UnityEngine;

namespace AllTimeRunAI.Jarvis
{
    public static class JarvisAutoEntry
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureEntryPoint()
        {
            var existing = Object.FindFirstObjectByType<JarvisUIController>();
            if (existing != null)
            {
                return;
            }

            var go = new GameObject("JARVIS_AutoEntry");
            go.AddComponent<JarvisUIController>();
        }
    }
}
