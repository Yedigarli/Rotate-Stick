using System.Collections;
using System.Collections.Generic;
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

    [Header("Target Animation (Menu Only)")]
    public GameObject targetObject; // Target Parent obyekti
    public float jumpHeight = 3.5f;
    private Vector3 originalPosition;

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

    void Start()
    {
        // Targetin ilkin yerini yadda saxla (yalnız menyuda lazımdır)
        if (targetObject != null)
            originalPosition = targetObject.transform.position;
    }

    public void RegisterButton(SkinButton btn)
    {
        if (!allButtons.Contains(btn))
            allButtons.Add(btn);
    }

    public void SelectSkin(SkinData skin)
    {
        if (skin == null)
            return;

        string skinKey = "Skin_" + skin.skinID;
        bool isUnlocked = PlayerPrefs.GetInt(skinKey, skin.unlockedByDefault ? 1 : 0) == 1;

        if (isUnlocked)
            CompleteSelection(skin);
        else
        {
            if (StarManager.Instance != null && StarManager.Instance.SpendStars(skin.price))
            {
                PlayerPrefs.SetInt(skinKey, 1);
                PlayerPrefs.Save();
                CompleteSelection(skin);
            }
        }
    }

    private void CompleteSelection(SkinData skin)
    {
        PlayerPrefs.SetString("SelectedSkin", skin.skinID);
        PlayerPrefs.Save();

        SkinApplier sceneApplier = FindFirstObjectByType<SkinApplier>();
        if (sceneApplier != null)
            sceneApplier.ApplySkin(skin);

        foreach (SkinButton btn in allButtons)
            btn.UpdateUI();
    }

    public void OpenSkins()
    {
        if (isAnimating)
            return;

        // 1. Topları gizlət (SetActive(false))
        SetBallsActive(false);

        // 2. Yeni top yaranmasını dayandır
        if (MainMenuManager.Instance != null)
            MainMenuManager.Instance.isSkinsOpen = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.isSettingsOpen = true;
            Time.timeScale = 0f;
        }

        skinsPanel.SetActive(true);
        foreach (SkinButton btn in allButtons)
            btn.UpdateUI();

        StopAllCoroutines();
        StartCoroutine(Animate(true));
    }

    public void CloseSkins()
    {
        if (isAnimating)
            return;

        // ⭐ 1. Gizli topları yenidən GÖSTƏR
        SetBallsActive(true);

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.gameoverPOPUP.activeSelf)
                Time.timeScale = 0f;
            else
            {
                Time.timeScale = 1f;
                GameManager.Instance.isSettingsOpen = false;
            }
        }
        else
            Time.timeScale = 1f;

        StopAllCoroutines();
        StartCoroutine(Animate(false));
    }

    IEnumerator Animate(bool open)
    {
        isAnimating = true;
        float t = 0f;

        // UI Dəyərləri
        float startA = open ? 0 : 1;
        float endA = open ? 1 : 0;
        Vector3 startS = open ? Vector3.one * 0.8f : Vector3.one;
        Vector3 endS = open ? Vector3.one : Vector3.one * 0.8f;

        // ⭐ Target Hərəkət Dəyərləri (Yalnız menyuda)
        Vector3 startPos = targetObject != null ? targetObject.transform.position : Vector3.zero;
        Vector3 endPos = open ? originalPosition + Vector3.up * jumpHeight : originalPosition;

        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / animDuration;
            float curve = p * p * (3 - 2 * p); // Smooth keçid

            // UI Animasiyası
            canvasGroup.alpha = Mathf.Lerp(startA, endA, p);
            skinsPanel.transform.localScale = Vector3.Lerp(startS, endS, p);

            // ⭐ Target Hərəkəti (Scale-ə toxunmadan)
            if (targetObject != null)
                targetObject.transform.position = Vector3.Lerp(startPos, endPos, curve);

            yield return null;
        }

        canvasGroup.alpha = endA;
        skinsPanel.transform.localScale = endS;

        if (targetObject != null)
            targetObject.transform.position = endPos;

        if (!open)
        {
            skinsPanel.SetActive(false);

            // ⭐ Topları geri qaytar
            SetBallsActive(true);

            // ⭐ Yeni top yaranmasına icazə ver
            if (MainMenuManager.Instance != null)
                MainMenuManager.Instance.isSkinsOpen = false;
        }

        isAnimating = false;
    }

    // ⭐ Topları Tag-ə görə gizlədib-açan funksiya
    private void SetBallsActive(bool state)
    {
        if (MainMenuManager.Instance == null)
            return;

        // Siyahıdakı hər bir topu yoxla
        for (int i = MainMenuManager.Instance.activeBalls.Count - 1; i >= 0; i--)
        {
            GameObject b = MainMenuManager.Instance.activeBalls[i];

            if (b != null)
            {
                b.SetActive(state);
            }
            else
            {
                // Əgər top artıq yoxdursa (null-dursa), siyahıdan birdəfəlik sil
                MainMenuManager.Instance.activeBalls.RemoveAt(i);
            }
        }
    }

    public void MakeSpriteGlow(SpriteRenderer sr, float intensity)
    {
        float factor = Mathf.Pow(2, intensity);
        sr.color = new Color(1 * factor, 1 * factor, 1 * factor, 1);
    }
}
