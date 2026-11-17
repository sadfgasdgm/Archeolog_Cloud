using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StartScreenBuilder
{
    const string Dir = "Assets/Scenes/";

    [MenuItem("Tools/Build Start Screen")]
    static void Build()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Остановите Play."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x1B, 0x14, 0x0E, 255);
        }
        new GameObject("LoadingScreen").AddComponent<LoadingScreen>();
        EditorSceneManager.SaveScene(scene, Dir + "Loading.unity");

        var list = new List<EditorBuildSettingsScene>();
        foreach (var n in new[] { "Loading", "MainMenu", "Expedition1" })
            if (File.Exists(Dir + n + ".unity"))
                list.Add(new EditorBuildSettingsScene(Dir + n + ".unity", true));
        EditorBuildSettings.scenes = list.ToArray();

        PlayFromStart();
        Debug.Log("Сцена Loading создана, порядок сцен в сборке настроен.");
    }

    [MenuItem("Tools/Play From Start Screen")]
    static void PlayFromStart()
    {
        EditorSceneManager.playModeStartScene =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(Dir + "Loading.unity");
    }

    [MenuItem("Tools/Play From Current Scene")]
    static void PlayCurrent() => EditorSceneManager.playModeStartScene = null;
}