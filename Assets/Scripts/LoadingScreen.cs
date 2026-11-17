using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class LoadingScreen : MonoBehaviour
{
    public static string Next;

    [SerializeField] string nextScene = "MainMenu";
    [SerializeField] float minShowTime = 3f;

    static readonly string[] Tips =
    {
        "Сначала найдите точку интереса сканером",
        "Лопата быстрая, но может повредить находку",
        "Рядом с предметом работайте кистью",
        "Таймера нет: торопиться не нужно",
        "Чем аккуратнее раскопки, тем выше сохранность"
    };

    Image fill, fire;
    TMP_Text percent, tip;
    CanvasGroup group;

    void Start()
    {
        Time.timeScale = 1f;
        Build();
        StartCoroutine(Run());
    }

    void Update()
    {
        if (fire != null)
        {
            float f = 0.75f + 0.25f * Mathf.PerlinNoise(Time.time * 6f, 0f);
            fire.color = new Color(1f, 0.55f * f + 0.2f, 0.1f, f);
            fire.rectTransform.localScale = new Vector3(1f, 0.8f + 0.4f * f, 1f);
        }
    }

    IEnumerator Run()
    {
        yield return Fade(0f, 1f, 0.6f);

        // что грузим Next
        string target;
        if (!string.IsNullOrEmpty(Next)) target = Next;
        else target = PlayerPrefs.GetInt("TutorialDone", 0) == 1 ? nextScene : "Training level";
        Next = null;

        var op = SceneManager.LoadSceneAsync(target);
        op.allowSceneActivation = false;

        float t = 0f, shown = 0f;
        while (shown < 1f)
        {
            t += Time.deltaTime;
            float real = Mathf.Clamp01(op.progress / 0.9f);
            float targetP = Mathf.Min(real, t / minShowTime);
            shown = Mathf.MoveTowards(shown, targetP, Time.deltaTime * 0.6f);
            SetProgress(shown);
            tip.text = Tips[(int)(t / 1.6f) % Tips.Length];
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
        yield return Fade(1f, 0f, 0.5f);
        op.allowSceneActivation = true;
    }

    IEnumerator Fade(float from, float to, float time)
    {
        for (float t = 0; t < time; t += Time.deltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        group.alpha = to;
    }

    void SetProgress(float p)
    {
        fill.rectTransform.anchorMax = new Vector2(p, 1f);
        percent.text = Mathf.RoundToInt(p * 100f) + "%";
    }

    //  интерфейс кодом 
    void Build()
    {
        var cgo = new GameObject("LoadingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = cgo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        group = cgo.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        var root = (RectTransform)cgo.transform;

        // небо
        var sky = R("Sky", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var skyImg = Img(sky, Color.white);
        skyImg.sprite = GradientSprite();

        // земля, палатка, костёр
        var ground = R("Ground", root, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 170), new Vector2(0, 340));
        Img(ground, new Color32(0x2B, 0x1D, 0x12, 255));

        var tent = R("Tent", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-330, 340), new Vector2(300, 300));
        tent.localRotation = Quaternion.Euler(0, 0, 45);
        Img(tent, new Color32(0x4A, 0x36, 0x24, 255));
        var door = R("Door", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-330, 345), new Vector2(60, 110));
        Img(door, new Color32(0x16, 0x0E, 0x08, 255));

        ground.SetAsLastSibling();   // земля закрывает низ палатки
        door.SetAsLastSibling();

        var glow = R("FireGlow", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(250, 350), new Vector2(46, 46));
        fire = Img(glow, new Color(1f, 0.6f, 0.1f));
        var logs = R("Logs", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(250, 322), new Vector2(110, 14));
        Img(logs, new Color32(0x3B, 0x24, 0x14, 255));

        // название
        var title = R("Title", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(1400, 180));
        Txt(title, "Археолог 3D", 150, new Color32(0xF7, 0xEB, 0xD0, 255));
        var sub = R("Subtitle", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -350), new Vector2(1200, 60));
        Txt(sub, "Спокойные экспедиции и аккуратные раскопки", 38, new Color32(0xE3, 0xC1, 0x8A, 255), FontStyles.Normal);

        // индикатор прогресса
        var barBg = R("BarBg", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(900, 26));
        Img(barBg, new Color(0, 0, 0, 0.55f));
        var f = R("Fill", barBg, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero);
        fill = Img(f, new Color32(0xF2, 0xA9, 0x2E, 255));

        var pc = R("Percent", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 160), new Vector2(300, 50));
        percent = Txt(pc, "0%", 36, new Color32(0xF7, 0xEB, 0xD0, 255));
        var tp = R("Tip", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 55), new Vector2(1400, 50));
        tip = Txt(tp, Tips[0], 32, new Color32(0xE3, 0xC1, 0x8A, 255), FontStyles.Italic);
    }

    static Sprite GradientSprite()
    {
        var tex = new Texture2D(1, 256, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color dusk = new Color32(0x1B, 0x2A, 0x4A, 255);
        Color horizon = new Color32(0xE8, 0x9A, 0x4B, 255);
        Color sand = new Color32(0x6B, 0x4A, 0x2A, 255);
        for (int y = 0; y < 256; y++)
        {
            float t = y / 255f;
            Color c = t < 0.4f ? Color.Lerp(sand, horizon, t / 0.4f)
                               : Color.Lerp(horizon, dusk, (t - 0.4f) / 0.6f);
            tex.SetPixel(0, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 256), new Vector2(0.5f, 0.5f));
    }

    static RectTransform R(string n, Transform p, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(n, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(p, false);
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    static Image Img(RectTransform rt, Color c)
    {
        var i = rt.gameObject.AddComponent<Image>();
        i.color = c;
        i.raycastTarget = false;
        return i;
    }

    static TMP_Text Txt(RectTransform rt, string s, float size, Color c, FontStyles st = FontStyles.Bold)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = s; t.fontSize = size; t.color = c; t.fontStyle = st;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }
}
