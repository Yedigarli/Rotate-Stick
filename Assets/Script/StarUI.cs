using DG.Tweening;
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

    // StarUI.cs (Əgər belə bir skriptin varsa, içinə bunu əlavə et)
    public void UpdateUI()
    {
        if (starText != null)
        {
            starText.text = PlayerPrefs.GetInt("Stars", 0).ToString();

            // Rəqəm hər dəfə artanda "titrəsin" (Juicy Effect)
            starText.transform.DOKill();
            starText.transform.localScale = Vector3.one;
            starText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f).SetUpdate(true);
        }
    }
}
