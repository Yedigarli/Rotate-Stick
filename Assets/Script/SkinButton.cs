using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinButton : MonoBehaviour
{
    public SkinData skin;
    public Image icon;
    public TMP_Text priceText;
    public GameObject lockIcon;
    public RectTransform iconFrame;
    public Vector2 unlockedIconPadding = new Vector2(6f, 6f);
    public Sprite lockedSprite;
    public bool hideLockedSkin = true;

    [Header("Visual Settings")]
    public Image buttonBg;
    public GameObject priceContainer;

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color selectedColor = Color.yellow;
    public Color unlockedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color lockedBgColor = new Color(0.18f, 0.18f, 0.18f, 0.9f);

    [Header("Animation Settings")]
    public float pressScale = 0.92f;
    public bool enableClickAnimation = true;
    public bool autoDisableClickAnimationByParent = true;
    public string clickAnimOnlyUnderParentName = "Item_1";

    [Header("Optional Groups")]
    public CanvasGroup rootGroup;
    public CanvasGroup priceGroup;

    private Tween selectTween;

    private void Awake()
    {
        if (rootGroup == null)
            rootGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        
        if (priceContainer != null && priceGroup == null)
            priceGroup = priceContainer.GetComponent<CanvasGroup>() ?? priceContainer.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (SkinsManager.Instance != null)
            SkinsManager.Instance.RegisterButton(this);

        if (skin != null && icon != null)
            icon.sprite = skin.sprite;

        UpdateUI();

        if (autoDisableClickAnimationByParent && !IsUnderParentNamed(clickAnimOnlyUnderParentName))
            enableClickAnimation = false;

        GetComponent<Button>().onClick.AddListener(Click);
    }

    public void UpdateUI()
    {
        if (skin == null) return;

        bool unlocked = PlayerPrefs.GetInt("Skin_" + skin.skinID, skin.unlockedByDefault ? 1 : 0) == 1;
        bool isSelected = SkinsManager.Instance != null && SkinsManager.Instance.IsButtonSelected(this, skin);

        if (!unlocked)
        {
            if (priceContainer != null) priceContainer.SetActive(true);
            if (priceGroup != null) priceGroup.alpha = 1f;
            if (lockIcon != null) lockIcon.SetActive(true);
            if (priceText != null) priceText.SetText("{0}", skin.price);
            if (buttonBg != null) buttonBg.DOColor(lockedBgColor, 0.18f).SetUpdate(true);
            if (icon != null)
            {
                if (hideLockedSkin && lockedSprite != null) icon.sprite = lockedSprite;
                icon.DOColor(Color.black, 0.18f).SetUpdate(true);
                FitIconInFrame(true);
            }
            if (rootGroup != null) rootGroup.alpha = 0.85f;
        }
        else
        {
            if (lockIcon != null) lockIcon.SetActive(false);
            if (priceContainer != null) priceContainer.SetActive(false);
            if (icon != null)
            {
                icon.sprite = skin.sprite;
                icon.DOColor(Color.white, 0.12f).SetUpdate(true);
                FitIconInFrame(false);
            }
            if (rootGroup != null) rootGroup.alpha = 1f;
            if (buttonBg != null) buttonBg.DOColor(isSelected ? selectedColor : unlockedColor, 0.18f).SetUpdate(true);
        }
        // Scale idarəsi ScrollAnimator və Click funksiyasına həvalə edilib
    }

    public void Click()
    {
        if (SkinsManager.Instance != null)
            SkinsManager.Instance.SelectSkin(skin, this);

        if (!enableClickAnimation) return;

        transform.DOKill();
        transform.localScale = Vector3.one * pressScale;
        transform.DOScale(1f, 0.12f).SetEase(Ease.OutBack).SetUpdate(true);
        PlaySelectedAnimation();
    }

    public void PlaySelectedAnimation()
    {
        selectTween?.Kill();
        transform.DOKill();
        if (icon != null) icon.transform.DOKill();

        Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        seq.Append(transform.DOScale(1.07f, 0.12f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack));
        if (icon != null)
            seq.Join(icon.transform.DOPunchScale(Vector3.one * 0.12f, 0.25f, 5, 0.8f));
        
        selectTween = seq;
    }

    public void StopClickAnimation()
    {
        selectTween?.Kill();
        transform.DOKill();
        if (icon != null) icon.transform.DOKill();
    }

    private bool IsUnderParentNamed(string parentName)
    {
        if (string.IsNullOrEmpty(parentName)) return true;
        Transform t = transform.parent;
        while (t != null) {
            if (t.name == parentName) return true;
            t = t.parent;
        }
        return false;
    }

    private void FitIconInFrame(bool locked)
    {
        if (icon == null) return;
        icon.rectTransform.anchorMin = Vector2.zero;
        icon.rectTransform.anchorMax = Vector2.one;
        float pX = locked ? 0f : unlockedIconPadding.x;
        float pY = locked ? 0f : unlockedIconPadding.y;
        icon.rectTransform.offsetMin = new Vector2(pX, pY);
        icon.rectTransform.offsetMax = new Vector2(-pX, -pY);
    }
}
