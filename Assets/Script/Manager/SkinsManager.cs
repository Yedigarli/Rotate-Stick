using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
    private SkinApplier cachedSceneApplier;

    [Header("Target Animation")]
    public GameObject targetObject;
    public float jumpHeight = 3.5f;
    private Vector3 originalPosition;
    private bool hasOriginalPos;

    private readonly List<SkinButton> allButtons = new List<SkinButton>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        if (skinsPanel != null)
            skinsPanel.SetActive(false);

        skinsbutton?.onClick.AddListener(OpenSkins);
        closeSkinsButton?.onClick.AddListener(CloseSkins);
    }

    public void RegisterButton(SkinButton btn)
    {
        if (btn != null && !allButtons.Contains(btn))
            allButtons.Add(btn);
    }

    public void SelectSkin(SkinData skin)
    {
        if (skin == null)
            return;

        UISoundManager.Instance?.PlayClick();
        string skinKey = "Skin_" + skin.skinID;
        bool isUnlocked = PlayerPrefs.GetInt(skinKey, skin.unlockedByDefault ? 1 : 0) == 1;

        if (isUnlocked)
        {
            CompleteSelection(skin);
            return;
        }

        if (StarManager.Instance != null && StarManager.Instance.SpendStars(skin.price))
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

        for (int i = 0; i < allButtons.Count; i++)
            allButtons[i].UpdateUI();
    }

    public void OpenSkins()
    {
        if (isAnimating)
            return;

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
            Time.timeScale = 0f;
        }

        skinsPanel.SetActive(true);
        skinsPanel.transform.DOKill();

        for (int i = 0; i < allButtons.Count; i++)
            allButtons[i].UpdateUI();

        StopAllCoroutines();
        StartCoroutine(Animate(true));
    }

    public void CloseSkins()
    {
        if (isAnimating)
            return;

        if (GameManager.Instance != null)
        {
            if (!GameManager.Instance.gameoverPOPUP.activeSelf)
            {
                Time.timeScale = 1f;
                GameManager.Instance.isSettingsOpen = false;
            }
        }
        else
        {
            Time.timeScale = 1f;
        }

        StopAllCoroutines();
        StartCoroutine(Animate(false));
    }

    private IEnumerator Animate(bool open)
    {
        isAnimating = true;
        float t = 0f;

        float startA = open ? 0f : 1f;
        float endA = open ? 1f : 0f;
        Vector3 startS = open ? Vector3.one * 0.86f : Vector3.one;
        Vector3 endS = open ? Vector3.one : Vector3.one * 0.86f;

        Vector3 currentTargetPos = targetObject != null ? targetObject.transform.position : Vector3.zero;
        Vector3 targetEndPos = open ? originalPosition + Vector3.up * jumpHeight : originalPosition;

        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / animDuration;
            float curve = p * p * (3f - 2f * p);

            canvasGroup.alpha = Mathf.Lerp(startA, endA, p);
            skinsPanel.transform.localScale = Vector3.Lerp(startS, endS, p);

            if (targetObject != null)
                targetObject.transform.position = Vector3.Lerp(currentTargetPos, targetEndPos, curve);

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
    public int GetUnlockedSkinCount()
    {
        if (skins == null || skins.Length == 0)
            return 0;

        int unlocked = 0;
        for (int i = 0; i < skins.Length; i++)
        {
            SkinData skin = skins[i];
            if (skin == null)
                continue;

            string skinKey = "Skin_" + skin.skinID;
            bool isUnlocked = PlayerPrefs.GetInt(skinKey, skin.unlockedByDefault ? 1 : 0) == 1;
            if (isUnlocked)
                unlocked++;
        }

        return unlocked;
    }

    public float GetUnlockedSkinsFill01()
    {
        if (skins == null || skins.Length == 0)
            return 0f;

        return Mathf.Clamp01((float)GetUnlockedSkinCount() / skins.Length);
    }
    private void SetBallsActive(bool state)
    {
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

        if (GameManager.Instance != null && !state && ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.DeactivateAll("Ball");
            ObjectPooler.Instance.DeactivateAll("StarBall");
        }
    }
}

