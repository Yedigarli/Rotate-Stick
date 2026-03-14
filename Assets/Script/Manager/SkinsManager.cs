using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
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

    [Header("Scroll UI")]
    public ScrollRect skinsScrollRect;
    public SkinsScrollAnimator scrollAnimator;
    public bool enableScrollEffects = true;

    [Header("Skin Menu UI")]
    public Image selectedSkinImage;
    public GameObject selectedSkinGlow;
    public TMP_Text unlockedCountText;
    public TMP_Text starCountText;
    public Button randomUnlockButton;
    public TMP_Text randomPriceText;
    public int randomUnlockPrice = 250;

    [Header("Target Animation")]
    public GameObject targetObject;
    public float jumpHeight = 3.5f;
    private Vector3 originalPosition;
    private bool hasOriginalPos;

    private readonly List<SkinButton> allButtons = new List<SkinButton>();
    private SkinButton selectedButton;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        if (skinsPanel != null)
            skinsPanel.SetActive(false);

        if (skinsScrollRect == null && skinsPanel != null)
            skinsScrollRect = skinsPanel.GetComponentInChildren<ScrollRect>(true);
        if (skinsScrollRect != null)
        {
            if (scrollAnimator == null)
                scrollAnimator = skinsScrollRect.GetComponent<SkinsScrollAnimator>() ?? skinsScrollRect.gameObject.AddComponent<SkinsScrollAnimator>();
            if (enableScrollEffects)
                scrollAnimator.Setup(skinsScrollRect);
            else
                scrollAnimator.scrollRect = skinsScrollRect;
        }

        skinsbutton?.onClick.AddListener(OpenSkins);
        closeSkinsButton?.onClick.AddListener(CloseSkins);
        randomUnlockButton?.onClick.AddListener(UnlockRandomSkin);
    }

    public void RegisterButton(SkinButton btn)
    {
        if (btn != null && !allButtons.Contains(btn))
            allButtons.Add(btn);
    }

    public void SelectSkin(SkinData skin)
    {
        SelectSkin(skin, null);
    }

    public void SelectSkin(SkinData skin, SkinButton sourceButton)
    {
        if (skin == null)
            return;

        UISoundManager.Instance?.PlayClick();
        string skinKey = "Skin_" + skin.skinID;
        bool isUnlocked = PlayerPrefs.GetInt(skinKey, skin.unlockedByDefault ? 1 : 0) == 1;

        if (isUnlocked)
        {
            CompleteSelection(skin, sourceButton);
            return;
        }

        if (StarManager.Instance != null && StarManager.Instance.SpendStars(skin.price))
        {
            PlayerPrefs.SetInt(skinKey, 1);
            PlayerPrefs.Save();
            CompleteSelection(skin, sourceButton);
        }
    }

    private void CompleteSelection(SkinData skin, SkinButton sourceButton)
    {
        PlayerPrefs.SetString("SelectedSkin", skin.skinID);
        PlayerPrefs.Save();

        if (cachedSceneApplier == null)
            cachedSceneApplier = FindFirstObjectByType<SkinApplier>();
        if (cachedSceneApplier != null)
            cachedSceneApplier.ApplySkin(skin);

        // Seçilmiş düyməni yeniləyirik
        selectedButton = sourceButton;

        for (int i = 0; i < allButtons.Count; i++)
        {
            SkinButton btn = allButtons[i];
            if (btn == null)
                continue;

            // Digər düymələrdə gözlənilməyən click animasiyasını dayandırırıq
            if (sourceButton == null || btn != sourceButton)
                btn.StopClickAnimation();

            // Burada UpdateUI çağırılır, amma animasiyanı idarə etmək üçün parametr göndərə bilərik
            // və ya UpdateUI içində məntiqi dəyişə bilərik.
            btn.UpdateUI();
        }

        UpdateSelectedPreview();
        UpdateSkinMenuUI();
    }

    public bool IsButtonSelected(SkinButton btn, SkinData skin)
    {
        if (btn == null || skin == null)
            return false;

        EnsureSelectedButton();
        return selectedButton == btn;
    }

    private void EnsureSelectedButton()
    {
        if (selectedButton != null)
            return;

        string selectedID = PlayerPrefs.GetString("SelectedSkin", "");
        for (int i = 0; i < allButtons.Count; i++)
        {
            SkinButton btn = allButtons[i];
            if (btn != null && btn.skin != null && btn.skin.skinID == selectedID)
            {
                selectedButton = btn;
                break;
            }
        }
    }

    private SkinButton FindFirstButtonForSkin(SkinData skin)
    {
        if (skin == null)
            return null;

        for (int i = 0; i < allButtons.Count; i++)
        {
            SkinButton btn = allButtons[i];
            if (btn != null && btn.skin == skin)
                return btn;
        }

        return null;
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

        UpdateSelectedPreview();
        UpdateSkinMenuUI();

        if (skinsScrollRect != null)
        {
            skinsScrollRect.verticalNormalizedPosition = 1f;
            if (scrollAnimator != null)
                scrollAnimator.Refresh();
        }

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

    private SkinData GetSelectedSkin()
    {
        if (skins == null || skins.Length == 0)
            return null;

        string selectedID = PlayerPrefs.GetString("SelectedSkin", "");
        for (int i = 0; i < skins.Length; i++)
        {
            SkinData skin = skins[i];
            if (skin != null && skin.skinID == selectedID)
                return skin;
        }

        for (int i = 0; i < skins.Length; i++)
        {
            SkinData skin = skins[i];
            if (skin == null)
                continue;
            string skinKey = "Skin_" + skin.skinID;
            bool unlocked = PlayerPrefs.GetInt(skinKey, skin.unlockedByDefault ? 1 : 0) == 1;
            if (unlocked)
                return skin;
        }

        return skins[0];
    }

    private void UpdateSelectedPreview()
    {
        SkinData skin = GetSelectedSkin();
        if (skin == null)
            return;

        if (selectedSkinImage != null)
            selectedSkinImage.sprite = skin.sprite;

        if (selectedSkinGlow != null)
        {
            selectedSkinGlow.SetActive(true);
            selectedSkinGlow.transform.DOKill();
            selectedSkinGlow.transform.localScale = Vector3.one;
            selectedSkinGlow.transform.DOScale(1.08f, 0.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(selectedSkinGlow, LinkBehaviour.KillOnDestroy);
        }
    }

    private void UpdateSkinMenuUI()
    {
        int total = skins != null ? skins.Length : 0;
        int unlocked = GetUnlockedSkinCount();

        if (unlockedCountText != null)
            unlockedCountText.SetText("{0}/{1}", unlocked, total);

        int stars = StarManager.Instance != null ? StarManager.Instance.GetStars() : PlayerPrefs.GetInt("Stars", 0);
        if (starCountText != null)
            starCountText.SetText("{0}", stars);

        if (randomPriceText != null)
            randomPriceText.SetText("{0}", randomUnlockPrice);

        bool hasLocked = GetLockedSkinCount() > 0;
        bool canAfford = stars >= randomUnlockPrice;
        if (randomUnlockButton != null)
            randomUnlockButton.interactable = hasLocked && canAfford;
    }

    public void UnlockRandomSkin()
    {
        if (skins == null || skins.Length == 0)
            return;

        List<SkinData> locked = new List<SkinData>();
        for (int i = 0; i < skins.Length; i++)
        {
            SkinData skin = skins[i];
            if (skin == null)
                continue;
            string skinKey = "Skin_" + skin.skinID;
            bool isUnlocked = PlayerPrefs.GetInt(skinKey, skin.unlockedByDefault ? 1 : 0) == 1;
            if (!isUnlocked)
                locked.Add(skin);
        }

        if (locked.Count == 0)
            return;
        if (StarManager.Instance == null || !StarManager.Instance.SpendStars(randomUnlockPrice))
            return;

        SkinData chosen = locked[Random.Range(0, locked.Count)];
        string chosenKey = "Skin_" + chosen.skinID;
        PlayerPrefs.SetInt(chosenKey, 1);
        PlayerPrefs.Save();

        CompleteSelection(chosen, FindFirstButtonForSkin(chosen));
    }

    private int GetLockedSkinCount()
    {
        if (skins == null || skins.Length == 0)
            return 0;

        int locked = 0;
        for (int i = 0; i < skins.Length; i++)
        {
            SkinData skin = skins[i];
            if (skin == null)
                continue;

            string skinKey = "Skin_" + skin.skinID;
            bool isUnlocked = PlayerPrefs.GetInt(skinKey, skin.unlockedByDefault ? 1 : 0) == 1;
            if (!isUnlocked)
                locked++;
        }

        return locked;
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
