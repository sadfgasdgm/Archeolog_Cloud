using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Цели миссии и условия успеха/провала для зон 2-4 (ТЗ). Зона 1 по-прежнему на MissionTracker.
//
//  Зона 2 «Лесное поселение»  (zone = 1)
//     Цели: найти 4 точки интереса; восстановить бронзовую пряжку из 3 фрагментов;
//           очистить цельный сосуд, сохранив не менее 80%.
//     Провал: сосуд 1-79% не принимается («Нужно не менее 80%»), перекапывается только сосуд (100%, исходный грунт);
//             фрагмент пряжки с сохранностью 0 - перекапывается только он, принятые остаются.
//  Зона 3 «Каменистый склон»  (zone = 2)
//     Цели: обследовать 3 террасы; найти 5 фрагментов панели; собрать панель, очистка не ниже 95%.
//     Успех: все 5 очищены, панель собрана, средняя сохранность >= 70%.
//     Провал: 0 - перекоп; после сборки средняя < 70% - перекапываются только фрагменты ниже 70%.
//  Зона 4 «Подземная камера»  (zone = 3)
//     Цели: открыть 4 раскопа в нишах; собрать 6 фрагментов диска; очистить все; восстановить диск и добавить в коллекцию.
//     Успех: средняя сохранность >= 75%.
//     Провал: 0 - перекоп; после сборки средняя < 75% - перекапываются только фрагменты ниже 75%.
public class ExpeditionMission : MonoBehaviour
{
    public static ExpeditionMission Instance { get; private set; }

    public int zone = 1;                     // индекс зоны в ExpeditionMap: 1, 2, 3

    public float MissionTime { get; private set; }
    public int DamageCount { get; private set; }
    public int RestartCount { get; private set; }

    class Part
    {
        public ArtifactKind kind;
        public DigSite site;
        public bool accepted, cleaned, restored;   // restored: принята до перезапуска игры, раскоп не строился
        public float integrity;
    }

    readonly Dictionary<ArtifactKind, Part> parts = new Dictionary<ArtifactKind, Part>();
    readonly List<ArtifactKind> fragments = new List<ArtifactKind>();   // то, что собирается на столе
    readonly HashSet<string> opened = new HashSet<string>();             // раскопы в нишах, где начали копать

    bool hasVessel, hasTerraces, needPoints, needOpened;
    int pointsNeeded, openedNeeded;
    float minAvg;                              // порог средней сохранности при сборке
    string itemName, collectionKey, fragmentWord;
    bool itemDone, vesselDone;
    float finishTimer = -1f, lastDamageTime = -10f, toastTimer;
    float assembledAvg;
    int lastTerraceMask = -1;

    TMP_Text goalText, counterText, toastText;
    GameObject panel;

    public int PieceCount => fragments.Count;

    public bool AssemblyReady
    {
        get
        {
            if (itemDone || fragments.Count == 0) return false;
            foreach (var k in fragments) if (!parts[k].accepted || !parts[k].cleaned) return false;
            return true;
        }
    }

    // название в окне очистки (для зон 2-4 - «Фрагмент пряжки 2», для зоны 1 - как раньше)
    public static string TitleOf(ArtifactKind k)
    {
        if (Instance != null) return Instance.Title(k);
        return MissionTracker.NameOf(k);
    }

    string Title(ArtifactKind k)
    {
        int idx = fragments.IndexOf(k);
        if (idx >= 0) return fragmentWord + " " + (idx + 1) + "/" + fragments.Count;
        return MissionTracker.NameOf(k);
    }

    bool ready;

    void Awake() { Instance = this; }

    // Настройка зоны. Вызывается ExpeditionLevels сразу после AddComponent (zone ещё не был известен в Awake).
    public void Init(int z)
    {
        if (ready) return;
        zone = z;
        ready = true;
        Setup();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnEnable()
    {
        DigSite.OnExtracted += HandleExtracted;
        DigSite.OnFailed += HandleFailed;
        DigSite.OnRejected += HandleRejected;
        DigSite.OnDamaged += HandleDamaged;
        LabStation.OnCleaned += HandleCleaned;
    }

    void OnDisable()
    {
        DigSite.OnExtracted -= HandleExtracted;
        DigSite.OnFailed -= HandleFailed;
        DigSite.OnRejected -= HandleRejected;
        DigSite.OnDamaged -= HandleDamaged;
        LabStation.OnCleaned -= HandleCleaned;
    }

    void Start()
    {
        Init(zone);                                  // если компонент поставлен в сцене вручную
        BuildUI();
        var lab = FindAnyObjectByType<LabStation>();
        if (lab != null) foreach (var k in pendingClean) lab.EnqueueRestored(k);
    }

    void Setup()
    {
        PlayerPrefs.SetInt("SelectedZone", zone);
        switch (zone)
        {
            case 1:
                for (int i = 0; i < 3; i++) fragments.Add((ArtifactKind)((int)ArtifactKind.Buckle1 + i));
                hasVessel = true; needPoints = true; pointsNeeded = 4; minAvg = 0f;
                itemName = "Бронзовая пряжка"; collectionKey = "Buckle"; fragmentWord = "Фрагмент пряжки";
                break;
            case 2:
                for (int i = 0; i < 5; i++) fragments.Add((ArtifactKind)((int)ArtifactKind.Panel1 + i));
                hasTerraces = true; needPoints = true; pointsNeeded = 5; minAvg = 70f;
                itemName = "Декоративная панель"; collectionKey = "Panel"; fragmentWord = "Фрагмент панели";
                break;
            default:
                for (int i = 0; i < 6; i++) fragments.Add((ArtifactKind)((int)ArtifactKind.Disc1 + i));
                needOpened = true; openedNeeded = 4; minAvg = 75f;
                itemName = "Церемониальный диск"; collectionKey = "Disc"; fragmentWord = "Фрагмент диска";
                break;
        }
        foreach (var k in fragments) parts[k] = new Part { kind = k };
        if (hasVessel) parts[ArtifactKind.Vessel] = new Part { kind = ArtifactKind.Vessel };
        RestoreRun();
    }

    // «Продолжить» (ТЗ 2.9): принятые находки/фрагменты, этап очистки, сборка, открытые раскопы, террасы
    readonly List<ArtifactKind> pendingClean = new List<ArtifactKind>();

    void RestoreRun()
    {
        foreach (var p in parts.Values)
        {
            if (!RunSave.HasExt(p.kind)) continue;
            p.accepted = true;
            p.restored = true;
            p.integrity = RunSave.GetExt(p.kind);
            p.cleaned = RunSave.HasClean(p.kind);
            if (!p.cleaned) pendingClean.Add(p.kind);
        }
        if (hasVessel) vesselDone = parts[ArtifactKind.Vessel].cleaned;
        itemDone = RunSave.HasAssembled;
        if (itemDone) assembledAvg = RunSave.GetAssembled();

        foreach (var n in RunSave.GetText("RunOpened").Split(',')) if (n.Length > 0) opened.Add(n);

        int mask;
        if (int.TryParse(RunSave.GetText("RunTerraces"), out mask))
            for (int i = 0; i < 3; i++) TerraceZone.Visited[i] = (mask & (1 << i)) != 0;
    }

    // ---------- события ----------
    void HandleExtracted(DigSite s)
    {
        Part p;
        if (!parts.TryGetValue(s.kind, out p)) return;
        p.site = s;
        p.accepted = true;
        p.cleaned = false;
        p.integrity = s.FinalIntegrity;
        p.restored = false;
        RunSave.SetExt(s.kind, p.integrity);                 // автосохранение: принятая находка
        PlayerPrefs.SetInt("HasSave", 1);
        Toast("Извлечено: " + Title(s.kind) + " (" + Mathf.RoundToInt(s.FinalIntegrity) + "%) - отнесите к столу");
    }

    void HandleFailed(DigSite s)
    {
        if (!parts.ContainsKey(s.kind)) return;
        RestartCount++;
        Toast("Находка повреждена! Раскоп перезапустится");
    }

    void HandleRejected(DigSite s)
    {
        if (!parts.ContainsKey(s.kind)) return;
        RestartCount++;
        Toast("Нужно не менее " + Mathf.RoundToInt(DigSite.VesselMin) + "%");
    }

    void HandleDamaged(DigSite s)
    {
        if (Time.time - lastDamageTime > 0.6f) DamageCount++;
        lastDamageTime = Time.time;
    }

    void HandleCleaned(ArtifactKind k)
    {
        Part p;
        if (!parts.TryGetValue(k, out p)) return;
        p.cleaned = true;
        RunSave.SetClean(k, p.integrity);                    // автосохранение: этап очистки завершён

        if (k == ArtifactKind.Vessel)
        {
            vesselDone = true;
            AddToCollection("Vessel", p.integrity);
            Toast("Сосуд очищен и принят (" + Mathf.RoundToInt(p.integrity) + "%)");
        }
        else
            Toast("Очищено: " + Title(k) + (AssemblyReady ? ". Восстановите предмет на столе" : ""));
        CheckFinish();
    }

    // Вызывает LabStation, когда игрок собрал фрагменты на столе.
    public void NotifyAssembled()
    {
        float avg = FragmentAverage();
        if (minAvg > 0f && avg < minAvg)
        {
            // сборка не принимается: перекапываются только фрагменты ниже порога, остальные сохраняются
            int redo = 0;
            foreach (var k in fragments)
            {
                var p = parts[k];
                if (p.integrity >= minAvg) continue;
                p.accepted = false;
                p.cleaned = false;
                RunSave.ClearPart(k);
                if (p.site != null && !p.restored) p.site.Redo();
                else if (ExpeditionLevels.Instance != null) ExpeditionLevels.Instance.RespawnSite(k);   // принята до перезапуска игры
                p.site = null;
                p.restored = false;
                redo++;
                RestartCount++;
            }
            Toast("Средняя сохранность " + Mathf.RoundToInt(avg) + "% ниже " + Mathf.RoundToInt(minAvg) +
                  "%. Перекопайте фрагменты с низкой сохранностью (" + redo + ")", 10f);
            return;
        }

        itemDone = true;
        assembledAvg = avg;
        RunSave.SetAssembled(avg);                           // автосохранение: сборка завершена
        AddToCollection(collectionKey, avg);
        Toast(itemName + " восстановлена и добавлена в коллекцию");
        CheckFinish();
    }

    float FragmentAverage()
    {
        float sum = 0f; int n = 0;
        foreach (var k in fragments) if (parts[k].accepted) { sum += parts[k].integrity; n++; }
        return n == 0 ? 0f : sum / n;
    }

    // средняя сохранность всех принятых находок (для звёзд)
    public float AvgIntegrity
    {
        get
        {
            float sum = 0f; int n = 0;
            foreach (var p in parts.Values) if (p.accepted) { sum += p.integrity; n++; }
            return n == 0 ? 0f : sum / n;
        }
    }

    public int Stars => AvgIntegrity >= 90f && RestartCount == 0 ? 3 : AvgIntegrity >= 80f ? 2 : 1;

    static void AddToCollection(string key, float integrity)
    {
        if (PlayerPrefs.GetFloat("ItemInt_" + key, -1f) < 0f)
            PlayerPrefs.SetInt("CollectionCount", PlayerPrefs.GetInt("CollectionCount", 0) + 1);
        PlayerPrefs.SetFloat("ItemInt_" + key, integrity);
        PlayerPrefs.Save();
    }

    // ---------- счётчики ----------
    int FoundPoints()
    {
        int n = 0;
        foreach (var p in ScanPoint.All) if (!p.IsFalse && p.Found) n++;
        foreach (var pt in parts.Values) if (pt.restored && pt.accepted) n++;   // принятые до перезапуска считаются найденными
        return n;
    }

    int Count(bool cleaned)
    {
        int n = 0;
        foreach (var k in fragments) if (cleaned ? parts[k].cleaned : parts[k].accepted) n++;
        return n;
    }

    bool GoalsDone()
    {
        if (!itemDone) return false;
        if (hasVessel && !vesselDone) return false;
        if (needPoints && FoundPoints() < pointsNeeded) return false;
        if (hasTerraces && TerraceZone.VisitedCount < 3) return false;
        if (needOpened && opened.Count < openedNeeded) return false;
        return true;
    }

    void CheckFinish()
    {
        if (finishTimer < 0f && GoalsDone())
        {
            finishTimer = 2f;
            Toast("Все цели выполнены. Готовим результаты...");
        }
    }

    // ---------- цикл ----------
    void Update()
    {
        if (panel == null) return;

        if (finishTimer > 0f)
        {
            finishTimer -= Time.unscaledDeltaTime;
            if (finishTimer <= 0f)
            {
                finishTimer = -1f;
                var items = new List<MissionTracker.Item>();
                items.Add(new MissionTracker.Item { kind = fragments[0], integrity = assembledAvg });
                if (hasVessel) items.Add(new MissionTracker.Item { kind = ArtifactKind.Vessel, integrity = parts[ArtifactKind.Vessel].integrity });
                float avg = AvgIntegrity;
                int stars = Stars;
                RunSave.Clear();
                ResultsScreen.Show(zone, stars, avg, MissionTime, DamageCount, RestartCount, items);
            }
            return;
        }

        MissionTime += Time.deltaTime;

        var cur = DigSite.Current;
        if (needOpened && cur != null && cur.Active)
        {
            string n = cur.name;
            if (n.EndsWith("_A") || n.EndsWith("_B")) n = n.Substring(0, n.Length - 2);
            if (opened.Add(n))
            {
                RunSave.SetText("RunOpened", string.Join(",", opened));
                PlayerPrefs.SetInt("HasSave", 1);
                Toast("Раскоп открыт (" + opened.Count + "/" + openedNeeded + ")");
            }
        }

        if (hasTerraces)
        {
            int mask = 0;
            for (int i = 0; i < 3; i++) if (TerraceZone.Visited[i]) mask |= 1 << i;
            if (mask != lastTerraceMask) { lastTerraceMask = mask; RunSave.SetText("RunTerraces", mask.ToString()); }
        }

        bool digging = cur != null && cur.Active;
        panel.SetActive(!digging);

        int found = FoundPoints();
        int nFrag = fragments.Count;
        int ext = Count(false), clean = Count(true);
        int terr = TerraceZone.VisitedCount;

        string goal;
        if (needPoints && found < pointsNeeded || hasTerraces && terr < 3)
        {
            goal = hasTerraces
                ? "Цель: обследуйте террасы (" + terr + "/3), найдите фрагменты (" + found + "/" + pointsNeeded + ")"
                : "Цель: найдите сканером точки интереса (" + found + "/" + pointsNeeded + ")";
        }
        else if (needOpened && opened.Count < openedNeeded)
            goal = "Цель: откройте раскопы в нишах (" + opened.Count + "/" + openedNeeded + ")";
        else if (ext < nFrag || hasVessel && !parts[ArtifactKind.Vessel].accepted)
            goal = "Цель: раскопайте и извлеките находки (" + (ext + (hasVessel && parts[ArtifactKind.Vessel].accepted ? 1 : 0)) + "/" + (nFrag + (hasVessel ? 1 : 0)) + ")";
        else if (clean < nFrag || hasVessel && !vesselDone)
            goal = "Цель: очистите находки на столе (" + (clean + (vesselDone ? 1 : 0)) + "/" + (nFrag + (hasVessel ? 1 : 0)) + ")";
        else if (!itemDone)
            goal = "Цель: восстановите предмет на столе (" + itemName + ")";
        else
            goal = "Все цели выполнены";
        goalText.text = goal;

        string c = hasTerraces ? "Террасы: " + terr + "/3   " : "";
        if (needOpened) c += "Раскопы: " + opened.Count + "/" + openedNeeded + "   ";
        if (needPoints) c += "Точки: " + found + "/" + pointsNeeded + "   ";
        c += "Фрагменты: " + clean + "/" + nFrag + (hasVessel ? "   Сосуд: " + (vesselDone ? "готов" : "-") : "");
        counterText.text = c;

        if (toastTimer > 0f)
        {
            toastTimer -= Time.unscaledDeltaTime;
            toastText.alpha = Mathf.Clamp01(toastTimer);
        }
        else toastText.alpha = 0f;
    }

    public void Toast(string text, float seconds = 4f)
    {
        if (toastText == null) return;
        toastText.text = text;
        toastTimer = seconds;
    }

    // ---------- интерфейс кодом (как у MissionTracker) ----------
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
        prt.sizeDelta = new Vector2(1100, 120);
        var img = pgo.GetComponent<Image>();
        img.color = new Color(0.17f, 0.11f, 0.07f, 0.82f);
        img.raycastTarget = false;
        panel = pgo;

        goalText = MakeText(prt, "Goal", new Vector2(0, -14), new Vector2(1070, 50), 34,
            new Color32(0xF7, 0xEB, 0xD0, 255), FontStyles.Bold);
        counterText = MakeText(prt, "Counter", new Vector2(0, -68), new Vector2(1070, 40), 28,
            new Color32(0xE3, 0xC1, 0x8A, 255), FontStyles.Normal);

        toastText = MakeText((RectTransform)cgo.transform, "Toast", new Vector2(0, -170), new Vector2(1700, 60), 38,
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
        t.enableAutoSizing = true;                 // на узких экранах телефона текст уменьшается, а не обрезается
        t.fontSizeMax = fontSize;
        t.fontSizeMin = fontSize * 0.55f;
        t.raycastTarget = false;
        return t;
    }
}
