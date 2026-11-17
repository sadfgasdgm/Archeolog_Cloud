using UnityEngine;

public enum SignalLevel { None, Weak, Medium, Strong, Exact }

public class Scanner : MonoBehaviour
{
    [SerializeField] float radius = 8f;          // радиус 
    [SerializeField] float exactDistance = 1.5f; // точная отметка до
    [SerializeField] float maxCharge = 20f;      // заряд
    [SerializeField] float rechargeTime = 10f;   // восстановление до полного

    public bool IsOn { get; private set; }
    public float Charge { get; private set; }
    public float MaxCharge => maxCharge;
    public SignalLevel Signal { get; private set; }
    public ScanPoint NearestPoint { get; private set; }

    void Awake() => Charge = maxCharge;

    public void Toggle()
    {
        if (!IsOn && Charge <= 0.5f) return;
        IsOn = !IsOn;
    }

    void Update()
    {
        if (IsOn)
        {
            Charge -= Time.deltaTime;
            if (Charge <= 0f) { Charge = 0f; IsOn = false; }
            UpdateSignal();
        }
        else
        {
            Charge = Mathf.Min(maxCharge, Charge + maxCharge / rechargeTime * Time.deltaTime);
            Signal = SignalLevel.None;
        }
    }

    void UpdateSignal()
    {
        float best = float.MaxValue;
        NearestPoint = null;
        foreach (ScanPoint p in ScanPoint.All)
        {
            if (p.Found) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < best) { best = d; NearestPoint = p; }
        }

        if (NearestPoint == null || best > radius) Signal = SignalLevel.None;
        else if (best <= exactDistance) Signal = SignalLevel.Exact;
        else if (best <= radius * 0.4f) Signal = SignalLevel.Strong;
        else if (best <= radius * 0.7f) Signal = SignalLevel.Medium;
        else Signal = SignalLevel.Weak;
    }

    public void MarkPoint()
    {
        if (Signal == SignalLevel.Exact && NearestPoint != null)
            NearestPoint.Mark();
    }
}