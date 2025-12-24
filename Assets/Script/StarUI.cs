using TMPro;
using UnityEngine;

public class StarUI : MonoBehaviour
{
    public static StarUI Instance;
    public TMP_Text starText;

    private void Awake()
    {
        Instance = this;
        UpdateUI();
    }

    public void UpdateUI()
    {
        int stars = PlayerPrefs.GetInt("Stars", 0);
        starText.text = stars.ToString();
    }
}
