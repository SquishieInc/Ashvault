#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ashvault.EditorTools
{
    public static class AshvaultSceneMenu
    {
        [MenuItem("Tools/Ashvault/Create or Rebuild Prototype Scene")]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("ASHVAULT - GAME DIRECTOR").AddComponent<Ashvault.AshGame>();
            if (!AssetDatabase.IsValidFolder("Assets/Ashvault/Scenes"))
                AssetDatabase.CreateFolder("Assets/Ashvault", "Scenes");
            const string path = "Assets/Ashvault/Scenes/Prototype.unity";
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            Selection.activeGameObject = GameObject.Find("ASHVAULT - GAME DIRECTOR");
            Debug.Log("Ashvault: generated Prototype.unity. Press Play; choose a class.");
        }
    }
}
#endif
