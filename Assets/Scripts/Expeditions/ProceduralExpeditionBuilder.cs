
using UnityEngine;


[DefaultExecutionOrder(-300)]
public class ProceduralExpeditionBuilder : MonoBehaviour
{
    public enum ExpeditionZone
    {
        ForestSettlement = 2,
        RockySlope = 3,
        UndergroundChamber = 4
    }

    [Header("Карта")]
    public ExpeditionZone zone = ExpeditionZone.ForestSettlement;
    public int seed = 22;
    public bool buildOnStart = true;
    public bool placeExistingPlayer = true;

    [Header("Префаб раскопа")]
    public GameObject digSitePrefab;

    Transform world;

    Material groundMat, grassMat, dirtMat, pathMat, rockMat, darkRockMat;
    Material woodMat, woodLightMat, leafMat, leafDarkMat, waterMat;
    Material goldMat, crystalMat, torchMat, stoneLightMat;

    Vector3 spawnPosition = new Vector3(0f, 0.1f, 12.8f);

    void Start()
    {
        if (buildOnStart)
            Build();
    }

    [ContextMenu("Build Expedition")]
    public void Build()
    {
        Transform old = transform.Find("GeneratedExpedition");

        if (old != null)
        {
            if (Application.isPlaying)
                Destroy(old.gameObject);
            else
                DestroyImmediate(old.gameObject);
        }

        world = new GameObject("GeneratedExpedition").transform;
        world.SetParent(transform, false);

        Random.InitState(seed + (int)zone * 100);

        MakeMaterials();

        switch (zone)
        {
            case ExpeditionZone.ForestSettlement:
                BuildForest();
                break;

            case ExpeditionZone.RockySlope:
                BuildSlope();
                break;

            case ExpeditionZone.UndergroundChamber:
                BuildCave();
                break;
        }

        if (placeExistingPlayer)
            PlacePlayer();
    }

    void MakeMaterials()
    {
        groundMat = Mats.Make(new Color(0.25f, 0.19f, 0.12f), 0.05f);
        grassMat = Mats.Make(new Color(0.20f, 0.34f, 0.14f), 0.02f);
        dirtMat = Mats.Make(new Color(0.38f, 0.25f, 0.14f), 0.02f);
        pathMat = Mats.Make(new Color(0.48f, 0.37f, 0.24f), 0.04f);
        rockMat = Mats.Make(new Color(0.34f, 0.36f, 0.35f), 0.12f);
        darkRockMat = Mats.Make(new Color(0.13f, 0.16f, 0.17f), 0.08f);

        woodMat = Mats.Make(new Color(0.22f, 0.12f, 0.065f), 0.08f);
        woodLightMat = Mats.Make(new Color(0.43f, 0.27f, 0.13f), 0.1f);

        leafMat = Mats.Make(new Color(0.10f, 0.29f, 0.12f), 0.02f);
        leafDarkMat = Mats.Make(new Color(0.055f, 0.19f, 0.09f), 0.02f);

        waterMat = Mats.Make(new Color(0.055f, 0.31f, 0.40f, 0.88f), 0.3f);

        goldMat = Mats.Make(new Color(0.72f, 0.47f, 0.13f), 0.55f);
        crystalMat = Mats.Glow(new Color(0.20f, 0.75f, 0.88f));
        torchMat = Mats.Glow(new Color(1f, 0.43f, 0.10f));

        stoneLightMat = Mats.Make(new Color(0.55f, 0.53f, 0.46f), 0.12f);
    }

    GameObject Prim(
        PrimitiveType type,
        string name,
        Vector3 pos,
        Vector3 scale,
        Material mat,
        bool collider = true,
        Vector3 rot = default)
    {
        GameObject g = GameObject.CreatePrimitive(type);

        g.name = name;
        g.transform.SetParent(world, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.transform.localRotation = Quaternion.Euler(rot);

        Renderer r = g.GetComponent<Renderer>();

        if (r != null && mat != null)
            r.sharedMaterial = mat;

        if (!collider)
        {
            Collider c = g.GetComponent<Collider>();

            if (c != null)
            {
                if (Application.isPlaying)
                    Destroy(c);
                else
                    DestroyImmediate(c);
            }
        }

        return g;
    }

    GameObject Cube(
        string n, Vector3 p, Vector3 s,
        Material m, bool col = true, Vector3 rot = default)
    {
        return Prim(PrimitiveType.Cube, n, p, s, m, col, rot);
    }

    GameObject Sphere(
        string n, Vector3 p, Vector3 s,
        Material m, bool col = false, Vector3 rot = default)
    {
        return Prim(PrimitiveType.Sphere, n, p, s, m, col, rot);
    }

    GameObject Cylinder(
        string n, Vector3 p, Vector3 s,
        Material m, bool col = true, Vector3 rot = default)
    {
        return Prim(PrimitiveType.Cylinder, n, p, s, m, col, rot);
    }

    // EXPEDITION 2 

    void BuildForest()
    {
        Cube("Forest_Base", new Vector3(0, -0.48f, 0),
            new Vector3(40, 0.96f, 30), groundMat);

        Cube("Forest_Grass", new Vector3(0, 0.025f, 0),
            new Vector3(39.8f, 0.05f, 29.8f), grassMat, false);

        Cube("Stream_Bed", new Vector3(0, -0.13f, 0),
            new Vector3(40, 0.16f, 3f), darkRockMat);

        Cube("Stream_Water", new Vector3(0, -0.015f, 0),
            new Vector3(40, 0.035f, 2.55f), waterMat, false);

        for (int i = 0; i < 18; i++)
        {
            float x = -18f + i * 2.1f;

            Sphere("Water_Ripple",
                new Vector3(x, 0.012f, Random.Range(-0.7f, 0.7f)),
                new Vector3(0.7f, 0.012f, 0.12f),
                stoneLightMat, false);
        }

        Cube("North_Shore", new Vector3(0, 0.07f, 1.63f),
            new Vector3(40, 0.14f, 0.42f), dirtMat, false);

        Cube("South_Shore", new Vector3(0, 0.07f, -1.63f),
            new Vector3(40, 0.14f, 0.42f), dirtMat, false);

        // Низкий мост.
        Cube("Bridge_Deck", new Vector3(0, 0.045f, 0),
            new Vector3(5f, 0.19f, 4.7f), woodLightMat, true);

        for (int i = 0; i < 9; i++)
        {
            Cube("Bridge_Plank_" + i,
                new Vector3(-2.2f + i * 0.55f, 0.148f, 0),
                new Vector3(0.48f, 0.025f, 4.55f),
                woodMat, false);
        }

        // Плавные подходы к мосту.
        Cube("Bridge_Ramp_North",
            new Vector3(0, 0.075f, 3f),
            new Vector3(4.8f, 0.13f, 1.8f),
            woodLightMat, true, new Vector3(4f, 0, 0));

        Cube("Bridge_Ramp_South",
            new Vector3(0, 0.075f, -3f),
            new Vector3(4.8f, 0.13f, 1.8f),
            woodLightMat, true, new Vector3(-4f, 0, 0));

        for (int side = -1; side <= 1; side += 2)
        {
            Cube("Bridge_Rail_" + side,
                new Vector3(side * 2.25f, 0.58f, 0),
                new Vector3(0.14f, 0.82f, 4.55f), woodMat);

            for (int z = -1; z <= 1; z++)
            {
                Cube("Bridge_RailPost",
                    new Vector3(side * 2.25f, 0.55f, z * 1.85f),
                    new Vector3(0.2f, 0.9f, 0.2f),
                    woodLightMat);
            }
        }

        PathSegment(new Vector3(0, 0.055f, 4.8f),
            new Vector3(3f, 0.10f, 5.4f), pathMat, 0);

        PathSegment(new Vector3(-5.5f, 0.055f, 8f),
            new Vector3(8.5f, 0.10f, 2.1f), pathMat, -12);

        RuinedGate(new Vector3(-5f, 0, 10f), 0.9f);
        RuinedWall(new Vector3(7f, 0, 10f), 5f, 0.7f);
        SmallCamp(new Vector3(3.5f, 0, 11.3f));

        int trees = 0;

        for (int i = 0; i < 180 && trees < 28; i++)
        {
            float x = Random.Range(-18f, 18f);
            float z = Random.Range(-13f, 13f);

            if (Mathf.Abs(z) < 3.8f)
                continue;

            if (Near(x, z, -12, 8, 3f) ||
                Near(x, z, 12, 8, 3f) ||
                Near(x, z, -12, -8, 3f) ||
                Near(x, z, 12, -8, 3f))
                continue;

            if (Near(x, z, 0, 11.5f, 4f))
                continue;

            ForestTree(x, z, Random.Range(0.85f, 1.35f));
            trees++;
        }

        // Корни вокруг первого раскопа.
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;

            Cube("Ancient_Root",
                new Vector3(-12 + Mathf.Cos(a) * 1.05f,
                    0.13f,
                    8 + Mathf.Sin(a) * 1.05f),
                new Vector3(2.2f, 0.18f, 0.22f),
                woodMat, false,
                new Vector3(0, a * Mathf.Rad2Deg, 9));
        }

        AddDigSite("Forest_Buckle_A", new Vector3(-12, 0.08f, 8));
        AddDigSite("Forest_Buckle_B", new Vector3(12, 0.08f, 8));
        AddDigSite("Forest_Buckle_C", new Vector3(-12, 0.08f, -8));
        AddDigSite("Forest_Vessel", new Vector3(12, 0.08f, -8));

        BuildLabTable(new Vector3(0, 0, 11.5f));

        SetPlayerStart(new Vector3(0, 0.12f, 12.8f));

        SetupOutdoorLight(new Color(0.78f, 0.88f, 1f), 1.05f);
    }

    void ForestTree(float x, float z, float s)
    {
        Cylinder("Tree_Trunk",
            new Vector3(x, 1.25f * s, z),
            new Vector3(0.32f * s, 1.25f * s, 0.32f * s),
            woodMat);

        Sphere("Tree_Canopy_Lower",
            new Vector3(x, 2.65f * s, z),
            new Vector3(2.4f * s, 2.1f * s, 2.3f * s),
            leafDarkMat);

        Sphere("Tree_Canopy_Upper",
            new Vector3(x + 0.18f * s, 3.45f * s, z - 0.08f * s),
            new Vector3(1.8f * s, 1.7f * s, 1.8f * s),
            leafMat);

        for (int k = 0; k < 3; k++)
        {
            Cylinder("Tree_Root",
                new Vector3(x + Random.Range(-0.5f, 0.5f),
                    0.12f,
                    z + Random.Range(-0.5f, 0.5f)),
                new Vector3(0.16f, 0.65f, 0.16f),
                woodMat, false,
                new Vector3(0, 0, Random.Range(55f, 85f)));
        }
    }

    void SmallCamp(Vector3 p)
    {
        Cube("Camp_Table", p + new Vector3(0, 0.75f, 0),
            new Vector3(1.8f, 0.13f, 0.9f), woodLightMat);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                Cube("Camp_TableLeg",
                    p + new Vector3(x * 0.72f, 0.36f, z * 0.30f),
                    new Vector3(0.12f, 0.72f, 0.12f), woodMat);
            }
        }

        Cylinder("Camp_FireRing",
            p + new Vector3(2f, 0.12f, 0),
            new Vector3(0.8f, 0.16f, 0.8f), rockMat);

        Sphere("Camp_Ember",
            p + new Vector3(2f, 0.32f, 0),
            new Vector3(0.38f, 0.35f, 0.38f), torchMat);

        PointLight(p + new Vector3(2, 0.7f, 0),
            5f, 0.8f, new Color(1f, 0.45f, 0.16f));
    }

    void RuinedGate(Vector3 p, float s)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Cube("Ruined_Gate_Pillar",
                p + new Vector3(side * 1.2f * s, 1.25f * s, 0),
                new Vector3(0.65f * s, 2.5f * s, 0.7f * s),
                stoneLightMat);
        }

        Cube("Ruined_Gate_Lintel",
            p + new Vector3(0, 2.55f * s, 0),
            new Vector3(3.3f * s, 0.45f * s, 0.85f * s),
            rockMat);
    }

    void RuinedWall(Vector3 p, float length, float height)
    {
        Cube("Ruined_Wall_Base",
            p + new Vector3(0, height * 0.5f, 0),
            new Vector3(length, height, 0.55f), rockMat);

        for (int i = 0; i < 4; i++)
        {
            Cube("Broken_Wall_Top",
                p + new Vector3(-length * 0.42f + i * length * 0.28f,
                    height + Random.Range(-0.12f, 0.18f), 0),
                new Vector3(0.8f, 0.28f, 0.7f),
                stoneLightMat, true,
                new Vector3(0, 0, Random.Range(-8f, 8f)));
        }
    }

    // EXPEDITION 3 — КАМЕНИСТЫЙ СКЛОН

    void BuildSlope()
    {
        Cube("Mountain_Base", new Vector3(0, -0.65f, 0),
            new Vector3(36, 1.3f, 32), darkRockMat);

        Terrace("Lower_Terrace", 0f, -8f, 30f, 12f);
        Terrace("Middle_Terrace", 3.5f, 0f, 27f, 11f);
        Terrace("Upper_Terrace", 7f, 8f, 23f, 10f);

        Ramp("Switchback_Ramp_Lower_Middle",
            new Vector3(-10f, 1.75f, -4f),
            new Vector3(3.8f, 0.45f, 8.8f), -21.8f);

        Ramp("Switchback_Ramp_Middle_Upper",
            new Vector3(10f, 5.25f, 4f),
            new Vector3(3.8f, 0.45f, 8.8f), -21.8f);

        for (int i = 0; i < 12; i++)
        {
            float y = 0.22f + i * 0.29f;
            float z = -7f + i * 0.72f;

            Cube("Carved_Stair",
                new Vector3(13.2f, y, z),
                new Vector3(2.8f, 0.42f, 0.9f),
                stoneLightMat);
        }

        RuinedWall(new Vector3(-13f, 0.05f, -8f), 5.5f, 1.1f);
        RuinedGate(new Vector3(0f, 3.55f, 1f), 0.85f);
        RuinedWall(new Vector3(10f, 7.05f, 9.5f), 5f, 1f);

        for (int i = 0; i < 32; i++)
        {
            float z = Random.Range(-14f, 14f);
            float x = Random.Range(-16f, 16f);
            float y = HeightAt(z);

            if (Near(x, z, -10, -4, 4f) ||
                Near(x, z, 10, 4, 4f))
                continue;

            Sphere("Mountain_Boulder",
                new Vector3(x, y + Random.Range(0.25f, 0.7f), z),
                new Vector3(Random.Range(0.65f, 1.7f),
                    Random.Range(0.45f, 1f),
                    Random.Range(0.7f, 1.6f)),
                rockMat);
        }

        for (int i = 0; i < 10; i++)
        {
            float x = Random.Range(-15f, 15f);
            float z = Random.Range(-13f, 13f);
            float y = HeightAt(z);

            Pine(x, y, z, Random.Range(0.8f, 1.2f));
        }

        AddDigSite("Slope_Panel_01", new Vector3(-9f, 0.08f, -8f));
        AddDigSite("Slope_Panel_02", new Vector3(-3f, 3.58f, -1f));
        AddDigSite("Slope_Panel_03", new Vector3(3f, 3.58f, 1f));
        AddDigSite("Slope_Panel_04", new Vector3(8f, 7.08f, 8f));
        AddDigSite("Slope_Panel_05", new Vector3(13f, 7.08f, 8f));

        BuildLabTable(new Vector3(-12f, 0f, 10f));

        SetPlayerStart(new Vector3(-13f, 0.12f, -10f));

        SetupOutdoorLight(new Color(1f, 0.91f, 0.78f), 1.15f);
    }

    void Terrace(string name, float y, float z, float width, float depth)
    {
        Cube(name + "_RockPlatform",
            new Vector3(0, y - 0.42f, z),
            new Vector3(width, 0.84f, depth), rockMat);

        Cube(name + "_Soil",
            new Vector3(0, y + 0.025f, z),
            new Vector3(width - 0.5f, 0.05f, depth - 0.5f),
            pathMat, false);

        Cube(name + "_RetainingWall",
            new Vector3(0, y - 0.05f, z - depth * 0.5f),
            new Vector3(width, 0.9f, 0.55f), darkRockMat);

        for (int i = 0; i < 5; i++)
        {
            Cube(name + "_EdgeStone",
                new Vector3(-width * 0.42f + i * width * 0.21f,
                    y + 0.18f, z + depth * 0.5f),
                new Vector3(0.75f, 0.35f, 0.55f),
                stoneLightMat);
        }
    }

    void Ramp(string name, Vector3 pos, Vector3 scale, float angle)
    {
        Cube(name, pos, scale, pathMat, true,
            new Vector3(angle, 0, 0));

        for (int side = -1; side <= 1; side += 2)
        {
            Cube(name + "_Side",
                pos + new Vector3(side * (scale.x * 0.5f + 0.12f),
                    0.22f, 0),
                new Vector3(0.22f, 0.42f, scale.z),
                rockMat, true,
                new Vector3(angle, 0, 0));
        }
    }

    void Pine(float x, float y, float z, float s)
    {
        Cylinder("Pine_Trunk",
            new Vector3(x, y + 0.75f * s, z),
            new Vector3(0.25f * s, 0.75f * s, 0.25f * s),
            woodMat);

        for (int i = 0; i < 3; i++)
        {
            Prim(PrimitiveType.Cylinder, "Pine_Crown",
                new Vector3(x, y + (1.35f + i * 0.55f) * s, z),
                new Vector3((1.8f - i * 0.38f) * s,
                    0.65f * s,
                    (1.8f - i * 0.38f) * s),
                leafDarkMat, false);
        }
    }

    float HeightAt(float z)
    {
        if (z < -3f) return 0f;
        if (z < 4f) return 3.5f;
        return 7f;
    }

    // EXPEDITION 4

    void BuildCave()
    {
        Cube("Cave_Floor", new Vector3(0, -0.38f, 0),
            new Vector3(18, 0.76f, 18), darkRockMat);

        Cube("Cave_Floor_Inlay", new Vector3(0, 0.015f, 0),
            new Vector3(16.8f, 0.04f, 16.8f), rockMat, false);

        Cube("Cave_Back_Wall", new Vector3(0, 3f, 8.9f),
            new Vector3(18, 6f, 0.8f), rockMat);

        Cube("Cave_Left_Wall", new Vector3(-8.9f, 3f, 0),
            new Vector3(0.8f, 6f, 18), rockMat);

        Cube("Cave_Right_Wall", new Vector3(8.9f, 3f, 0),
            new Vector3(0.8f, 6f, 18), rockMat);

        Cube("Cave_Front_Left", new Vector3(-6.1f, 3f, -8.9f),
            new Vector3(5.8f, 6f, 0.8f), rockMat);

        Cube("Cave_Front_Right", new Vector3(6.1f, 3f, -8.9f),
            new Vector3(5.8f, 6f, 0.8f), rockMat);

        for (int i = 0; i < 22; i++)
        {
            float a = i / 22f * Mathf.PI * 2f;
            float r = Random.Range(6.7f, 8f);

            Sphere("Cave_Ceiling_Rock",
                new Vector3(Mathf.Cos(a) * r,
                    Random.Range(5.2f, 5.9f),
                    Mathf.Sin(a) * r),
                new Vector3(Random.Range(1.2f, 2.2f),
                    Random.Range(0.55f, 1.1f),
                    Random.Range(1.2f, 2.2f)),
                darkRockMat);
        }

        Cylinder("Ceremonial_Plinth_Base",
            new Vector3(0, 0.18f, 0),
            new Vector3(2.6f, 0.18f, 2.6f),
            stoneLightMat);

        Cylinder("Ceremonial_Plinth_Top",
            new Vector3(0, 0.34f, 0),
            new Vector3(1.8f, 0.16f, 1.8f),
            goldMat, false);

        Niche("Niche_NW", -5.8f, 5.8f);
        Niche("Niche_NE", 5.8f, 5.8f);
        Niche("Niche_SW", -5.8f, -5.8f);
        Niche("Niche_SE", 5.8f, -5.8f);

        AddDigSite("Cave_Disc_01", new Vector3(-5.8f, 0.08f, 5.8f));
        AddDigSite("Cave_Disc_02", new Vector3(5.8f, 0.08f, 5.8f));
        AddDigSite("Cave_Disc_03", new Vector3(-5.8f, 0.08f, -5.8f));
        AddDigSite("Cave_Disc_04", new Vector3(5.8f, 0.08f, -5.8f));

        LayeredFragmentDisplay(new Vector3(-5.8f, 0.12f, 5.8f));

        BuildLabTable(new Vector3(0f, 0f, -7f));

        CrystalCluster(new Vector3(-7.6f, 0f, 1f));
        CrystalCluster(new Vector3(7.5f, 0f, -1f));

        Torch(new Vector3(-3f, 0f, 7.6f));
        Torch(new Vector3(3f, 0f, 7.6f));
        Torch(new Vector3(-7.5f, 0f, -3f));
        Torch(new Vector3(7.5f, 0f, 3f));

        PointLight(new Vector3(0, 4.8f, 0),
            12f, 1.8f, new Color(1f, 0.78f, 0.55f));

        PointLight(new Vector3(-6f, 3.8f, 6f),
            5f, 1f, new Color(0.35f, 0.75f, 1f));

        PointLight(new Vector3(6f, 3.8f, -6f),
            5f, 1f, new Color(0.35f, 0.75f, 1f));

        SetPlayerStart(new Vector3(0f, 0.12f, -7.4f));

        SetupCaveLight();
    }

    void Niche(string name, float x, float z)
    {
        Cube(name + "_Back", new Vector3(x, 1.5f, z),
            new Vector3(3.6f, 3f, 0.5f), darkRockMat);

        Cube(name + "_Left", new Vector3(x - 1.8f, 1.5f, z - 0.8f),
            new Vector3(0.45f, 3f, 2f), rockMat);

        Cube(name + "_Right", new Vector3(x + 1.8f, 1.5f, z - 0.8f),
            new Vector3(0.45f, 3f, 2f), rockMat);

        Cube(name + "_Lintel", new Vector3(x, 3f, z - 0.8f),
            new Vector3(4f, 0.42f, 2f), stoneLightMat);

        Cube(name + "_Shelf", new Vector3(x, 0.32f, z - 0.85f),
            new Vector3(2.8f, 0.28f, 1.5f), rockMat);

        for (int i = -1; i <= 1; i++)
        {
            Cube(name + "_Rune",
                new Vector3(x + i * 0.55f, 1.55f, z - 0.25f),
                new Vector3(0.08f, 0.85f, 0.04f),
                goldMat, false);
        }
    }

    void LayeredFragmentDisplay(Vector3 p)
    {
        Cube("Two_Layer_Dig_Upper_Soil",
            p + new Vector3(0, 0.12f, 0),
            new Vector3(1.1f, 0.10f, 0.85f),
            dirtMat, false);

        Cube("Two_Layer_Dig_Lower_Soil",
            p + new Vector3(0, -0.04f, 0),
            new Vector3(0.85f, 0.08f, 0.65f),
            groundMat, false);

        Sphere("Disc_Fragment_Upper_Display",
            p + new Vector3(-0.2f, 0.24f, 0),
            new Vector3(0.36f, 0.09f, 0.28f),
            goldMat, false);

        Sphere("Disc_Fragment_Lower_Display",
            p + new Vector3(0.22f, 0.08f, 0.12f),
            new Vector3(0.30f, 0.08f, 0.25f),
            stoneLightMat, false);
    }

    void CrystalCluster(Vector3 p)
    {
        for (int i = 0; i < 4; i++)
        {
            Prim(PrimitiveType.Cube, "Glowing_Crystal",
                p + new Vector3((i - 1.5f) * 0.22f,
                    0.45f + i * 0.08f, 0),
                new Vector3(0.18f, 0.9f + i * 0.12f, 0.18f),
                crystalMat, false,
                new Vector3(0, 0, Random.Range(-14f, 14f)));
        }

        PointLight(p + Vector3.up * 1.2f,
            3.5f, 0.75f, new Color(0.25f, 0.75f, 1f));
    }

    void Torch(Vector3 p)
    {
        Cylinder("Torch_Post",
            p + new Vector3(0, 1.1f, 0),
            new Vector3(0.12f, 1.1f, 0.12f), woodMat);

        Sphere("Torch_Flame",
            p + new Vector3(0, 2f, 0),
            new Vector3(0.32f, 0.48f, 0.32f), torchMat, false);

        PointLight(p + new Vector3(0, 2f, 0),
            5f, 1.15f, new Color(1f, 0.48f, 0.18f));
    }

    // ЛАБОРАТОРИЯ

    void BuildLabTable(Vector3 pos)
    {
        Transform table = new GameObject("LabTable").transform;
        table.SetParent(world, false);
        table.localPosition = pos;

        CubeAt(table, "LabTop",
            new Vector3(0, 0.9f, 0),
            new Vector3(2.2f, 0.15f, 1.2f),
            woodLightMat, true);

        foreach (float x in new[] { -0.9f, 0.9f })
        {
            foreach (float z in new[] { -0.42f, 0.42f })
            {
                CubeAt(table, "LabLeg",
                    new Vector3(x, 0.43f, z),
                    new Vector3(0.12f, 0.85f, 0.12f),
                    woodMat, true);
            }
        }

        GameObject station = new GameObject("LabStation");
        station.transform.SetParent(table, false);
        station.transform.localPosition = new Vector3(0, 0, -0.8f);

        station.AddComponent<LabStation>();

        CubeAt(table, "Artifact_Tray",
            new Vector3(-0.48f, 1.02f, 0),
            new Vector3(0.62f, 0.06f, 0.55f),
            darkRockMat, false);

        CubeAt(table, "Brush_Handle",
            new Vector3(0.42f, 1.04f, 0.12f),
            new Vector3(0.62f, 0.06f, 0.08f),
            woodMat, false);
    }

    void CubeAt(
        Transform parent,
        string name,
        Vector3 pos,
        Vector3 scale,
        Material mat,
        bool collider)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);

        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;

        Renderer r = g.GetComponent<Renderer>();

        if (r != null)
            r.sharedMaterial = mat;

        if (!collider)
        {
            Collider c = g.GetComponent<Collider>();

            if (c != null)
            {
                if (Application.isPlaying)
                    Destroy(c);
                else
                    DestroyImmediate(c);
            }
        }
    }

    // ТОЧКИ РАСКОПОК

    void AddDigSite(string siteName, Vector3 position)
    {
        GameObject site;

        if (digSitePrefab != null)
        {
            site = Instantiate(
                digSitePrefab,
                position,
                Quaternion.identity,
                world
            );

            site.name = siteName;
        }
        else
        {
            site = new GameObject(siteName);
            site.transform.SetParent(world, false);
            site.transform.localPosition = position;

            GameObject marker = GameObject.CreatePrimitive(
                PrimitiveType.Cylinder
            );

            marker.name = "DigSite_Marker";
            marker.transform.SetParent(site.transform, false);
            marker.transform.localPosition = new Vector3(0, 0.04f, 0);
            marker.transform.localScale = new Vector3(1.5f, 0.08f, 1.5f);

            Renderer renderer = marker.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = dirtMat;
        }

        // Добавляем компоненты.
        if (site.GetComponent<DigSite>() == null)
            site.AddComponent<DigSite>();

        if (site.GetComponent<ScanPoint>() == null)
            site.AddComponent<ScanPoint>();
    }

    // ОБЩИЕ МЕТОДЫ

    void PathSegment(Vector3 pos, Vector3 scale, Material mat, float yaw)
    {
        Cube("Forest_Path", pos, scale, mat, false,
            new Vector3(0, yaw, 0));
    }

    void PointLight(Vector3 pos, float range, float intensity, Color color)
    {
        GameObject go = new GameObject("Environment_Light");

        go.transform.SetParent(world, false);
        go.transform.localPosition = pos;

        Light l = go.AddComponent<Light>();

        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity;
        l.color = color;
    }

    void SetupOutdoorLight(Color tint, float intensity)
    {
        Light[] lights = FindObjectsByType<Light>(
            FindObjectsSortMode.None
        );

        bool hasDirectional = false;

        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional)
            {
                hasDirectional = true;
                break;
            }
        }

        if (!hasDirectional)
        {
            GameObject sun = new GameObject("Expedition_Sun");

            Light l = sun.AddComponent<Light>();

            l.type = LightType.Directional;
            l.color = tint;
            l.intensity = intensity;

            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }
    }

    void SetupCaveLight()
    {
        RenderSettings.ambientMode =
            UnityEngine.Rendering.AmbientMode.Flat;

        RenderSettings.ambientLight =
            new Color(0.28f, 0.30f, 0.34f);
    }

    void PlacePlayer()
    {
        PlayerController p = FindAnyObjectByType<PlayerController>();

        if (p == null)
            return;

        CharacterController cc =
            p.GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        p.transform.position = spawnPosition;
        p.transform.rotation =
            Quaternion.LookRotation(Vector3.forward);

        if (cc != null)
            cc.enabled = true;

        Physics.SyncTransforms();
    }

    bool Near(
        float x, float z,
        float px, float pz,
        float radius)
    {
        return Vector2.Distance(
            new Vector2(x, z),
            new Vector2(px, pz)
        ) < radius;
    }

    void SetPlayerStart(Vector3 p)
    {
        spawnPosition = p;
    }
}
