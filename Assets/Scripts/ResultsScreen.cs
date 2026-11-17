using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public static class ResultsScreen
{
    static readonly Color Dark = new Color32(0x2B, 0x1D, 0x12, 245);
    static readonly Color Sand = new Color32(0xE3, 0xC1, 0x8A, 255);
    static readonly Color Amber = new Color32(0xF2, 0xA9, 0x2E, 255);
    static readonly Color Cream = new Color32(0xF7, 0xEB, 0xD0, 255);
    static readonly Color Ink = new Color32(0x3B, 0x2A, 0x1E, 255);
    static readonly Color Good = new Color32(0x6B, 0xC2, 0x5A, 255);
    static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);

    public static void Show(int zone, int stars, float avg, float time, int damage, int restart,
                            List<MissionTracker.Item> items = null)
    {
        // звёзды считаем строго
        stars = 1;
        if (avg >= 80f) stars = 2;
        if (avg >= 90f && restart == 0) stars = 3;

        // Новый лучший результат больше звёзд или выше средняя сохранность, чем сохранено раньше
        bool newBest = stars > ExpeditionMap.Stars(zone) || avg > PlayerPrefs.GetFloat("ZoneBest" + zone, 0f) + 0.01f;
        ExpeditionMap.SaveResult(zone, stars, avg);

        // Продолжить в меню ведёт только к следующей незавершённой экспедиции
        int next = zone + 1;
        bool hasNext = next < ExpeditionMap.Count;
        if (hasNext) PlayerPrefs.SetInt("SelectedZone", next);
        if (hasNext && ExpeditionMap.IsReady(next)) PlayerPrefs.SetInt("HasSave", 1);
        else PlayerPrefs.DeleteKey("HasSave");
        PlayerPrefs.Save();

        // за окном результатов игра стоит кнопки возвращают
        Time.timeScale = 0f;

        var cgo = new GameObject("ResultsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        var sc = cgo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        var root = (RectTransform)cgo.transform;

        var dim = R("Dim", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(dim, new Color(0, 0, 0, 0.65f), true);
        var box = R("Box", root, Mid, Mid, Vector2.zero, new Vector2(900, 1040));
        Img(box, Dark, true);

        Txt(R("Title", box, Mid, Mid, new Vector2(0, 455), new Vector2(840, 80)), ExpeditionMap.NameOf(zone), 58, Cream, FontStyles.Bold);
        Txt(R("Sub", box, Mid, Mid, new Vector2(0, 390), new Vector2(840, 50)), "Экспедиция завершена", 32, Sand, FontStyles.Normal);

        string starStr = "";
        for (int i = 0; i < 3; i++) starStr += i < stars ? "★" : "☆";
        Txt(R("Stars", box, Mid, Mid, new Vector2(0, 285), new Vector2(840, 130)), starStr, 96, Amber, FontStyles.Bold);

        if (newBest)
            Txt(R("NewBest", box, Mid, Mid, new Vector2(0, 205), new Vector2(840, 50)), "Новый лучший результат!", 36, Good, FontStyles.Bold);

        float m = time / 60f, s2 = time % 60f;
        string timeStr = Mathf.FloorToInt(m) + ":" + Mathf.FloorToInt(s2).ToString("00");
        Row(box, 125, "Время экспедиции", timeStr);
        Row(box, 45, "Средняя сохранность", Mathf.RoundToInt(avg) + "%");
        Row(box, -35, "Повреждений сохранности", damage.ToString());
        Row(box, -115, "Перезапусков раскопок", restart.ToString());

        Btn(box, "Коллекция", -235, Sand, () => GoMenu("collection"));

        Btn(box, "Повторить экспедицию", -345, Sand, () =>
        {
            PlayerPrefs.SetInt("SelectedZone", zone);
            PlayerPrefs.Save();
            Time.timeScale = 1f;
            SceneManager.LoadScene(ExpeditionMap.SceneOf(zone));
        });

        if (hasNext)
        {
            Btn(box, "Следующая экспедиция", -455, Amber, () =>
            {
                if (ExpeditionMap.IsReady(next))
                {
                    PlayerPrefs.SetInt("SelectedZone", next);
                    PlayerPrefs.Save();
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(ExpeditionMap.SceneOf(next));
                }
                else GoMenu("map");   // уровень ещё не готов покажем карту со статусом
            });
        }
        else
        {
            // четвёртая зона вместо Следующей экспедиции»  Открыть коллекцию в ней Все экспедиции завершены
            Btn(box, "Открыть коллекцию", -455, Amber, () => GoMenu("collection"));
        }

        // карточки найденных обязательных артефактов с итоговой сохранностью 
        if (items != null && items.Count > 0)
        {
            var side = R("Items", root, Mid, Mid, new Vector2(700, 0), new Vector2(440, 120 + items.Count * 110));
            Img(side, Dark, false);
            Txt(R("ItemsTitle", side, Mid, Mid, new Vector2(0, side.sizeDelta.y / 2f - 50), new Vector2(420, 60)),
                "Находки", 38, Cream, FontStyles.Bold);
            for (int i = 0; i < items.Count; i++)
            {
                float y = side.sizeDelta.y / 2f - 150 - i * 110;
                Txt(R("Item" + i, side, Mid, Mid, new Vector2(0, y), new Vector2(420, 100)),
                    MissionTracker.NameOf(items[i].kind) + "\n" + Mathf.RoundToInt(items[i].integrity) + "%",
                    30, Sand, FontStyles.Normal);
            }
        }
    }

    // главное меню откроет нужный экран
    static void GoMenu(string open)
    {
        PlayerPrefs.SetString("MenuOpen", open);
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        LoadingScreen.Next = "MainMenu";
        SceneManager.LoadScene("Loading");
    }

    static void Row(RectTransform parent, float y, string label, string value)
    {
        var l = Txt(R("RowL", parent, Mid, Mid, new Vector2(-150, y), new Vector2(520, 60)), label, 34, Sand, FontStyles.Normal);
        l.alignment = TextAlignmentOptions.MidlineLeft;
        var v = Txt(R("RowV", parent, Mid, Mid, new Vector2(300, y), new Vector2(200, 60)), value, 38, Cream, FontStyles.Bold);
        v.alignment = TextAlignmentOptions.MidlineRight;
    }

    static Button Btn(RectTransform parent, string text, float y, Color color, UnityAction onClick)
    {
        var b = R("Btn" + text, parent, Mid, Mid, new Vector2(0, y), new Vector2(700, 96));
        var img = Img(b, color, true);
        var btn = b.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        Txt(R("Label", b, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), text, 42, Ink, FontStyles.Bold);
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
