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

    [Header("Visual Settings")]
    public Image buttonBg;
    public GameObject priceContainer;

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color selectedColor = Color.yellow;
    public Color unlockedColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    private Tween selectTween;

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

        bool unlocked = PlayerPrefs.GetInt("Skin_" + skin.skinID, skin.unlockedByDefault ? 1 : 0) == 1;
        string selectedID = PlayerPrefs.GetString("SelectedSkin", "");

        if (!unlocked)
        {
            priceContainer.SetActive(true);
            lockIcon.SetActive(true);
            priceText.SetText("{0}", skin.price);
            buttonBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            icon.color = Color.black;
            return;
        }

        priceContainer.SetActive(false);
        lockIcon.SetActive(false);
        icon.color = Color.white;

        bool isSelected = selectedID == skin.skinID;
        buttonBg.color = isSelected ? selectedColor : unlockedColor;

        if (isSelected)
            PlaySelectedAnimation();
        else
            transform.localScale = Vector3.one;
    }

    public void PlaySelectedAnimation()
    {
        selectTween?.Kill();
        transform.DOKill();
        icon.transform.DOKill();

        transform.localScale = Vector3.one;
        icon.transform.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(transform.DOScale(1.07f, 0.12f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack));
        seq.Join(icon.transform.DOPunchScale(Vector3.one * 0.12f, 0.25f, 5, 0.8f));
        selectTween = seq;
    }

    public void Click()
    {
        transform.DOKill();
        transform.localScale = Vector3.one * 0.92f;
        transform.DOScale(1f, 0.12f).SetEase(Ease.OutBack).SetUpdate(true);

        if (SkinsManager.Instance != null)
            SkinsManager.Instance.SelectSkin(skin);
    }

    public void MakeSpriteGlow(SpriteRenderer sr, float intensity)
    {
        float factor = Mathf.Pow(2, intensity);
        sr.color = new Color(1 * factor, 1 * factor, 1 * factor, 1);
    }
}
