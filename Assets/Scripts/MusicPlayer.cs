using UnityEngine;
using UnityEngine.SceneManagement;

// Спокойная фоновая музыка меню и экспедиций
public class MusicPlayer : MonoBehaviour
{
    static MusicPlayer inst;
    AudioSource src;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (inst != null) return;
        var go = new GameObject("MusicPlayer");
        DontDestroyOnLoad(go);
        inst = go.AddComponent<MusicPlayer>();
    }

    void Awake()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.clip = Build();
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
        src.Play();
    }

    void Update()
    {
        // на экране загрузки тихо в остальных сценах  по настройке Музыка
        bool play = SceneManager.GetActiveScene().name != "Loading";
        float want = play ? SettingsStore.Music * 0.35f : 0f;
        // общая громкость 
        float master = Mathf.Max(AudioListener.volume, 0.0001f);
        src.volume = Mathf.MoveTowards(src.volume, Mathf.Clamp01(want / master), Time.unscaledDeltaTime * 0.8f);
    }

    // мягкий пэд
    static AudioClip Build()
    {
        const int rate = 22050;
        const float seg = 6f;
        float[][] chords =
        {
            new[] { 220.00f, 261.63f, 329.63f },
            new[] { 174.61f, 220.00f, 261.63f },
            new[] { 261.63f, 329.63f, 392.00f },
            new[] { 196.00f, 246.94f, 293.66f },
        };
        int n = (int)(rate * seg * chords.Length);
        var d = new float[n];

        for (int k = 0; k < chords.Length; k++)
        {
            int start = (int)((k * seg - 1.5f) * rate);
            int len = (int)(9f * rate);
            foreach (float f in chords[k])
            {
                for (int i = 0; i < len; i++)
                {
                    float t = i / (float)rate;
                    float u = i / (float)len;
                    float env = Mathf.Sin(Mathf.PI * u);
                    env *= env;
                    float s = Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * f * 1.003f * t) * 0.4f;
                    int idx = ((start + i) % n + n) % n;
                    d[idx] += s * env * 0.12f;
                }
            }
        }

        var rnd = new System.Random(7);
        float[] pent = { 440f, 523.25f, 659.25f, 784f, 880f };
        for (int b = 0; b < 16; b++)
        {
            float tt = b * 1.5f + (float)rnd.NextDouble() * 0.4f;
            float f = pent[rnd.Next(pent.Length)];
            int st = (int)(tt * rate);
            int len = (int)(3f * rate);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)rate;
                float env = (1f - Mathf.Exp(-t * 60f)) * Mathf.Exp(-t * 1.8f);
                d[(st + i) % n] += Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.06f;
            }
        }

        float peak = 0.001f;
        for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(d[i]));
        for (int i = 0; i < n; i++) d[i] = d[i] / peak * 0.6f;

        var c = AudioClip.Create("calmMusic", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }
}
