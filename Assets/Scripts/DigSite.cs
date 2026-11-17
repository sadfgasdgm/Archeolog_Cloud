using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public enum DigTool { Spade, Scraper, Brush }

// что лежит в раскопе
public enum ArtifactKind
{
    Bowl, TabletLeft, TabletRight, Junk,
    Buckle1, Buckle2, Buckle3, Vessel,
    Panel1, Panel2, Panel3, Panel4, Panel5,
    Disc1, Disc2, Disc3, Disc4, Disc5, Disc6
}

public class DigChunk : MonoBehaviour
{
    public float amount = 3f;
    public bool nearArtifact;
    public bool removed;
    public DigChunk above;   // слой сверху (его нужно убрать первым)
    public DigChunk below;   // слой снизу (открывается после удаления этого)
}

// Материалы
public static class Mats
{
    static Shader Lit()
    {
        var s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        return s;
    }

    public static Material Make(Color c, float smooth = 0.1f)
    {
        var m = new Material(Lit());
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        return m;
    }

    public static Material Glow(Color c)
    {
        var m = Make(c, 0.2f);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 1.4f);
        return m;
    }
}

public class DigSite : MonoBehaviour
{
    public static DigSite Current { get; private set; }

    public static event System.Action<DigSite> OnExtracted;
    public static event System.Action<DigSite> OnFailed;    // сохранность 0 перезапуск
    public static event System.Action<DigSite> OnDamaged;   // сохранность снижена ударом рядом с находкой
    public static event System.Action<DigTool> OnToolHit;   // снят кусок грунта
    public static event System.Action<DigSite> OnRejected;  // сосуд ниже 80% раскоп перезапускается
    public const float VesselMin = 80f;                     // порог для сосуда (зона 2)

    public ArtifactKind kind = ArtifactKind.Bowl;

    [SerializeField] int size = 8;
    [SerializeField] float cell = 0.5f;
    [SerializeField] float layerHeight = 0.2f;
    [SerializeField] float nearRadius = 0.8f;
    [SerializeField] float extractThreshold = 0.7f;   // 70%
    [SerializeField] float hitInterval = 0.1f;
    [SerializeField] bool debugRay = false;           
    [SerializeField] Color topColor = new Color(0.78f, 0.64f, 0.42f);
    [SerializeField] Color deepColor = new Color(0.46f, 0.31f, 0.19f);

    public float Integrity { get; private set; } = 100f;   // старт 100%
    public float FinalIntegrity { get; private set; }
    public float FreedPercent => chunks.Count == 0 ? 0f : (float)removed / chunks.Count;
    public bool Active { get; private set; }
    public bool Extracted { get; private set; }
    public bool Failed { get; private set; }
    public bool CanExtract => Active && !Failed && FreedPercent >= extractThreshold;
    public DigTool Tool = DigTool.Spade;

    readonly List<DigChunk> chunks = new List<DigChunk>();
    GameObject artifact, outline;
    Material topMat, deepMat;
    PlayerController player;
    CameraFollow cam;
    int removed;
    float timer;

    //                              лопата  скребок  кисть
    static readonly float[] Power = { 3f, 1.5f, 0.6f };
    static readonly float[] Penalty = { 5f, 2f, 0f };

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        cam = FindPlayerCameraFollow();
        topMat = Mats.Make(topColor, 0.05f);
        deepMat = Mats.Make(deepColor, 0.05f);
        Build();
    }

    CameraFollow FindPlayerCameraFollow()
    {
        var follows = FindObjectsByType<CameraFollow>(FindObjectsSortMode.None);
        foreach (var follow in follows)
            if (follow != null && follow.isActiveAndEnabled && follow.HasTarget &&
                follow.ControlledCamera != null && follow.ControlledCamera.isActiveAndEnabled)
                return follow;


        return FindAnyObjectByType<CameraFollow>();
    }

    //  построение раскопа
    void Build()
    {
        artifact = MakeArtifact();

        float off = (size - 1) * cell / 2f;
        for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
            {
                var deep = MakeChunk(x, z, off, true);
                var top = MakeChunk(x, z, off, false);
                top.below = deep;
                deep.above = top;
            }
    }

    DigChunk MakeChunk(int x, int z, float off, bool deepLayer)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = deepLayer ? "SoilDeep" : "SoilTop";
        go.transform.SetParent(transform, false);
        float y = layerHeight * 0.5f + (deepLayer ? 0f : layerHeight);
        go.transform.localPosition = new Vector3(x * cell - off, y, z * cell - off);
        go.transform.localScale = new Vector3(cell * 0.98f, layerHeight, cell * 0.98f);
        go.GetComponent<Renderer>().sharedMaterial = deepLayer ? deepMat : topMat;

        var c = go.AddComponent<DigChunk>();
        var p = go.transform.localPosition;
        c.nearArtifact = deepLayer && new Vector2(p.x, p.z).magnitude < nearRadius;
        chunks.Add(c);
        go.SetActive(false);
        return c;
    }

    GameObject MakeArtifact()
    {
        GameObject g;
        switch (kind)
        {
            case ArtifactKind.Bowl:
                g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                g.transform.localScale = new Vector3(0.55f, 0.28f, 0.55f);
                g.transform.localPosition = new Vector3(0, 0.07f, 0);
                g.GetComponent<Renderer>().sharedMaterial = Mats.Make(new Color(0.72f, 0.40f, 0.25f), 0.3f);
                break;
            case ArtifactKind.TabletLeft:
            case ArtifactKind.TabletRight:
                g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.transform.localScale = new Vector3(0.5f, 0.08f, 0.36f);
                g.transform.localPosition = new Vector3(0, 0.08f, 0);
                g.transform.localRotation = Quaternion.Euler(0, kind == ArtifactKind.TabletLeft ? 20f : -15f, 0);
                g.GetComponent<Renderer>().sharedMaterial = Mats.Make(new Color(0.58f, 0.57f, 0.52f), 0.1f);
                break;
            case ArtifactKind.Buckle1:
            case ArtifactKind.Buckle2:
            case ArtifactKind.Buckle3:
                g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.transform.localScale = new Vector3(0.3f, 0.05f, 0.18f);
                g.transform.localPosition = new Vector3(0, 0.07f, 0);
                g.GetComponent<Renderer>().sharedMaterial = Mats.Make(new Color(0.65f, 0.45f, 0.2f), 0.5f);
                break;
            case ArtifactKind.Vessel:
                g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                g.transform.localScale = new Vector3(0.45f, 0.4f, 0.45f);
                g.transform.localPosition = new Vector3(0, 0.1f, 0);
                g.GetComponent<Renderer>().sharedMaterial = Mats.Make(new Color(0.6f, 0.35f, 0.22f), 0.3f);
                break;
            case ArtifactKind.Panel1:
            case ArtifactKind.Panel2:
            case ArtifactKind.Panel3:
            case ArtifactKind.Panel4:
            case ArtifactKind.Panel5:
            case ArtifactKind.Disc1:
            case ArtifactKind.Disc2:
            case ArtifactKind.Disc3:
            case ArtifactKind.Disc4:
            case ArtifactKind.Disc5:
            case ArtifactKind.Disc6:
                g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.transform.localScale = new Vector3(0.4f, 0.06f, 0.3f);
                g.transform.localPosition = new Vector3(0, 0.07f, 0);
                g.GetComponent<Renderer>().sharedMaterial = Mats.Make(new Color(0.7f, 0.6f, 0.35f), 0.3f);
                break;
            default: // ложная точка
                g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                g.transform.localScale = new Vector3(0.12f, 0.1f, 0.12f);
                g.transform.localPosition = new Vector3(0, 0.07f, 0);
                g.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                g.GetComponent<Renderer>().sharedMaterial = Mats.Make(new Color(0.45f, 0.5f, 0.55f), 0.6f);
                break;
        }
        g.name = "Artifact";
        g.transform.SetParent(transform, false);
        Destroy(g.GetComponent<Collider>());
        return g;
    }

    // контур зоны раскопки появляется после отметки точки
    public void ShowOutline()
    {
        if (outline != null || Extracted) return;
        outline = new GameObject("Outline");
        outline.transform.SetParent(transform, false);

        float len = size * cell + 0.3f;
        float h = len / 2f;
        const float t = 0.1f;
        var m = Mats.Glow(new Color(1f, 0.55f, 0.1f));
        Bar(new Vector3(0, 0.03f, h), new Vector3(len, 0.04f, t), m);
        Bar(new Vector3(0, 0.03f, -h), new Vector3(len, 0.04f, t), m);
        Bar(new Vector3(h, 0.03f, 0), new Vector3(t, 0.04f, len), m);
        Bar(new Vector3(-h, 0.03f, 0), new Vector3(t, 0.04f, len), m);
    }

    void Bar(Vector3 pos, Vector3 scale, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = "OutlineBar";
        g.transform.SetParent(outline.transform, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        Destroy(g.GetComponent<Collider>());
    }

    // раскопка
    static bool Visible(DigChunk c) => !c.removed && (c.above == null || c.above.removed);

    void Refresh()
    {
        foreach (var c in chunks) c.gameObject.SetActive(Active && Visible(c));
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;   // на паузе копать нельзя
        if (!Active || Failed) return;

        timer -= Time.deltaTime;
        if (timer > 0f || !Input.GetMouseButton(0) || Input.touchCount >= 2) return;

        // не копаем сквозь кнопки интерфейса
        if (EventSystem.current != null)
        {
            bool overUI = Input.touchCount > 0
                ? EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
                : EventSystem.current.IsPointerOverGameObject();
            if (overUI) return;
        }

        Camera mainCam = GetDigCamera();
        if (mainCam == null) return;

        // берём ближайший кусок грунта этого раскопа
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        DigChunk best = null;
        float bestDist = float.MaxValue;
        foreach (var h in Physics.RaycastAll(ray, 60f))
        {
            var ch = h.collider.GetComponent<DigChunk>();
            if (ch != null && ch.transform.parent == transform && h.distance < bestDist)
            {
                best = ch;
                bestDist = h.distance;
            }
        }

        if (debugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * 20f, Color.red, 0.5f);
            Debug.Log("DigSite: камера=" + mainCam.name + ", mouse=" + Input.mousePosition +
                      ", rect=" + mainCam.pixelRect + ", попадание=" + (best != null ? best.name : "нет"));
        }

        if (best != null)
        {
            Hit(best);
            timer = hitInterval;
        }
    }


    Camera GetDigCamera()
    {
        if (cam != null && cam.HasTarget)
        {
            var c = cam.ControlledCamera;
            if (c != null && c.isActiveAndEnabled && c.targetTexture == null) return c;
        }

        var m = Camera.main;
        if (m != null && m.isActiveAndEnabled) return m;

        // запасной вариант
        Camera best = null;
        foreach (var c in Camera.allCameras)
            if (c.targetTexture == null && (best == null || c.depth > best.depth)) best = c;
        return best;
    }

    void Hit(DigChunk c)
    {
        int t = (int)Tool;

        // сильное воздействие рядом с находкой снижает сохранность
        if (c.nearArtifact && Penalty[t] > 0f)
        {
            float previousIntegrity = Integrity;
            Integrity = Mathf.Max(0f, Integrity - Penalty[t]);
            if (Integrity < previousIntegrity)
                OnDamaged?.Invoke(this);
            if (Integrity <= 0f) { Fail(); return; }
        }

        c.amount -= Power[t];
        if (c.amount <= 0f && !c.removed)
        {
            c.removed = true;
            c.gameObject.SetActive(false);
            removed++;
            if (c.below != null) c.below.gameObject.SetActive(true);   // открылся нижний слой
            OnToolHit?.Invoke(Tool);   // звук удара инструмента
        }
    }

    public void SetTool(int index) => Tool = (DigTool)index;

    public void Begin()
    {
        if (Extracted || Active) return;
        Current = this;
        Active = true;
        Refresh();
        if (player != null)
        {
            player.CanMove = false;
            player.SendMessage("OnDigStart", SendMessageOptions.DontRequireReceiver);
        }
        if (cam != null) cam.EnterDig(transform);
    }

    public void Extract()
    {
        if (!CanExtract) return;

        // зоны 2 сосуд с сохранностью 1-79% не принимается, перекапывается только он
        if (kind == ArtifactKind.Vessel && Integrity < VesselMin)
        {
            Failed = true;
            OnRejected?.Invoke(this);
            Invoke(nameof(Restart), 1.5f);
            return;
        }

        Extracted = true;
        FinalIntegrity = Integrity;
        artifact.SetActive(false);
        if (outline != null) Destroy(outline);
        End();
        OnExtracted?.Invoke(this);   // дальше находку подхватят LabStation и MissionTracker
    }

    public void Cancel() => End();

    void End()
    {
        Active = false;
        Refresh();
        if (player != null) player.CanMove = true;
        if (cam != null) cam.ExitDig();
        if (Current == this) Current = null;
    }

    // Повторная раскопка уже принятого фрагмента
    // грунт исходный, сохранность 100%, остальные принятые фрагменты не затрагиваются
    public void Redo()
    {
        CancelInvoke(nameof(Restart));
        Extracted = false;
        Active = false;
        FinalIntegrity = 0f;
        if (artifact != null) artifact.SetActive(true);
        Restart();
        ShowOutline();
    }

    void Fail()
    {
        // сохранность 0  раскоп повреждён и перезапускается
        Failed = true;
        OnFailed?.Invoke(this);
        Invoke(nameof(Restart), 1.5f);
    }

    void Restart()
    {
        removed = 0;
        Integrity = 100f;
        Failed = false;
        foreach (var c in chunks)
        {
            c.amount = 3f;
            c.removed = false;
        }
        Refresh();
    }
}
