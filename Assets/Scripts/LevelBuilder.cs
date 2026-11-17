using System.Collections.Generic;
using UnityEngine;


[DefaultExecutionOrder(-200)]
public class LevelBuilder : MonoBehaviour
{
    [Header("Зона 35x35 м")]
    [SerializeField] float half = 17.5f;
    [SerializeField] int seed = 7;

    [Header("Точки раскопок и артефакты")]
    [SerializeField]
    Vector2[] PointPos =
    {
        new Vector2(-10f, 8f), new Vector2(11f, 6f), new Vector2(-9f, -9f), new Vector2(10f, -10f)
    };

    [SerializeField]
    ArtifactKind[] PointKind =
    {
        ArtifactKind.Bowl, ArtifactKind.TabletLeft, ArtifactKind.TabletRight, ArtifactKind.Junk
    };

    static readonly Vector2 PlayerStart = new Vector2(1.5f, 10f);
    static readonly Vector3 Camp = new Vector3(0f, 0f, 14.5f);

    Transform root;
    Texture2D sandTex;
    Material sandMat, farMat, rockMat, rockMat2, woodMat, fenceMat, tentMat, darkMat, bushMat, fireMat, stoneMat;
    readonly List<Vector3> reserved = new List<Vector3>();  

    void Awake()
    {
        const string resetKey = "FoundMarksReset_v2";
        if (PlayerPrefs.GetInt(resetKey, 0) == 0)
        {
            for (int i = 1; i <= 4; i++)
            {
                PlayerPrefs.DeleteKey("Found_ScanPoint_" + i);

                for (int copy = 1; copy <= 20; copy++)
                    PlayerPrefs.DeleteKey("Found_ScanPoint_" + i + " (" + copy + ")");
            }

            PlayerPrefs.SetInt(resetKey, 1);
            PlayerPrefs.Save();
        }

        Random.InitState(seed);
        root = new GameObject("SandCamp").transform;

        ClearOld();
        MakeMaterials();

        foreach (var p in PointPos) reserved.Add(new Vector3(p.x, p.y, 4f));
        reserved.Add(new Vector3(Camp.x, Camp.z, 6f));
        reserved.Add(new Vector3(PlayerStart.x, PlayerStart.y, 2.5f));

        BuildGround();
        BuildFence();
        BuildScenery();
        BuildCamp();
        BuildPoints();
        SetupLight();
        PlacePlayer();

        if (FindAnyObjectByType<MissionTracker>() == null)
            gameObject.AddComponent<MissionTracker>();
    }

    // подготовка
    void ClearOld()
    {
        foreach (var sp in FindObjectsByType<ScanPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            sp.gameObject.SetActive(false);
            Destroy(sp.gameObject);
        }
        var oldGround = GameObject.Find("Ground");
        if (oldGround != null) oldGround.SetActive(false);
    }

    void MakeMaterials()
    {
        sandTex = SandTexture();
        sandMat = Mats.Make(Color.white, 0.02f);
        SetTex(sandMat, sandTex, 12f);
        farMat = Mats.Make(new Color(0.92f, 0.86f, 0.78f), 0.02f);
        SetTex(farMat, sandTex, 60f);

        rockMat = Mats.Make(new Color(0.52f, 0.45f, 0.38f), 0.1f);
        rockMat2 = Mats.Make(new Color(0.62f, 0.54f, 0.44f), 0.1f);
        woodMat = Mats.Make(new Color(0.33f, 0.22f, 0.13f), 0.1f);
        fenceMat = Mats.Make(new Color(0.9f, 0.5f, 0.15f), 0.1f);
        tentMat = Mats.Make(new Color(0.85f, 0.78f, 0.6f), 0.05f);
        darkMat = Mats.Make(new Color(0.12f, 0.08f, 0.05f), 0.05f);
        bushMat = Mats.Make(new Color(0.45f, 0.5f, 0.25f), 0.05f);
        fireMat = Mats.Glow(new Color(1f, 0.55f, 0.1f));
        stoneMat = Mats.Make(new Color(0.4f, 0.4f, 0.4f), 0.1f);
    }

    static Texture2D SandTexture()
    {
        const int n = 256;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, true);
        t.wrapMode = TextureWrapMode.Repeat;
        var px = new Color[n * n];
        Color light = new Color(0.88f, 0.74f, 0.50f);
        Color dark = new Color(0.72f, 0.57f, 0.36f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (float)n, v = y / (float)n;
                float w = Mathf.Sin(2f * Mathf.PI * (2f * u + 3f * v)) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * (5f * u - 2f * v + 0.3f)) * 0.3f
                        + Mathf.Sin(2f * Mathf.PI * (9f * u + 7f * v)) * 0.2f;
                float grain = Random.value * 0.3f - 0.15f;
                float k = Mathf.Clamp01(0.5f + w * 0.35f + grain);
                px[y * n + x] = Color.Lerp(dark, light, k);
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    static void SetTex(Material m, Texture2D t, float tiling)
    {
        var scale = new Vector2(tiling, tiling);
        if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", t); m.SetTextureScale("_BaseMap", scale); }
        if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", t); m.SetTextureScale("_MainTex", scale); }
    }

    GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale,
                    Material mat, bool collider, Vector3 euler = default)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.transform.localRotation = Quaternion.Euler(euler);
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Destroy(g.GetComponent<Collider>());
        return g;
    }

    bool Free(Vector2 p, float r)
    {
        foreach (var v in reserved)
            if (Vector2.Distance(p, new Vector2(v.x, v.y)) < v.z + r) return false;
        return true;
    }

    // земля и окружение 
    void BuildGround()
    {
        Prim(PrimitiveType.Cube, "SandGround", root, new Vector3(0, -0.5f, 0),
             new Vector3(half * 2f + 2f, 1f, half * 2f + 2f), sandMat, true);

        Prim(PrimitiveType.Cube, "FarGround", root, new Vector3(0, -0.6f, 0),
             new Vector3(400f, 1f, 400f), farMat, false);

        for (int i = 0; i < 16; i++)
        {
            float a = i / 16f * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
            float r = Random.Range(48f, 80f);
            var pos = new Vector3(Mathf.Cos(a) * r, -1.5f, Mathf.Sin(a) * r);
            var s = new Vector3(Random.Range(30f, 55f), Random.Range(5f, 10f), Random.Range(25f, 40f));
            Prim(PrimitiveType.Sphere, "Dune", root, pos, s, farMat, false, new Vector3(0, Random.Range(0f, 180f), 0));
        }
    }

    void BuildFence()
    {
        var f = new GameObject("Fence").transform;
        f.SetParent(root, false);

        const int seg = 14;
        Vector3[] c =
        {
            new Vector3(-half, 0, -half), new Vector3(half, 0, -half),
            new Vector3(half, 0, half), new Vector3(-half, 0, half)
        };

        for (int s = 0; s < 4; s++)
        {
            Vector3 a = c[s], b = c[(s + 1) % 4];
            for (int i = 0; i <= seg; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (float)seg);
                Prim(PrimitiveType.Cube, "Post", f, p + Vector3.up * 0.6f, new Vector3(0.12f, 1.2f, 0.12f), fenceMat, false);
            }

            Vector3 mid = (a + b) / 2f;
            bool alongX = Mathf.Abs(a.z - b.z) < 0.01f;
            Vector3 rail = alongX ? new Vector3(half * 2f, 0.06f, 0.06f) : new Vector3(0.06f, 0.06f, half * 2f);
            Prim(PrimitiveType.Cube, "Rail", f, mid + Vector3.up * 0.55f, rail, fenceMat, false);
            Prim(PrimitiveType.Cube, "Rail", f, mid + Vector3.up * 0.95f, rail, fenceMat, false);

            Vector3 ws = alongX ? new Vector3(half * 2f, 2.5f, 0.3f) : new Vector3(0.3f, 2.5f, half * 2f);
            var wall = Prim(PrimitiveType.Cube, "Wall", f, mid + Vector3.up * 1.25f, ws, fenceMat, true);
            wall.GetComponent<Renderer>().enabled = false;
        }
    }

    void BuildScenery()
    {
        for (int i = 0; i < 34; i++)
        {
            float along = Random.Range(-half - 6f, half + 6f);
            float off = half + Random.Range(2f, 8f);
            Vector2 p;
            switch (i % 4)
            {
                case 0: p = new Vector2(along, off); break;
                case 1: p = new Vector2(along, -off); break;
                case 2: p = new Vector2(off, along); break;
                default: p = new Vector2(-off, along); break;
            }
            Rock(p, Random.Range(1.5f, 4.5f), false);
        }

        int placed = 0;
        for (int i = 0; i < 200 && placed < 20; i++)
        {
            Vector2 p = new Vector2(Random.Range(-half + 1.5f, half - 1.5f), Random.Range(-half + 1.5f, half - 1.5f));
            if (!Free(p, 1.5f)) continue;
            placed++;
            if (placed % 3 == 0) Bush(p);
            else Rock(p, Random.Range(0.4f, 1.1f), true);
        }
    }

    void Rock(Vector2 p, float size, bool solid)
    {
        var mat = Random.value > 0.5f ? rockMat : rockMat2;
        Prim(PrimitiveType.Cube, "Rock", root, new Vector3(p.x, size * 0.25f, p.y),
             new Vector3(size, size * Random.Range(0.5f, 1f), size * Random.Range(0.7f, 1.2f)),
             mat, solid, new Vector3(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f)));
    }

    void Bush(Vector2 p)
    {
        Prim(PrimitiveType.Sphere, "Bush", root, new Vector3(p.x, 0.25f, p.y), new Vector3(0.9f, 0.6f, 0.9f), bushMat, false);
        Prim(PrimitiveType.Sphere, "Bush", root, new Vector3(p.x + 0.4f, 0.18f, p.y + 0.2f), new Vector3(0.6f, 0.4f, 0.6f), bushMat, false);
    }

    // лагерь 
    void BuildCamp()
    {
        var camp = new GameObject("Camp").transform;
        camp.SetParent(root, false);
        camp.position = Camp;

        var tent = new GameObject("Tent").transform;
        tent.SetParent(camp, false);
        tent.localPosition = new Vector3(0, 0, 1.2f);
        Prim(PrimitiveType.Cube, "TentL", tent, new Vector3(-0.55f, 0.5f, 0), new Vector3(1.7f, 0.06f, 2.4f), tentMat, true, new Vector3(0, 0, 50f));
        Prim(PrimitiveType.Cube, "TentR", tent, new Vector3(0.55f, 0.5f, 0), new Vector3(1.7f, 0.06f, 2.4f), tentMat, true, new Vector3(0, 0, -50f));
        Prim(PrimitiveType.Cube, "TentDoor", tent, new Vector3(0, 0.45f, -1.2f), new Vector3(1.0f, 0.9f, 0.05f), darkMat, false);

        var table = new GameObject("LabTable").transform;
        table.SetParent(camp, false);
        table.localPosition = new Vector3(4f, 0, -0.5f);
        Prim(PrimitiveType.Cube, "Top", table, new Vector3(0, 0.85f, 0), new Vector3(1.8f, 0.08f, 0.9f), woodMat, true);
        foreach (float sx in new[] { -0.8f, 0.8f })
            foreach (float sz in new[] { -0.38f, 0.38f })
                Prim(PrimitiveType.Cube, "Leg", table, new Vector3(sx, 0.42f, sz), new Vector3(0.08f, 0.85f, 0.08f), woodMat, false);
        Prim(PrimitiveType.Cube, "Notebook", table, new Vector3(-0.5f, 0.92f, 0), new Vector3(0.4f, 0.04f, 0.3f), tentMat, false, new Vector3(0, 15f, 0));

        table.gameObject.AddComponent<LabStation>();

        Prim(PrimitiveType.Cube, "Crate", camp, new Vector3(4f, 0.3f, -2.4f), new Vector3(0.6f, 0.6f, 0.6f), woodMat, true, new Vector3(0, 20f, 0));
        Prim(PrimitiveType.Cube, "Crate", camp, new Vector3(4.7f, 0.2f, -2.2f), new Vector3(0.4f, 0.4f, 0.4f), woodMat, true, new Vector3(0, -10f, 0));

        var fire = new GameObject("Campfire").transform;
        fire.SetParent(camp, false);
        fire.localPosition = new Vector3(-4f, 0, -1.5f);
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f;
            Prim(PrimitiveType.Cube, "Stone", fire, new Vector3(Mathf.Cos(a) * 0.5f, 0.08f, Mathf.Sin(a) * 0.5f),
                 new Vector3(0.2f, 0.16f, 0.2f), stoneMat, false, new Vector3(0, a * Mathf.Rad2Deg, 0));
        }
        Prim(PrimitiveType.Cube, "Log", fire, new Vector3(0, 0.1f, 0), new Vector3(0.9f, 0.12f, 0.12f), woodMat, false, new Vector3(0, 30f, 0));
        Prim(PrimitiveType.Cube, "Log", fire, new Vector3(0, 0.1f, 0), new Vector3(0.9f, 0.12f, 0.12f), woodMat, false, new Vector3(0, -30f, 0));
        Prim(PrimitiveType.Sphere, "Flame", fire, new Vector3(0, 0.38f, 0), new Vector3(0.35f, 0.5f, 0.35f), fireMat, false);

        var lightGo = new GameObject("FireLight");
        lightGo.transform.SetParent(fire, false);
        lightGo.transform.localPosition = new Vector3(0, 0.8f, 0);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.6f, 0.25f);
        l.range = 7f;
        l.intensity = 2f;
    }

    // точки интереса(с автосохранением
    void BuildPoints()
    {
        for (int i = 0; i < PointPos.Length; i++)
        {
            if (PointKind[i] != ArtifactKind.Junk && RunSave.HasExt(PointKind[i])) continue;

            var go = new GameObject("ScanPoint_" + (i + 1));
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(PointPos[i].x, 0f, PointPos[i].y);

            var site = go.AddComponent<DigSite>();
            site.kind = PointKind[i];
            go.AddComponent<ScanPoint>();

            if (PlayerPrefs.GetInt("Found_" + go.name, 0) == 1)
                go.GetComponent<ScanPoint>().Restore();
        }
    }

    //  свет и игрок
    void SetupLight()
    {
        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            l.color = new Color(1f, 0.93f, 0.8f);
            l.intensity = 1.2f;
            l.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
            break;
        }
    }

    void PlacePlayer()
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p == null) return;

        var cc = p.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        p.transform.position = new Vector3(PlayerStart.x, p.transform.position.y + 0.05f, PlayerStart.y);
        p.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, -1f));
        if (cc != null) cc.enabled = true;
    }
}