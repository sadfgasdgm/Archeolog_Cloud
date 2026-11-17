using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class DigHUD : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Slider integritySlider;
    [SerializeField] TMP_Text infoText;
    [SerializeField] Image[] toolImages;   // 0 лопата, 1 скребок, 2 кисть
    [SerializeField] Image cancelImage;    

    static readonly Color Cream = new Color32(0xF7, 0xEB, 0xD0, 255);
    static readonly Color Sand = new Color32(0xE3, 0xC1, 0x8A, 255);
    static readonly Color Selected = new Color32(0x4C, 0xD9, 0x64, 255);
    static readonly Color Good = new Color32(0x6B, 0xC2, 0x5A, 255);
    static readonly Color Bad = new Color32(0xD9, 0x4B, 0x3A, 255);

    float cancelFlash;
    float lastIntegrity = -1f;
    float accent;   // краткий акцент при каждом повреждении


    void Start()
    {
        if (cancelImage != null)
            cancelImage.color = Sand;
    }

    void Update()
    {
        if (panel == null) return;

        var s = DigSite.Current;
        bool show = s != null && s.Active;
        if (panel.activeSelf != show) panel.SetActive(show);
        if (!show) { lastIntegrity = -1f; return; }

        if (lastIntegrity >= 0f && s.Integrity < lastIntegrity - 0.01f) accent = 0.35f;
        lastIntegrity = s.Integrity;
        accent = Mathf.Max(0f, accent - Time.unscaledDeltaTime);

        float v = s.Integrity / 100f;
        if (integritySlider != null)
        {
            integritySlider.value = v;
            if (integritySlider.fillRect != null)
            {
                var im = integritySlider.fillRect.GetComponent<Image>();
                if (im != null)
                {
                    if (v <= 0.25f)
                    {
                        // Предупреждение
                        float pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
                        im.color = new Color(Bad.r, Bad.g, Bad.b, pulse);
                    }
                    else
                    {
                        im.color = accent > 0f ? Color.white : Color.Lerp(Bad, Good, v);
                    }
                }
            }
        }

        if (infoText != null) infoText.color = accent > 0f ? Bad : Cream;

        if (infoText != null)
            infoText.text = s.Failed
                ? "Находка повреждена, раскоп перезапускается"
                : $"Освобождено: {Mathf.RoundToInt(s.FreedPercent * 100)}%    Сохранность: {Mathf.RoundToInt(s.Integrity)}%"
                  + (s.kind == ArtifactKind.Vessel ? $"   (нужно не менее {Mathf.RoundToInt(DigSite.VesselMin)}%)" : "");

        // Выбранный инструмент подсвечивается зелёным.
        if (toolImages != null)
        {
            for (int i = 0; i < toolImages.Length; i++)
            {
                if (toolImages[i] != null)
                    toolImages[i].color = (int)s.Tool == i ? Selected : Sand;
            }
        }

        // Подсветка нижней кнопки Отмена
        if (cancelImage != null)
        {
            cancelFlash = Mathf.Max(0f, cancelFlash - Time.unscaledDeltaTime);
            cancelImage.color = cancelFlash > 0f ? Selected : Sand;
        }
    }

    public void SetTool(int index)
    {
        if (DigSite.Current != null)
            DigSite.Current.SetTool(index);
    }

    public void Cancel()
    {
        cancelFlash = 0.25f;
        if (DigSite.Current != null)
            DigSite.Current.Cancel();
    }
}
