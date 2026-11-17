using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class LabStation : Interactable
{
    public static event System.Action<ArtifactKind> OnCleaned;
    public static event System.Action<ArtifactKind> OnAssembled;
    public static event System.Action OnBack;   // вышли из лаборатории без завершения
    public static bool Busy { get; private set; }

    [SerializeField] bool demoAssembly;   // обучение после очистки даёт учебную сборку из двух половинок
    [SerializeField] float reach = 1.0f;  // запас радиуса  стол большой, а LabStation стоит в одной точке

    readonly List<DigSite> toClean = new List<DigSite>();
    bool leftClean, rightClean, demoPending, assembledReal;
    float leftInt, rightInt;

    LabStage stage;
    CleaningStage cleaning;
    AssemblyStage assembly;
    LabUI ui;
    DigSite cleaningSite;
    bool demoSession;
    PlayerController player;

    protected override void OnEnable() { base.OnEnable(); DigSite.OnExtracted += HandleExtracted; }
    protected override void OnDisable() { base.OnDisable(); DigSite.OnExtracted -= HandleExtracted; }

    void Awake() { Busy = false; }   // статическое поле не должно залипать между сценами/запусками

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        // в сцене обучения учебная сборка нужна всегда, даже если забыли поставить галочку
        if (!demoAssembly && FindAnyObjectByType<TutorialManager>() != null) demoAssembly = true;
    }

    void OnDestroy()
    {
        if (stage != null) stage.Close();
        Busy = false;
    }

    void HandleExtracted(DigSite s)
    {
        if (s.kind == ArtifactKind.Junk) return;   // мусор в коллекцию не идёт
        toClean.Add(s);
    }

    //Продолжить принята, но ещё не очищенная находка возвращается в очередь на очистку 
    public void EnqueueRestored(ArtifactKind kind)
    {
        var go = new GameObject("RestoredFind_" + kind);
        go.SetActive(false);                               // раскоп не строится нужен только вид находки
        go.transform.SetParent(transform, false);
        var s = go.AddComponent<DigSite>();
        s.kind = kind;
        toClean.Add(s);
    }

    bool RealAssemblyReady => leftClean && rightClean && !assembledReal;
    bool MissionAssemblyReady => ExpeditionMission.Instance != null && ExpeditionMission.Instance.AssemblyReady;

    //  Interactable 
    public override string Label => toClean.Count > 0 ? "Очистить" : "Восстановить";
    public override bool Visible => !Busy && (toClean.Count > 0 || demoPending || RealAssemblyReady || MissionAssemblyReady);
    public override bool Enabled => true;
    public override float ExtraReach => reach;

    public override void Interact()
    {
        if (Busy) return;
        if (toClean.Count > 0) StartCleaning(toClean[0]);
        else StartAssembly();
    }

    //  очистка
    void StartCleaning(DigSite site)
    {
        Open(false);
        cleaningSite = site;
        cleaning = stage.Root.AddComponent<CleaningStage>();
        cleaning.Init(stage.Cam, site.kind, Random.Range(4, 7));   // ТЗ: 3-8 зон загрязнения

        ui = new LabUI("Очистка: " + ExpeditionMission.TitleOf(site.kind), "Готово", FinishCleaning, Close);
        ui.Main.interactable = false;
    }

    void FinishCleaning()
    {
        if (cleaning == null || !cleaning.Ready) return;
        var site = cleaningSite;
        toClean.Remove(site);
        var kind = site.kind;

        if (kind == ArtifactKind.TabletLeft) { leftClean = true; leftInt = site.FinalIntegrity; }
        else if (kind == ArtifactKind.TabletRight) { rightClean = true; rightInt = site.FinalIntegrity; }
        else if (ExpeditionMission.Instance != null) { /* зоны 2-4: учёт и коллекцию ведёт ExpeditionMission */ }
        else AddToCollection(kind, site.FinalIntegrity);

        if (demoAssembly && !demoDone) demoPending = true;

        cleaning = null;
        Close();
        OnCleaned?.Invoke(kind);
        if (MissionTracker.Instance != null)
            MissionTracker.Instance.Toast("Очищено: " + MissionTracker.NameOf(kind));
    }

    bool demoDone;

    // восстановление 
    void StartAssembly()
    {
        demoSession = !RealAssemblyReady && !MissionAssemblyReady && demoPending;
        Open(true);
        assembly = stage.Root.AddComponent<AssemblyStage>();
        assembly.Init(stage.Cam, MissionAssemblyReady ? ExpeditionMission.Instance.PieceCount : 2);

        ui = new LabUI("Восстановление: соедините фрагменты", "Готово", FinishAssembly, Close);
        ui.Main.interactable = false;
        ui.AddButton("↺ -15°", new Vector2(-260, 150), () => assembly.Rotate(-15f));
        ui.AddButton("↻ +15°", new Vector2(260, 150), () => assembly.Rotate(15f));
    }

    void FinishAssembly()
    {
        if (assembly == null || !assembly.Done) return;
        if (ExpeditionMission.Instance != null && !demoSession)
        {
            // зоны 2-4
            assembly = null;
            Close();
            ExpeditionMission.Instance.NotifyAssembled();
            return;
        }
        if (demoSession) { demoPending = false; demoDone = true; }
        else { assembledReal = true; AddToCollection(ArtifactKind.TabletLeft, (leftInt + rightInt) / 2f); }

        assembly = null;
        Close();
        OnAssembled?.Invoke(ArtifactKind.TabletLeft);
        if (MissionTracker.Instance != null)
            MissionTracker.Instance.Toast("Артефакт восстановлен");
    }

    // ключи
    static void AddToCollection(ArtifactKind kind, float integrity)
    {
        string key = kind == ArtifactKind.TabletLeft || kind == ArtifactKind.TabletRight ? "TTablet" : kind.ToString();
        if (PlayerPrefs.GetFloat("ItemInt_" + key, -1f) < 0f)
            PlayerPrefs.SetInt("CollectionCount", PlayerPrefs.GetInt("CollectionCount", 0) + 1);
        PlayerPrefs.SetFloat("ItemInt_" + key, integrity);
        PlayerPrefs.Save();
    }

    void Update()
    {
        if (cleaning != null && ui != null)
        {
            ui.Info.text = "Очищено: " + Mathf.FloorToInt(cleaning.Progress * 100f) + "% (нужно 95%)"
                         + (cleaning.Ready ? "\nНажмите «Готово»" : "\nСвайп кистью по грязи. Пустое место вращает предмет. Проверьте обратную сторону.");
            ui.Main.interactable = cleaning.Ready;
        }
        if (assembly != null && ui != null)
        {
            ui.Info.text = assembly.Done ? "Артефакт восстановлен!" : "Тащите фрагменты. Грани подсветятся, когда подходят.";
            ui.Main.interactable = assembly.Done;
        }
    }

    // сцена-витрина
    void Open(bool top)
    {
        Busy = true;
        stage = LabStage.Open(top);
        if (player != null) player.CanMove = false;
    }

    void Close()
    {
        if (ui != null) Destroy(ui.Root);
        if (stage != null) stage.Close();
        bool unfinished = cleaning != null || assembly != null;
        ui = null; stage = null; cleaning = null; assembly = null;
        Busy = false;
        if (player != null && DigSite.Current == null) player.CanMove = true;
        if (unfinished) OnBack?.Invoke();
    }
}


public class LabStage
{
    public GameObject Root;
    public Camera Cam;
    Camera main;

    public static LabStage Open(bool top)
    {
        var s = new LabStage();
        s.Root = new GameObject("LabStage");
        s.Root.transform.position = new Vector3(0f, 500f, 0f);

        var cg = new GameObject("LabCam");
        cg.transform.SetParent(s.Root.transform, false);
        s.Cam = cg.AddComponent<Camera>();
        s.Cam.clearFlags = CameraClearFlags.SolidColor;
        s.Cam.backgroundColor = new Color(0.80f, 0.79f, 0.76f);   // нейтральный фон
        s.Cam.nearClipPlane = 0.05f;
        if (top)
        {
            s.Cam.orthographic = true;
            s.Cam.orthographicSize = 2.3f;
            cg.transform.localPosition = new Vector3(0f, 5f, 0f);
            cg.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            cg.transform.localPosition = new Vector3(0f, 1.3f, -2.6f);
            cg.transform.LookAt(s.Root.transform.position);
        }

        // точечный свет
        var lg = new GameObject("LabLight");
        lg.transform.SetParent(s.Root.transform, false);
        lg.transform.localPosition = new Vector3(0.5f, 3f, -2f);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 14f;
        l.intensity = 4f;
        l.color = Color.white;


        Camera game = null;
        var cf = Object.FindAnyObjectByType<CameraFollow>();
        if (cf != null)
        {
            game = cf.GetComponent<Camera>();
            if (game == null) game = cf.GetComponentInChildren<Camera>();
        }
        if (game == null) game = Camera.main;
        s.main = game;
        if (s.main != null) s.main.enabled = false;
        s.Cam.depth = 100f;   // лаборатория всегда поверх
        return s;
    }

    public void Close()
    {
        if (main != null) main.enabled = true;
        Object.Destroy(Root);
    }
}

// Небольшой интерфейс окна очистки/сборки
public class LabUI
{
    static readonly Color Sand = new Color32(0xE3, 0xC1, 0x8A, 255);
    static readonly Color Amber = new Color32(0xF2, 0xA9, 0x2E, 255);
    static readonly Color Cream = new Color32(0xF7, 0xEB, 0xD0, 255);
    static readonly Color Ink = new Color32(0x3B, 0x2A, 0x1E, 255);
    static readonly Color Dark = new Color32(0x2B, 0x1D, 0x12, 235);

    public GameObject Root;
    public TMP_Text Info;
    public Button Main;
    Transform canvas;

    public LabUI(string title, string doneText, UnityAction onDone, UnityAction onBack)
    {
        Root = new GameObject("LabCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = Root.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 40;
        var sc = Root.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        canvas = Root.transform;

        var top = R("Top", canvas, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(1300, 150));
        var bg = top.gameObject.AddComponent<Image>();
        bg.color = Dark; bg.raycastTarget = false;
        Txt(R("Title", top, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(1260, 56)), title, 44, Cream, FontStyles.Bold);
        Info = Txt(R("Info", top, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 42), new Vector2(1260, 70)), "", 30, Sand, FontStyles.Normal);
        Info.textWrappingMode = TextWrappingModes.Normal;

        // основная кнопка внизу по центру
        Main = MakeButton(doneText, new Vector2(0, 90), new Vector2(520, 100), onDone, Amber);
        AddButtonAt("Назад", new Vector2(150, -80), new Vector2(0, 1), onBack);   //  кнопке паузы
    }

    // дополнительная кнопка pos смещение от нижнего центра экрана (для поворота ±15°)
    public Button AddButton(string text, Vector2 pos, UnityAction click)
        => MakeButton(text, new Vector2(pos.x, 230f), new Vector2(340, 100), click, Sand);

    Button MakeButton(string text, Vector2 pos, Vector2 size, UnityAction click, Color color)
    {
        var anchor = new Vector2(0.5f, 0f);
        var b = R("Btn" + text, canvas, anchor, anchor, pos, size);
        var img = b.gameObject.AddComponent<Image>();
        img.color = color;
        var btn = b.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(click);
        var cb = btn.colors;
        cb.disabledColor = new Color(0.5f, 0.45f, 0.4f, 0.75f);
        btn.colors = cb;
        Txt(R("Label", b, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), text, 44, Ink, FontStyles.Bold);
        return btn;
    }

    void AddButtonAt(string text, Vector2 pos, Vector2 anchor, UnityAction click)
    {
        var b = R("Btn" + text, canvas, anchor, anchor, pos, new Vector2(240, 90));
        var img = b.gameObject.AddComponent<Image>();
        img.color = Sand;
        var btn = b.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(click);
        Txt(R("Label", b, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), text, 40, Ink, FontStyles.Bold);
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
