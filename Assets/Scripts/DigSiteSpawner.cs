using UnityEngine;

public class DigSiteSpawner : MonoBehaviour
{
    public GameObject digSitePrefab;          // префаб Точка раскопки
    public Vector3[] positions;               // координаты

    void Start()
    {
        for (int i = 0; i < positions.Length; i++)
        {
            var go = Instantiate(digSitePrefab, positions[i], Quaternion.identity);
            go.name = "Точка раскопки " + (i + 1);   // уникальное имя нужно для сохранения
            var sp = go.GetComponent<ScanPoint>();
            if (sp != null && PlayerPrefs.GetInt("Found_" + go.name, 0) == 1)
                sp.Restore();                         // восстановить отметку после перезапуска
        }
    }
}