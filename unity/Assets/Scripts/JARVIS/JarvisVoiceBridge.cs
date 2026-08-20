using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.iOS;

namespace AllTimeRunAI.Jarvis
{
    public class JarvisVoiceBridge : MonoBehaviour
    {
        [SerializeField] private JarvisUIController controller;
        [SerializeField] private string locale = "ko-KR";
        [SerializeField] private bool logDebug = false;

        private const string CallbackObjectName = "JarvisVoiceBridge";
        private bool initialized;
        private string EffectiveLocale => string.IsNullOrWhiteSpace(locale) ? "ko-KR" : locale;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void JarvisVoice_Init(string receiverObjectName, string localeCode);
        [DllImport("__Internal")] private static extern void JarvisVoice_StartListening();
        [DllImport("__Internal")] private static extern void JarvisVoice_StopListening();
        [DllImport("__Internal")] private static extern void JarvisVoice_Release();
#endif

        private void Awake()
        {
            gameObject.name = CallbackObjectName;
            if (controller == null)
            {
                controller = FindFirstObjectByType<JarvisUIController>();
            }

            InitializePlatformBridge();
        }

        public void BeginPushToTalk()
        {
            RequestPermissionsIfNeeded();
        }

        public void EndPushToTalk()
        {
            // reserved
        }

        public void StartListening()
        {
            if (!initialized)
            {
                InitializePlatformBridge();
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var bridge = new AndroidJavaClass("com.alltimerunai.voice.VoiceBridge");
                bridge.CallStatic("startListening");
            }
            catch (System.Exception ex)
            {
                OnVoiceError("Android 음성 시작 실패: " + ex.Message);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            JarvisVoice_StartListening();
#else
            if (logDebug) Debug.Log("[JarvisVoiceBridge] Mock StartListening");
#endif
        }

        public void StopListening()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var bridge = new AndroidJavaClass("com.alltimerunai.voice.VoiceBridge");
                bridge.CallStatic("stopListening");
            }
            catch (System.Exception ex)
            {
                OnVoiceError("Android 음성 종료 실패: " + ex.Message);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            JarvisVoice_StopListening();
#else
            if (logDebug) Debug.Log("[JarvisVoiceBridge] Mock StopListening");
#endif
        }

        private void InitializePlatformBridge()
        {
            if (initialized)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var bridge = new AndroidJavaClass("com.alltimerunai.voice.VoiceBridge");
                bridge.CallStatic("init", activity, CallbackObjectName, EffectiveLocale);
            }
            catch (System.Exception ex)
            {
                OnVoiceError("Android 브리지 초기화 실패: " + ex.Message);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            JarvisVoice_Init(CallbackObjectName, EffectiveLocale);
#else
            if (logDebug)
            {
                Debug.Log("[JarvisVoiceBridge] Editor mock init locale: " + EffectiveLocale);
            }
#endif
            initialized = true;
        }

        private void RequestPermissionsIfNeeded()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Application.RequestUserAuthorization(UserAuthorization.Microphone);
            }
#endif
        }

        private void OnDestroy()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var bridge = new AndroidJavaClass("com.alltimerunai.voice.VoiceBridge");
                bridge.CallStatic("release");
            }
            catch
            {
                // ignore on shutdown
            }
#elif UNITY_IOS && !UNITY_EDITOR
            JarvisVoice_Release();
#endif
        }

        // Called by native plugin via UnitySendMessage
        public void OnVoiceFinalText(string text)
        {
            if (logDebug) Debug.Log("[JarvisVoiceBridge] Final: " + text);
            controller?.OnVoiceFinalText(text);
        }

        // Called by native plugin via UnitySendMessage
        public void OnVoicePartialText(string text)
        {
            if (logDebug) Debug.Log("[JarvisVoiceBridge] Partial: " + text);
            controller?.OnVoicePartialText(text);
        }

        // Called by native plugin via UnitySendMessage ("Listening|msg")
        public void OnVoiceState(string payload)
        {
            if (logDebug) Debug.Log("[JarvisVoiceBridge] State: " + payload);
            controller?.OnVoiceState(payload);
        }

        // Called by native plugin via UnitySendMessage
        public void OnVoiceError(string error)
        {
            if (logDebug) Debug.LogWarning("[JarvisVoiceBridge] Error: " + error);
            controller?.OnVoiceError(error);
        }
    }
}
