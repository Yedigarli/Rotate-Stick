using TMPro;
using UnityEngine;

public class LifeManager : MonoBehaviour
{
    public static LifeManager Instance;
    public int currentLives;
    public TMP_Text lifeText; // UlduzlarÄ±n altÄ±nda gÃ¶rÃ¼nÉ™cÉ™k yazÄ±

    private void Awake()
    {
        Instance = this;
        // YaddaÅŸdan ÅŸans sayÄ±nÄ± Ã§É™kirik
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
            lifeText.SetText("{0}", currentLives);
    }
}

