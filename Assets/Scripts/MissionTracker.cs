using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class MissionTracker : MonoBehaviour
{
    public static MissionTracker Instance { get; private set; }

    public class Item
    {
        public ArtifactKind kind;
        public float integrity;    // сохранность на момент извлечения
    }

    public readonly List<Item> Collected = new List<Item>();   // извлечённые 

    public float MissionTime { get; private set; }
    public int DamageCount { get; private set; }    // раз сохранность упала до 0
    public int RestartCount { get; private set; }   // повторов раскопок из-за повреждения

    public bool BowlDone { get; private set; }      // чаша в коллекции
    public bool TabletDone { get; private set; }    // табличка в коллекции
    public int HalvesCleaned { get; private set; }  

    float bowlInt, leftInt, rightInt;
    float finishTimer = -1f;
    int restoredAccepted;          // сколько находок принято до перезапуска игры
    float lastDamageTime = -10f;

    TMP_Text goalText, counterText, toastText;
    GameObject panel;
    float toastTimer;

    void Awake() => Instance = this;

    void OnEnable()
    {
        DigSite.OnExtracted += HandleExtracted;
        DigSite.OnFailed += HandleFailed;
        DigSite.OnDamaged += HandleDamaged;
        LabStation.OnCleaned += HandleCleaned;
        LabStation.OnAssembled += HandleAssembled;
    }

    void OnDisable()
    {
        DigSite.OnExtracted -= HandleExtracted;
        DigSite.OnFailed -= HandleFailed;
        DigSite.OnDamaged -= HandleDamaged;
        LabStation.OnCleaned -= HandleCleaned;
        LabStation.OnAssembled -= HandleAssembled;
    }

    void Start()
    {
        RestoreRun();
        BuildUI();
    }

    // Продолжить возвращаем то что принято в текущем прохождении 
    void RestoreRun()
    {
        void Add(ArtifactKind k)
        {
            if (!RunSave.HasExt(k)) return;
            float v = RunSave.GetExt(k);
            Collected.Add(new Item { kind = k, integrity = v });
            if (k == ArtifactKind.Bowl) bowlInt = v;
            else if (k == ArtifactKind.TabletLeft) leftInt = v;
            else rightInt = v;
            restoredAccepted++;
        }
        Add(ArtifactKind.Bowl);
        Add(ArtifactKind.TabletLeft);
        Add(ArtifactKind.TabletRight);

        BowlDone = RunSave.HasClean(ArtifactKind.Bowl);
        HalvesCleaned = (RunSave.HasClean(ArtifactKind.TabletLeft) ? 1 : 0) + (RunSave.HasClean(ArtifactKind.TabletRight) ? 1 : 0);
        TabletDone = RunSave.HasAssembled;
    }

    public static string NameOf(ArtifactKind k)
    {
        switch (k)
        {
            case ArtifactKind.Bowl: return "Керамическая чаша";
            case ArtifactKind.TabletLeft: return "Левая половина таблички";
            case ArtifactKind.TabletRight: return "Правая половина таблички";
            case ArtifactKind.Buckle1: case ArtifactKind.Buckle2: case ArtifactKind.Buckle3: return "Бронзовая пряжка";
            case ArtifactKind.Vessel: return "Глиняный сосуд";
            case ArtifactKind.Panel1: case ArtifactKind.Panel2: case ArtifactKind.Panel3:
            case ArtifactKind.Panel4: case ArtifactKind.Panel5: return "Декоративная панель";
            case ArtifactKind.Disc1: case ArtifactKind.Disc2: case ArtifactKind.Disc3:
            case ArtifactKind.Disc4: case ArtifactKind.Disc5: case ArtifactKind.Disc6: return "Церемониальный диск";
            default: return "Современный мусор";
        }
    }

    // средняя сохранность принятых предметов
    public float AvgIntegrity
    {
        get
        {
            float sum = 0f; int n = 0;
            if (bowlInt > 0f) { sum += bowlInt; n++; }
            if (leftInt > 0f) { sum += leftInt; n++; }
            if (rightInt > 0f) { sum += rightInt; n++; }
            return n == 0 ? 0f : sum / n;
        }
    }

    //  1 - завершена 2 - средняя сохранность >= 80% 3  >= 90% и без повторного запуска раскопки
    public int Stars => AvgIntegrity >= 90f && RestartCount == 0 ? 3 : AvgIntegrity >= 80f ? 2 : 1;

    // события
    void HandleExtracted(DigSite s)
    {
        if (s.kind == ArtifactKind.Junk)
        {
            Toast("Это не коллекционный предмет");
            return;
        }

        Collected.Add(new Item { kind = s.kind, integrity = s.FinalIntegrity });
        if (s.kind == ArtifactKind.Bowl) bowlInt = s.FinalIntegrity;
        else if (s.kind == ArtifactKind.TabletLeft) leftInt = s.FinalIntegrity;
        else rightInt = s.FinalIntegrity;

        Toast("Извлечено: " + NameOf(s.kind) + " (" + Mathf.RoundToInt(s.FinalIntegrity) + "%) - отнесите к столу");
    }

    // повреждение сохранности инструментом серия ударов подряд считается одним случаем
    void HandleDamaged(DigSite s)
    {
        if (Time.time - lastDamageTime > 0.6f) DamageCount++;
        lastDamageTime = Time.time;
    }

    void HandleFailed(DigSite s)
    {
        RestartCount++;
        Toast("Находка повреждена! Раскоп перезапустится");
    }

    void HandleCleaned(ArtifactKind k)
    {
        if (k == ArtifactKind.Bowl) BowlDone = true;
        else if (k == ArtifactKind.TabletLeft || k == ArtifactKind.TabletRight) HalvesCleaned++;
        else return;

        Toast("Очищено: " + NameOf(k));
        CheckFinish();
    }

    void HandleAssembled(ArtifactKind k)
    {
        if (TabletDone) return;
        TabletDone = true;
        Toast("Табличка восстановлена");
        CheckFinish();
    }

    void CheckFinish()
    {
        if (BowlDone && TabletDone && finishTimer < 0f)
        {
            finishTimer = 2f;
            Toast("Все находки в коллекции. Готовим результаты...");
        }
    }

    //  цикл
    void Update()
    {
        if (panel == null) return;

        if (finishTimer > 0f)
        {
            finishTimer -= Time.unscaledDeltaTime;
            if (finishTimer <= 0f)
            {
                finishTimer = -1f;
                // копия списка до очистки сохранения, чтобы карточки находок остались на экране
                var items = new List<Item>(Collected);
                RunSave.Clear();   // прохождение завершено следующий запуск зоны начнётся заново
                ResultsScreen.Show(0, Stars, AvgIntegrity, MissionTime, DamageCount, RestartCount, items);
            }
            return;
        }

        MissionTime += Time.deltaTime;

        int total = restoredAccepted, found = restoredAccepted;   // принятые до перезапуска считаются найденными
        foreach (var p in ScanPoint.All)
        {
            if (p.IsFalse) continue;
            total++;
            if (p.Found) found++;
        }

        // во время раскопа верхнюю панель занимает DigHUD
        bool digging = DigSite.Current != null && DigSite.Current.Active;
        panel.SetActive(!digging);

        int cleaned = (BowlDone ? 1 : 0) + HalvesCleaned;
        string goal;
        if (found < total)
            goal = "Цель: найдите сканером точки интереса (" + found + "/" + total + ")";
        else if (Collected.Count < total)
            goal = "Цель: раскопайте и извлеките находки (" + Collected.Count + "/" + total + ")";
        else if (cleaned < 3)
            goal = "Цель: очистите находки на столе (" + cleaned + "/3)";
        else if (!TabletDone)
            goal = "Цель: восстановите табличку на столе";
        else
            goal = "Все находки в коллекции";

        goalText.text = goal;

        // ИСПРАВЛЕНО
        int req = (BowlDone ? 1 : 0) + (TabletDone ? 1 : 0);
        counterText.text = "Обязательные предметы: " + req + "/2     Точки найдены: " + found + "/" + total;

        if (toastTimer > 0f)
        {
            toastTimer -= Time.unscaledDeltaTime;
            toastText.alpha = Mathf.Clamp01(toastTimer);
        }
        else toastText.alpha = 0f;
    }

    public void Toast(string text)
    {
        if (toastText == null) return;
        toastText.text = text;
        toastTimer = 4f;
    }

    // интерфейс кодом
    void BuildUI()
    {
        var cgo = new GameObject("MissionCanvas", typeof(Canvas), typeof(CanvasScaler));
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var sc = cgo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;

        var pgo = new GameObject("GoalPanel", typeof(RectTransform), typeof(Image));
        var prt = (RectTransform)pgo.transform;
        prt.SetParent(cgo.transform, false);
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 1f);
        prt.anchoredPosition = new Vector2(0, -24);
        prt.sizeDelta = new Vector2(920, 120);
        var img = pgo.GetComponent<Image>();
        img.color = new Color(0.17f, 0.11f, 0.07f, 0.82f);
        img.raycastTarget = false;
        panel = pgo;

        goalText = MakeText(prt, "Goal", new Vector2(0, -14), new Vector2(900, 50), 38,
            new Color32(0xF7, 0xEB, 0xD0, 255), FontStyles.Bold);
        counterText = MakeText(prt, "Counter", new Vector2(0, -68), new Vector2(900, 40), 30,
            new Color32(0xE3, 0xC1, 0x8A, 255), FontStyles.Normal);

        toastText = MakeText((RectTransform)cgo.transform, "Toast", new Vector2(0, -170), new Vector2(1500, 60), 40,
            new Color32(0xF2, 0xA9, 0x2E, 255), FontStyles.Bold);
        toastText.alpha = 0f;
    }

    static TMP_Text MakeText(RectTransform parent, string name, Vector2 pos, Vector2 size, float fontSize, Color color, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = fontSize;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }
}
