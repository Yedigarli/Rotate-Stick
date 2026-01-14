using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance;

    [Header("UI Elements")]
    public RectTransform giftPanel;
    public Button getGiftButton;
    public TMP_Text timerText;
    public Image buttonGlow;

    [Header("Professional Reward Visuals")]
    public TMP_Text rewardText;

    [ColorUsage(true, true)]
    public Color rewardColor = Color.yellow;
    private int displayedRewardValue = 0;

    [Header("Star Animation Settings")]
    public GameObject starPrefab;
    public Transform starParent;
    public Transform starStart;
    public Transform starEnd;
    public float moveDuration = 0.8f;
    public Ease moveEase = Ease.InBack;

    [Header("Distribution Settings")]
    public int starAmount = 30;
    public float totalDelay = 0.5f;

    [Header("Cooldown Settings")]
    public float giftCooldownHours = 24f;
    private string lastGiftKey = "FreeGift_Daily_Timer";

    private int starsReachedTarget = 0;
    private bool isReadyStateActive = false; // Animasiyanın sonsuz dövrəyə girməməsi üçün kontrol

    private void Awake()
    {
        Instance = this;
        if (giftPanel != null)
        {
            giftPanel.gameObject.SetActive(false);
            giftPanel.anchoredPosition = new Vector2(-1200f, giftPanel.anchoredPosition.y);
        }

        if (rewardText != null && starEnd != null)
        {
            rewardText.gameObject.SetActive(false);
            rewardText.transform.SetParent(starEnd);
            RectTransform rt = rewardText.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 80f);
            rt.localScale = Vector3.one * 1.5f;
            rewardText.alignment = TextAlignmentOptions.Center;
            rewardText.transform.SetAsLastSibling();
        }
    }

    private void Update() => UpdateTimer();

    private void UpdateTimer()
    {
        if (getGiftButton == null || timerText == null)
            return;

        string lastTimeStr = PlayerPrefs.GetString(lastGiftKey, string.Empty);

        // Əgər heç vaxt götürülməyibsə
        if (string.IsNullOrEmpty(lastTimeStr))
        {
            if (!isReadyStateActive)
                SetReady();
            return;
        }

        if (DateTime.TryParse(lastTimeStr, out DateTime lastTime))
        {
            TimeSpan remaining = TimeSpan.FromHours(giftCooldownHours) - (DateTime.Now - lastTime);

            if (remaining.TotalSeconds <= 0)
            {
                if (!isReadyStateActive)
                    SetReady();
            }
            else
            {
                // Cooldown davam edir
                isReadyStateActive = false;
                getGiftButton.interactable = false;
                timerText.text = string.Format(
                    "{0:D2}:{1:D2}:{2:D2}",
                    remaining.Hours,
                    remaining.Minutes,
                    remaining.Seconds
                );

                if (buttonGlow != null)
                    buttonGlow.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

                getGiftButton.transform.DOKill(); // Cooldown zamanı animasiyanı dayandır
            }
        }
    }

    private void SetReady()
    {
        if (getGiftButton == null)
            return;

        isReadyStateActive = true;
        getGiftButton.interactable = true;
        timerText.text = "CLAIM GIFT!";

        getGiftButton.transform.DOKill();
        getGiftButton.transform.localScale = Vector3.one; // Miqyası sıfırla
        getGiftButton
            .transform.DOPunchScale(Vector3.one * 0.15f, 1f, 5)
            .SetLoops(-1)
            .SetUpdate(true);
    }

    public void CheckGiftStatus()
    {
        if (giftPanel == null)
            return;
        giftPanel.gameObject.SetActive(true);
        giftPanel.DOAnchorPosX(0f, 0.6f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void OnGetButtonClick()
    {
        isReadyStateActive = false; // Statusu sıfırla
        if (getGiftButton != null)
        {
            getGiftButton.interactable = false;
            getGiftButton.transform.DOKill();
            getGiftButton.transform.localScale = Vector3.one;
        }
        UISoundManager.Instance?.PlayStarSFX();

        PlayerPrefs.SetString(lastGiftKey, DateTime.Now.ToString());
        StartStarAnimationWithRandomReward(
            30,
            50,
            getGiftButton != null ? getGiftButton.transform : null
        );
    }

    public void StartStarAnimationWithRandomReward(
        int visualAmount,
        int realReward,
        Transform customStart = null
    )
    {
        starsReachedTarget = 0;
        displayedRewardValue = 0;
        float starPerDelay = totalDelay / visualAmount;
        int rewardPerStar = realReward / visualAmount;
        int extraReward = realReward % visualAmount;

        Transform startPos = (customStart != null) ? customStart : starStart;

        for (int i = 0; i < visualAmount; i++)
        {
            int amt = rewardPerStar + (i == visualAmount - 1 ? extraReward : 0);
            ShowCoin(i * starPerDelay, amt, visualAmount, startPos);
        }
    }

    public void StartStarAnimationOnly(int amount) =>
        StartStarAnimationWithRandomReward(amount, amount);

    private void ShowCoin(float delay, int rewardAmount, int totalStars, Transform actualStart)
    {
        if (starPrefab == null || actualStart == null || starEnd == null)
            return;

        var star = Instantiate(starPrefab, starParent);
        star.transform.position = actualStart.position;
        star.transform.localScale = Vector3.zero;

        Sequence s = DOTween.Sequence().SetUpdate(true);
        s.Append(star.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
        s.Join(
            star.transform.DOMove(
                actualStart.position + (Vector3)UnityEngine.Random.insideUnitCircle * 150f,
                0.4f
            )
        );
        s.AppendInterval(0.1f);
        s.Append(star.transform.DOMove(starEnd.position, moveDuration).SetEase(moveEase));
        s.Join(star.transform.DOScale(Vector3.one * 0.6f, moveDuration));
        s.SetDelay(delay);
        s.OnComplete(() =>
        {
            starsReachedTarget++;
            if (starEnd != null)
            {
                starEnd.DOKill(true);
                starEnd.DOPunchScale(Vector3.one * 0.2f, 0.2f).SetUpdate(true);
            }

            if (StarManager.Instance != null)
                StarManager.Instance.AddStar(rewardAmount);

            UpdateRewardDisplay(rewardAmount);

            if (starsReachedTarget >= totalStars)
                FinalizeRewardDisplay();

            Destroy(star);
        });
    }

    public void StartStarAnimation_NoTimer(
        int visualAmount,
        int realReward,
        Transform customStart = null
    )
    {
        starsReachedTarget = 0;
        displayedRewardValue = 0;

        float starPerDelay = totalDelay / visualAmount;
        int rewardPerStar = realReward / visualAmount;
        int extraReward = realReward % visualAmount;

        Transform startPos = (customStart != null) ? customStart : starStart;

        for (int i = 0; i < visualAmount; i++)
        {
            int amt = rewardPerStar + (i == visualAmount - 1 ? extraReward : 0);
            ShowCoin(i * starPerDelay, amt, visualAmount, startPos);
        }
    }

    private void UpdateRewardDisplay(int inc)
    {
        if (rewardText == null)
            return;
        rewardText.gameObject.SetActive(true);
        displayedRewardValue += inc;
        rewardText.text = "+" + displayedRewardValue;
        rewardText.transform.DOKill(true);
        rewardText.transform.DOPunchScale(Vector3.one * 0.2f, 0.1f).SetUpdate(true);
    }

    private void FinalizeRewardDisplay()
    {
        if (rewardText == null)
            return;
        DOVirtual
            .DelayedCall(
                1f,
                () =>
                {
                    if (rewardText != null)
                        rewardText.gameObject.SetActive(false);
                }
            )
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        if (giftPanel != null)
            giftPanel.DOKill();

        if (getGiftButton != null && getGiftButton.transform != null)
            getGiftButton.transform.DOKill();

        if (rewardText != null && rewardText.transform != null)
            rewardText.transform.DOKill();

        isReadyStateActive = false;
    }
}
