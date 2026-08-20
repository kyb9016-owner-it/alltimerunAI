using UnityEngine;

namespace AllTimeRunAI.Jarvis
{
    /// <summary>
    /// Figma/JARVIS HUD가 씬에 있을 때만 레거시 프로토타입(Home/Run/Result 패널)을 숨깁니다.
    /// JARVIS HUD가 없으면 프로토타입이 그대로 보이도록 동작합니다.
    /// </summary>
    public static class DisableLegacyPrototypeOnFigmaHud
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void DisableLegacyPrototype()
        {
            // JARVIS HUD가 씬에 있을 때만 레거시 프로토타입 비활성화 (프로토타입만 있는 씬에서는 화면 유지)
            var useFigmaHud = Object.FindFirstObjectByType<JarvisHudBootstrap>() != null;
            if (!useFigmaHud)
            {
                return;
            }

            var legacy = Object.FindFirstObjectByType<AiPetGamePrototype>();
            if (legacy != null && legacy.enabled)
            {
                legacy.enabled = false;
            }

            SetInactiveIfExists("HomePanel");
            SetInactiveIfExists("RunPanel");
            SetInactiveIfExists("ResultPanel");
            SetInactiveIfExists("ShopPanel");
        }

        private static void SetInactiveIfExists(string name)
        {
            var go = GameObject.Find(name);
            if (go != null && go.activeSelf)
            {
                go.SetActive(false);
            }
        }
    }
}
