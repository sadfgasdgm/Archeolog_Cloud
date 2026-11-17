using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DirtSpot : MonoBehaviour
{
    public float amount = 1f;     // 1 = грязь целиком, 0 = чисто
    public float weight = 1f;     // размер пятна
    public Vector3 baseScale;
}

// Очистка находки на столе
public class CleaningStage : MonoBehaviour
{
    public const float Need = 0.95f;   // суммарная очистка не менее 95%

    public float Progress { get; private set; }
    public bool Ready { get; private set; }

    readonly List<DirtSpot> spots = new List<DirtSpot>();
    Camera cam;
    Transform pivot;
    Vector2 lastMouse;
    float timer;

    const float BrushPower = 0.06f;   
    const float BrushInterval = 0.03f;

    public void Init(Camera c, ArtifactKind kind, int spotCount)
    {
        cam = c;
        pivot = new GameObject("Model").transform;
        pivot.SetParent(transform, false);
        pivot.localRotation = Quaternion.Euler(20f, 0f, 0f);

        bool bowl = kind == ArtifactKind.Bowl || kind == ArtifactKind.Vessel;
        var model = GameObject.CreatePrimitive(bowl ? PrimitiveType.Sphere : PrimitiveType.Cube);
        model.name = "Artifact";
        model.transform.SetParent(pivot, false);
        Vector3 size = bowl ? new Vector3(1.6f, 0.9f, 1.6f) : new Vector3(1.8f, 0.24f, 1.3f);
        model.transform.localScale = size;
        model.GetComponent<Renderer>().sharedMaterial = bowl
            ? Mats.Make(new Color(0.72f, 0.40f, 0.25f), 0.3f)
            : Mats.Make(new Color(0.62f, 0.61f, 0.56f), 0.1f);

        var dirtMat = Mats.Make(new Color(0.30f, 0.20f, 0.12f), 0f);
        Vector3 half = size * 0.5f;

        for (int i = 0; i < spotCount; i++)
        {
            // чётные 
            float side = (i % 2 == 0) ? 1f : -1f;
            float w = Random.Range(0.7f, 1.3f);
            Vector3 pos;
            Quaternion rot;

            if (bowl)
            {
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * side;
                if (Mathf.Abs(dir.y) < 0.25f) dir.y = 0.25f * side;
                dir.Normalize();
                pos = Vector3.Scale(dir, half) * 0.99f;
                rot = Quaternion.FromToRotation(Vector3.up, dir);
            }
            else
            {
                pos = new Vector3(Random.Range(-half.x * 0.75f, half.x * 0.75f), side * half.y * 0.95f,
                                  Random.Range(-half.z * 0.6f, half.z * 0.6f));
                rot = Quaternion.identity;
            }

            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = "Dirt";
            g.transform.SetParent(pivot, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = rot;
            var scale = new Vector3(0.42f * w, 0.1f, 0.42f * w);
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = dirtMat;

            var d = g.AddComponent<DirtSpot>();
            d.weight = w;
            d.baseScale = scale;
            spots.Add(d);
        }
        Recalc();
    }

    static bool OverUI()
    {
        if (EventSystem.current == null) return false;
        return Input.touchCount > 0
            ? EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
            : EventSystem.current.IsPointerOverGameObject();
    }

    void Update()
    {
        if (Time.timeScale == 0f || cam == null) return;

        Vector2 mp = Input.mousePosition;
        if (Input.GetMouseButtonDown(0)) lastMouse = mp;
        if (!Input.GetMouseButton(0)) return;
        if (OverUI()) { lastMouse = mp; return; }

        timer -= Time.deltaTime;

        DirtSpot hit = null;
        float best = float.MaxValue;
        foreach (var h in Physics.RaycastAll(cam.ScreenPointToRay(mp), 30f))
        {
            var d = h.collider.GetComponent<DirtSpot>();
            if (d != null && d.amount > 0f && h.distance < best) { hit = d; best = h.distance; }
        }

        if (hit != null)
        {
            if (timer <= 0f)
            {
                hit.amount = Mathf.Max(0f, hit.amount - BrushPower);
                float k = Mathf.Lerp(0.2f, 1f, hit.amount);
                hit.transform.localScale = new Vector3(hit.baseScale.x * k, hit.baseScale.y, hit.baseScale.z * k);
                if (hit.amount <= 0.01f) { hit.amount = 0f; hit.gameObject.SetActive(false); }
                timer = BrushInterval;
                Recalc();
            }
        }
        else
        {
            // пустая область 
            Vector2 d = mp - lastMouse;
            pivot.Rotate(Vector3.up, -d.x * 0.4f, Space.World);
            pivot.Rotate(Vector3.right, d.y * 0.4f, Space.World);
        }
        lastMouse = mp;
    }

    void Recalc()
    {
        float total = 0f, left = 0f;
        bool bigLeft = false;
        foreach (var s in spots)
        {
            total += s.weight;
            left += s.weight * s.amount;
            if (s.amount > 0.3f) bigLeft = true;   // крупное пятно
        }
        Progress = total <= 0f ? 1f : 1f - left / total;
        Ready = Progress >= Need && !bigLeft;
    }
}
