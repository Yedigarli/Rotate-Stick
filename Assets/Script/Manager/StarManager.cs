using UnityEngine;

public class StarManager : MonoBehaviour
{
    public static StarManager Instance;

    [Header("Data")]
    public int stars;

    private static readonly string StarsKey = "Stars";
    private StarUI cachedStarUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        stars = PlayerPrefs.GetInt(StarsKey, 0);
        cachedStarUI = StarUI.Instance;
    }

    public void AddStar(int amount)
    {
        stars += amount;
        PlayerPrefs.SetInt(StarsKey, stars);
        UpdateStarUI();
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
        PlayerPrefs.Save();

        UpdateStarUI();
        return true;
    }

    private void UpdateStarUI()
    {
        if (cachedStarUI == null)
            cachedStarUI = StarUI.Instance != null ? StarUI.Instance : FindFirstObjectByType<StarUI>();

        cachedStarUI?.UpdateUI();
    }

    public int GetStars()
    {
        return stars;
    }
}
