using DG.Tweening;
using TMPro;
using UnityEngine;

public class StarUI : MonoBehaviour
{
    public static StarUI Instance;
    public TMP_Text starText;

    private void Awake()
    {
        // Singleton nizamlanması
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (starText != null)
        {
            // 1. PERFORMANS: PlayerPrefs oxumaq əvəzinə Manager-dəki hazır rəqəmi götür
            int starCount = 0;
            if (StarManager.Instance != null)
            {
                starCount = StarManager.Instance.stars;
            }
            else
            {
                starCount = PlayerPrefs.GetInt("Stars", 0);
            }

            // 2. PERFORMANS: ToString() əvəzinə SetText("{0}") istifadə et (No Garbage)
            starText.SetText("{0}", starCount);

            // 3. JUICY EFFECT: Hamar animasiya
            starText.transform.DOKill();
            starText.transform.localScale = Vector3.one;
            starText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 1, 0.1f).SetUpdate(true);
        }
    }
}
