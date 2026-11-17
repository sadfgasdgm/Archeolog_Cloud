using UnityEngine;


public class GameSounds : MonoBehaviour
{
    [SerializeField] float stepVolume = 0.18f;

    AudioSource sfx;
    AudioClip stepA, stepB, spade, scraper, brush, failSnd, extractSnd, cleanSnd, joinSnd;
    float stepTimer, stepFlip;
    PlayerController player;
    CharacterController cc;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (FindAnyObjectByType<PlayerController>() == null) return;
        if (FindAnyObjectByType<GameSounds>() != null) return;
        new GameObject("GameSounds").AddComponent<GameSounds>();
    }

    void OnEnable()
    {
        DigSite.OnToolHit += OnToolHit;
        DigSite.OnFailed += OnFailedEv;
        DigSite.OnExtracted += OnExtractedEv;
        LabStation.OnCleaned += OnCleanedEv;
        LabStation.OnAssembled += OnAssembledEv;
    }

    void OnDisable()
    {
        DigSite.OnToolHit -= OnToolHit;
        DigSite.OnFailed -= OnFailedEv;
        DigSite.OnExtracted -= OnExtractedEv;
        LabStation.OnCleaned -= OnCleanedEv;
        LabStation.OnAssembled -= OnAssembledEv;
    }

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        cc = player != null ? player.GetComponent<CharacterController>() : null;

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f;

        stepA = Thump(95f, 0.12f, 0.9f);
        stepB = Thump(82f, 0.13f, 0.85f);
        spade = Thump(60f, 0.18f, 1.2f);
        scraper = Noise(0.10f, 0.5f, 2200f);
        brush = Noise(0.07f, 0.22f, 5200f);
        failSnd = Slide(220f, 90f, 0.55f, 0.5f);
        extractSnd = Chord(new[] { 523f, 659f, 784f }, 0.7f, 0.35f);
        cleanSnd = Slide(660f, 990f, 0.3f, 0.3f);
        joinSnd = Slide(440f, 660f, 0.18f, 0.4f);
    }

    void Update()
    {
        if (sfx != null) sfx.volume = 1f;   // Звуки уже учтены в AudioListener.volume (иначе громкость применялась бы дважды)

        // шаги
        bool digging = DigSite.Current != null && DigSite.Current.Active;
        if (cc != null && cc.isGrounded && cc.velocity.magnitude > 0.4f && !digging)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                sfx.PlayOneShot(stepFlip < 0.5f ? stepA : stepB, stepVolume);
                stepFlip = 1f - stepFlip;
                stepTimer = player != null && player.Fast ? 0.35f : 0.5f;
            }
        }
        else stepTimer = 0f;
    }

    // события 
    void OnToolHit(DigTool t)
    {
        switch (t)
        {
            case DigTool.Spade: sfx.PlayOneShot(spade, 0.9f); break;
            case DigTool.Scraper: sfx.PlayOneShot(scraper, 0.8f); break;
            case DigTool.Brush: sfx.PlayOneShot(brush, 0.7f); break;
        }
    }

    void OnFailedEv(DigSite s)
    {
        sfx.PlayOneShot(failSnd, 1f);
        SettingsStore.Vibrate(2);
    }

    void OnExtractedEv(DigSite s)
    {
        if (s.kind == ArtifactKind.Junk) return;
        sfx.PlayOneShot(extractSnd, 0.9f);
        SettingsStore.Vibrate(1);
    }

    void OnCleanedEv(ArtifactKind k) => sfx.PlayOneShot(cleanSnd, 0.8f);
    void OnAssembledEv(ArtifactKind k) => sfx.PlayOneShot(joinSnd, 0.9f);

    // синтез
    static AudioClip Thump(float freq, float length, float gain)
    {
        const int rate = 44100;
        int n = (int)(rate * length);
        var d = new float[n];
        float prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-t * 22f);
            float noise = (Random.value * 2f - 1f) * 0.35f;
            prev = prev * 0.7f + noise * 0.3f;
            d[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.8f + prev) * env * gain * 0.4f;
        }
        var c = AudioClip.Create("thump" + freq, n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    static AudioClip Noise(float length, float gain, float cutoff)
    {
        const int rate = 44100;
        int n = (int)(rate * length);
        var d = new float[n];
        float prev = 0f;
        float k = cutoff / 20000f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-t * 18f);
            float noise = Random.value * 2f - 1f;
            prev = prev + k * (noise - prev);
            d[i] = prev * env * gain * 0.5f;
        }
        var c = AudioClip.Create("noise" + cutoff, n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    static AudioClip Slide(float f0, float f1, float length, float gain)
    {
        const int rate = 44100;
        int n = (int)(rate * length);
        var d = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float k = t / length;
            float f = Mathf.Lerp(f0, f1, k);
            phase += 2f * Mathf.PI * f / rate;
            float env = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((length - t) / 0.12f);
            d[i] = Mathf.Sin(phase) * env * gain * 0.4f;
        }
        var c = AudioClip.Create("slide" + f0, n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    static AudioClip Chord(float[] freqs, float length, float gain)
    {
        const int rate = 44100;
        int n = (int)(rate * length);
        var d = new float[n];
        var phase = new float[freqs.Length];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Clamp01(t / 0.02f) * Mathf.Exp(-t * 4f);
            float s = 0f;
            for (int j = 0; j < freqs.Length; j++)
            {
                phase[j] += 2f * Mathf.PI * freqs[j] / rate;
                s += Mathf.Sin(phase[j]);
            }
            d[i] = s / freqs.Length * env * gain * 0.5f;
        }
        var c = AudioClip.Create("chord", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }
}
