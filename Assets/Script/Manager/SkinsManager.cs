using System.Collections;
using System.Collections.Generic; // List-dən istifadə üçün mütləqdir
using UnityEngine;
using UnityEngine.UI;

public class SkinsManager : MonoBehaviour
{
    public static SkinsManager Instance;

    public GameObject skinsPanel;
    public CanvasGroup canvasGroup;

    public Button skinsbutton;
    public Button closeSkinsButton;

    public float animDuration = 0.25f;
    private bool isAnimating;

    [Header("Skin System")]
    public SkinData[] skins;
    public SkinApplier previewApplier;

    // Bütün düymələri yadda saxlayan siyahı
    private List<SkinButton> allButtons = new List<SkinButton>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        skinsPanel.SetActive(false);

        if (skinsbutton != null)
            skinsbutton.onClick.AddListener(OpenSkins);
        if (closeSkinsButton != null)
            closeSkinsButton.onClick.AddListener(CloseSkins);
    }

    // Düymələr yarandıqda özlərini bura əlavə edir
    public void RegisterButton(SkinButton btn)
    {
        if (!allButtons.Contains(btn))
            allButtons.Add(btn);
    }

    public void SelectSkin(SkinData skin)
    {
        // ... (mövcud satınalma kodların olduğu kimi qalır)

        // 1. Seçimi yadda saxla
        PlayerPrefs.SetString("SelectedSkin", skin.skinID);
        PlayerPrefs.Save();

        // 2. Səhnədəki Ayını tap və dərhal dəyiş (Həm Menu, həm Game üçün)
        SkinApplier sceneApplier = FindFirstObjectByType<SkinApplier>();
        if (sceneApplier != null)
        {
            sceneApplier.ApplySkin(skin);
        }

        // 3. Düymələri yenilə
        foreach (SkinButton btn in allButtons)
        {
            btn.UpdateUI();
        }
    }

    public void OpenSkins()
    {
        if (isAnimating)
            return;

        // ⭐ Oyunu dayandır (Oyun səhnəsindəsənsə)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isSettingsOpen = true;
            Time.timeScale = 0f;
        }

        skinsPanel.SetActive(true);
        foreach (SkinButton btn in allButtons)
            btn.UpdateUI();
        StartCoroutine(Animate(true));
    }

    public void CloseSkins()
    {
        if (isAnimating)
            return;

        // ⭐ ƏSAS HİSSƏ BURADIR:
        // Əgər GameOver popup-ı hazırda ekrandadırsa, zaman 0 olaraq qalsın.
        // Əgər GameOver ekranda DEYİLSƏ (yəni oyun zamanı açmısansa), zaman 1 olsun.
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.gameoverPOPUP.activeSelf)
            {
                Time.timeScale = 0f; // GameOver açıqdırsa, oyunu başlatma
            }
            else
            {
                Time.timeScale = 1f; // Normal oyun zamanı açılıbsa, davam et
                GameManager.Instance.isSettingsOpen = false;
            }
        }
        else
        {
            Time.timeScale = 1f; // Menu səhnəsindəsənsə normal davam et
        }

        StartCoroutine(Animate(false));
    }

    IEnumerator Animate(bool open)
    {
        isAnimating = true;
        float t = 0f;
        float startA = open ? 0 : 1;
        float endA = open ? 1 : 0;
        Vector3 startS = open ? Vector3.one * 0.8f : Vector3.one;
        Vector3 endS = open ? Vector3.one : Vector3.one * 0.8f;

        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / animDuration;
            canvasGroup.alpha = Mathf.Lerp(startA, endA, p);
            skinsPanel.transform.localScale = Vector3.Lerp(startS, endS, p);
            yield return null;
        }

        canvasGroup.alpha = endA;
        skinsPanel.transform.localScale = endS;
        if (!open)
            skinsPanel.SetActive(false);
        isAnimating = false;
    }

    public void MakeSpriteGlow(SpriteRenderer sr, float intensity)
    {
        // Spritun rəngini HDR faktoruna vuraraq parladırıq
        float factor = Mathf.Pow(2, intensity);
        sr.color = new Color(1 * factor, 1 * factor, 1 * factor, 1);
    }
}
