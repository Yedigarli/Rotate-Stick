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
    public float lockedScale = 1f;
    public float pressScale = 0.92f;

    [Header("Optional Groups")]
    public CanvasGroup rootGroup;
    public CanvasGroup priceGroup;

    private Tween selectTween;

    private void Awake()
    {
        if (rootGroup == null)
            rootGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        if (priceContainer != null && priceGroup == null)
            priceGroup =
                priceContainer.GetComponent<CanvasGroup>()
                ?? priceContainer.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (SkinsManager.Instance != null)
            SkinsManager.Instance.RegisterButton(this);

        if (skin != null && icon != null)
            icon.sprite = skin.sprite;

        UpdateUI();

        GetComponent<Button>().onClick.AddListener(Click);
    }

    private void OnEnable()
    {
        if (skin == null)
            return;

        StopAllCoroutines();
        StartCoroutine(EnableRoutine());
    }

    private IEnumerator EnableRoutine()
    {
        yield return null;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (skin == null)
            return;

        bool unlocked =
            PlayerPrefs.GetInt("Skin_" + skin.skinID, skin.unlockedByDefault ? 1 : 0) == 1;
        string selectedID = PlayerPrefs.GetString("SelectedSkin", "");

        if (!unlocked)
        {
            if (priceContainer != null)
                priceContainer.SetActive(true);
            if (priceGroup != null)
                priceGroup.alpha = 1f;
            if (lockIcon != null)
                lockIcon.SetActive(true);
            if (priceText != null)
                priceText.SetText("{0}", skin.price);

            if (buttonBg != null)
                buttonBg
                    .DOColor(lockedBgColor, 0.18f)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            if (icon != null)
            {
                if (hideLockedSkin && lockedSprite != null)
                    icon.sprite = lockedSprite;
                icon.DOColor(Color.black, 0.18f)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
                FitIconInFrame(true);
            }
            if (rootGroup != null)
                rootGroup.alpha = 0.85f;

            transform.localScale = Vector3.one;
            return;
        }

        if (lockIcon != null)
            lockIcon.SetActive(false);
        if (priceGroup != null)
        {
            priceGroup.DOKill();
            priceGroup
                .DOFade(0f, 0.12f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (priceContainer != null)
                        priceContainer.SetActive(false);
                });
        }
        else if (priceContainer != null)
        {
            priceContainer.SetActive(false);
        }

        if (icon != null)
        {
            icon.sprite = skin.sprite;
            icon.DOColor(Color.white, 0.12f)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            FitIconInFrame(false);
        }
        if (rootGroup != null)
            rootGroup.alpha = 1f;

        bool isSelected = selectedID == skin.skinID;
        if (buttonBg != null)
            buttonBg
                .DOColor(isSelected ? selectedColor : unlockedColor, 0.18f)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        if (isSelected)
            PlaySelectedAnimation();
        else
            transform.localScale = Vector3.one;
    }

    public void PlaySelectedAnimation()
    {
        selectTween?.Kill();
        transform.DOKill();
        if (icon != null)
            icon.transform.DOKill();

        transform.localScale = Vector3.one;
        if (icon != null)
            icon.transform.localScale = Vector3.one;

        Sequence seq = DOTween
            .Sequence()
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        seq.Append(transform.DOScale(1.07f, 0.12f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack));
        if (icon != null)
        {
            seq.Join(
                icon.transform.DOPunchScale(Vector3.one * 0.12f, 0.25f, 5, 0.8f)
                    .SetLink(icon.gameObject, LinkBehaviour.KillOnDestroy)
            );
        }
        selectTween = seq;
    }

    public void Click()
    {
        transform.DOKill();
        transform.localScale = Vector3.one * pressScale;
        transform
            .DOScale(1f, 0.12f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        if (SkinsManager.Instance != null)
            SkinsManager.Instance.SelectSkin(skin);
    }

    private void FitIconInFrame(bool locked)
    {
        if (icon == null)
            return;

        // Anchor-ları tam mərkəzə və ya tam yayılmağa (stretch) uyğunlaşdırırıq
        icon.rectTransform.anchorMin = Vector2.zero;
        icon.rectTransform.anchorMax = Vector2.one;

        // Padding (boşluq) dəyərini müəyyən edirik
        // Kilidlidirsə 0 (tam doldur), deyilsə təyin etdiyiniz padding-i tətbiq et
        float paddingX = locked ? 0f : unlockedIconPadding.x;
        float paddingY = locked ? 0f : unlockedIconPadding.y;

        // offsetMin sol və aşağı, offsetMax isə sağ və yuxarı boşluqları təmsil edir
        icon.rectTransform.offsetMin = new Vector2(paddingX, paddingY);
        icon.rectTransform.offsetMax = new Vector2(-paddingX, -paddingY);
    }

    private void OnDisable()
    {
        selectTween?.Kill();
        transform.DOKill();
        if (icon != null)
            icon.transform.DOKill();
    }
}
