using UnityEngine;

public class StarManager : MonoBehaviour
{
    public static StarManager Instance;

    [Header("Data")]
    public int stars;

    // String Caching (Performance)
    private static readonly string StarsKey = "Stars";

    private void Awake()
    {
        // Singleton Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // DontDestroyOnLoad(gameObject); // Əgər ulduzların səhnələr arası keçməsini istəyirsənsə bunu aç

        // Yaddaşdan yüklə
        stars = PlayerPrefs.GetInt(StarsKey, 0);
    }

    // Əgər GameManager-də event yoxdursa, Start-dakı hissəni silirik
    // Çünki sən GameManager-də ulduzları birbaşa AddStar ilə çağırırsan.

    public void AddStar(int amount)
    {
        stars += amount;

        // Hər saniyə diski yormamaq üçün PlayerPrefs-i yazırıq amma Save() etmirik
        PlayerPrefs.SetInt(StarsKey, stars);

        // UI-nı yenilə
        if (StarUI.Instance != null)
        {
            StarUI.Instance.UpdateUI();
        }
        else
        {
            // Əgər StarUI tapılmasa, alternativ olaraq tapmağa çalışaq (Fail-safe)
            FindFirstObjectByType<StarUI>()?.UpdateUI();
        }
    }

    public bool SpendStars(int amount)
    {
        if (stars < amount)
        {
            Debug.Log("Yetərli ulduz yoxdur!");
            return false;
        }

        stars -= amount;
        PlayerPrefs.SetInt(StarsKey, stars);
        PlayerPrefs.Save(); // Xərcləmə vacib olduğu üçün dərhal yadda saxlayırıq

        if (StarUI.Instance != null)
            StarUI.Instance.UpdateUI();

        return true;
    }

    // GameManager-dən birbaşa çağırıla bilən köməkçi funksiya
    public int GetStars()
    {
        return stars;
    }
}
