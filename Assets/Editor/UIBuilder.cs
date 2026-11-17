using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UIBuilder
{
    static readonly Color Dark = new Color32(0x2B, 0x1D, 0x12, 215);
    static readonly Color Sand = new Color32(0xE3, 0xC1, 0x8A, 255);
    static readonly Color Amber = new Color32(0xF2, 0xA9, 0x2E, 255);
    static readonly Color Cream = new Color32(0xF7, 0xEB, 0xD0, 255);
    static readonly Color Ink = new Color32(0x3B, 0x2A, 0x1E, 255);

    static Canvas canvas;

    [MenuItem("Tools/Build UI")]
    static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Остановите Play и запустите Tools/Build UI ещё раз.");
            return;
        }

        canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("В сцене нет Canvas."); return; }

        var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // ---------- джойстик ----------
        var joy = Find(canvas.transform, "JoystickBG");
        if (joy != null)
        {
            var rt = (RectTransform)joy;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(260, 260);
            rt.sizeDelta = new Vector2(300, 300);
            joy.GetComponent<Image>().color = new Color(Dark.r, Dark.g, Dark.b, 0.55f);

            var handle = Find(joy, "Handle");
            if (handle != null)
            {
                var h = (RectTransform)handle;
                h.anchorMin = h.anchorMax = new Vector2(0.5f, 0.5f);
                h.pivot = new Vector2(0.5f, 0.5f);
                h.anchoredPosition = Vector2.zero;
                h.sizeDelta = new Vector2(130, 130);
                var hi = handle.GetComponent<Image>();
                hi.color = new Color(Sand.r, Sand.g, Sand.b, 0.9f);
                hi.raycastTarget = false;
            }
        }

        // ---------- сканер: заряд и сигнал (вверху слева) ----------
        var charge = Find(canvas.transform, "ChargeSlider");
        if (charge != null)
        {
            Place(charge, new Vector2(0, 1), new Vector2(40, -40), new Vector2(400, 26));
            Bar(charge.GetComponent<Slider>(), Amber, 1f);
        }
        var sig = Find(canvas.transform, "SignalText");
        if (sig != null)
        {
            Place(sig, new Vector2(0, 1), new Vector2(40, -80), new Vector2(400, 44));
            Label(sig, 30, Cream, TextAlignmentOptions.Left, FontStyles.Bold);
        }

        // ---------- правая колонка кнопок ----------
        StyleBtn("BtnAction", new Vector2(-60, 60), new Vector2(340, 120), Amber, 38, "Действие");
        StyleBtn("BtnFast", new Vector2(-60, 200), new Vector2(340, 90), Sand, 32, "Быстрый шаг");
        StyleBtn("BtnMark", new Vector2(-60, 310), new Vector2(340, 90), Sand, 32, "Отметить");
        StyleBtn("BtnScanner", new Vector2(-60, 420), new Vector2(340, 90), Sand, 32, "Сканер");

        // ---------- панель раскопа ----------
        var panel = Find(canvas.transform, "DigPanel");
        if (panel != null)
        {
            Place(panel, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1000, 250));
            var pi = panel.GetComponent<Image>();
            if (pi == null) pi = panel.gameObject.AddComponent<Image>();
            pi.sprite = sprite;
            pi.type = Image.Type.Sliced;
            pi.color = Dark;

            var info = Find(panel, "InfoText");
            if (info != null)
            {
                Place(info, new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(960, 50));
                Label(info, 32, Cream, TextAlignmentOptions.Center, FontStyles.Bold);
            }
            var integ = Find(panel, "IntegritySlider");
            if (integ != null)
            {
                Place(integ, new Vector2(0.5f, 1), new Vector2(0, -85), new Vector2(600, 22));
                Bar(integ.GetComponent<Slider>(), new Color32(0x6B, 0xC2, 0x5A, 255), 1f);
            }

            ToolBtn(panel, "BtnSpade", -345, "Лопата");
            ToolBtn(panel, "BtnScraper", -115, "Скребок");
            ToolBtn(panel, "BtnBrush", 115, "Кисть");
            ToolBtn(panel, "BtnCancel", 345, "Отмена");
        }

        Wire();
        Set(panel, "cancelImage", Find(panel, "BtnCancel")?.GetComponent<Image>());


        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("UI готов. Нажмите Ctrl+S, чтобы сохранить сцену.");
    }

    // ---------- привязка полей и On Click ----------
    static void Wire()
    {
        var player = GameObject.Find("Player");
        var ui = GameObject.Find("UI");
        if (player == null || ui == null) { Debug.LogError("Нужны объекты Player и UI."); return; }

        var action = Find(canvas.transform, "BtnAction");
        var interactor = player.GetComponent<Interactor>();
        if (interactor != null && action != null)
        {
            Set(interactor, "button", action.GetComponent<Button>());
            Set(interactor, "label", action.GetComponentInChildren<TMP_Text>(true));
        }

        var scanner = player.GetComponent<Scanner>();
        var sui = ui.GetComponent<ScannerUI>();
        if (sui != null)
        {
            Set(sui, "scanner", scanner);
            Set(sui, "chargeSlider", Find(canvas.transform, "ChargeSlider")?.GetComponent<Slider>());
            Set(sui, "signalText", Find(canvas.transform, "SignalText")?.GetComponent<TMP_Text>());
            Set(sui, "markButton", Find(canvas.transform, "BtnMark")?.GetComponent<Button>());
        }

        var hud = ui.GetComponent<DigHUD>();
        var panel = Find(canvas.transform, "DigPanel");
        if (hud != null && panel != null)
        {
            Set(hud, "panel", panel.gameObject);
            Set(hud, "integritySlider", Find(panel, "IntegritySlider")?.GetComponent<Slider>());
            Set(hud, "infoText", Find(panel, "InfoText")?.GetComponent<TMP_Text>());

            var so = new SerializedObject(hud);
            var arr = so.FindProperty("toolImages");
            if (arr != null)
            {
                string[] names = { "BtnSpade", "BtnScraper", "BtnBrush" };
                arr.arraySize = 3;
                for (int i = 0; i < 3; i++)
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = Find(panel, names[i])?.GetComponent<Image>();
                so.ApplyModifiedProperties();
            }

            Hook(Find(panel, "BtnSpade"), b => UnityEventTools.AddIntPersistentListener(b.onClick, hud.SetTool, 0));
            Hook(Find(panel, "BtnScraper"), b => UnityEventTools.AddIntPersistentListener(b.onClick, hud.SetTool, 1));
            Hook(Find(panel, "BtnBrush"), b => UnityEventTools.AddIntPersistentListener(b.onClick, hud.SetTool, 2));
            Hook(Find(panel, "BtnCancel"), b => UnityEventTools.AddPersistentListener(b.onClick, hud.Cancel));
        }

        var pc = player.GetComponent<PlayerController>();
        if (pc != null) Hook(Find(canvas.transform, "BtnFast"), b => UnityEventTools.AddPersistentListener(b.onClick, pc.ToggleFast));
        if (scanner != null) Hook(Find(canvas.transform, "BtnMark"), b => UnityEventTools.AddPersistentListener(b.onClick, scanner.MarkPoint));
    }

    // ---------- вспомогательные ----------
    static Transform Find(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t != root && t.name == name) return t;
        return null;
    }

    static void Hook(Transform t, System.Action<Button> add)
    {
        if (t == null) return;
        var b = t.GetComponent<Button>();
        if (b == null) return;
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(b.onClick, i);
        add(b);
        EditorUtility.SetDirty(b);
    }

    static void Set(Object target, string field, Object value)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) return;
        p.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    static RectTransform Place(Transform t, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static void Label(Transform t, int size, Color color, TextAlignmentOptions align, FontStyles style)
    {
        var tx = t.GetComponent<TMP_Text>();
        if (tx == null) return;
        tx.fontSize = size;
        tx.color = color;
        tx.alignment = align;
        tx.fontStyle = style;
        tx.textWrappingMode = TextWrappingModes.NoWrap;
    }

    static void StyleBtn(string name, Vector2 pos, Vector2 size, Color bg, int fontSize, string text)
    {
        var t = Find(canvas.transform, name);
        if (t == null) return;
        Place(t, new Vector2(1, 0), pos, size);
        Skin(t, bg, fontSize, text);
    }

    static void ToolBtn(Transform panel, string name, float x, string text)
    {
        var t = Find(panel, name);
        if (t == null) return;
        Place(t, new Vector2(0.5f, 1), new Vector2(x, -140), new Vector2(210, 80));
        Skin(t, Sand, 32, text);
    }

    static void Skin(Transform t, Color bg, int fontSize, string text)
    {
        var img = t.GetComponent<Image>();
        if (img != null) img.color = bg;

        var b = t.GetComponent<Button>();
        if (b != null)
        {
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.96f, 0.88f);
            cb.pressedColor = new Color(0.75f, 0.7f, 0.6f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
            b.colors = cb;
        }

        var tx = t.GetComponentInChildren<TMP_Text>(true);
        if (tx != null)
        {
            tx.text = text;
            tx.fontSize = fontSize;
            tx.color = Ink;
            tx.fontStyle = FontStyles.Bold;
            tx.alignment = TextAlignmentOptions.Center;
            tx.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    static void Bar(Slider s, Color fill, float value)
    {
        if (s == null) return;
        s.interactable = false;
        s.value = value;

        var bg = s.transform.Find("Background");
        if (bg != null) bg.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        if (s.fillRect != null) s.fillRect.GetComponent<Image>().color = fill;
        if (s.handleRect != null) s.handleRect.gameObject.SetActive(false);
    }
}