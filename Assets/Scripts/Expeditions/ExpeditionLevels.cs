using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;



public enum ExpeditionZone { Auto = 0, Forest = 2, Slope = 3, Cave = 4 }

// Зона 3
public class TerraceZone : MonoBehaviour
{
    public static event System.Action<int> OnVisited;          // 0 нижняя, 1 средняя, 2 верхняя
    public static readonly bool[] Visited = new bool[3];
    public static int VisitedCount { get { int n = 0; foreach (var v in Visited) if (v) n++; return n; } }

    public int index;
    public Bounds area;
    PlayerController player;

    void Update()
    {
        if (player == null) { player = FindAnyObjectByType<PlayerController>(); if (player == null) return; }
        if (Visited[index] || !area.Contains(player.transform.position)) return;
        Visited[index] = true;
        OnVisited?.Invoke(index);
    }
}

[DefaultExecutionOrder(-200)]
public class ExpeditionLevels : MonoBehaviour
{
    [SerializeField] ExpeditionZone zone = ExpeditionZone.Auto;
    [SerializeField] int seed = 11;

    public static ExpeditionLevels Instance { get; private set; }

    class SiteInfo { public string name; public Vector3 pos; }
    readonly Dictionary<ArtifactKind, SiteInfo> siteInfo = new Dictionary<ArtifactKind, SiteInfo>();

    Transform root;      // статичный декор
    Transform dyn;       // раскопы, лабораторный стол не склеиваются
    readonly List<Vector3> reserved = new List<Vector3>();                    //  радиус декор сюда не ставим
    readonly Dictionary<DigSite, GameObject> pairs = new Dictionary<DigSite, GameObject>();

    Material grassMat, pathMat, dirtMat, rockMat, rockMat2, woodMat, fenceMat, tentMat, darkMat, stoneMat, fireMat,
             leafMat, leafMat2, bushMat, waterMat, farMat, lowMat, midMat, upMat, rampMat, caveRock, caveFloor, glowMat;
    Color soilTop = new Color(0.78f, 0.64f, 0.42f), soilDeep = new Color(0.46f, 0.31f, 0.19f);

    void OnDestroy() { if (Instance == this) Instance = null; }
    void OnEnable() { DigSite.OnExtracted += HandleExtracted; }
    void OnDisable() { DigSite.OnExtracted -= HandleExtracted; }

    void Awake()
    {
        Instance = this;
        if (zone == ExpeditionZone.Auto) zone = FromScene(SceneManager.GetActiveScene().name);
        PlayerPrefs.SetInt("SelectedZone", (int)zone - 1);      // ключи автосохранения RunSave привязаны к зоне
        Random.InitState(seed);
        System.Array.Clear(TerraceZone.Visited, 0, TerraceZone.Visited.Length);

        root = new GameObject("Expedition" + (int)zone).transform;
        dyn = new GameObject("ExpeditionDynamic").transform;
        ClearOld();
        MakeMaterials();

        switch (zone)
        {
            case ExpeditionZone.Forest: BuildForest(); break;
            case ExpeditionZone.Slope: BuildSlope(); break;
            default: BuildCave(); break;
        }

        // мобильная производительность статичный декор склеивается в несколько
        try { StaticBatchingUtility.Combine(root.gameObject); } catch (System.Exception) { }

        // цели миссии и условия успеха/провала (ExpeditionMission.cs)
        if (FindAnyObjectByType<ExpeditionMission>() == null)
            gameObject.AddComponent<ExpeditionMission>().Init((int)zone - 1);
    }

    static ExpeditionZone FromScene(string n)
    {
        if (n.Contains("3")) return ExpeditionZone.Slope;
        if (n.Contains("4")) return ExpeditionZone.Cave;
        return ExpeditionZone.Forest;
    }

    
    //  ЗОНА 2 
    void BuildForest()
    {
        const float hx = 20f, hz = 15f;
        soilTop = new Color(0.34f, 0.25f, 0.15f); soilDeep = new Color(0.21f, 0.15f, 0.09f);
        bushMat = Mats.Make(new Color(0.22f, 0.40f, 0.18f), 0.05f);

        Vector2 start = new Vector2(-12f, 7.5f);
        Vector2[] pos = { new Vector2(-14f, -9f), new Vector2(-7f, 3f), new Vector2(9f, 7.5f), new Vector2(12f, -9f) };
        ArtifactKind[] kind = { ArtifactKind.Buckle1, ArtifactKind.Buckle2, ArtifactKind.Buckle3, ArtifactKind.Vessel };
        Vector3 camp = new Vector3(-15.5f, 0f, 12.5f);

        foreach (var p in pos) reserved.Add(new Vector3(p.x, p.y, 5f));
        reserved.Add(new Vector3(camp.x, camp.z, 6f));
        reserved.Add(new Vector3(start.x, start.y, 2.5f));
        reserved.Add(new Vector3(0f, 0f, 4.5f));                       // мост

        // земля: два берега + ручей посередине
        Slab(-hx - 0.5f, -1.5f, -hz - 0.5f, hz + 0.5f, 0f, grassMat, dirtMat);
        Slab(1.5f, hx + 0.5f, -hz - 0.5f, hz + 0.5f, 0f, grassMat, dirtMat);
        Prim(PrimitiveType.Cube, "Bed", root, new Vector3(0, -1.2f, 0), new Vector3(3f, 1f, hz * 2f + 1f), dirtMat, false);
        Prim(PrimitiveType.Cube, "Water", root, new Vector3(0, -0.45f, 0), new Vector3(3f, 0.06f, hz * 2f + 1f), waterMat, false);
        Prim(PrimitiveType.Cube, "FarGround", root, new Vector3(0, -0.6f, 0), new Vector3(400f, 1f, 400f), farMat, false);
        Invisible(new Vector3(0, 1f, (-hz - 1.6f) / 2f - 0.2f), new Vector3(3.2f, 2f, hz - 1.6f + 1f));
        Invisible(new Vector3(0, 1f, (hz + 1.6f) / 2f + 0.2f), new Vector3(3.2f, 2f, hz - 1.6f + 1f));
        for (int i = 0; i < 14; i++)                                     // камни у воды
        {
            float z = Random.Range(-hz + 1f, hz - 1f);
            if (Mathf.Abs(z) < 3f) continue;
            Prim(PrimitiveType.Sphere, "Pebble", root, new Vector3(Random.Range(-1.6f, 1.6f), -0.4f, z),
                 Vector3.one * Random.Range(0.3f, 0.7f), stoneMat, false);
        }

        // мостик 
        Prim(PrimitiveType.Cube, "Bridge", root, new Vector3(0, 0.04f, 0), new Vector3(4.8f, 0.16f, 3f), woodMat, true);
        for (int i = -4; i <= 4; i++)
            Prim(PrimitiveType.Cube, "Plank", root, new Vector3(i * 0.5f, 0.125f, 0), new Vector3(0.05f, 0.01f, 3f), darkMat, false);
        Rail(new Vector3(-2.4f, 0.12f, 1.55f), new Vector3(2.4f, 0.12f, 1.55f), 1.2f);
        Rail(new Vector3(-2.4f, 0.12f, -1.55f), new Vector3(2.4f, 0.12f, -1.55f), 1.2f);

        PerimeterFence(hx, hz);

        // тропы
        Trail(new[] { start, pos[1], new Vector2(-3.5f, 0.5f), new Vector2(0, 0), new Vector2(3.5f, 0.5f), pos[2] });
        Trail(new[] { pos[1], new Vector2(-10f, -3f), pos[0] });
        Trail(new[] { new Vector2(3.5f, 0.5f), new Vector2(8f, -4f), new Vector2(12.2f, -5.6f) });

        // руины поселения
        Ruin(new Vector2(-5f, -11f), 5f, 4f, 10f);
        Ruin(new Vector2(4f, 12f), 5f, 3.5f, -20f);
        Ruin(new Vector2(15f, 2f), 4f, 3f, 35f);

        // корни вокруг сосуда узкий проход ~1.5 м с севера и с запада
        Vector2 v = pos[3];
        Vector3 tA = new Vector3(9f, 0, -7.2f), tB = new Vector3(15.2f, 0, -8.6f), tC = new Vector3(12.5f, 0, -12.6f);
        BigTree(tA, 9f, 0.6f); BigTree(tB, 10f, 0.65f); BigTree(tC, 9.5f, 0.6f);
        Root(tA, new Vector3(11.4f, 0, -5.9f), 0.8f, true);
        Root(tB, new Vector3(12.9f, 0, -6.2f), 0.8f, true);
        Root(tA, new Vector3(8.8f, 0, -9.6f), 0.8f, true);
        Root(tC, new Vector3(9.8f, 0, -10.8f), 0.8f, true);
        Root(tC, new Vector3(15.3f, 0, -11f), 0.8f, true);
        Root(tB, new Vector3(16.8f, 0, -10.6f), 0.7f, true);

        // декоративные деревья 28 внутри зоны + лес за оградой
        int placed = 0;
        var spots = new List<Vector2>();
        for (int i = 0; i < 900 && placed < 28; i++)
        {
            Vector2 p = new Vector2(Random.Range(-hx + 1.5f, hx - 1.5f), Random.Range(-hz + 1.5f, hz - 1.5f));
            if (Mathf.Abs(p.x) < 3.4f || !Free(p, 1.2f)) continue;
            bool close = false;
            foreach (var s in spots) if (Vector2.Distance(s, p) < 3.2f) { close = true; break; }
            if (close) continue;
            spots.Add(p); placed++;
            var t = Tree(p, Random.Range(5.5f, 8.5f), Random.Range(0.25f, 0.42f), true);
            for (int r = 0; r < 2; r++)
            {
                float a = Random.value * Mathf.PI * 2f;
                Root(new Vector3(p.x, 0, p.y), new Vector3(p.x + Mathf.Cos(a) * 1.3f, 0, p.y + Mathf.Sin(a) * 1.3f), 0.25f, false);
            }
        }
        for (int i = 0; i < 44; i++)
        {
            float along = Random.Range(-hx - 8f, hx + 8f);
            Vector2 p;
            switch (i % 4)
            {
                case 0: p = new Vector2(along, hz + Random.Range(2f, 9f)); break;
                case 1: p = new Vector2(along, -hz - Random.Range(2f, 9f)); break;
                case 2: p = new Vector2(hx + Random.Range(2f, 9f), Random.Range(-hz - 8f, hz + 8f)); break;
                default: p = new Vector2(-hx - Random.Range(2f, 9f), Random.Range(-hz - 8f, hz + 8f)); break;
            }
            Tree(p, Random.Range(6.5f, 10.5f), Random.Range(0.3f, 0.5f), false);
        }
        for (int i = 0, b = 0; i < 300 && b < 26; i++)
        {
            Vector2 p = new Vector2(Random.Range(-hx + 1f, hx - 1f), Random.Range(-hz + 1f, hz - 1f));
            if (Mathf.Abs(p.x) < 3f || !Free(p, 0.8f)) continue;
            b++;
            if (b % 4 == 0) Rock(p, Random.Range(0.4f, 1f), true, 0f); else Bush(p, 0f);
        }

        Camp(camp, 0f);
        LabTable(camp + new Vector3(4f, 0, -0.5f), 0f);

        // точки раскопок имена уникальны для зоны, чтобы не пересекаться с Zone 1
        SpawnPoint("Z2_Point_1", new Vector3(pos[0].x, 0, pos[0].y), kind[0]);
        SpawnPoint("Z2_Point_2", new Vector3(pos[1].x, 0, pos[1].y), kind[1]);
        SpawnPoint("Z2_Point_3", new Vector3(pos[2].x, 0, pos[2].y), kind[2]);
        var vessel = SpawnPoint("Z2_Point_4", new Vector3(v.x, 0, v.y), kind[3]);
        SetField(vessel, "nearRadius", 1.4f);                            // сосуд у корней зона риска шире

        var sun = Sun(new Color(0.95f, 1f, 0.82f), 1.15f, Quaternion.Euler(55f, -20f, 0f));
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.60f, 0.72f, 0.62f); RenderSettings.fogDensity = 0.007f;
        PlacePlayer(start, new Vector3(1f, 0, -0.3f));
    }

    //  ЗОНА 3 
    void BuildSlope()
    {
        const float hx = 22f, hz = 20f;
        soilTop = new Color(0.58f, 0.46f, 0.33f); soilDeep = new Color(0.40f, 0.30f, 0.21f);
        bushMat = Mats.Make(new Color(0.55f, 0.50f, 0.28f), 0.05f);

        Vector2 start = new Vector2(1.5f, -13f);
        Vector3 camp = new Vector3(0f, 0f, -17f);
        Vector3[] p = {
            new Vector3(-15f, 0f, -14f), new Vector3(15f, 0f, -14f),     // нижняя
            new Vector3(-14f, 3.5f, 1f), new Vector3(12f, 3.5f, 3f),     // средняя
            new Vector3(8f, 7f, 15f) };                                  // верхняя
        foreach (var q in p) reserved.Add(new Vector3(q.x, q.z, 4.5f));
        reserved.Add(new Vector3(camp.x, camp.z, 6.5f));
        reserved.Add(new Vector3(start.x, start.y, 2.5f));
        for (float x = 3f; x <= 15f; x += 3f) reserved.Add(new Vector3(x, -5.5f, 3f));           // пандус
        for (float x = -22f; x <= 4f; x += 4f) reserved.Add(new Vector3(x, -5.5f, 2.5f));        // площадка
        for (float x = -17f; x <= -1f; x += 3f) reserved.Add(new Vector3(x, 6.5f, 3f));          // лестница

        // террасы
        Prim(PrimitiveType.Cube, "FarGround", root, new Vector3(0, -0.6f, 0), new Vector3(400f, 1f, 400f), farMat, false);
        Slab(-hx, hx, -hz, -4f, 0f, lowMat, rockMat);
        Slab(-hx, hx, -4f, 8f, 3.5f, midMat, rockMat2);
        Slab(-hx, 4f, -7f, -4f, 3.5f, midMat, rockMat2);                 // верхняя площадка пандуса
        Slab(-hx, hx, 8f, hz, 7f, upMat, rockMat);
        Slab(-6f, 0f, 5f, 8f, 7f, upMat, rockMat);                       // площадка лестницы

        // проход 1 пандус 10 м, 19 градусов
        RampMesh("RampA", new Vector3(14f, 0f, -5.5f), new Vector3(4f, 3.5f, -5.5f), 3f, -0.5f, rampMat);
        // проход 2: лестница, ступень 19 см
        Stairs(new Vector3(-16f, 3.5f, 6.5f), new Vector3(-6f, 7f, 6.5f), 3f, 18, rampMat);

        // перила по краям видимые + невидимые стены
        Rail(new Vector3(14f, 0f, -7f), new Vector3(4f, 3.5f, -7f), 1.2f);
        Rail(new Vector3(4f, 3.5f, -7f), new Vector3(-hx, 3.5f, -7f), 1.2f);
        Rail(new Vector3(4f, 3.5f, -4f), new Vector3(hx, 3.5f, -4f), 1.2f);
        Rail(new Vector3(-16f, 3.5f, 5f), new Vector3(-6f, 7f, 5f), 1.2f);
        Rail(new Vector3(-6f, 7f, 5f), new Vector3(0f, 7f, 5f), 1.2f);
        Rail(new Vector3(-hx, 7f, 8f), new Vector3(-6f, 7f, 8f), 1.2f);
        Rail(new Vector3(0f, 7f, 8f), new Vector3(hx, 7f, 8f), 1.2f);
        // периметр
        Rail(new Vector3(-hx, 0f, -hz), new Vector3(hx, 0f, -hz), 3f);
        Rail(new Vector3(-hx, 7f, hz), new Vector3(hx, 7f, hz), 3f);
        Rail(new Vector3(-hx, 0f, -hz), new Vector3(-hx, 0f, -7f), 3f);
        Rail(new Vector3(-hx, 3.5f, -7f), new Vector3(-hx, 3.5f, 8f), 3f);
        Rail(new Vector3(-hx, 7f, 8f), new Vector3(-hx, 7f, hz), 3f);
        Rail(new Vector3(hx, 0f, -hz), new Vector3(hx, 0f, -4f), 3f);
        Rail(new Vector3(hx, 3.5f, -4f), new Vector3(hx, 3.5f, 8f), 3f);
        Rail(new Vector3(hx, 7f, 8f), new Vector3(hx, 7f, hz), 3f);

        // фон скалы за склоном
        for (int i = 0; i < 9; i++)
        {
            float x = -42f + i * 10.5f + Random.Range(-3f, 3f);
            Prim(PrimitiveType.Cube, "Cliff", root, new Vector3(x, 5f, hz + Random.Range(8f, 16f)),
                 new Vector3(Random.Range(10f, 16f), Random.Range(14f, 24f), Random.Range(8f, 12f)),
                 i % 2 == 0 ? rockMat : rockMat2, false, new Vector3(0, Random.Range(-15f, 15f), 0));
        }
        for (int i = 0; i < 6; i++)
        {
            float z = -20f + i * 9f;
            Prim(PrimitiveType.Cube, "Cliff", root, new Vector3(hx + Random.Range(7f, 12f), 3f, z),
                 new Vector3(Random.Range(8f, 12f), Random.Range(8f, 14f), Random.Range(8f, 12f)), rockMat, false);
            Prim(PrimitiveType.Cube, "Cliff", root, new Vector3(-hx - Random.Range(7f, 12f), 3f, z),
                 new Vector3(Random.Range(8f, 12f), Random.Range(8f, 14f), Random.Range(8f, 12f)), rockMat2, false);
        }

        // валуны и сухие кусты на всех трёх террасах
        float[] zmin = { -19f, -3f, 9f }, zmax = { -8.5f, 4.5f, 19f }, top = { 0f, 3.5f, 7f };
        int placed = 0;
        for (int i = 0; i < 900 && placed < 40; i++)
        {
            int t = placed % 3;
            Vector2 q = new Vector2(Random.Range(-hx + 1.5f, hx - 1.5f), Random.Range(zmin[t], zmax[t]));
            if (!Free(q, 1.3f)) continue;
            reserved.Add(new Vector3(q.x, q.y, 0.9f));
            placed++;
            if (placed % 4 == 0) Bush(q, top[t]); else Rock(q, Random.Range(0.5f, 1.6f), true, top[t]);
        }

        Camp(camp, 0f);
        LabTable(camp + new Vector3(4f, 0, -0.5f), 0f);

        for (int i = 0; i < p.Length; i++)
            SpawnPoint("Z3_Point_" + (i + 1), p[i], (ArtifactKind)((int)ArtifactKind.Panel1 + i));

        // обследована терраса
        MakeTerraceZone(0, new Bounds(new Vector3(0, 0.75f, -13.5f), new Vector3(44f, 3.5f, 13f)));
        MakeTerraceZone(1, new Bounds(new Vector3(0, 4.25f, 2.5f), new Vector3(44f, 3.5f, 11f)));
        MakeTerraceZone(2, new Bounds(new Vector3(0, 8f, 14f), new Vector3(44f, 4f, 12f)));

        Sun(new Color(1f, 0.93f, 0.8f), 1.25f, Quaternion.Euler(48f, -30f, 0f));
        PlacePlayer(start, Vector3.forward);
    }


    //  ЗОНА 4
    void BuildCave()
    {
        soilTop = new Color(0.40f, 0.33f, 0.27f); soilDeep = new Color(0.25f, 0.20f, 0.16f);

        // полы
        Floor(-9.5f, 9.5f, -9.5f, 9.5f);
        Floor(-2f, 2f, -17f, -9.5f);
        Floor(-7.8f, -2.2f, 9.5f, 15f);
        Floor(2.2f, 7.8f, 9.5f, 15f);
        Floor(-15f, -9.5f, -2.8f, 2.8f);
        Floor(9.5f, 15f, -2.8f, 2.8f);

        // стены камеры с проёмами: вход и 4 ниши
        const float H = 6f, NH = 4.5f;
        CaveWall(new Vector2(-9.5f, -9.5f), new Vector2(-2f, -9.5f), new Vector2(0, 1), 0, H);
        CaveWall(new Vector2(2f, -9.5f), new Vector2(9.5f, -9.5f), new Vector2(0, 1), 0, H);
        CaveWall(new Vector2(-2f, -9.5f), new Vector2(2f, -9.5f), new Vector2(0, 1), 4f, H);          // притолока входа
        CaveWall(new Vector2(-9.5f, 9.5f), new Vector2(-7.8f, 9.5f), new Vector2(0, -1), 0, H);
        CaveWall(new Vector2(-2.2f, 9.5f), new Vector2(2.2f, 9.5f), new Vector2(0, -1), 0, H);
        CaveWall(new Vector2(7.8f, 9.5f), new Vector2(9.5f, 9.5f), new Vector2(0, -1), 0, H);
        CaveWall(new Vector2(-7.8f, 9.5f), new Vector2(-2.2f, 9.5f), new Vector2(0, -1), NH, H);      // притолоки ниш
        CaveWall(new Vector2(2.2f, 9.5f), new Vector2(7.8f, 9.5f), new Vector2(0, -1), NH, H);
        CaveWall(new Vector2(-9.5f, -9.5f), new Vector2(-9.5f, -2.8f), new Vector2(1, 0), 0, H);
        CaveWall(new Vector2(-9.5f, 2.8f), new Vector2(-9.5f, 9.5f), new Vector2(1, 0), 0, H);
        CaveWall(new Vector2(-9.5f, -2.8f), new Vector2(-9.5f, 2.8f), new Vector2(1, 0), NH, H);
        CaveWall(new Vector2(9.5f, -9.5f), new Vector2(9.5f, -2.8f), new Vector2(-1, 0), 0, H);
        CaveWall(new Vector2(9.5f, 2.8f), new Vector2(9.5f, 9.5f), new Vector2(-1, 0), 0, H);
        CaveWall(new Vector2(9.5f, -2.8f), new Vector2(9.5f, 2.8f), new Vector2(-1, 0), NH, H);
        // коридор входа
        CaveWall(new Vector2(-2f, -17f), new Vector2(-2f, -9.5f), new Vector2(1, 0), 0, 4f);
        CaveWall(new Vector2(2f, -17f), new Vector2(2f, -9.5f), new Vector2(-1, 0), 0, 4f);
        // ниши
        NicheWalls(new Vector2(-7.8f, 9.5f), new Vector2(-2.2f, 15f), 'N', NH);
        NicheWalls(new Vector2(2.2f, 9.5f), new Vector2(7.8f, 15f), 'N', NH);
        NicheWalls(new Vector2(-15f, -2.8f), new Vector2(-9.5f, 2.8f), 'W', NH);
        NicheWalls(new Vector2(9.5f, -2.8f), new Vector2(15f, 2.8f), 'E', NH);
        // потолки видны только снизу
        Ceiling(-9.5f, 9.5f, -9.5f, 9.5f, H);
        Ceiling(-2f, 2f, -17f, -9.5f, 4f);
        Ceiling(-7.8f, -2.2f, 9.5f, 15f, NH); Ceiling(2.2f, 7.8f, 9.5f, 15f, NH);
        Ceiling(-15f, -9.5f, -2.8f, 2.8f, NH); Ceiling(9.5f, 15f, -2.8f, 2.8f, NH);

        // центр алтарь, колонны, ритуальный круг
        Prim(PrimitiveType.Cylinder, "Circle", root, new Vector3(0, 0.015f, 0), new Vector3(6.4f, 0.01f, 6.4f), darkMat, false);
        Prim(PrimitiveType.Cylinder, "Altar", root, new Vector3(0, 0.2f, 0), new Vector3(3.2f, 0.2f, 3.2f), stoneMat, true);
        Prim(PrimitiveType.Cylinder, "AltarGlow", root, new Vector3(0, 0.41f, 0), new Vector3(2.4f, 0.01f, 2.4f), glowMat, false);
        foreach (float sx in new[] { -5.5f, 5.5f })
            foreach (float sz in new[] { -3.5f, 3.5f })
            {
                Prim(PrimitiveType.Cylinder, "Column", root, new Vector3(sx, 3f, sz), new Vector3(1.1f, 3f, 1.1f), rockMat2, true);
                Prim(PrimitiveType.Sphere, "Brazier", root, new Vector3(sx, 0.35f, sz * 1.5f), new Vector3(0.5f, 0.35f, 0.5f), fireMat, false);
            }
        foreach (var c in new[] { new Vector2(-8.3f, -8.3f), new Vector2(8.3f, -8.3f), new Vector2(-8.3f, 8.3f), new Vector2(8.3f, 8.3f) })
            Prim(PrimitiveType.Sphere, "Stalagmite", root, new Vector3(c.x, 0.7f, c.y), new Vector3(0.7f, 1.8f, 0.7f), rockMat, true);
        for (int i = 0; i < 18; i++)                                       // сталактиты
        {
            Vector2 q = new Vector2(Random.Range(-9f, 9f), Random.Range(-9f, 9f));
            if (q.magnitude < 3f) continue;
            Prim(PrimitiveType.Sphere, "Stalactite", root, new Vector3(q.x, 5.6f, q.y),
                 new Vector3(0.45f, Random.Range(1.2f, 2.4f), 0.45f), rockMat, false);
        }
        // выход наружу светится
        var exit = Prim(PrimitiveType.Quad, "ExitGlow", root, new Vector3(0, 2f, -17.05f), new Vector3(4f, 4f, 1f), Mats.Glow(new Color(0.8f, 0.9f, 1f)), false, new Vector3(0, 180f, 0));
        Prim(PrimitiveType.Cube, "ExitStop", root, new Vector3(0, 2f, -17.6f), new Vector3(4.5f, 4f, 1f), caveRock, true);

        // стол у входа
        LabTable(new Vector3(-5.5f, 0, -7.5f), 0f);
        Prim(PrimitiveType.Cube, "Crate", root, new Vector3(-7.2f, 0.3f, -8.4f), new Vector3(0.6f, 0.6f, 0.6f), woodMat, true, new Vector3(0, 20f, 0));
        Prim(PrimitiveType.Cube, "Crate", root, new Vector3(-7.4f, 0.2f, -7.6f), new Vector3(0.4f, 0.4f, 0.4f), woodMat, true, new Vector3(0, -10f, 0));

        // свет грунт и находки читаемы везде
        PointLight(new Vector3(0, 5f, 0), new Color(1f, 0.85f, 0.65f), 17f, 5f);
        PointLight(new Vector3(-5f, 3.2f, 12f), new Color(1f, 0.8f, 0.55f), 9f, 4f);
        PointLight(new Vector3(5f, 3.2f, 12f), new Color(1f, 0.8f, 0.55f), 9f, 4f);
        PointLight(new Vector3(-12.2f, 3.2f, 0f), new Color(1f, 0.8f, 0.55f), 9f, 4f);
        PointLight(new Vector3(12.2f, 3.2f, 0f), new Color(1f, 0.8f, 0.55f), 9f, 4f);
        PointLight(new Vector3(-5.5f, 2.4f, -7.5f), new Color(1f, 0.75f, 0.45f), 7f, 3f);

        // раскопы: 1 + 2 + 2 + 1 = 6 фрагментов диска
        SpawnPoint("Z4_Point_1", new Vector3(-5f, 0, 12.3f), ArtifactKind.Disc1);
        SpawnPair("Z4_Point_2", new Vector3(5f, 0, 12.3f), ArtifactKind.Disc2, ArtifactKind.Disc3);
        SpawnPair("Z4_Point_3", new Vector3(-12.3f, 0, 0f), ArtifactKind.Disc4, ArtifactKind.Disc5);
        SpawnPoint("Z4_Point_4", new Vector3(12.3f, 0, 0f), ArtifactKind.Disc6);

        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (l.type == LightType.Directional) l.intensity = 0f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.34f, 0.31f, 0.38f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.05f, 0.04f, 0.06f); RenderSettings.fogDensity = 0.010f;
        foreach (var cam in Camera.allCameras)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
        }
        PlacePlayer(new Vector2(0f, -14f), Vector3.forward);
    }


    //  ТОЧКИ РАСКОПОК
    DigSite SpawnPoint(string name, Vector3 pos, ArtifactKind kind, bool dormant = false)
    {
        if (!siteInfo.ContainsKey(kind)) siteInfo[kind] = new SiteInfo { name = name, pos = pos };
        if (RunSave.HasExt(kind)) return null;           // находка уже принята в сохранённом прохождении

        var go = new GameObject(name);
        go.transform.SetParent(dyn, false);
        go.transform.position = pos;
        if (dormant) go.SetActive(false);                // Awake/Start сработают при включении
        var site = go.AddComponent<DigSite>();
        site.kind = kind;
        SetField(site, "topColor", soilTop);
        SetField(site, "deepColor", soilDeep);
        var sp = go.AddComponent<ScanPoint>();
        if (!dormant && PlayerPrefs.GetInt("Found_" + name, 0) == 1) sp.Restore();   // отмеченные точки
        return site;
    }

    // Два фрагмента в одном раскопе на разной глубине
    void SpawnPair(string name, Vector3 pos, ArtifactKind upper, ArtifactKind lower)
    {
        SpawnPoint(name + "_A", pos + Vector3.up * 0.4f, upper);
        SpawnPoint(name + "_B", pos, lower);
    }

    // повторная раскопка принятого фрагмента после загрузки сохранения сборка отклонена по сохранности
    public void RespawnSite(ArtifactKind kind)
    {
        SiteInfo i;
        if (!siteInfo.TryGetValue(kind, out i)) return;
        var s = SpawnPoint(i.name, i.pos, kind);
        if (s != null) s.GetComponent<ScanPoint>().Mark();
    }

    void HandleExtracted(DigSite s)
    {
        GameObject low;
        if (!pairs.TryGetValue(s, out low)) return;
        pairs.Remove(s);
        for (int i = s.transform.childCount - 1; i >= 0; i--)
        {
            var c = s.transform.GetChild(i);
            if (c.name.StartsWith("Soil")) Destroy(c.gameObject);
        }
        low.SetActive(true);
        var sp = low.GetComponent<ScanPoint>();
        if (sp != null) sp.Mark();                       // нижний слой сразу размечен, повторный скан не нужен
    }

    static void SetField(DigSite s, string field, object value)
    {
        if (s == null) return;
        var f = typeof(DigSite).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(s, value);
    }

    void MakeTerraceZone(int index, Bounds area)
    {
        var go = new GameObject("TerraceZone" + (index + 1));
        go.transform.SetParent(root, false);
        var tz = go.AddComponent<TerraceZone>();
        tz.index = index; tz.area = area;
    }


    //  ГЕОМЕТРИЯ
    void ClearOld()
    {
        foreach (var sp in FindObjectsByType<ScanPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            sp.gameObject.SetActive(false);
            Destroy(sp.gameObject);
        }
        var g = GameObject.Find("Ground"); if (g != null) g.SetActive(false);
        var old = GameObject.Find("SandCamp"); if (old != null) old.SetActive(false);
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

    void Invisible(Vector3 pos, Vector3 scale, Vector3 euler = default)
    {
        var w = Prim(PrimitiveType.Cube, "Wall", root, pos, scale, darkMat, true, euler);
        w.GetComponent<Renderer>().enabled = false;
    }

    bool Free(Vector2 p, float r)
    {
        foreach (var v in reserved)
            if (Vector2.Distance(p, new Vector2(v.x, v.y)) < v.z + r) return false;
        return true;
    }

    // терраса тело (стена-обрыв) + верхняя плита с текстурой
    void Slab(float x0, float x1, float z0, float z1, float top, Material plate, Material body)
    {
        float w = x1 - x0, d = z1 - z0, cx = (x0 + x1) / 2f, cz = (z0 + z1) / 2f;
        float bh = top - 0.05f + 1f;
        Prim(PrimitiveType.Cube, "TerraceBody", root, new Vector3(cx, -1f + bh / 2f, cz), new Vector3(w, bh, d), body, true);
        Prim(PrimitiveType.Cube, "TerraceTop", root, new Vector3(cx, top - 0.05f, cz), new Vector3(w + 0.02f, 0.1f, d + 0.02f), plate, true);
    }

    // сплошной пандус призма с наклонной верхней гранью без дыр под ним
    void RampMesh(string name, Vector3 a, Vector3 b, float width, float baseY, Material mat)
    {
        Vector3 dir = new Vector3(b.x - a.x, 0, b.z - a.z).normalized;
        Vector3 n = new Vector3(-dir.z, 0, dir.x) * (width / 2f);
        Vector3 t0 = a + n, t1 = a - n, t2 = b - n, t3 = b + n;
        Vector3 b0 = new Vector3(t0.x, baseY, t0.z), b1 = new Vector3(t1.x, baseY, t1.z),
                b2 = new Vector3(t2.x, baseY, t2.z), b3 = new Vector3(t3.x, baseY, t3.z);
        Vector3 c = (t0 + t1 + t2 + t3 + b0 + b1 + b2 + b3) / 8f;
        var v = new List<Vector3>(); var t = new List<int>();
        Quad(v, t, t0, t1, t2, t3, c); Quad(v, t, b0, b1, b2, b3, c);
        Quad(v, t, t0, t3, b3, b0, c); Quad(v, t, t1, t2, b2, b1, c);
        Quad(v, t, t0, t1, b1, b0, c); Quad(v, t, t3, t2, b2, b3, c);

        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        var m = new Mesh();
        m.SetVertices(v); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = m;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<MeshCollider>().sharedMesh = m;
    }

    static void Quad(List<Vector3> v, List<int> t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 center)
    {
        Vector3 fc = (p0 + p1 + p2 + p3) / 4f;
        bool outward = Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), fc - center) > 0f;
        int i = v.Count;
        v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3);
        if (outward) { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
        else { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
    }

    // лестница ступень не выше 0.25 м  прыжки не нужны
    void Stairs(Vector3 a, Vector3 b, float width, int steps, Material mat)
    {
        Vector3 d = new Vector3(b.x - a.x, 0, b.z - a.z);
        float len = d.magnitude;
        Vector3 dir = d / len;
        float tread = len / steps, rise = (b.y - a.y) / steps;
        Vector3 rot = Quaternion.LookRotation(dir).eulerAngles;
        float baseY = a.y - 0.5f;
        for (int i = 0; i < steps; i++)
        {
            float topY = a.y + rise * (i + 1);
            float h = topY - baseY;
            Vector3 c = new Vector3(a.x, 0, a.z) + dir * (tread * (i + 0.5f));
            Prim(PrimitiveType.Cube, "Step", root, new Vector3(c.x, baseY + h / 2f, c.z), new Vector3(width, h, tread), mat, true, rot);
        }
    }

    // перила: стойки + две планки + невидимая стена для коллизии
    void Rail(Vector3 a, Vector3 b, float colliderH)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.01f) return;
        Vector3 rot = Quaternion.LookRotation(d).eulerAngles;
        var f = new GameObject("Rail").transform;
        f.SetParent(root, false);
        int n = Mathf.Max(1, Mathf.CeilToInt(len / 2.2f));
        for (int i = 0; i <= n; i++)
            Prim(PrimitiveType.Cube, "Post", f, Vector3.Lerp(a, b, i / (float)n) + Vector3.up * 0.6f, new Vector3(0.12f, 1.2f, 0.12f), fenceMat, false);
        foreach (float h in new[] { 0.55f, 0.95f })
            Prim(PrimitiveType.Cube, "Bar", f, (a + b) / 2f + Vector3.up * h, new Vector3(0.06f, 0.06f, len), fenceMat, false, rot);
        var w = Prim(PrimitiveType.Cube, "RailWall", f, (a + b) / 2f + Vector3.up * (colliderH / 2f), new Vector3(0.25f, colliderH, len), fenceMat, true, rot);
        w.GetComponent<Renderer>().enabled = false;
    }

    void PerimeterFence(float hx, float hz)
    {
        Rail(new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz), 3f);
        Rail(new Vector3(hx, 0, -hz), new Vector3(hx, 0, hz), 3f);
        Rail(new Vector3(hx, 0, hz), new Vector3(-hx, 0, hz), 3f);
        Rail(new Vector3(-hx, 0, hz), new Vector3(-hx, 0, -hz), 3f);
    }

    void Trail(Vector2[] pts)
    {
        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector2 a = pts[i], b = pts[i + 1];
            float len = Vector2.Distance(a, b);
            for (float s = 0; s < len; s += 1f)
            {
                Vector2 p = Vector2.Lerp(a, b, s / len) + new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(-0.15f, 0.15f));
                if (Mathf.Abs(p.x) < 2.5f && Mathf.Abs(p.y) < 2f) continue;      // не на мосту
                Prim(PrimitiveType.Cube, "Trail", root, new Vector3(p.x, 0.012f, p.y), new Vector3(Random.Range(0.8f, 1.2f), 0.02f, Random.Range(0.8f, 1.2f)),
                     pathMat, false, new Vector3(0, Random.Range(0f, 90f), 0));
                if (((int)s) % 3 == 0) reserved.Add(new Vector3(p.x, p.y, 2f));
            }
        }
    }

    //  ДЕКОР
    Transform Tree(Vector2 p, float h, float r, bool solid)
    {
        var t = new GameObject("Tree").transform;
        t.SetParent(root, false);
        t.localPosition = new Vector3(p.x, 0, p.y);
        Prim(PrimitiveType.Cylinder, "Trunk", t, new Vector3(0, h / 2f, 0), new Vector3(r * 2f, h / 2f, r * 2f), woodMat, solid);
        var m = Random.value > 0.5f ? leafMat : leafMat2;
        float cr = h * 0.36f;
        Prim(PrimitiveType.Sphere, "Crown", t, new Vector3(0, h + cr * 0.2f, 0), new Vector3(cr * 2f, cr * 1.6f, cr * 2f), m, false);
        Prim(PrimitiveType.Sphere, "Crown", t, new Vector3(cr * 0.5f, h - cr * 0.2f, cr * 0.3f), new Vector3(cr * 1.4f, cr * 1.2f, cr * 1.4f), m, false);
        Prim(PrimitiveType.Sphere, "Crown", t, new Vector3(-cr * 0.4f, h - cr * 0.3f, -cr * 0.4f), new Vector3(cr * 1.3f, cr * 1.1f, cr * 1.3f), m, false);
        return t;
    }

    void BigTree(Vector3 p, float h, float r) { Tree(new Vector2(p.x, p.z), h, r, true); }

    // корень лежащий наполовину в земле цилиндр
    void Root(Vector3 from, Vector3 to, float thick, bool solid)
    {
        Vector3 d = to - from;
        float len = d.magnitude;
        var g = Prim(PrimitiveType.Cylinder, "Root", root, (from + to) / 2f + Vector3.up * 0.02f, new Vector3(thick, len / 2f, thick), woodMat, solid);
        g.transform.localRotation = Quaternion.LookRotation(d) * Quaternion.Euler(90f, 0, 0);
    }

    void Rock(Vector2 p, float size, bool solid, float y)
    {
        var mat = Random.value > 0.5f ? rockMat : rockMat2;
        Prim(PrimitiveType.Cube, "Rock", root, new Vector3(p.x, y + size * 0.25f, p.y),
             new Vector3(size, size * Random.Range(0.5f, 1f), size * Random.Range(0.7f, 1.2f)),
             mat, solid, new Vector3(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f)));
    }

    void Bush(Vector2 p, float y)
    {
        Prim(PrimitiveType.Sphere, "Bush", root, new Vector3(p.x, y + 0.25f, p.y), new Vector3(0.9f, 0.6f, 0.9f), bushMat, false);
        Prim(PrimitiveType.Sphere, "Bush", root, new Vector3(p.x + 0.4f, y + 0.18f, p.y + 0.2f), new Vector3(0.6f, 0.4f, 0.6f), bushMat, false);
    }

    void Ruin(Vector2 c, float w, float d, float yaw)
    {
        reserved.Add(new Vector3(c.x, c.y, Mathf.Max(w, d) * 0.6f + 0.8f));
        var g = new GameObject("Ruin").transform;
        g.SetParent(root, false);
        g.localPosition = new Vector3(c.x, 0, c.y);
        g.localRotation = Quaternion.Euler(0, yaw, 0);
        Prim(PrimitiveType.Cube, "Wall", g, new Vector3(0, 0.5f, d / 2f), new Vector3(w, 1f, 0.35f), rockMat, true);
        Prim(PrimitiveType.Cube, "Wall", g, new Vector3(-w / 2f, 0.4f, 0), new Vector3(0.35f, 0.8f, d), rockMat2, true);
        Prim(PrimitiveType.Cube, "Wall", g, new Vector3(w / 2f, 0.3f, d * 0.25f), new Vector3(0.35f, 0.6f, d * 0.5f), rockMat, true);
        Prim(PrimitiveType.Cube, "Wall", g, new Vector3(-w * 0.3f, 0.35f, -d / 2f), new Vector3(w * 0.4f, 0.7f, 0.35f), rockMat2, true);
        Prim(PrimitiveType.Cube, "Hearth", g, new Vector3(0, 0.05f, 0), new Vector3(1f, 0.1f, 1f), stoneMat, false);
    }

    // лагерь и стол
    void Camp(Vector3 pos, float yaw)
    {
        var camp = new GameObject("Camp").transform;
        camp.SetParent(root, false);
        camp.position = pos;
        camp.rotation = Quaternion.Euler(0, yaw, 0);

        var tent = new GameObject("Tent").transform;
        tent.SetParent(camp, false);
        tent.localPosition = new Vector3(0, 0, 1.2f);
        Prim(PrimitiveType.Cube, "TentL", tent, new Vector3(-0.55f, 0.5f, 0), new Vector3(1.7f, 0.06f, 2.4f), tentMat, true, new Vector3(0, 0, 50f));
        Prim(PrimitiveType.Cube, "TentR", tent, new Vector3(0.55f, 0.5f, 0), new Vector3(1.7f, 0.06f, 2.4f), tentMat, true, new Vector3(0, 0, -50f));
        Prim(PrimitiveType.Cube, "TentDoor", tent, new Vector3(0, 0.45f, -1.2f), new Vector3(1.0f, 0.9f, 0.05f), darkMat, false);

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
        var lg = new GameObject("FireLight");
        lg.transform.SetParent(fire, false);
        lg.transform.localPosition = new Vector3(0, 0.8f, 0);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.6f, 0.25f); l.range = 7f; l.intensity = 2f;
    }

    void LabTable(Vector3 pos, float yaw)
    {
        var table = new GameObject("LabTable").transform;
        table.SetParent(dyn, false);
        table.position = pos;
        table.rotation = Quaternion.Euler(0, yaw, 0);
        Prim(PrimitiveType.Cube, "Top", table, new Vector3(0, 0.85f, 0), new Vector3(1.8f, 0.08f, 0.9f), woodMat, true);
        foreach (float sx in new[] { -0.8f, 0.8f })
            foreach (float sz in new[] { -0.38f, 0.38f })
                Prim(PrimitiveType.Cube, "Leg", table, new Vector3(sx, 0.42f, sz), new Vector3(0.08f, 0.85f, 0.08f), woodMat, false);
        Prim(PrimitiveType.Cube, "Notebook", table, new Vector3(-0.5f, 0.92f, 0), new Vector3(0.4f, 0.04f, 0.3f), tentMat, false, new Vector3(0, 15f, 0));
        table.gameObject.AddComponent<LabStation>();
    }

    // пещера 
    void Floor(float x0, float x1, float z0, float z1)
    {
        Prim(PrimitiveType.Cube, "Floor", root, new Vector3((x0 + x1) / 2f, -0.25f, (z0 + z1) / 2f),
             new Vector3(x1 - x0, 0.5f, z1 - z0), caveFloor, true);
    }

    void Ceiling(float x0, float x1, float z0, float z1, float y)
    {
        var q = Prim(PrimitiveType.Quad, "Ceiling", root, new Vector3((x0 + x1) / 2f, y, (z0 + z1) / 2f),
                     new Vector3(x1 - x0, z1 - z0, 1f), caveRock, false, new Vector3(-90f, 0, 0));
    }

    // стена видимая лицевая грань смотрит внутрь (снаружи прозрачна для камеры + невидимое твёрдое тело
    void CaveWall(Vector2 a, Vector2 b, Vector2 inward, float y0, float y1)
    {
        Vector2 m = (a + b) / 2f;
        float len = Vector2.Distance(a, b), h = y1 - y0;
        Vector3 n = new Vector3(inward.x, 0, inward.y).normalized;
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "WallFace";
        q.transform.SetParent(root, false);
        q.transform.localPosition = new Vector3(m.x, y0 + h / 2f, m.y);
        q.transform.localRotation = Quaternion.LookRotation(-n);
        q.transform.localScale = new Vector3(len, h, 1f);
        q.GetComponent<Renderer>().sharedMaterial = caveRock;
        Destroy(q.GetComponent<Collider>());
        if (y0 > 0.01f) return;                                        // притолока коллизии не даёт
        Vector3 dir = new Vector3(b.x - a.x, 0, b.y - a.y);
        var w = Prim(PrimitiveType.Cube, "WallSolid", root, new Vector3(m.x, h / 2f, m.y) - n * 0.5f,
                     new Vector3(1f, h, len), caveRock, true, Quaternion.LookRotation(dir).eulerAngles);
        w.GetComponent<Renderer>().enabled = false;
    }

    void NicheWalls(Vector2 min, Vector2 max, char side, float h)
    {
        if (side == 'N')
        {
            CaveWall(new Vector2(min.x, min.y), new Vector2(min.x, max.y), new Vector2(1, 0), 0, h);
            CaveWall(new Vector2(max.x, min.y), new Vector2(max.x, max.y), new Vector2(-1, 0), 0, h);
            CaveWall(new Vector2(min.x, max.y), new Vector2(max.x, max.y), new Vector2(0, -1), 0, h);
        }
        else
        {
            float dx = side == 'W' ? 1f : -1f;
            float back = side == 'W' ? min.x : max.x;
            CaveWall(new Vector2(min.x, min.y), new Vector2(max.x, min.y), new Vector2(0, 1), 0, h);
            CaveWall(new Vector2(min.x, max.y), new Vector2(max.x, max.y), new Vector2(0, -1), 0, h);
            CaveWall(new Vector2(back, min.y), new Vector2(back, max.y), new Vector2(dx, 0), 0, h);
        }
    }

    void PointLight(Vector3 pos, Color c, float range, float intensity)
    {
        var g = new GameObject("CaveLight");
        g.transform.SetParent(root, false);
        g.transform.position = pos;
        var l = g.AddComponent<Light>();
        l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity;
    }

    // свет и игрок
    Light Sun(Color c, float intensity, Quaternion rot)
    {
        foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            l.color = c; l.intensity = intensity; l.transform.rotation = rot;
            return l;
        }
        return null;
    }

    void PlacePlayer(Vector2 pos, Vector3 look)
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p == null) return;
        var cc = p.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        p.transform.position = new Vector3(pos.x, p.transform.position.y + 0.05f, pos.y);
        p.transform.rotation = Quaternion.LookRotation(look);
        if (cc != null) cc.enabled = true;
    }

    //материалы 
    void MakeMaterials()
    {
        grassMat = TexMat(new Color(0.22f, 0.36f, 0.16f), new Color(0.33f, 0.47f, 0.22f), 12f);
        pathMat = Mats.Make(new Color(0.45f, 0.34f, 0.22f), 0.03f);
        dirtMat = Mats.Make(new Color(0.28f, 0.20f, 0.13f), 0.03f);
        farMat = TexMat(new Color(0.18f, 0.28f, 0.14f), new Color(0.26f, 0.36f, 0.19f), 60f);
        lowMat = TexMat(new Color(0.72f, 0.60f, 0.42f), new Color(0.85f, 0.72f, 0.52f), 10f);
        midMat = TexMat(new Color(0.66f, 0.50f, 0.34f), new Color(0.80f, 0.62f, 0.42f), 10f);
        upMat = TexMat(new Color(0.58f, 0.42f, 0.30f), new Color(0.72f, 0.54f, 0.38f), 10f);
        rampMat = Mats.Make(new Color(0.62f, 0.50f, 0.36f), 0.05f);
        rockMat = Mats.Make(new Color(0.52f, 0.45f, 0.38f), 0.1f);
        rockMat2 = Mats.Make(new Color(0.62f, 0.54f, 0.44f), 0.1f);
        woodMat = Mats.Make(new Color(0.33f, 0.22f, 0.13f), 0.1f);
        fenceMat = Mats.Make(new Color(0.9f, 0.5f, 0.15f), 0.1f);
        tentMat = Mats.Make(new Color(0.85f, 0.78f, 0.6f), 0.05f);
        darkMat = Mats.Make(new Color(0.12f, 0.08f, 0.05f), 0.05f);
        stoneMat = Mats.Make(new Color(0.4f, 0.4f, 0.4f), 0.1f);
        fireMat = Mats.Glow(new Color(1f, 0.55f, 0.1f));
        glowMat = Mats.Glow(new Color(0.35f, 0.75f, 0.9f));
        leafMat = Mats.Make(new Color(0.18f, 0.38f, 0.16f), 0.05f);
        leafMat2 = Mats.Make(new Color(0.25f, 0.45f, 0.18f), 0.05f);
        bushMat = Mats.Make(new Color(0.22f, 0.40f, 0.18f), 0.05f);
        waterMat = Mats.Make(new Color(0.18f, 0.42f, 0.55f), 0.9f);
        caveRock = TexMat(new Color(0.24f, 0.21f, 0.20f), new Color(0.38f, 0.34f, 0.31f), 4f, 0.05f);
        caveFloor = TexMat(new Color(0.30f, 0.26f, 0.22f), new Color(0.44f, 0.39f, 0.33f), 8f, 0.05f);
    }

    static Material TexMat(Color a, Color b, float tiling, float smooth = 0.02f)
    {
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (float)n, v = y / (float)n;
                float w = Mathf.Sin(2f * Mathf.PI * (3f * u + 2f * v)) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * (7f * u - 5f * v + 0.3f)) * 0.3f
                        + Mathf.Sin(2f * Mathf.PI * (11f * u + 9f * v)) * 0.2f;
                float grain = Random.value * 0.35f - 0.17f;
                px[y * n + x] = Color.Lerp(a, b, Mathf.Clamp01(0.5f + w * 0.3f + grain));
            }
        tex.SetPixels(px);
        tex.Apply();
        var m = Mats.Make(Color.white, smooth);
        var scale = new Vector2(tiling, tiling);
        if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", scale); }
        if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", tex); m.SetTextureScale("_MainTex", scale); }
        return m;
    }
}
