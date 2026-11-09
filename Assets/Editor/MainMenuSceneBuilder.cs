using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Replaces the legacy MainMenu scene with the new title screen bootstrap.</summary>
public static class MainMenuSceneBuilder
{
    [MenuItem("Tools/Starlight Guardian/Build Main Menu")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("Main Menu");
        MainMenuController menu = root.AddComponent<MainMenuController>();
        var background = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Title Screen/Title Screen Illustration.png");
        var field = typeof(MainMenuController).GetField("titleBackground", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.SetValue(menu, background);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("MainMenu rebuilt with the title screen controller and illustration.");
    }
}
