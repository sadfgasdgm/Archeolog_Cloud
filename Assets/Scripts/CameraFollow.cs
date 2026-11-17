using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;

    [Header("Исследование (ТЗ: 3,5 м)")]
    [SerializeField] float exploreDistance = 3.5f;
    [SerializeField] float exploreMinPitch = 20f;
    [SerializeField] float exploreMaxPitch = 70f;

    [Header("Раскопка (ТЗ: 45-70°, масштаб 1,0-1,8)")]
    [SerializeField] float digDistance = 5f;
    [SerializeField] float digMinPitch = 45f;
    [SerializeField] float digMaxPitch = 70f;
    [SerializeField] float zoomMin = 1f;
    [SerializeField] float zoomMax = 1.8f;

    [Header("Общее")]
    [SerializeField] float smooth = 6f;
    [SerializeField] float touchSensitivity = 0.15f;
    [SerializeField] float mouseSensitivity = 3f;

    Transform digFocus;
    float yaw;
    float explorePitch = 45f;
    float digPitch = 60f;
    float zoom = 1f;

    Vector3 focusPos;
    float dist, pitch;
    bool init;

    // пальцы
    readonly HashSet<int> rotFingers = new HashSet<int>();

    // Позволяет другим системам отличать рабочую камеру игрока от пустой/резервной камеры.
    public bool HasTarget => target != null;
    public Camera ControlledCamera
    {
        get
        {
            var c = GetComponent<Camera>();
            if (c == null) c = GetComponentInChildren<Camera>();
            return c;
        }
    }

    void Start()
    {
        touchSensitivity = PlayerPrefs.GetFloat("Sens", 0.275f);   
    }

    // вызывается из DigSite
    public void EnterDig(Transform focus) { digFocus = focus; zoom = 1f; rotFingers.Clear(); }
    public void ExitDig() { digFocus = null; rotFingers.Clear(); }

    void LateUpdate()
    {
        if (target == null) return;

        ReadInput();

        bool dig = digFocus != null;
        Vector3 wantFocus = dig ? digFocus.position + Vector3.up * 0.2f
                                : target.position + Vector3.up * 1.2f;
        float wantDist = dig ? digDistance / zoom : exploreDistance;
        float wantPitch = dig ? digPitch : explorePitch;

        if (!init) { focusPos = wantFocus; dist = wantDist; pitch = wantPitch; init = true; }

        // плавный переход к центру рабочей области
        float k = 1f - Mathf.Exp(-(dig ? smooth : smooth * 2f) * Time.deltaTime);
        focusPos = Vector3.Lerp(focusPos, wantFocus, k);
        dist = Mathf.Lerp(dist, wantDist, k);
        pitch = Mathf.Lerp(pitch, wantPitch, k);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 pos = focusPos - rot * Vector3.forward * dist;
        if (pos.y < 0.3f) pos.y = 0.3f;  

        transform.position = pos;
        transform.LookAt(focusPos);
    }

    static bool OverUI(int fingerId)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    void ReadInput()
    {
        if (Time.timeScale == 0f) { rotFingers.Clear(); return; }   // на паузе камера не реагирует

        bool dig = digFocus != null;

        if (Input.touchCount > 0)
        {
            if (!dig)
            {
                // исследование
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);

                    if (t.phase == TouchPhase.Began)
                    {
                        if (t.position.x > Screen.width * 0.5f && !OverUI(t.fingerId))
                            rotFingers.Add(t.fingerId);
                    }
                    else if (t.phase == TouchPhase.Moved)
                    {
                        if (rotFingers.Contains(t.fingerId))
                            Rotate(t.deltaPosition * touchSensitivity);
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        rotFingers.Remove(t.fingerId);
                    }
                }
            }
            else if (Input.touchCount == 2)
            {
                // раскоп
                Touch a = Input.GetTouch(0), b = Input.GetTouch(1);
                float prev = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
                float cur = (a.position - b.position).magnitude;
                float pinch = cur - prev;

                AddZoom(pinch * 0.005f);
                if (Mathf.Abs(pinch) < 4f)
                    Rotate((a.deltaPosition + b.deltaPosition) * 0.5f * touchSensitivity);
            }
            return;
        }

        rotFingers.Clear();

        // мышь для проверки в редакторе
        if (Input.GetMouseButton(1))
            Rotate(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseSensitivity);
        if (dig)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f) AddZoom(scroll * 2f);
        }
    }

    void Rotate(Vector2 delta)
    {
        yaw += delta.x;
        if (digFocus != null) digPitch = Mathf.Clamp(digPitch - delta.y, digMinPitch, digMaxPitch);
        else explorePitch = Mathf.Clamp(explorePitch - delta.y, exploreMinPitch, exploreMaxPitch);
    }

    void AddZoom(float d) => zoom = Mathf.Clamp(zoom + d, zoomMin, zoomMax);

    public void SetSensitivity(float v) => touchSensitivity = v;
}
