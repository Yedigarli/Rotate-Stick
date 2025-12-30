using TMPro;
using UnityEngine;

public class LifeManager : MonoBehaviour
{
    public static LifeManager Instance;
    public int currentLives;
    public TMP_Text lifeText; // Ulduzların altında görünəcək yazı

    private void Awake()
    {
        Instance = this;
        // Yaddaşdan şans sayını çəkirik
        currentLives = PlayerPrefs.GetInt("PlayerLives", 0);
        UpdateUI();
    }

    public void AddLives(int amount)
    {
        currentLives += amount;
        PlayerPrefs.SetInt("PlayerLives", currentLives);
        PlayerPrefs.Save();
        UpdateUI();
    }

    public bool SpendLife()
    {
        if (currentLives > 0)
        {
            currentLives--;
            PlayerPrefs.SetInt("PlayerLives", currentLives);
            PlayerPrefs.Save();
            UpdateUI();
            return true;
        }
        return false;
    }

    public void UpdateUI()
    {
        if (lifeText != null)
            lifeText.text = currentLives.ToString();
    }
}
