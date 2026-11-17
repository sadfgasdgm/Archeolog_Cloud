
using System.Collections.Generic;
using UnityEngine;

public class ScanPoint : Interactable
{
    public static readonly List<ScanPoint> All = new List<ScanPoint>();

    public bool Found { get; private set; }

    public bool IsFalse => site != null && site.kind == ArtifactKind.Junk;

    DigSite site;

    void Awake()
    {
        site = GetComponent<DigSite>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        All.Add(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        All.Remove(this);
    }

    public override string Label =>
        site != null && site.Active ? "Извлечь" : "Начать раскопку";

    public override bool Visible =>
        Found && site != null && !site.Extracted;

    public override bool Enabled =>
        site != null && (site.Active ? site.CanExtract : true);

    public override void Interact()
    {
        if (site == null) return;

        if (site.Active)
        {
            site.Extract();
            if (site.Extracted)
            {
                var m = transform.Find("Marker");
                if (m != null) Destroy(m.gameObject);
            }
        }
        else
            site.Begin();
    }

    public void Mark()
    {
        if (Found) return;

        Found = true;

        PlayerPrefs.SetInt("Found_" + gameObject.name, 1);
        PlayerPrefs.Save();

        var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "Marker";
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = new Vector3(0, 0.05f, 0);
        marker.transform.localScale = new Vector3(0.8f, 0.05f, 0.8f);
        marker.GetComponent<Renderer>().sharedMaterial = Mats.Glow(Color.yellow);

        Destroy(marker.GetComponent<Collider>());

        if (site != null)
            site.ShowOutline();

        Debug.Log("Точка отмечена: " + name);
    }

    public void Restore()
    {
        if (PlayerPrefs.GetInt("Found_" + gameObject.name, 0) == 1)
        {
            Mark();
        }
    }
}
