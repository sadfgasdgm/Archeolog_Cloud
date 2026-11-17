using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class ScannerSound : MonoBehaviour
{
    [SerializeField] Scanner scanner;
    [SerializeField, Range(0f, 1f)] float volume = 0.6f;
    [SerializeField] bool idleTick = true;   // тихий тик когда сигнала нет

    AudioSource src;
    AudioClip weak, medium, strong, exact, idle, mark;
    float timer;
    int foundCount;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        if (scanner == null) scanner = GetComponent<Scanner>();

        weak = Beep(520f, 0.13f, 0.55f);
        medium = Beep(740f, 0.12f, 0.70f);
        strong = Beep(1000f, 0.10f, 0.85f);
        exact = Beep(1400f, 0.07f, 1.00f);
        idle = Beep(180f, 0.04f, 0.25f);
        mark = MarkSound();
    }

    void Update()
    {
        src.volume = SettingsStore.Sound * volume;

        // звук отметки точки считаем найденные точки
        int found = 0;
        foreach (var p in ScanPoint.All) if (p.Found) found++;
        if (found > foundCount) src.PlayOneShot(mark, volume);
        foundCount = found;

        if (scanner == null || !scanner.IsOn) { timer = 0f; return; }

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        switch (scanner.Signal)
        {
            case SignalLevel.Weak:
                src.PlayOneShot(weak, volume);
                timer = 1.2f;
                break;
            case SignalLevel.Medium:
                src.PlayOneShot(medium, volume);
                timer = 0.6f;
                break;
            case SignalLevel.Strong:
                src.PlayOneShot(strong, volume);
                timer = 0.28f;
                break;
            case SignalLevel.Exact:
                src.PlayOneShot(exact, volume);
                timer = 0.11f;
                break;
            default:
                if (idleTick) src.PlayOneShot(idle, volume * 0.5f);
                timer = 2.0f;
                break;
        }
    }

    // писк основной тон + лёгкая гармоника плавное нарастание и затухание
    static AudioClip Beep(float freq, float length, float gain)
    {
        const int rate = 44100;
        int n = (int)(rate * length);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float attack = Mathf.Clamp01(i / (rate * 0.005f));
            float release = Mathf.Clamp01((n - i) / (rate * 0.03f));
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t)
                       + 0.25f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
            data[i] = wave * attack * release * gain * 0.4f;
        }
        var clip = AudioClip.Create("beep" + freq, n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // мягкий двойной сигнал при отметке точки
    static AudioClip MarkSound()
    {
        const int rate = 44100;
        float length = 0.45f;
        int n = (int)(rate * length);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float freq = t < 0.2f ? 660f : 990f;
            float local = t < 0.2f ? t : t - 0.2f;
            float attack = Mathf.Clamp01(local / 0.01f);
            float release = Mathf.Clamp01(1f - local / 0.25f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * attack * release * 0.45f;
        }
        var clip = AudioClip.Create("mark", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
