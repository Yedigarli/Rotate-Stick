using TMPro;
using UnityEngine;

public class StarUI : MonoBehaviour
{
    public static StarUI Instance;
    public TMP_Text starText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Oyun açılan kimi rəqəmi yenilə
        UpdateUI();
    }

    public void UpdateUI()
    {
        // Əgər ulduz artdısa və ya menyu açıldısa, yaddaşdan ən son halı çək
        int stars = PlayerPrefs.GetInt("Stars", 0);
        if (starText != null)
        {
            starText.text = stars.ToString();
        }
    }
}
