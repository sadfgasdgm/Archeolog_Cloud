using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class MainMenu : MonoBehaviour
{
    [SerializeField] Button continueButton;

    static readonly Color Sand = new Color32(0xE3, 0xC1, 0x8A, 255);
    static readonly Color Amber = new Color32(0xF2, 0xA9, 0x2E, 255);
    static readonly Color Cream = new Color32(0xF7, 0xEB, 0xD0, 255);
    static readonly Color Ink = new Color32(0x3B, 0x2A, 0x1E, 255);
    static readonly Color Dark = new Color32(0x2B, 0x1D, 0x12, 245);
    static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);

    Transform canvas;
    GameObject settingsPanel, collectionPanel, mapPanel, detailPanel;
    Slider sSound, sMusic, sSens;
    TMP_Dropdown dGfx, dVib;
    GameObject previewRoot;
    Transform previewModel;
    RenderTexture previewRT;

    // все предметы кампании Коллекция 
    struct Item { public string name, key, desc; public int zone, shape; public Vector2 sil; }

    static readonly Item[] Items =
    {
        new Item { name = "Керамическая чаша",      key = "ItemInt_Bowl",   zone = 0, shape = 0, sil = new Vector2(170, 100), desc = "Глиняная чаша для воды и еды. Найдена на открытом песчаном участке." },
        new Item { name = "Каменная табличка",      key = "ItemInt_TTablet", zone = 0, shape = 1, sil = new Vector2(190, 90),  desc = "Табличка из двух фрагментов. Соединена в лаборатории по линии разлома." },
        new Item { name = "Бронзовая пряжка",       key = "ItemInt_Buckle", zone = 1, shape = 2, sil = new Vector2(120, 120), desc = "Пряжка из трёх фрагментов, найденная рядом с корнями старого поселения." },
        new Item { name = "Глиняный сосуд",         key = "ItemInt_Vessel", zone = 1, shape = 3, sil = new Vector2(110, 150), desc = "Цельный сосуд. Очень хрупкий: лопата рядом с ним опасна." },
        new Item { name = "Декоративная панель",    key = "ItemInt_Panel",  zone = 2, shape = 4, sil = new Vector2(200, 120), desc = "Панель из пяти фрагментов, собранная из находок на трёх террасах." },
        new Item { name = "Церемониальный диск",    key = "ItemInt_Disc",   zone = 3, shape = 5, sil = new Vector2(150, 150), desc = "Диск из шести фрагментов из центральной камеры подземной пещеры." },
    };

    void Start()
    {
        Time.timeScale = 1f;
        canvas = transform;
        AudioListener.volume = SettingsStore.Sound;

        // масштаб по высоте
        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
        }

        StyleLayout();
        BuildSettings();
        BuildCollection();

        if (continueButton == null)
        {
            var t = Find("BtnContinue");
            if (t != null) continueButton = t.GetComponent<Button>();
        }
        if (continueButton != null)
            continueButton.interactable = PlayerPrefs.HasKey("HasSave") && ExpeditionMap.Available(PlayerPrefs.GetInt("SelectedZone", 0));

        // после обучения открывается карта экспедиций с экрана результатов
        string open = PlayerPrefs.GetString("MenuOpen", "");
        if (open != "")
        {
            PlayerPrefs.DeleteKey("MenuOpen");
            if (open == "map") OnExpeditions();
            else if (open == "collection") OnCollection();
        }
    }

    void OnDestroy() { KillPreview(); }

    void Update()
    {
        if (previewModel != null) previewModel.Rotate(0f, 35f * Time.deltaTime, 0f, Space.World);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (detailPanel != null) CloseDetail();
            else if (settingsPanel.activeSelf) CloseSettings(false);
            else if (collectionPanel.activeSelf) collectionPanel.SetActive(false);
            else if (mapPanel != null && mapPanel.activeSelf) Destroy(mapPanel);
        }
    }

    //  действия кнопок
    public void OnExpeditions()
    {
        // Экспедиция открывает карту выбора зон
        mapPanel = ExpeditionMap.Show(transform, zone =>
        {
            Destroy(mapPanel);
            PlayerPrefs.SetInt("HasSave", 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(ExpeditionMap.SceneOf(zone));
        },
        () => Destroy(mapPanel));
    }

    // Продолжить последний выбранный доступный участок
    public void OnContinue()
    {
        int zone = PlayerPrefs.GetInt("SelectedZone", 0);
        if (ExpeditionMap.Available(zone))
            SceneManager.LoadScene(ExpeditionMap.SceneOf(zone));
    }

    public void OnCollection()
    {
        collectionPanel.transform.SetAsLastSibling();
        collectionPanel.SetActive(true);
    }

    public void OnSettings()
    {
        SettingsStore.Reload();
        RefreshSettings();
        settingsPanel.transform.SetAsLastSibling();
        settingsPanel.SetActive(true);
    }


    void CloseSettings(bool save)
    {
        if (save) SettingsStore.Commit();
        else SettingsStore.Reload();
        settingsPanel.SetActive(false);
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

    //  выравнивание и оформление 
    void StyleLayout()
    {
        var title = Find("Title");
        if (title != null)
        {
            var rt = (RectTransform)title;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -70);
            rt.sizeDelta = new Vector2(1500, 190);
            var tx = title.GetComponent<TMP_Text>();
            tx.fontSize = 130;
            tx.color = Cream;
            tx.fontStyle = FontStyles.Bold;
            tx.alignment = TextAlignmentOptions.Center;
            tx.textWrappingMode = TextWrappingModes.NoWrap;
        }

        var holder = Find("MenuButtons");
        if (holder != null)
        {
            var rt = (RectTransform)holder;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -330);
            rt.sizeDelta = new Vector2(620, 640);

            var lg = holder.GetComponent<VerticalLayoutGroup>();
            if (lg == null) lg = holder.gameObject.AddComponent<VerticalLayoutGroup>();
            lg.spacing = 24;
            lg.padding = new RectOffset(0, 0, 0, 0);
            lg.childAlignment = TextAnchor.UpperCenter;
            lg.childControlWidth = lg.childControlHeight = true;
            lg.childForceExpandWidth = lg.childForceExpandHeight = true;
        }

        foreach (var n in new[] { "BtnExpeditions", "BtnContinue", "BtnCollection", "BtnSettings" })
        {
            var t = Find(n);
            if (t != null) Skin(t, n == "BtnExpeditions" ? Amber : Sand);
        }

        Hook("BtnExpeditions", OnExpeditions);
        Hook("BtnContinue", OnContinue);
        Hook("BtnCollection", OnCollection);
        Hook("BtnSettings", OnSettings);
    }

    void Hook(string name, UnityAction action)
    {
        var t = Find(name);
        if (t == null) return;
        var b = t.GetComponent<Button>();
        if (b == null) return;
        b.onClick = new Button.ButtonClickedEvent();
        b.onClick.AddListener(action);
    }

    void Skin(Transform t, Color bg)
    {
        var img = t.GetComponent<Image>();
        if (img != null) img.color = bg;
        var b = t.GetComponent<Button>();
        if (b != null)
        {
            var c = b.colors;
            c.normalColor = Color.white;
            c.highlightedColor = new Color(1f, 0.96f, 0.88f);
            c.pressedColor = new Color(0.75f, 0.7f, 0.6f);
            c.selectedColor = Color.white;
            c.disabledColor = new Color(0.5f, 0.45f, 0.4f, 0.75f);
            b.colors = c;
        }
        var tx = t.GetComponentInChildren<TMP_Text>(true);
        if (tx != null)
        {
            tx.fontSize = 48;
            tx.color = Ink;
            tx.fontStyle = FontStyles.Bold;
            tx.alignment = TextAlignmentOptions.Center;
            tx.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    Transform Find(string n)
    {
        foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
            if (t.name == n) return t;
        return null;
    }

    // панель настроек
    void BuildSettings()
    {
        var p = Panel("SettingsPanel", new Vector2(820, 860));
        Txt(R("Title", p, Mid, Mid, new Vector2(0, 350), new Vector2(700, 90)), "Настройки", 70, Cream);
        CloseX(p, () => CloseSettings(false));

        sSound = MakeSlider(p, "Звук", 190, 0f, 1f, SettingsStore.Sound, v => SettingsStore.Sound = v);
        sMusic = MakeSlider(p, "Музыка", 50, 0f, 1f, SettingsStore.Music, v => SettingsStore.Music = v);
        sSens = MakeSlider(p, "Чувствительность камеры", -90, 0.05f, 0.5f, SettingsStore.Sens, v => SettingsStore.Sens = v);
        dGfx = MakeDrop(p, "Качество графики", -230, SettingsStore.GraphicsNames, SettingsStore.Graphics, v => SettingsStore.Graphics = v);
        dVib = MakeDrop(p, "Вибрация", -370, SettingsStore.VibrationNames, SettingsStore.Vibration, v => SettingsStore.Vibration = v);

        //  изменения применяются только по Сохранить
        Btn(p, "Сохранить", -480, Amber, () => CloseSettings(true));
        Btn(p, "Назад", -590, Sand, () => CloseSettings(false));

        settingsPanel = p.parent.gameObject;
        settingsPanel.SetActive(false);
    }

    void CloseX(RectTransform parent, UnityAction click)
    {
        var b = R("BtnClose", parent, Vector2.one, Vector2.one, new Vector2(-60, -60), new Vector2(84, 84));
        var img = Img(b, Sand, true);
        var btn = b.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(click);
        Txt(R("Label", b, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "×", 64, Ink);
    }

    // панель коллекции
   
    void BuildCollection()
    {
        var p = Panel("CollectionPanel", new Vector2(1500, 920));
        Txt(R("Title", p, Mid, Mid, new Vector2(0, 395), new Vector2(900, 90)), "Коллекция", 66, Cream);

        int found = 0;
        for (int i = 0; i < Items.Length; i++) if (IsFound(i)) found++;
        Txt(R("Info", p, Mid, Mid, new Vector2(0, 335), new Vector2(1200, 50)),
            "Предметов в коллекции: " + found + " из " + Items.Length, 34, Sand, FontStyles.Normal);

        // все четыре экспедиции пройдены
        bool all = true;
        for (int z = 0; z < ExpeditionMap.Count; z++) if (ExpeditionMap.Stars(z) <= 0) all = false;
        if (all)
            Txt(R("AllDone", p, Mid, Mid, new Vector2(0, 292), new Vector2(1200, 44)), "Все экспедиции завершены", 30, new Color32(0x6B, 0xC2, 0x5A, 255), FontStyles.Bold);

        for (int i = 0; i < Items.Length; i++)
        {
            int col = i % 3, row = i / 3;
            Card(p, i, new Vector2(-430 + col * 430, 110 - row * 330));
        }

        Btn(p, "Назад", -405, Amber, () => collectionPanel.SetActive(false));

        collectionPanel = p.parent.gameObject;
        collectionPanel.SetActive(false);
    }

    static bool IsFound(int i) => PlayerPrefs.GetFloat(Items[i].key, -1f) >= 0f;

    void Card(RectTransform parent, int index, Vector2 pos)
    {
        var it = Items[index];
        bool found = IsFound(index);

        var c = R("Card" + index, parent, Mid, Mid, pos, new Vector2(400, 300));
        var img = Img(c, found ? new Color32(0x4A, 0x36, 0x24, 255) : new Color32(0x33, 0x27, 0x1C, 255), found);

        if (found)
        {
            // тап по найденному предмету открывает карточку крупным планом
            var btn = c.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            int idx = index;
            btn.onClick.AddListener(() => OpenDetail(idx));
        }

        var sil = R("Sil", c, Mid, Mid, new Vector2(0, 55), it.sil);
        var si = sil.gameObject.AddComponent<Image>();
        si.raycastTarget = false;
        si.color = found ? new Color32(0xE3, 0xC1, 0x8A, 255) : new Color32(0x5B, 0x4B, 0x3A, 255);
        si.sprite = (it.shape == 0 || it.shape == 2 || it.shape == 3 || it.shape == 5) ? CircleSprite() : SquareSprite();

        var nm = Txt(R("Name", c, Mid, Mid, new Vector2(0, -70), new Vector2(370, 70)), found ? it.name : "?????", 30, Cream, FontStyles.Bold);
        nm.textWrappingMode = TextWrappingModes.Normal;
        var info = Txt(R("Info", c, Mid, Mid, new Vector2(0, -125), new Vector2(370, 50)),
            found ? "Сохранность: " + Mathf.RoundToInt(PlayerPrefs.GetFloat(it.key, 0f)) + "%" : "Не найден",
            26, found ? new Color32(0x6B, 0xC2, 0x5A, 255) : Sand, FontStyles.Normal);
        info.textWrappingMode = TextWrappingModes.Normal;
    }

    //  крупный план предмета 
    void OpenDetail(int index)
    {
        if (detailPanel != null) CloseDetail();
        var it = Items[index];

        var p = Panel("DetailPanel", new Vector2(1000, 880));
        detailPanel = p.parent.gameObject;

        Txt(R("Title", p, Mid, Mid, new Vector2(0, 375), new Vector2(900, 80)), it.name, 56, Cream);

        ShowPreview(it.shape);
        var raw = R("Preview", p, Mid, Mid, new Vector2(0, 115), new Vector2(440, 440)).gameObject.AddComponent<RawImage>();
        raw.texture = previewRT;
        raw.raycastTarget = false;

        Txt(R("Integrity", p, Mid, Mid, new Vector2(0, -135), new Vector2(900, 50)),
            "Лучшая сохранность: " + Mathf.RoundToInt(PlayerPrefs.GetFloat(it.key, 0f)) + "%", 38, new Color32(0x6B, 0xC2, 0x5A, 255));
        Txt(R("Exp", p, Mid, Mid, new Vector2(0, -185), new Vector2(900, 44)),
            "Найден в экспедиции: " + ExpeditionMap.NameOf(it.zone), 30, Sand, FontStyles.Normal);
        var d = Txt(R("Desc", p, Mid, Mid, new Vector2(0, -255), new Vector2(880, 110)), it.desc, 28, Cream, FontStyles.Normal);
        d.textWrappingMode = TextWrappingModes.Normal;

        Btn(p, "Назад", -375, Amber, CloseDetail);
    }

    void CloseDetail()
    {
        KillPreview();
        if (detailPanel != null) Destroy(detailPanel);
        detailPanel = null;
    }

    // вращающаяся модель
    void ShowPreview(int shape)
    {
        KillPreview();
        previewRT = new RenderTexture(512, 512, 16);
        previewRoot = new GameObject("Preview3D");
        previewRoot.transform.position = new Vector3(0f, -2000f, 0f);

        var cg = new GameObject("PreviewCam");
        cg.transform.SetParent(previewRoot.transform, false);
        cg.transform.localPosition = new Vector3(0f, 0.9f, -3f);
        cg.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        var cam = cg.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.17f, 0.12f, 0.08f, 1f);
        cam.fieldOfView = 35f;
        cam.nearClipPlane = 0.1f;
        cam.targetTexture = previewRT;

        var lg = new GameObject("PreviewLight");
        lg.transform.SetParent(previewRoot.transform, false);
        lg.transform.localPosition = new Vector3(1.5f, 3f, -2.5f);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 14f;
        l.intensity = 4f;

        var model = new GameObject("Model").transform;
        model.SetParent(previewRoot.transform, false);
        BuildModel(shape, model);
        previewModel = model;
    }

    void KillPreview()
    {
        previewModel = null;
        if (previewRoot != null) Destroy(previewRoot);
        previewRoot = null;
        if (previewRT != null)
        {
            previewRT.Release();
            Destroy(previewRT);
            previewRT = null;
        }
    }

    static void Part(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Color c)
    {
        var g = GameObject.CreatePrimitive(t);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = Mats.Make(c, 0.2f);
        Destroy(g.GetComponent<Collider>());
    }

    static void BuildModel(int shape, Transform m)
    {
        var clay = new Color(0.72f, 0.40f, 0.25f);
        var stone = new Color(0.62f, 0.61f, 0.56f);
        var bronze = new Color(0.72f, 0.5f, 0.2f);
        var bg = new Color(0.17f, 0.12f, 0.08f);
        switch (shape)
        {
            case 0: // чаша
                Part(m, PrimitiveType.Sphere, Vector3.zero, new Vector3(1.5f, 0.8f, 1.5f), clay);
                break;
            case 1: // табличка
                Part(m, PrimitiveType.Cube, Vector3.zero, new Vector3(1.7f, 0.22f, 1.2f), stone);
                break;
            case 2: // пряжка 
                Part(m, PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.4f, 0.08f, 1.4f), bronze);
                Part(m, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.8f, 0.1f, 0.8f), bg);
                Part(m, PrimitiveType.Cube, new Vector3(0f, 0.06f, 0.1f), new Vector3(0.12f, 0.08f, 1.0f), bronze);
                break;
            case 3: // сосуд
                Part(m, PrimitiveType.Sphere, new Vector3(0f, -0.1f, 0f), new Vector3(1.0f, 1.1f, 1.0f), clay);
                Part(m, PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(0.42f, 0.25f, 0.42f), clay);
                break;
            case 4: // панель
                Part(m, PrimitiveType.Cube, Vector3.zero, new Vector3(1.9f, 0.12f, 1.4f), stone);
                for (int i = 0; i < 5; i++)
                    Part(m, PrimitiveType.Cube, new Vector3(-0.72f + i * 0.36f, 0.08f, 0f), new Vector3(0.2f, 0.06f, 1.0f), clay);
                break;
            default: // диск
                Part(m, PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.6f, 0.07f, 1.6f), bronze);
                Part(m, PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0f), new Vector3(0.4f, 0.2f, 0.4f), stone);
                break;
        }
        m.localRotation = Quaternion.Euler(25f, 0f, 0f);
    }

    static Sprite circleSprite;
    static Sprite squareSprite;

    static Sprite CircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        var t = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var px = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                if (new Vector2(x - 32f, y - 32f).magnitude <= 30f) px[y * 64 + x] = Color.white;
        t.SetPixels(px);
        t.Apply();
        circleSprite = Sprite.Create(t, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        return circleSprite;
    }

    static Sprite SquareSprite()
    {
        if (squareSprite != null) return squareSprite;
        var t = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var px = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                if (x >= 2 && x < 62 && y >= 12 && y < 52) px[y * 64 + x] = Color.white;
        t.SetPixels(px);
        t.Apply();
        squareSprite = Sprite.Create(t, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        return squareSprite;
    }

    // конструкторы интерфейса
    RectTransform Panel(string name, Vector2 size)
    {
        var dim = R(name, canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(dim, new Color(0, 0, 0, 0.7f), true);
        var box = R("Box", dim, Mid, Mid, Vector2.zero, size);
        Img(box, Dark, true);
        return box;
    }

    void Btn(RectTransform parent, string text, float y, Color color, UnityAction click)
    {
        var b = R("Btn" + text, parent, Mid, Mid, new Vector2(0, y), new Vector2(560, 100));
        var img = Img(b, color, true);
        var btn = b.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(click);
        Txt(R("Label", b, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), text, 44, Ink);
    }

    // слайдер
    Slider MakeSlider(RectTransform parent, string label, float y, float min, float max, float val, UnityAction<float> cb)
    {
        var root = R(label + "Slider", parent, Mid, Mid, new Vector2(0, y), new Vector2(560, 36));
        Txt(R("Label", root, Mid, Mid, new Vector2(0, 58), new Vector2(680, 50)), label, 36, Cream, FontStyles.Normal);

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
        s.minValue = min; s.maxValue = max;
        s.value = val;
        s.onValueChanged.AddListener(cb);
        return s;
    }

    // выпадающий список
    TMP_Dropdown MakeDrop(RectTransform parent, string label, float y, string[] options, int value, UnityAction<int> cb)
    {
        var root = R(label + "Drop", parent, Mid, Mid, new Vector2(0, y), new Vector2(560, 76));
        Txt(R("Label", root, Mid, Mid, new Vector2(0, 66), new Vector2(680, 50)), label, 36, Cream, FontStyles.Normal);

        var bg = root.gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.55f);

        var d = root.gameObject.AddComponent<TMP_Dropdown>();
        d.targetGraphic = bg;

        var caption = R("Caption", root, Vector2.zero, Vector2.one, new Vector2(-12, 0), Vector2.zero);
        var ct = caption.gameObject.AddComponent<TextMeshProUGUI>();
        ct.text = options[Mathf.Clamp(value, 0, options.Length - 1)];
        ct.fontSize = 34;
        ct.color = Cream;
        ct.alignment = TextAlignmentOptions.Midline;
        ct.raycastTarget = false;
        d.captionText = ct;

        var tmpl = R("Template", root, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -84));
        var ti = tmpl.gameObject.AddComponent<Image>();
        ti.color = Dark;
        var item = R("Item", tmpl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var ii = item.gameObject.AddComponent<Image>();
        ii.color = Color.clear;
        ii.raycastTarget = false;
        var lbl = R("Item Label", item, Vector2.zero, Vector2.one, new Vector2(-12, 0), Vector2.zero);
        var it = lbl.gameObject.AddComponent<TextMeshProUGUI>();
        it.fontSize = 32;
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
