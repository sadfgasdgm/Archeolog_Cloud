using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScannerUI : MonoBehaviour
{
    [SerializeField] Scanner scanner;
    [SerializeField] Slider chargeSlider;
    [SerializeField] TMP_Text signalText;
    [SerializeField] Button markButton;

    static readonly string[] Names =
        { "нет сигнала", "слабый", "средний", "сильный", "точная зона" };

    void Update()
    {
        if (scanner == null) return;

        float v = scanner.Charge / scanner.MaxCharge;
        if (chargeSlider != null)
        {
            chargeSlider.value = v;

            // цвет заряда: обычный, предупреждение <=20% 0  недоступен 
            var fill = chargeSlider.fillRect != null ? chargeSlider.fillRect.GetComponent<Image>() : null;
            if (fill != null)
            {
                if (v <= 0.01f) fill.color = new Color(0.5f, 0.45f, 0.4f);
                else if (v <= 0.2f) fill.color = new Color32(0xD9, 0x4B, 0x3A, 255);   // красный
                else fill.color = new Color32(0x6B, 0xC2, 0x5A, 255);                  // зелёный
            }
        }

        // шкала сигнала скрыта, пока сканер выключен 
        if (signalText != null)
        {
            signalText.gameObject.SetActive(scanner.IsOn);
            signalText.text = "Сигнал: " + Names[(int)scanner.Signal];
        }

        if (markButton != null)
            markButton.interactable = scanner.Signal == SignalLevel.Exact;
    }
}
