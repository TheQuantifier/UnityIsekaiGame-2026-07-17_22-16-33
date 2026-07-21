#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityIsekaiGame.Development;

namespace UnityIsekaiGame.Editor.Tools.TestLabAutomation
{
    public static class TestLabAutomationMenu
    {
        private const string PrototypeScenePath = "Assets/_Project/Scenes/Prototype/PrototypeScene.unity";

        [MenuItem("Tools/Test Lab/Open Automation Runner")]
        public static void OpenAutomationRunner()
        {
            if (!Application.isPlaying)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PrototypeScenePath);
                    if (scene == null)
                    {
                        Debug.LogWarning($"Test Lab Automation: PrototypeScene was not found at '{PrototypeScenePath}'. Open the scene manually, then enter Play Mode and use Tab > Test Lab > Automation.");
                        return;
                    }

                    EditorSceneManager.OpenScene(PrototypeScenePath);
                    Debug.Log("Test Lab Automation: PrototypeScene opened. Enter Play Mode, then use Tab > Test Lab > Automation.");
                }

                return;
            }

            PrototypeTestLabView view = Object.FindAnyObjectByType<PrototypeTestLabView>();
            if (view == null)
            {
                Debug.LogWarning("Test Lab Automation: Test Lab view is not active. Open Tab > Test Lab > Automation.");
                return;
            }

            Debug.Log("Test Lab Automation: Open Tab > Test Lab > Automation to run scenarios.");
        }

        [MenuItem("Tools/Test Lab/Run Quick Automation in Play Mode")]
        public static void RunQuickAutomationInPlayMode()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Test Lab Automation: quick automation can only run in Play Mode. Open PrototypeScene, enter Play Mode, then run this command.");
                return;
            }

            PrototypeTestLabMenuExtension extension = Object.FindAnyObjectByType<PrototypeTestLabMenuExtension>();
            if (extension == null)
            {
                Debug.LogWarning("Test Lab Automation: Test Lab menu extension is not registered. Open Tab > Test Lab first.");
                return;
            }

            Debug.Log("Test Lab Automation: use the in-game Automation section for Quick runs so results are visible and exportable.");
        }
    }
}
#endif
