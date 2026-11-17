#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Создаёт Expedition2/3/4 на базе текущей Expedition1,
/// сохраняя игрока, камеры, UI, EventSystem и остальные общие объекты.</summary>
public static class CreateExpeditionScenes
{
    [MenuItem("Archeolog/Create Expedition 2-4 Scenes")]
    public static void CreateScenes()
    {
        const string basePath = "Assets/Scenes/Expedition1.unity";
        if (!System.IO.File.Exists(basePath))
        {
            EditorUtility.DisplayDialog("Экспедиции", "Не найдена сцена Assets/Scenes/Expedition1.unity", "OK");
            return;
        }

        string[] names = { "Expedition2", "Expedition3", "Expedition4" };
        ProceduralExpeditionBuilder.ExpeditionZone[] zones = {
            ProceduralExpeditionBuilder.ExpeditionZone.ForestSettlement,
            ProceduralExpeditionBuilder.ExpeditionZone.RockySlope,
            ProceduralExpeditionBuilder.ExpeditionZone.UndergroundChamber
        };

        for (int i = 0; i < names.Length; i++)
        {
            var scene = EditorSceneManager.OpenScene(basePath, OpenSceneMode.Single);

            // Удаляем генератор старой песчаной карты, но оставляем игрока/камеру/UI.
            foreach (var oldBuilder in Object.FindObjectsByType<LevelBuilder>(FindObjectsSortMode.None))
                Object.DestroyImmediate(oldBuilder.gameObject);

            // Сбрасываем временные раскопы/точки, если они были размещены прямо в сцене.
            foreach (var oldSite in Object.FindObjectsByType<DigSite>(FindObjectsSortMode.None))
                Object.DestroyImmediate(oldSite.gameObject);

            var host = new GameObject("ExpeditionWorldBuilder");
            var builder = host.AddComponent<ProceduralExpeditionBuilder>();
            builder.zone = zones[i];
            builder.seed = 22 + i;
            builder.buildOnStart = true;
            builder.placeExistingPlayer = true;

            string path = "Assets/Scenes/" + names[i] + ".unity";
            EditorSceneManager.SaveScene(scene, path);
        }

        // Обновляем Build Settings: новые сцены добавляются после первой экспедиции.
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (string n in names)
        {
            string p = "Assets/Scenes/" + n + ".unity";
            string guid = AssetDatabase.AssetPathToGUID(p);
            if (!string.IsNullOrEmpty(guid))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                bool exists = list.Exists(x => x.path == assetPath);
                if (!exists) list.Add(new EditorBuildSettingsScene(assetPath, true));
            }
        }
        EditorBuildSettings.scenes = list.ToArray();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Экспедиции созданы", "Созданы сцены Expedition2, Expedition3 и Expedition4. Откройте каждую сцену и проверьте генерацию в Play Mode.", "OK");
    }
}
#endif


