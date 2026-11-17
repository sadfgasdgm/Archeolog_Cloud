using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Interactor : MonoBehaviour
{
    [SerializeField] float radius = 1.1f;   // радиус взаимодействия 1,1 м
    [SerializeField] Button button;
    [SerializeField] TMP_Text label;

    Interactable current;
    readonly List<Collider> cols = new List<Collider>();

    void Start()
    {
        button.onClick.AddListener(OnPressed);
        button.gameObject.SetActive(false);
    }

    // Расстояние по горизонтали до ближайшей точки объекта
    float DistanceTo(Interactable i)
    {
        Vector3 p = transform.position;
        i.GetComponentsInChildren(false, cols);

        bool has = false;
        Bounds b = new Bounds();
        foreach (var c in cols)
        {
            if (c == null || !c.enabled || c.isTrigger) continue;
            if (!has) { b = c.bounds; has = true; }
            else b.Encapsulate(c.bounds);
        }

        if (!has)
        {
            Vector3 d0 = i.transform.position - p;
            d0.y = 0f;
            return d0.magnitude - i.ExtraReach;   // нет коллайдеров считаем от центра с запасом объекта
        }

        p.y = b.center.y;                  // высоту не учитываем
        Vector3 d = b.ClosestPoint(p) - p;
        d.y = 0f;
        return d.magnitude;
    }

    void Update()
    {
        Interactable best = null;
        float bestDist = radius;
        foreach (var i in Interactable.Registry)
        {
            if (!i.Visible) continue;
            float d = DistanceTo(i);
            if (d <= bestDist) { bestDist = d; best = i; }
        }

        if (best != current)
        {
            if (current != null) current.Highlight(false);
            current = best;
            if (current != null) current.Highlight(true);
            button.gameObject.SetActive(current != null);
        }

        if (current != null)
        {
            label.text = current.Label;
            button.interactable = current.Enabled;
        }
    }

    void OnPressed()
    {
        if (current != null && current.Enabled) current.Interact();
    }
}
