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
            skinsbutton.onClick.AddListener(OpenSettings);
        if (closeSkinsButton != null)
            closeSkinsButton.onClick.AddListener(CloseSettings);
    }

    // Düymələr yarandıqda özlərini bura əlavə edir
    public void RegisterButton(SkinButton btn)
    {
        if (!allButtons.Contains(btn))
            allButtons.Add(btn);
    }

    public void SelectSkin(SkinData skin)
    {
        // 1. Kilid və Satınalma məntiqi
        bool unlocked =
            PlayerPrefs.GetInt("Skin_" + skin.skinID, skin.unlockedByDefault ? 1 : 0) == 1;

        if (!unlocked)
        {
            if (StarManager.Instance != null && !StarManager.Instance.SpendStars(skin.price))
                return; // Pul çatmasa dayandır

            PlayerPrefs.SetInt("Skin_" + skin.skinID, 1);
        }

        // 2. Seçimi yadda saxla
        PlayerPrefs.SetString("SelectedSkin", skin.skinID);
        PlayerPrefs.Save();

        // 3. Menyu topunu yenilə
        if (previewApplier != null)
            previewApplier.ApplySkin(skin);

        // 4. Bütün düymələrin yazısını (SELECTED/USE) yenilə
        foreach (SkinButton btn in allButtons)
        {
            btn.UpdateUI();
        }
    }

    public void OpenSettings()
    {
        if (isAnimating)
            return;
        skinsPanel.SetActive(true);
        StartCoroutine(Animate(true));
    }

    public void CloseSettings()
    {
        if (isAnimating)
            return;
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
}
