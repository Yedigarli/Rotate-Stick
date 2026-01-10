using System.Collections;
using System.Collections.Generic;
using DG.Tweening; // DOTween əlavə edildi
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
    private SkinApplier cachedSceneApplier; // Keşlənmiş applier

    [Header("Target Animation")]
    public GameObject targetObject;
    public float jumpHeight = 3.5f;
    private Vector3 originalPosition;
    private bool hasOriginalPos = false; // Mövqe qeyd edilibmi?

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

    public void RegisterButton(SkinButton btn)
    {
        if (!allButtons.Contains(btn))
            allButtons.Add(btn);
    }

    public void SelectSkin(SkinData skin)
    {
        if (skin == null)
            return;

        UISoundManager.Instance?.PlayClick(); // 🔊
        string skinKey = "Skin_" + skin.skinID;
        bool isUnlocked = PlayerPrefs.GetInt(skinKey, skin.unlockedByDefault ? 1 : 0) == 1;

        if (isUnlocked)
            CompleteSelection(skin);
        else if (StarManager.Instance != null && StarManager.Instance.SpendStars(skin.price))
        {
            PlayerPrefs.SetInt(skinKey, 1);
            PlayerPrefs.Save();
            CompleteSelection(skin);
        }
    }

    private void CompleteSelection(SkinData skin)
    {
        PlayerPrefs.SetString("SelectedSkin", skin.skinID);
        PlayerPrefs.Save();

        if (cachedSceneApplier == null)
            cachedSceneApplier = FindFirstObjectByType<SkinApplier>();
        if (cachedSceneApplier != null)
            cachedSceneApplier.ApplySkin(skin);

        foreach (SkinButton btn in allButtons)
            btn.UpdateUI();
    }

    public void OpenSkins()
    {
        if (isAnimating)
            return;

        UISoundManager.Instance?.PlayClick(); // 🔊

        // Target mövqeyini ilk dəfə açılışda götür (Menyu üçün)
        if (targetObject != null && !hasOriginalPos)
        {
            originalPosition = targetObject.transform.position;
            hasOriginalPos = true;
        }

        SetBallsActive(false);

        if (MainMenuManager.Instance != null)
            MainMenuManager.Instance.isSkinsOpen = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.isSettingsOpen = true;
            // Əgər DOTween animasiyalarınız varsa, onları burada Pause edə bilərsiniz
            Time.timeScale = 0f;
        }

        skinsPanel.SetActive(true);
        skinsPanel.transform.DOKill(); // DOTween toqquşmasını önlə

        foreach (SkinButton btn in allButtons)
            btn.UpdateUI();

        StopAllCoroutines();
        StartCoroutine(Animate(true));
    }

    public void CloseSkins()
    {
        if (isAnimating)
            return;

        UISoundManager.Instance?.PlayClick(); // 🔊

        if (GameManager.Instance != null)
        {
            // Əgər GameOver panel açıqdırsa zamanı başlatma
            if (!GameManager.Instance.gameoverPOPUP.activeSelf)
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

        float startA = open ? 0 : 1;
        float endA = open ? 1 : 0;
        Vector3 startS = open ? Vector3.one * 0.8f : Vector3.one;
        Vector3 endS = open ? Vector3.one : Vector3.one * 0.8f;

        // Target hərəkəti (Hər dəfə original mövqeyə görə hesabla)
        Vector3 currentTargetPos =
            targetObject != null ? targetObject.transform.position : Vector3.zero;
        Vector3 targetEndPos = open ? originalPosition + Vector3.up * jumpHeight : originalPosition;

        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / animDuration;
            float curve = p * p * (3 - 2 * p);

            canvasGroup.alpha = Mathf.Lerp(startA, endA, p);
            skinsPanel.transform.localScale = Vector3.Lerp(startS, endS, p);

            if (targetObject != null)
                targetObject.transform.position = Vector3.Lerp(
                    currentTargetPos,
                    targetEndPos,
                    curve
                );

            yield return null;
        }

        canvasGroup.alpha = endA;
        skinsPanel.transform.localScale = endS;
        if (targetObject != null)
            targetObject.transform.position = targetEndPos;

        if (!open)
        {
            skinsPanel.SetActive(false);
            SetBallsActive(true);
            if (MainMenuManager.Instance != null)
                MainMenuManager.Instance.isSkinsOpen = false;
        }

        isAnimating = false;
    }

    private void SetBallsActive(bool state)
    {
        // 1. Menyudakı topları gizlə
        if (MainMenuManager.Instance != null)
        {
            for (int i = MainMenuManager.Instance.activeBalls.Count - 1; i >= 0; i--)
            {
                GameObject b = MainMenuManager.Instance.activeBalls[i];
                if (b != null)
                    b.SetActive(state);
                else
                    MainMenuManager.Instance.activeBalls.RemoveAt(i);
            }
        }

        // 2. Oyundakı (Game Scene) topları gizlə
        if (GameManager.Instance != null)
        {
            GameObject[] gameBalls = GameObject.FindGameObjectsWithTag("Ball");
            foreach (GameObject b in gameBalls)
                b.SetActive(state);
        }
    }
}
