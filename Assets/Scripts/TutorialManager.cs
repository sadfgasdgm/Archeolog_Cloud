using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class TutorialManager : MonoBehaviour
{
    enum Step { Move, ScanOn, Find, StartDig, Spade, Brush, Extract, Clean, Assemble, Finish }

    [SerializeField] string loadingScene = "Loading";
    [SerializeField] string menuScene = "MainMenu";
    [SerializeField] float hintArrowDelay = 12f;   // через сколько секунд показать стрелку

    static readonly Color Dark = new Color32(0x2B, 0x1D, 0x12, 235);
    static readonly Color Sand = new Color32(0xE3, 0xC1, 0x8A, 255);
    static readonly Color Amber = new Color32(0xF2, 0xA9, 0x2E, 255);
    static readonly Color Cream = new Color32(0xF7, 0xEB, 0xD0, 255);
    static readonly Color Ink = new Color32(0x3B, 0x2A, 0x1E, 255);

    Step step;
    float stepTime;
    PlayerController player;
    Scanner scanner;
    CameraFollow cam;
    Vector3 startPos;
    float startYaw;
    bool moved, rotated, finishing;

    TMP_Text titleText, bodyText, counterText;
    GameObject arrow, btnSkip, btnFinish;
    Transform arrowTarget;

    CameraFollow FindPlayerCameraFollow()
    {
        var follows = FindObjectsByType<CameraFollow>(FindObjectsSortMode.None);
        foreach (var follow in follows)
            if (follow != null && follow.isActiveAndEnabled && follow.HasTarget &&
                follow.ControlledCamera != null && follow.ControlledCamera.isActiveAndEnabled)
                return follow;
        return FindAnyObjectByType<CameraFollow>();
    }

    void EnsureMainCamera()
    {
        Camera c = cam != null ? cam.ControlledCamera : null;
        if (c == null || !c.isActiveAndEnabled) c = Camera.main;
        if (c == null)
            foreach (var x in Camera.allCameras) { c = x; break; }
        if (c == null) return;

        if (!c.CompareTag("MainCamera"))
        {
            var old = GameObject.FindWithTag("MainCamera");
            if (old != null && old != c.gameObject) old.tag = "Untagged";
            c.gameObject.tag = "MainCamera";
        }
    }

    void Start()
    {
        Time.timeScale = 1f;
        player = FindAnyObjectByType<PlayerController>();
        scanner = FindAnyObjectByType<Scanner>();
        cam = FindPlayerCameraFollow();
        if (player != null) startPos = player.transform.position;
        if (cam != null) startYaw = cam.transform.eulerAngles.y;
        EnsureMainCamera();

        BuildUI();
        BuildArrow();
        SetStep(Step.Move);
    }

    void OnDestroy() { Time.timeScale = 1f; }

    //  состояние шагов 
    void SetStep(Step s)
    {
        step = s;
        stepTime = 0f;
        arrowTarget = null;

        string t = "", b = "";
        switch (s)
        {
            case Step.Move:
                t = "Передвижение и камера";
                b = "Левым джойстиком идите по лагерю.\nСвайпом по правой части экрана поверните камеру\n(на ПК: WASD и правая кнопка мыши).\nКнопка «Быстрый шаг» ускоряет бег вне раскопа.";
                break;
            case Step.ScanOn:
                t = "Сканер";
                b = "Нажмите кнопку «Сканер», чтобы включить его.\nСканер тратит заряд, вне режима он восстанавливается.\nПока он включён, можно ходить и вращать камеру.";
                break;
            case Step.Find:
                t = "Поиск точки";
                b = "Ходите по участку и следите за шкалой сигнала:\nслабый, средний, сильный.\nКогда сигнал станет «точная зона» (до 1,5 м), нажмите «Отметить точку».";
                break;
            case Step.StartDig:
                t = "Начало раскопки";
                b = "Встаньте у отмеченной точки (радиус 1,1 м).\nНажмите контекстную кнопку «Начать раскопку».\nКопать можно только внутри контура.";
                break;
            case Step.Spade:
                t = "Лопата: верхний слой";
                b = "Выберите «Лопату» и снимите верхний слой грунта.\nЛопата быстрая, но рядом с находкой уменьшает сохранность.\nЕсли сохранность уйдёт в 0, раскоп перезапустится.";
                break;
            case Step.Brush:
                t = "Кисть: рядом с предметом";
                b = "Теперь выберите «Кисть» и расчистите находку.\nКисть не наносит повреждений.\nКнопка «Извлечь» станет активной после 70% освобождения.";
                break;
            case Step.Extract:
                t = "Извлечение";
                b = "Нажмите «Извлечь».\nНаходка отправится на лабораторный стол.";
                break;
            case Step.Clean:
                t = "Очистка на столе";
                b = "Подойдите к столу и нажмите «Очистить».\nСвайп кистью по грязи убирает её, свайп по пустому месту вращает предмет.\nПроверьте и обратную сторону. Нужно 95%, затем «Готово».";
                break;
            case Step.Assemble:
                t = "Восстановление из фрагментов";
                b = "У стола нажмите «Восстановить».\nПеретащите фрагменты пальцем, поворачивайте кнопками ±15°.\nПри совпадении граней они соединятся сами.";
                break;
            case Step.Finish:
                t = "Обучение пройдено!";
                b = "Чаша в коллекции.\nНажмите «В экспедицию»\nи начните работу на Песчаной стоянке.";
                PlayerPrefs.SetInt("TutorialDone", 1);
                PlayerPrefs.SetInt("SelectedZone", 0);   // первая зона доступна и выбрана
                PlayerPrefs.Save();
                break;
        }

        titleText.text = t;
        bodyText.text = b;
        counterText.text = s == Step.Finish
            ? "Обучение пройдено"
            : "Шаг " + ((int)s + 1) + " из " + System.Enum.GetValues(typeof(Step)).Length;
        if (btnFinish != null) btnFinish.SetActive(s == Step.Finish);
        if (btnSkip != null) btnSkip.SetActive(s != Step.Finish);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        stepTime += Time.deltaTime;

        switch (step)
        {
            case Step.Move:
                if (player != null && Vector3.Distance(player.transform.position, startPos) > 3f) moved = true;
                if (cam != null && Mathf.Abs(Mathf.DeltaAngle(cam.transform.eulerAngles.y, startYaw)) > 30f) rotated = true;
                if (moved && rotated) SetStep(Step.ScanOn);
                break;

            case Step.ScanOn:
                if (scanner != null && scanner.IsOn) SetStep(Step.Find);
                break;

            case Step.Find:
                arrowTarget = stepTime > hintArrowDelay ? NearestUnfound() : null;
                if (AnyFound()) SetStep(Step.StartDig);
                break;

            case Step.StartDig:
                arrowTarget = FirstFoundPoint();
                if (DigSite.Current != null && DigSite.Current.Active) SetStep(Step.Spade);
                break;

            case Step.Spade:
                if (!Digging()) { SetStep(Step.StartDig); break; }
                if (DigSite.Current.FreedPercent >= 0.5f) SetStep(Step.Brush);   // 50%  весь верхний слой
                break;

            case Step.Brush:
                if (!Digging()) { SetStep(Step.StartDig); break; }
                if (DigSite.Current.CanExtract) SetStep(Step.Extract);
                break;

            case Step.Extract:
                if (!Digging()) SetStep(Step.StartDig);
                break;

            case Step.Clean:
            case Step.Assemble:
                arrowTarget = LabTransform();
                break;
        }

        UpdateArrow();
    }

    void OnEnable()
    {
        DigSite.OnExtracted += OnExtracted;
        LabStation.OnCleaned += OnCleaned;
        LabStation.OnAssembled += OnAssembled;
    }

    void OnDisable()
    {
        DigSite.OnExtracted -= OnExtracted;
        LabStation.OnCleaned -= OnCleaned;
        LabStation.OnAssembled -= OnAssembled;
    }

    void OnExtracted(DigSite s) { if (step <= Step.Extract) SetStep(Step.Clean); }
    void OnCleaned(ArtifactKind k) { if (step == Step.Clean) SetStep(Step.Assemble); }
    void OnAssembled(ArtifactKind k) { if (step == Step.Assemble) SetStep(Step.Finish); }

    //  завершение
    void Skip() => Leave(false);

    // обучение после полного прохождения сразу открывается карта экспедиций
    public void ToMenu() => Leave(true);

    void Leave(bool openMap)
    {
        if (finishing) return;
        finishing = true;
        if (openMap) PlayerPrefs.SetString("MenuOpen", "map");   // MainMenu откроет карту первая зона выбрана, остальные закрыты
        PlayerPrefs.SetInt("TutorialDone", 1);
        PlayerPrefs.SetInt("SelectedZone", 0);
        PlayerPrefs.Save();
        LoadingScreen.Next = menuScene;   // патч
        SceneManager.LoadScene(loadingScene);
    }

    // вспомогательное
    static bool Digging() => DigSite.Current != null && DigSite.Current.Active;

    static bool AnyFound()
    {
        foreach (var p in ScanPoint.All) if (p.Found) return true;
        return false;
    }

    static Transform FirstFoundPoint()
    {
        foreach (var p in ScanPoint.All) if (p.Found && p.Visible) return p.transform;
        return null;
    }

    Transform NearestUnfound()
    {
        if (player == null) return null;
        Transform best = null;
        float bd = float.MaxValue;
        foreach (var p in ScanPoint.All)
        {
            if (p.Found) continue;
            float d = Vector3.Distance(p.transform.position, player.transform.position);
            if (d < bd) { bd = d; best = p.transform; }
        }
        return best;
    }

    static Transform LabTransform()
    {
        var l = FindAnyObjectByType<LabStation>();
        return l != null ? l.transform : null;
    }

    // стрелка-подсказка над целью
    void BuildArrow()
    {
        arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arrow.name = "TutorialArrow";
        arrow.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        arrow.GetComponent<Renderer>().sharedMaterial = Mats.Glow(Amber);
        Destroy(arrow.GetComponent<Collider>());
        arrow.SetActive(false);
    }

    void UpdateArrow()
    {
        bool show = arrowTarget != null && !Digging() && !LabStation.Busy;
        if (arrow.activeSelf != show) arrow.SetActive(show);
        if (!show) return;
        arrow.transform.position = arrowTarget.position + Vector3.up * (2.2f + Mathf.Sin(Time.time * 4f) * 0.2f);
        arrow.transform.rotation = Quaternion.Euler(0, Time.time * 90f, 180f);
    }

    // интерфейс кодом
    void BuildUI()
    {
        var cgo = new GameObject("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25;
        var sc = cgo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;

        // панель слева по центру не перекрывает джойстик свайп камеры и инструменты
        var p = R("Panel", cgo.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(330, 130), new Vector2(620, 420));
        var bg = p.gameObject.AddComponent<Image>();
        bg.color = Dark;
        bg.raycastTarget = false;

        counterText = Txt(R("Counter", p, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(580, 34)), "", 26, Sand, FontStyles.Normal);
        titleText = Txt(R("Title", p, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(580, 56)), "", 40, Amber, FontStyles.Bold);
        bodyText = Txt(R("Body", p, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -215), new Vector2(580, 220)), "", 28, Cream, FontStyles.Normal);
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.alignment = TextAlignmentOptions.Top;

        // Пропустить обучение
        var sk = R("Skip", cgo.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(330, -120), new Vector2(380, 70));
        var img = sk.gameObject.AddComponent<Image>();
        img.color = Sand;
        var btn = sk.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(Skip);
        Txt(R("Label", sk, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "Пропустить обучение", 30, Ink, FontStyles.Bold);
        btnSkip = sk.gameObject;

        // В экспедицию  только на последнем шаге
        var fin = R("ToExpedition", cgo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(560, 110));
        var fim = fin.gameObject.AddComponent<Image>();
        fim.color = Amber;
        var fb = fin.gameObject.AddComponent<Button>();
        fb.targetGraphic = fim;
        fb.onClick.AddListener(ToMenu);
        Txt(R("Label", fin, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "В экспедицию", 46, Ink, FontStyles.Bold);
        btnFinish = fin.gameObject;
        btnFinish.SetActive(false);
    }

    static RectTransform R(string n, Transform parent, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(n, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
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
