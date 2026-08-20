using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace AllTimeRunAI.Jarvis.Editor
{
    public static class TmpFontAutoFixer
    {
        [MenuItem("Tools/JARVIS/Fix Missing TMP Fonts")]
        public static void FixMissingTmpFonts()
        {
            TMP_FontAsset defaultFont = null;
            try
            {
                defaultFont = TMP_Settings.defaultFontAsset;
            }
            catch
            {
                // TMP settings asset not initialized yet.
            }

            if (defaultFont == null)
            {
                defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }

            if (defaultFont == null)
            {
                Debug.LogError("[TMP Fix] Default TMP font not found. Run: Window > TextMeshPro > Import TMP Essential Resources");
                return;
            }

            var fixedCount = 0;
            var sceneCount = SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                var roots = scene.GetRootGameObjects();
                for (var r = 0; r < roots.Length; r++)
                {
                    var texts = roots[r].GetComponentsInChildren<TMP_Text>(true);
                    for (var t = 0; t < texts.Length; t++)
                    {
                        var tmp = texts[t];
                        if (tmp == null || tmp.font != null)
                        {
                            continue;
                        }
                        Undo.RecordObject(tmp, "Fix TMP Font");
                        tmp.font = defaultFont;
                        EditorUtility.SetDirty(tmp);
                        fixedCount++;
                    }
                }
            }

            if (fixedCount > 0)
            {
                EditorSceneManager.MarkAllScenesDirty();
                Debug.Log("[TMP Fix] Assigned default TMP font to " + fixedCount + " object(s).");
            }
        }

        [InitializeOnLoadMethod]
        private static void AutoRunOncePerReload()
        {
            // Auto-fix on script reload so the editor stops spamming mesh/font warnings.
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }
                TMP_FontAsset defaultFont = null;
                try
                {
                    defaultFont = TMP_Settings.defaultFontAsset;
                }
                catch
                {
                    // TMP settings not ready.
                }

                if (defaultFont == null &&
                    Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") == null)
                {
                    // Skip until TMP essentials are imported.
                    return;
                }
                FixMissingTmpFonts();
            };
        }
    }
}
