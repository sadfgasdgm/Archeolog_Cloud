using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public static readonly List<Interactable> Registry = new List<Interactable>();

    public abstract string Label { get; }        // надпись на кнопке
    public virtual bool Visible => true;         // доступен ли объект вообще
    public virtual bool Enabled => true;         // нажимается ли кнопка
    // запас радиуса для объектов без коллайдеров
    public virtual float ExtraReach => 0f;
    public abstract void Interact();

    protected virtual void OnEnable() => Registry.Add(this);
    protected virtual void OnDisable() => Registry.Remove(this);

    // подсветка объекта свечением
    public void Highlight(bool on)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            var m = r.material;
            if (on)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(0.9f, 0.75f, 0.2f));
            }
            else m.SetColor("_EmissionColor", Color.black);
        }
    }
}