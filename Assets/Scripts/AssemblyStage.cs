using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class AssemblyStage : MonoBehaviour
{
    public bool Done { get; private set; }

    const float DistTol = 0.15f;   // допуск по расстоянию
    const float AngTol = 15f;      // допуск по углу

    float W = 1.0f;                // ширина фрагмента = расстояние между центрами в собранном виде

    Camera cam;
    Plane plane;
    Vector3 grabOffset;
    bool dragging;
    int selected;

    readonly List<Transform> pieces = new List<Transform>();
    readonly List<Renderer> bodies = new List<Renderer>();
    readonly List<Renderer> leftEdge = new List<Renderer>();   
    readonly List<Renderer> rightEdge = new List<Renderer>(); 
    bool[] joined;

    static readonly Color Stone = new Color(0.62f, 0.61f, 0.56f);
    static readonly Color StoneSel = new Color(0.80f, 0.78f, 0.66f);
    static readonly Color EdgeIdle = new Color(0.25f, 0.22f, 0.18f);
    static readonly Color EdgeGlow = new Color(1f, 0.7f, 0.1f);

    static readonly float[] Yaws = { 25f, -55f, 70f, -30f, 100f, -80f };

    public void Init(Camera c) { Init(c, 2); }

    public void Init(Camera c, int count)
    {
        cam = c;
        plane = new Plane(Vector3.up, transform.position);
        count = Mathf.Max(2, count);
        W = count <= 3 ? 1.0f : 4.8f / count;
        joined = new bool[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 pos;
            if (count == 2) pos = i == 0 ? new Vector3(-1.5f, 0f, -0.3f) : new Vector3(1.6f, 0f, 0.5f);
            else pos = new Vector3(Mathf.Lerp(-2.6f, 2.6f, (i % 3) / 2f), 0f, i < 3 ? 1.3f : -1.3f);
            float yaw = count == 2 ? (i == 0 ? 25f : -55f) : Yaws[i % Yaws.Length];
            MakePiece(i, count, pos, yaw);
        }
        selected = count - 1;
        Paint();
    }

    void MakePiece(int i, int count, Vector3 localPos, float yaw)
    {
        var root = new GameObject("Fragment" + (i + 1)).transform;
        root.SetParent(transform, false);
        root.localPosition = localPos;
        root.localRotation = Quaternion.Euler(0f, yaw, 0f);

        var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
        m.name = "Body";
        m.transform.SetParent(root, false);
        m.transform.localScale = new Vector3(W, 0.18f, 1.3f);
        var body = m.GetComponent<Renderer>();
        body.material = Mats.Make(Stone, 0.1f);

        leftEdge.Add(i > 0 ? MakeEdge(root, -W * 0.5f) : null);
        rightEdge.Add(i < count - 1 ? MakeEdge(root, W * 0.5f) : null);

        pieces.Add(root);
        bodies.Add(body);
    }

    Renderer MakeEdge(Transform root, float x)
    {
        var e = GameObject.CreatePrimitive(PrimitiveType.Cube);
        e.name = "Edge";
        e.transform.SetParent(root, false);
        e.transform.localScale = new Vector3(0.07f, 0.2f, 1.3f);
        e.transform.localPosition = new Vector3(x, 0f, 0f);
        Destroy(e.GetComponent<Collider>());
        var r = e.GetComponent<Renderer>();
        r.material = Mats.Make(EdgeIdle, 0.1f);
        return r;
    }

    public void Rotate(float deg)
    {
        if (Done || selected < 0 || selected >= pieces.Count) return;
        pieces[selected].Rotate(0f, deg, 0f, Space.World);
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
        if (Time.timeScale == 0f || cam == null || Done) return;

        if (Input.GetMouseButtonDown(0) && !OverUI())
        {
            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 30f))
            {
                int idx = pieces.IndexOf(hit.collider.transform.parent);
                if (idx >= 0)
                {
                    selected = idx;
                    dragging = true;
                    if (plane.Raycast(cam.ScreenPointToRay(Input.mousePosition), out float d0))
                        grabOffset = pieces[selected].position - cam.ScreenPointToRay(Input.mousePosition).GetPoint(d0);
                    Paint();
                }
            }
        }

        if (dragging && Input.GetMouseButton(0))
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float d))
            {
                Vector3 p = ray.GetPoint(d) + grabOffset;
                var o = transform.position;
                p.x = Mathf.Clamp(p.x, o.x - 3.2f, o.x + 3.2f);
                p.z = Mathf.Clamp(p.z, o.z - 2.1f, o.z + 2.1f);
                p.y = o.y;
                pieces[selected].position = p;
            }
        }
        if (Input.GetMouseButtonUp(0)) dragging = false;

        Check();
    }

    void Check()
    {
        // все стыки в покое
        for (int k = 0; k < pieces.Count; k++)
        {
            if (leftEdge[k] != null && !joined[k]) SetColor(leftEdge[k].material, EdgeIdle);
            if (rightEdge[k] != null && (k + 1 >= pieces.Count || !joined[k + 1])) SetColor(rightEdge[k].material, EdgeIdle);
        }

        for (int k = 1; k < pieces.Count; k++)
        {
            if (joined[k]) continue;
            var prev = pieces[k - 1];
            var cur = pieces[k];

            // положение 
            Vector3 rel = prev.InverseTransformPoint(cur.position);
            float dist = Vector2.Distance(new Vector2(rel.x, rel.z), new Vector2(W, 0f));
            float ang = Mathf.Abs(Mathf.DeltaAngle(prev.eulerAngles.y, cur.eulerAngles.y));

            bool near = dist < 0.7f && ang < 45f;
            if (near)
            {
                SetColor(rightEdge[k - 1].material, EdgeGlow);
                SetColor(leftEdge[k].material, EdgeGlow);
            }

            if (dist <= DistTol && ang <= AngTol)
            {
                cur.position = prev.TransformPoint(new Vector3(W, 0f, 0f));
                cur.rotation = prev.rotation;
                cur.SetParent(prev, true);
                joined[k] = true;
                rightEdge[k - 1].gameObject.SetActive(false);
                leftEdge[k].gameObject.SetActive(false);
                selected = k - 1;
                Paint();
            }
        }

        bool all = true;
        for (int k = 1; k < joined.Length; k++) if (!joined[k]) { all = false; break; }
        if (all) Done = true;
    }

    void Paint()
    {
        for (int i = 0; i < bodies.Count; i++)
            SetColor(bodies[i].material, i == selected ? StoneSel : Stone);
    }

    static void SetColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }
}
