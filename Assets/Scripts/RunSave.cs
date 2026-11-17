using UnityEngine;


public static class RunSave
{
    // префикс текущей зон
    static string Z => "Z" + PlayerPrefs.GetInt("SelectedZone", 0) + "_";

    static float Get(string key) => PlayerPrefs.GetFloat(Z + key, -1f);
    static void Set(string key, float v) { PlayerPrefs.SetFloat(Z + key, Mathf.Max(0f, v)); PlayerPrefs.Save(); }

    public static bool HasExt(ArtifactKind k) => Get("RunExt_" + k) >= 0f;
    public static float GetExt(ArtifactKind k) => Mathf.Max(0f, Get("RunExt_" + k));
    public static void SetExt(ArtifactKind k, float v) => Set("RunExt_" + k, v);

    public static bool HasClean(ArtifactKind k) => Get("RunClean_" + k) >= 0f;
    public static float GetClean(ArtifactKind k) => Mathf.Max(0f, Get("RunClean_" + k));
    public static void SetClean(ArtifactKind k, float v) => Set("RunClean_" + k, v);

    // зоны 2-4 убрать одну принятую находку её перекапывают
    public static void ClearPart(ArtifactKind k)
    {
        PlayerPrefs.DeleteKey(Z + "RunExt_" + k);
        PlayerPrefs.DeleteKey(Z + "RunClean_" + k);
        PlayerPrefs.DeleteKey(Z + "RunAssembled");
        PlayerPrefs.Save();
    }

    public static string GetText(string key) => PlayerPrefs.GetString(Z + key, "");
    public static void SetText(string key, string v) { PlayerPrefs.SetString(Z + key, v); PlayerPrefs.Save(); }

    public static bool HasAssembled => Get("RunAssembled") >= 0f;
    public static float GetAssembled() => Mathf.Max(0f, Get("RunAssembled"));
    public static void SetAssembled(float v) => Set("RunAssembled", v);

    // начало нового прохождения принятые находки и отметки точек сбрасываются
    public static void Clear()
    {
        foreach (ArtifactKind k in System.Enum.GetValues(typeof(ArtifactKind)))
        {
            PlayerPrefs.DeleteKey(Z + "RunExt_" + k);
            PlayerPrefs.DeleteKey(Z + "RunClean_" + k);
        }
        PlayerPrefs.DeleteKey(Z + "RunAssembled");
        PlayerPrefs.DeleteKey(Z + "RunOpened");      // зона 4 открытые ниши
        PlayerPrefs.DeleteKey(Z + "RunTerraces");    // зона 3 обследованные террасы

        // отметки точек интереса 
        PlayerPrefs.DeleteKey("Found_ScanPoint");
        for (int i = 1; i <= 4; i++)
        {
            PlayerPrefs.DeleteKey("Found_ScanPoint_" + i);
            for (int copy = 1; copy <= 20; copy++)
                PlayerPrefs.DeleteKey("Found_ScanPoint_" + i + " (" + copy + ")");
        }
        // зоны 2-4 отметки точек 
        for (int z = 2; z <= 4; z++)
            for (int i = 1; i <= 6; i++)
            {
                string n = "Found_Z" + z + "_Point_" + i;
                PlayerPrefs.DeleteKey(n);
                PlayerPrefs.DeleteKey(n + "_A");
                PlayerPrefs.DeleteKey(n + "_B");
            }
        PlayerPrefs.Save();
    }
}
