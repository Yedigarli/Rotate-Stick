using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance; // Bunu Awake-dən yuxarıda elan et
    [Header("UI Elements")]
    public RectTransform giftPanel;      // Soldan gələcək panel
    public Button getGiftButton;        // Hədiyyə alma düyməsi
    public TMP_Text timerText;          // Saatı göstərən mətn
    public Image buttonGlow;            // Düymə hazır olanda parlaması üçün (opsional)

    [Header("Star Animation Settings")]
    public GameObject starPrefab;
    public Transform starParent;
    public Transform starStart;
    public Transform starEnd;
    public float moveDuration = 0.8f;
    public Ease moveEase = Ease.InBack;

    [Header("Distribution Settings")]
    public int starAmount = 15;
    public float totalDelay = 0.5f;

    [Header("Cooldown Settings")]
    public float giftCooldownHours = 0.001f;
    private string lastGiftKey = "LastGiftTime";
    private bool isReady = false;

    private void Awake()
    {
        Instance = this;
        // Paneli dərhal gizlə və ekranın çox uzağına qoy
        giftPanel.gameObject.SetActive(false);
        giftPanel.anchoredPosition = new Vector2(-1200f, giftPanel.anchoredPosition.y);
    }

    private void Update()
    {
        if (!isReady)
        {
            UpdateTimer();
        }
    }

    // --- TAYMER MƏNTİQİ ---
    private void UpdateTimer()
    {
        string lastTimeStr = PlayerPrefs.GetString(lastGiftKey, string.Empty);

        if (string.IsNullOrEmpty(lastTimeStr))
        {
            SetReady();
            return;
        }

        DateTime lastTime = DateTime.Parse(lastTimeStr);
        TimeSpan elapsed = DateTime.Now - lastTime;
        TimeSpan cooldown = TimeSpan.FromHours(giftCooldownHours);
        TimeSpan remaining = cooldown - elapsed;

        if (remaining.TotalSeconds <= 0)
        {
            SetReady();
        }
        else
        {
            isReady = false;
            getGiftButton.interactable = false;
            timerText.text = string.Format("{0:D2}:{1:D2}:{2:D2}",
                remaining.Hours, remaining.Minutes, remaining.Seconds);
        }
    }

    private void SetReady()
    {
        isReady = true;
        getGiftButton.interactable = true;
        timerText.text = "CLAIM GIFT!";

        // Düymə hazır olanda kiçik bir titrəmə effekti ver
        getGiftButton.transform.DOKill();
        getGiftButton.transform.DOPunchScale(Vector3.one * 0.1f, 1f, 2).SetLoops(-1).SetUpdate(true);
    }

    public void CheckGiftStatus()
    {
        UpdateTimer(); // Vaxtın bitib-bitmədiyini yoxla

        if (isReady)
        {
            // Yalnız hədiyyə hazır olduqda paneli göstər
            ShowFreeGift();
        }
        else
        {
            // Hədiyyə hazır deyilsə, paneli gizli saxla (ekranın çölündə)
            giftPanel.anchoredPosition = new Vector2(-1200f, giftPanel.anchoredPosition.y);
            giftPanel.gameObject.SetActive(false);
        }
    }

    // --- PANEL ANİMASİYALARI ---
    public void ShowFreeGift()
    {
        giftPanel.gameObject.SetActive(true);
        // Soldan mərkəzə gəlir
        giftPanel.DOAnchorPosX(0f, 0.6f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void CloseFreeGift()
    {
        giftPanel.DOAnchorPosX(-1200f, 0.5f).SetEase(Ease.InBack).SetUpdate(true)
                 .OnComplete(() => giftPanel.gameObject.SetActive(false));
    }

    // --- ULDUZ ALMA VƏ ANİMASİYA ---
    public void OnGetButtonClick()
    {
        // Düyməni dərhal bağla ki, təkrar basılmasın
        getGiftButton.interactable = false;
        getGiftButton.transform.DOKill();
        getGiftButton.transform.localScale = Vector3.one;

        // Vaxtı yadda saxla
        PlayerPrefs.SetString(lastGiftKey, DateTime.Now.ToString());
        isReady = false;

        // Ulduzları uçur
        float starPerDelay = totalDelay / starAmount;
        for (int i = 0; i < starAmount; i++)
        {
            ShowCoin(i * starPerDelay);
        }

        // 2 saniyə sonra paneli bağla
        DOVirtual.DelayedCall(2f, CloseFreeGift).SetUpdate(true);
    }

    private void ShowCoin(float delay)
    {
        var starObject = Instantiate(starPrefab, starParent);
        Vector3 myVisualScale = new Vector3(0.8f, 0.8f, 0.8f);

        // Başlanğıc vəziyyəti
        var spreadOffset = new Vector3(UnityEngine.Random.Range(-150f, 150f), UnityEngine.Random.Range(-150f, 150f), 0f);
        starObject.transform.position = starStart.position;
        starObject.transform.localScale = Vector3.zero;

        Sequence s = DOTween.Sequence();

        // 1. Sıçrayış (Pop out)
        s.Append(starObject.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
        s.Join(starObject.transform.DOMove(starStart.position + spreadOffset, 0.4f).SetEase(Ease.OutQuart));

        s.AppendInterval(0.05f);

        // 2. Hədəfə uçuş
        s.Append(starObject.transform.DOMove(starEnd.position, moveDuration).SetEase(moveEase));
        s.Join(starObject.transform.DOScale(myVisualScale, moveDuration));

        s.SetDelay(delay);
        s.SetUpdate(true);

        s.OnComplete(() =>
        {
            // Hədəf nöqtəsini titrət
            starEnd.DOKill();
            starEnd.localScale = myVisualScale;
            starEnd.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.2f).SetUpdate(true);

            // Real ulduz sayını artır (StarManager-in varsa bura yaz)
            if (StarManager.Instance != null) StarManager.Instance.AddStar(1);

            Destroy(starObject);
        });
    }

    // Bu funksiyanı TaskManager-in içinə əlavə et
    public void StartStarAnimationOnly(int amount)
    {
        starAmount = amount;
        float starPerDelay = totalDelay / starAmount;

        for (int i = 0; i < starAmount; i++)
        {
            ShowCoin(i * starPerDelay);
        }
    }
}
