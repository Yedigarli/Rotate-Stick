using UnityEngine;

public class StarManager : MonoBehaviour
{
    public static StarManager Instance;
    public int stars;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        stars = PlayerPrefs.GetInt("Stars", 0);
    }

    public void AddStar(int amount)
    {
        stars += amount;
        PlayerPrefs.SetInt("Stars", stars);

        if (StarUI.Instance != null)
            StarUI.Instance.UpdateUI();
    }

    public bool SpendStars(int amount)
    {
        if (stars < amount)
            return false;

        stars -= amount;
        PlayerPrefs.SetInt("Stars", stars);

        if (StarUI.Instance != null)
            StarUI.Instance.UpdateUI();

        return true;
    }
}
