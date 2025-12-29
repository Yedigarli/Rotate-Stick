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
    public Image buttonBg;
    public GameObject priceContainer;

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color selectedColor = Color.yellow;
    public Color unlockedColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Gümüşü/Boz rəng (unlocked üçün)

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

    private void OnEnable()
    {
        if (skin != null)
        {
            // Bir kadr gözləyirik ki, bütün UI elementləri "Wake Up" olsun
            StopAllCoroutines();
            StartCoroutine(EnableRoutine());
        }
    }

    IEnumerator EnableRoutine()
    {
        yield return null; // 1 frame gözlə
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (skin == null)
            return;

        // PlayerPrefs-dən ən son seçimi dərhal oxu
        bool unlocked =
            PlayerPrefs.GetInt("Skin_" + skin.skinID, skin.unlockedByDefault ? 1 : 0) == 1;
        string selectedID = PlayerPrefs.GetString("SelectedSkin", "");

        if (!unlocked)
        {
            priceContainer.SetActive(true);
            lockIcon.SetActive(true);
            priceText.text = skin.price.ToString();
            buttonBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            icon.color = Color.black;
            StopScaleAnim();
        }
        else
        {
            priceContainer.SetActive(false);
            lockIcon.SetActive(false);
            icon.color = Color.white;

            // Debug ataq ki, kodun bura girdiyini konsolda görək
            if (selectedID == skin.skinID)
            {
                buttonBg.color = selectedColor;
                StartScaleAnim();
            }
            else
            {
                buttonBg.color = unlockedColor;
                StopScaleAnim();
            }
        }
    }

    void StartScaleAnim()
    {
        // Əgər artıq işləyirsə, təzədən başlatma
        if (scaleCoroutine == null)
        {
            //scaleCoroutine = StartCoroutine(PulseAnimation());
        }
    }

    void StopScaleAnim()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        icon.transform.localScale = Vector3.one;
    }

    // IEnumerator PulseAnimation()
    // {
    //     // UI panelləri adətən Time.timeScale = 0 olsa da işləməlidir
    //     while (true)
    //     {
    //         float duration = 0.8f;
    //         float elapsed = 0f;

    //         while (elapsed < duration)
    //         {
    //             elapsed += Time.unscaledDeltaTime; // Oyun dayansa belə animasiya işləsin
    //             float s = Mathf.Lerp(1f, 1.15f, Mathf.Sin((elapsed / duration) * Mathf.PI));
    //             icon.transform.localScale = new Vector3(s, s, 1f);
    //             yield return null;
    //         }
    //     }
    // }

    public void Click()
    {
        // Düyməyə basanda vizual geri bildirim
        StopAllCoroutines(); // ResetScale üçün köhnəni dayandır
        transform.localScale = Vector3.one * 0.9f;
        StartCoroutine(ResetScale());

        if (SkinsManager.Instance != null)
            SkinsManager.Instance.SelectSkin(skin);
    }

    IEnumerator ResetScale()
    {
        yield return new WaitForSecondsRealtime(0.05f);
        transform.localScale = Vector3.one;
        // Əgər seçilidirsə, Pulse animasiyasını geri qaytar (UpdateUI bunu edəcək)
        UpdateUI();
    }

    public void MakeSpriteGlow(SpriteRenderer sr, float intensity)
    {
        // Spritun rəngini HDR faktoruna vuraraq parladırıq
        float factor = Mathf.Pow(2, intensity);
        sr.color = new Color(1 * factor, 1 * factor, 1 * factor, 1);
    }
}
