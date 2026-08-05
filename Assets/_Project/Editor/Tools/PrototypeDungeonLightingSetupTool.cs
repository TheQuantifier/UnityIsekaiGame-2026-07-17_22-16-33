using UnityEditor;
using UnityEngine;
using UnityIsekaiGame.WorldEnvironment;

namespace UnityIsekaiGame.Editor
{
    public static class PrototypeDungeonLightingSetupTool
    {
        private const string DungeonPrefabPath = "Assets/_Project/Prototype/Prefabs/Buildings/PrototypeDungeon/Dungeon1.prefab";

        [MenuItem("Tools/Prototype Scene/Dungeon Kits/Apply Dungeon Darkness Profile")]
        public static void ApplyDungeonDarknessProfile()
        {
            var root = PrefabUtility.LoadPrefabContents(DungeonPrefabPath);
            try
            {
                var profile = root.GetComponent<PrototypeDungeonLightingProfile>();
                if (profile == null)
                {
                    profile = root.AddComponent<PrototypeDungeonLightingProfile>();
                }

                profile.Apply();
                EditorUtility.SetDirty(profile);
                PrefabUtility.SaveAsPrefabAsset(root, DungeonPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Applied dungeon darkness profile to {DungeonPrefabPath}.");
        }
    }
}
