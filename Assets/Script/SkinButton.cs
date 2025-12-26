using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinButton : MonoBehaviour
{
    public SkinData skin;
    public Image icon;
    public TMP_Text priceText;
    public GameObject lockIcon;

    [Header("Visual Settings")]
    public Image buttonBg;         // Düymənin fonu
    public GameObject priceContainer; // Qiymət yazısının və onun arxa fonunun olduğu ana obyekt
    public Color selectedColor = Color.yellow;
    public Color unlockedColor = new Color(1, 1, 1, 0); // Tam şəffaf (arxa fon yoxdur)

    private Coroutine scaleCoroutine;

    private void Start()
    {
        if (SkinsManager.Instance != null)
            SkinsManager.Instance.RegisterButton(this);

        if (skin != null)
        {
            icon.sprite = skin.sprite;
            UpdateUI();
        }

        GetComponent<Button>().onClick.AddListener(Click);
    }

    public void UpdateUI()
    {
        if (skin == null) return;

        bool unlocked = PlayerPrefs.GetInt("Skin_" + skin.skinID, skin.unlockedByDefault ? 1 : 0) == 1;
        string selectedID = PlayerPrefs.GetString("SelectedSkin", "");

        if (!unlocked)
        {
            // ALINMAYIB: Qiyməti göstər, kilid ikonunu göstər
            priceContainer.SetActive(true);
            lockIcon.SetActive(true);
            priceText.text = skin.price.ToString();
            buttonBg.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Kilidli vaxtı bir az boz fon
            StopScaleAnim();
        }
        else
        {
            // ALINIB: Qiymət konteynerini və kilidi tam sil (deaktiv et)
            priceContainer.SetActive(false);
            lockIcon.SetActive(false);

            if (selectedID == skin.skinID)
            {
                // SEÇİLİB: Arxa fon SARI olsun və animasiya başlasın
                buttonBg.color = selectedColor;
                StartScaleAnim();
            }
            else
            {
                // İSTİFADƏYƏ HAZIR (Amma seçilməyib): Arxa fon olmasın (şəffaf)
                buttonBg.color = unlockedColor;
                StopScaleAnim();
            }
        }
    }

    // Animasiyanı başladan hissə
    void StartScaleAnim()
    {
        if (scaleCoroutine == null)
            scaleCoroutine = StartCoroutine(PulseAnimation());
    }

    void StopScaleAnim()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        icon.transform.localScale = Vector3.one; // Ölçünü normala qaytar
    }

    IEnumerator PulseAnimation()
    {
        while (true)
        {
            float duration = 0.8f; // Animasiya sürəti
            float elapsed = 0f;

            // Böyümə
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.Lerp(1f, 1.15f, Mathf.Sin((elapsed / duration) * Mathf.PI));
                icon.transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }
    }

    public void Click()
    {
        if (SkinsManager.Instance != null)
            SkinsManager.Instance.SelectSkin(skin);
    }
}
