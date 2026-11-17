using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class PauseMenu : MonoBehaviour
{
    static readonly Color Dark = new Color32(0x2B, 0x1D, 0x12, 240);
    static readonly Color Sand = new Color32(0xE3, 0xC1, 0x8A, 255);
    static readonly Color Amber = new Color32(0xF2, 0xA9, 0x2E, 255);
    static readonly Color Cream = new Color32(0xF7, 0xEB, 0xD0, 255);
    static readonly Color Ink = new Color32(0x3B, 0x2A, 0x1E, 255);
    static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);

    GameObject pauseButton, overlay, menuPanel, settingsPanel, confirmPanel;
    TMP_Text confirmText;
    System.Action confirmYes;
    Slider sSound, sMusic, sSens;
    TMP_Dropdown dGfx, dVib;
    bool paused;

    // создаётся сам в любой игровой сцене
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (s, m) => Spawn();
        Spawn();
    }

    static void Spawn()
    {
        if (FindAnyObjectByType<PlayerController>() == null) return;
        if (FindAnyObjectByType<PauseMenu>() != null) return;
        new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioListener.volume = SettingsStore.Sound;
        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Build();
        SetPaused(false);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void OnEnable() => LabStation.OnBack += OnLabBack;
    void OnDisable() => LabStation.OnBack -= OnLabBack;

    // вышли из лаборатории без завершения - игра встаёт на паузу
    void OnLabBack() => SetPaused(true);

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (confirmPanel.activeSelf) HideConfirm();
        else if (settingsPanel.activeSelf) CloseSettings(false);
        else SetPaused(!paused);
    }

    void OnApplicationPause(bool pause)
    {
        if (pause && !paused && menuPanel != null) SetPaused(true);
    }

    void SetPaused(bool value)
    {
        if (settingsPanel.activeSelf) SettingsStore.Reload();   // несохранённые настройки не остаются
        paused = value;
        Time.timeScale = value ? 0f : 1f;
        AudioListener.pause = value;
        overlay.SetActive(value);
        menuPanel.SetActive(value);
        settingsPanel.SetActive(false);
        confirmPanel.SetActive(false);
        pauseButton.SetActive(!value);
        if (!value) PlayerPrefs.Save();
    }

    //  настройки 
    void ShowSettings(bool on)
    {
        if (on)
        {
            SettingsStore.Reload();
            RefreshSettings();
        }
        settingsPanel.SetActive(on);
        menuPanel.SetActive(!on);
    }

 
    void CloseSettings(bool save)
    {
        if (save) SettingsStore.Commit();
        else SettingsStore.Reload();
        ShowSettings(false);
    }

    void RefreshSettings()
    {
        sSound.SetValueWithoutNotify(SettingsStore.Sound);
        sMusic.SetValueWithoutNotify(SettingsStore.Music);
        sSens.SetValueWithoutNotify(SettingsStore.Sens);
        dGfx.SetValueWithoutNotify(SettingsStore.Graphics);
        dGfx.RefreshShownValue();
        dVib.SetValueWithoutNotify(SettingsStore.Vibration);
        dVib.RefreshShownValue();
    }

    // подтверждения
    void Confirm(string message, System.Action yes)
    {
        confirmYes = yes;
        confirmText.text = message;
        menuPanel.SetActive(false);
        confirmPanel.SetActive(true);
    }

    void HideConfirm()
    {
        confirmPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    void AskRestart()
    {
        Confirm("Начать экспедицию заново?\nНесохранённый прогресс текущего прохождения будет удалён. Лучшие результаты и коллекция сохранятся.", Restart);
    }

    void AskMenu()
    {
        if (HasUnsavedProgress())
            Confirm("После последнего автосохранения есть изменения.\nЕсли выйти, этот участок прогресса будет потерян. Выйти в главное меню?", ToMenu);
        else
            ToMenu();
    }

    // есть ли изменения после последнего автосохранения раскоп в процессе очистка/сборка открыта
    static bool HasUnsavedProgress()
    {
        foreach (var s in FindObjectsByType<DigSite>(FindObjectsSortMode.None))
            if (!s.Extracted && (s.Active || s.FreedPercent > 0f || s.Integrity < 100f)) return true;
        return LabStation.Busy;
    }

    // удаляем отметки точек текущего прохождения
    static void ClearRunProgress()
    {
        RunSave.Clear();   // принятые находки этап очистки/сборки открытые раскопы отметки точек зон 2-4 
        PlayerPrefs.DeleteKey("Found_ScanPoint");
        for (int i = 1; i <= 4; i++)
        {
            PlayerPrefs.DeleteKey("Found_ScanPoint_" + i);
            for (int copy = 1; copy <= 20; copy++)
                PlayerPrefs.DeleteKey("Found_ScanPoint_" + i + " (" + copy + ")");
        }
        PlayerPrefs.Save();
    }

    void Restart()
    {
        ClearRunProgress();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ToMenu()
    {
        SettingsStore.Reload();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("MainMenu");
    }

    // интерфейс кодом
    void Build()
    {
        var cgo = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var sc = cgo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        var root = (RectTransform)cgo.transform;

        // кнопка паузы
        var pb = R("BtnPause", root, Vector2.one, Vector2.one, new Vector2(-80, -80), new Vector2(110, 110));
        Img(pb, new Color(Sand.r, Sand.g, Sand.b, 0.9f), true);
        AddButton(pb, () => SetPaused(true));
        Txt(R("Label", pb, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "II", 60, Ink);
        pauseButton = pb.gameObject;

        // затемнение
        var ov = R("Overlay", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(ov, new Color(0, 0, 0, 0.65f), true);
        overlay = ov.gameObject;

        // главная панель
        var mp = R("MenuPanel", root, Mid, Mid, Vector2.zero, new Vector2(760, 780));
        Img(mp, Dark, true);
        Txt(R("Title", mp, Mid, Mid, new Vector2(0, 310), new Vector2(600, 90)), "Пауза", 72, Cream);
        Btn(mp, "Продолжить", 160, Amber, () => SetPaused(false));
        Btn(mp, "Настройки", 30, Sand, () => ShowSettings(true));
        Btn(mp, "Начать экспедицию заново", -100, Sand, AskRestart, 36, 680);
        Btn(mp, "Выйти в главное меню", -230, Sand, AskMenu, 38, 680);
        menuPanel = mp.gameObject;

        // настройки применяются по Сохранить в шапке заголовок и крестик
        var sp = R("SettingsPanel", root, Mid, Mid, Vector2.zero, new Vector2(760, 860));
        Img(sp, Dark, true);
        Txt(R("Title", sp, Mid, Mid, new Vector2(0, 360), new Vector2(600, 90)), "Настройки", 64, Cream);
        CloseX(sp, () => CloseSettings(false));
        sSound = MakeSlider(sp, "Звук", 210, 0f, 1f, SettingsStore.Sound, v => SettingsStore.Sound = v);
        sMusic = MakeSlider(sp, "Музыка", 80, 0f, 1f, SettingsStore.Music, v => SettingsStore.Music = v);
        sSens = MakeSlider(sp, "Чувствительность камеры", -50, 0.05f, 0.5f, SettingsStore.Sens, v => SettingsStore.Sens = v);
        dGfx = MakeDrop(sp, "Качество графики", -190, SettingsStore.GraphicsNames, SettingsStore.Graphics, v => SettingsStore.Graphics = v);
        dVib = MakeDrop(sp, "Вибрация", -330, SettingsStore.VibrationNames, SettingsStore.Vibration, v => SettingsStore.Vibration = v);
        Btn(sp, "Сохранить", -460, Amber, () => CloseSettings(true));
        Btn(sp, "Назад", -570, Sand, () => CloseSettings(false));
        settingsPanel = sp.gameObject;

        // подтверждение перезапуск/выход
        var cp = R("ConfirmPanel", root, Mid, Mid, Vector2.zero, new Vector2(880, 620));
        Img(cp, Dark, true);
        confirmText = Txt(R("Text", cp, Mid, Mid, new Vector2(0, 140), new Vector2(800, 300)), "", 38, Cream, FontStyles.Normal);
        confirmText.textWrappingMode = TextWrappingModes.Normal;
        Btn(cp, "Подтвердить", -130, Amber, () => { var y = confirmYes; confirmPanel.SetActive(false); y?.Invoke(); });
        Btn(cp, "Отмена", -245, Sand, HideConfirm);
        confirmPanel = cp.gameObject;
    }

    void CloseX(RectTransform parent, UnityAction click)
    {
        var b = R("BtnClose", parent, Vector2.one, Vector2.one, new Vector2(-60, -60), new Vector2(84, 84));
        Img(b, Sand, true);
        AddButton(b, click);
        Txt(R("Label", b, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "×", 64, Ink);
    }

    void Btn(RectTransform parent, string text, float y, Color color, UnityAction onClick, float font = 44f, float width = 560f)
    {
        var b = R("Btn" + text, parent, Mid, Mid, new Vector2(0, y), new Vector2(width, 100));
        Img(b, color, true);
        AddButton(b, onClick);
        Txt(R("Label", b, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), text, font, Ink);
    }

    Slider MakeSlider(RectTransform parent, string label, float y, float min, float max, float val, UnityAction<float> cb)
    {
        var root = R(label + "Slider", parent, Mid, Mid, new Vector2(0, y), new Vector2(560, 36));
        Txt(R("Label", root, Mid, Mid, new Vector2(0, 55), new Vector2(600, 50)), label, 34, Cream, FontStyles.Normal);

        var bg = R("Background", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(bg, new Color(0, 0, 0, 0.55f), true);
        var fa = R("Fill Area", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var fill = R("Fill", fa, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(fill, Amber);
        var ha = R("Handle Slide Area", root, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-34, 0));
        var h = R("Handle", ha, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(34, 18));
        var hi = Img(h, Cream, true);

        var s = root.gameObject.AddComponent<Slider>();
        s.fillRect = fill;
        s.handleRect = h;
        s.targetGraphic = hi;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = min; s.maxValue = max;
        s.value = val;
        s.onValueChanged.AddListener(cb);
        return s;
    }

    TMP_Dropdown MakeDrop(RectTransform parent, string label, float y, string[] options, int value, UnityAction<int> cb)
    {
        var root = R(label + "Drop", parent, Mid, Mid, new Vector2(0, y), new Vector2(560, 72));
        Txt(R("Label", root, Mid, Mid, new Vector2(0, 62), new Vector2(600, 50)), label, 34, Cream, FontStyles.Normal);

        var bg = root.gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.55f);

        var d = root.gameObject.AddComponent<TMP_Dropdown>();
        d.targetGraphic = bg;

        var caption = R("Caption", root, Vector2.zero, Vector2.one, new Vector2(-12, 0), Vector2.zero);
        var ct = caption.gameObject.AddComponent<TextMeshProUGUI>();
        ct.text = options[Mathf.Clamp(value, 0, options.Length - 1)];
        ct.fontSize = 32;
        ct.color = Cream;
        ct.alignment = TextAlignmentOptions.Midline;
        ct.raycastTarget = false;
        d.captionText = ct;

        var tmpl = R("Template", root, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -80));
        var ti = tmpl.gameObject.AddComponent<Image>();
        ti.color = Dark;
        var item = R("Item", tmpl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var ii = item.gameObject.AddComponent<Image>();
        ii.color = Color.clear;
        ii.raycastTarget = false;
        var lbl = R("Item Label", item, Vector2.zero, Vector2.one, new Vector2(-12, 0), Vector2.zero);
        var it = lbl.gameObject.AddComponent<TextMeshProUGUI>();
        it.fontSize = 30;
        it.color = Cream;
        it.alignment = TextAlignmentOptions.Midline;
        it.raycastTarget = false;
        d.itemText = it;
        d.template = tmpl;

        for (int i = 0; i < options.Length; i++)
            d.options.Add(new TMP_Dropdown.OptionData(options[i]));
        d.value = Mathf.Clamp(value, 0, options.Length - 1);
        d.onValueChanged.AddListener(cb);
        return d;
    }

    static void AddButton(RectTransform rt, UnityAction onClick)
    {
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = rt.GetComponent<Image>();
        b.onClick.AddListener(onClick);
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

    static Image Img(RectTransform rt, Color c, bool raycast = false)
    {
        var i = rt.gameObject.AddComponent<Image>();
        i.color = c;
        i.raycastTarget = raycast;
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
