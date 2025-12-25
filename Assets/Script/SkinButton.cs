using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinButton : MonoBehaviour
{
    public SkinData skin;
    public Image icon;
    public TMP_Text priceText;
    public GameObject lockIcon;

    private void Start()
    {
        // Manager-ə "mən buradayam" deyir
        if (SkinsManager.Instance != null)
        {
            SkinsManager.Instance.RegisterButton(this);
        }

        if (skin != null)
        {
            icon.sprite = skin.sprite;
            UpdateUI();
        }

        // Düyməyə klik funksiyasını avtomatik bağla
        GetComponent<Button>().onClick.AddListener(Click);
    }

    public void UpdateUI()
    {
        if (skin == null)
            return;

        bool unlocked =
            PlayerPrefs.GetInt("Skin_" + skin.skinID, skin.unlockedByDefault ? 1 : 0) == 1;
        string selectedID = PlayerPrefs.GetString("SelectedSkin", "");

        lockIcon.SetActive(!unlocked);

        if (unlocked)
        {
            // Əgər yaddaşdakı ID bu düymənin ID-si ilə eynidirsə
            if (selectedID == skin.skinID)
            {
                priceText.text = "SELECTED";
            }
            else
            {
                priceText.text = "USE";
            }
        }
        else
        {
            priceText.text = skin.price.ToString();
        }
    }

    public void Click()
    {
        if (SkinsManager.Instance != null)
        {
            SkinsManager.Instance.SelectSkin(skin);
        }
    }
}
