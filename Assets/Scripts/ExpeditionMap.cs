using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

 
public static class ExpeditionMap
{
    struct Zone { public string name, desc; public int finds; public string scene; }

    static readonly Zone[] Zones =
    {
        new Zone { name = "Песчаная стоянка",   desc = "Открытый песчаный участок. Чаша и табличка из двух фрагментов.", finds = 2, scene = "Expedition1" },
        new Zone { name = "Лесное поселение",   desc = "Корни и хрупкая находка. Бронзовая пряжка и сосуд.",            finds = 2, scene = "Expedition2" },
        new Zone { name = "Каменистый склон",   desc = "Три террасы и декоративная панель из 5 фрагментов.",           finds = 1, scene = "Expedition3" },
        new Zone { name = "Подземная камера",   desc = "Пещера с нишами. Церемониальный диск из 6 фрагментов.",        finds = 1, scene = "Expedition4" },
    };

    // когда уровень готов  добавьте его сцену сюда
    static readonly string[] ReadyScenes = { "Expedition1", "Expedition2", "Expedition3", "Expedition4" };

    static bool Ready(string scene)
    {
        for (int i = 0; i < ReadyScenes.Length; i++)
            if (ReadyScenes[i] == scene) return true;
        return false;
    }

    static readonly Color Dark = new Color32(0x2B, 0x1D, 0x12, 245);
    static readonly Color Card = new Color32(0x4A, 0x36, 0x24, 255);
    static readonly Color Sand = new Color32(0xE3, 0xC1, 0x8A, 255);
    static readonly Color Amber = new Color32(0xF2, 0xA9, 0x2E, 255);
    static readonly Color Cream = new Color32(0xF7, 0xEB, 0xD0, 255);
    static readonly Color Ink = new Color32(0x3B, 0x2A, 0x1E, 255);
    static readonly Color Locked = new Color32(0x3A, 0x2E, 0x24, 255);
    static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);

    public static string SceneOf(int zone) => Zones[Mathf.Clamp(zone, 0, Zones.Length - 1)].scene;
    public static string NameOf(int zone) => Zones[Mathf.Clamp(zone, 0, Zones.Length - 1)].name;
    public static int Count => Zones.Length;
    public static bool IsReady(int zone) => Ready(Zones[Mathf.Clamp(zone, 0, Zones.Length - 1)].scene);

    public static int Stars(int zone) => PlayerPrefs.GetInt("ZoneStars" + zone, 0);

    // зона доступна если это первая или предыдущая пройдена
    public static bool Available(int zone) => zone == 0 || Stars(zone - 1) > 0;

    public static void SaveResult(int zone, int stars, float avgIntegrity)
    {
        if (stars > Stars(zone)) PlayerPrefs.SetInt("ZoneStars" + zone, stars);
        if (avgIntegrity > PlayerPrefs.GetFloat("ZoneBest" + zone, 0f)) PlayerPrefs.SetFloat("ZoneBest" + zone, avgIntegrity);
        PlayerPrefs.Save();
    }

    public static GameObject Show(Transform parent, UnityAction<int> onStart, UnityAction onBack)
    {
        if (parent == null)
        {
            var cgo = new GameObject("MapCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = cgo.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 200;
            var sc = cgo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            parent = cgo.transform;
        }

        // после обучения первая зона уже выбрана иначе берём сохранённый выбор, если он доступен
        int selected = PlayerPrefs.GetInt("SelectedZone", 0);
        if (!Available(selected)) selected = 0;

        var root = R("ExpeditionMap", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(root, Dark, true);
        root.SetAsLastSibling();

        Build(root, selected, onStart, onBack);
        return root.gameObject;
    }

    static void Build(RectTransform root, int selected, UnityAction<int> onStart, UnityAction onBack)
    {
        // очистка перед перерисовкой
        for (int i = root.childCount - 1; i >= 0; i--) Object.Destroy(root.GetChild(i).gameObject);

        // Назад + заголовок
        Txt(R("Title", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(900, 100)), "Экспедиции", 80, Cream, FontStyles.Bold);
        if (onBack != null)
            Btn(root, "Назад", new Vector2(0, 1), new Vector2(190, -90), new Vector2(280, 90), Sand, onBack);

        // карточки четырёх зон
        float cw = 400f, gap = 30f;
        float x0 = -(cw * 4 + gap * 3) / 2f + cw / 2f;
        for (int i = 0; i < Zones.Length; i++)
        {
            int zi = i;
            bool open = Available(i);
            bool passed = Stars(i) > 0;
            bool sel = open && i == selected;

            var card = R("Zone" + (i + 1), root, Mid, Mid, new Vector2(x0 + i * (cw + gap), 0), new Vector2(cw, 560));
            var img = Img(card, !open ? Locked : (sel ? Amber : Card), true);
            var b = card.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.interactable = open;
            b.onClick.AddListener(() =>
            {
                PlayerPrefs.SetInt("SelectedZone", zi);
                Build(root, zi, onStart, onBack);
            });

            Color tc = sel ? Ink : (open ? Cream : new Color(0.6f, 0.55f, 0.5f));
            Txt(R("Num", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(360, 70)), "Зона " + (i + 1), 52, tc, FontStyles.Bold);
            var nm = Txt(R("Name", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(360, 70)), Zones[i].name, 34, tc, FontStyles.Bold);
            nm.textWrappingMode = TextWrappingModes.Normal;

            var ds = Txt(R("Desc", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -260), new Vector2(350, 150)), open ? Zones[i].desc : "Закрыта. Пройдите предыдущую зону.", 26, tc, FontStyles.Normal);
            ds.textWrappingMode = TextWrappingModes.Normal;
            ds.alignment = TextAlignmentOptions.Top;

            Txt(R("Finds", card, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 170), new Vector2(360, 40)), "Обязательных находок: " + Zones[i].finds, 26, tc, FontStyles.Normal);
            float best = PlayerPrefs.GetFloat("ZoneBest" + i, 0f);
            Txt(R("Best", card, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 125), new Vector2(360, 40)),
                passed ? "Лучшая сохранность: " + Mathf.RoundToInt(best) + "%" : "Сохранность: -", 26, tc, FontStyles.Normal);

            string stars = "";
            for (int s = 0; s < 3; s++) stars += s < Stars(i) ? "★" : "☆";
            Txt(R("Stars", card, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(360, 60)), stars, 52, sel ? Ink : Amber, FontStyles.Bold);

            string state = !open ? "ЗАКРЫТА" : !Ready(Zones[i].scene) ? "В РАЗРАБОТКЕ"
                : (sel ? "ВЫБРАНА" : (passed ? "ПРОЙДЕНА" : "ДОСТУПНА"));
            Txt(R("State", card, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(360, 36)), state, 24, tc, FontStyles.Bold);
        }

        // Начать экспедицию / Повторить экспедицию  только для готовых уровней
        string label = !Ready(Zones[selected].scene) ? "Уровень в разработке"
            : Stars(selected) > 0 ? "Повторить экспедицию" : "Начать экспедицию";
        var startBtn = Btn(root, label, new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(760, 110), Amber, () =>
        {
            PlayerPrefs.SetInt("SelectedZone", selected);
            PlayerPrefs.Save();
            onStart?.Invoke(selected);
        });
        startBtn.interactable = Ready(Zones[selected].scene);
    }

    static Button Btn(RectTransform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, Color color, UnityAction click)
    {
        var b = R("Btn" + text, parent, anchor, anchor, pos, size);
        var img = Img(b, color, true);
        var btn = b.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(click);
        Txt(R("Label", b, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), text, 44, Ink, FontStyles.Bold);
        return btn;
    }

    static RectTransform R(string n, Transform p, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(n, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(p, false);
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot = Mid;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    static Image Img(RectTransform rt, Color c, bool raycast)
    {
        var i = rt.gameObject.AddComponent<Image>();
        i.color = c;
        i.raycastTarget = raycast;
        return i;
    }

    static TMP_Text Txt(RectTransform rt, string s, float size, Color c, FontStyles st)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = s; t.fontSize = size; t.color = c; t.fontStyle = st;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }
}
