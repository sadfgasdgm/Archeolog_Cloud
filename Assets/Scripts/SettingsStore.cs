using UnityEngine;


public static class SettingsStore
{
    public static event System.Action Committed;

    static bool loaded;
    static float sound, music, sens;
    static int graphics, vibration;

    public static float Sound { get { Ensure(); return sound; } set { sound = value; } }
    public static float Music { get { Ensure(); return music; } set { music = value; } }
    public static float Sens { get { Ensure(); return sens; } set { sens = value; } }
    public static int Graphics { get { Ensure(); return graphics; } set { graphics = Mathf.Clamp(value, 0, 2); } }   // 0 низкое, 1 среднее, 2 высокое
    public static int Vibration { get { Ensure(); return vibration; } set { vibration = Mathf.Clamp(value, 0, 3); } }  // 0 выкл 1 низкая 3 высокая

    public static string[] GraphicsNames { get { Ensure(); return new[] { "Низкое", "Среднее", "Высокое" }; } }
    public static string[] VibrationNames { get { Ensure(); return new[] { "Выкл", "Низкая", "Средняя", "Высокая" }; } }

    static void Ensure()
    {
        if (loaded) return;
        loaded = true;
        sound = PlayerPrefs.GetFloat("Sound", 1f);
        music = PlayerPrefs.GetFloat("Music", 1f);          // по умолчанию 100%
        sens = PlayerPrefs.GetFloat("Sens", 0.275f);       // среднее положение
        graphics = PlayerPrefs.GetInt("Graphics", 1);
        vibration = PlayerPrefs.GetInt("Vibration", 1);
    }

    // отменить несохранённые изменения снова читаем значения из крестик/Назад
    public static void Reload()
    {
        loaded = false;
        Ensure();
    }

    // применить и сохранить всё разом
    public static void Commit()
    {
        Ensure();
        AudioListener.volume = sound;
        PlayerPrefs.SetFloat("Sound", sound);
        PlayerPrefs.SetFloat("Music", music);
        PlayerPrefs.SetFloat("Sens", sens);
        ApplyGraphics(graphics);
        PlayerPrefs.SetInt("Graphics", graphics);
        PlayerPrefs.SetInt("Vibration", vibration);
        PlayerPrefs.Save();

        var cam = Object.FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.SetSensitivity(sens);

        Committed?.Invoke();
    }

    static void ApplyGraphics(int g)
    {
        QualitySettings.shadows = g == 0 ? ShadowQuality.Disable : g == 1 ? ShadowQuality.HardOnly : ShadowQuality.All;
        QualitySettings.pixelLightCount = g == 0 ? 4 : g == 1 ? 8 : 16;


        object pipe = null;
        var qs = typeof(QualitySettings);
        foreach (var name in new[] { "renderPipeline", "currentRenderPipeline" })
        {
            var p = qs.GetProperty(name);
            if (p != null) { pipe = p.GetValue(null); if (pipe != null) break; }
        }
        if (pipe != null)
        {
            var prop = pipe.GetType().GetProperty("shadowResolution");
            if (prop != null) prop.SetValue(pipe, g == 0 ? 25f : g == 1 ? 51f : 102f, null);
        }
    }

    // виброреакция на телефоне
    public static void Vibrate(int times = 1)
    {
        Ensure();
        if (vibration == 0) return;

#if UNITY_ANDROID || UNITY_IOS
        if (Application.platform != RuntimePlatform.Android) return;
        for (int i = 0; i < times; i++) Handheld.Vibrate();
#endif
    }
}
