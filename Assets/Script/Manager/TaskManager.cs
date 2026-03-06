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
    private int displayedRewardValue;

    [Header("Star Animation Settings")]
    public GameObject starPrefab;
    public Transform starParent;
    public Transform starStart;
    public Transform starEnd;
    public float moveDuration = 0.8f;
    public Ease moveEase = Ease.InBack;

    [Header("Distribution Settings")]
    public int starAmount = 10;
    public float totalDelay = 0.4f;

    [Header("Cooldown Settings")]
    public float giftCooldownHours = 24f;

    private int starsReachedTarget;
    private bool isReadyStateActive;
    private DateTime? nextGiftTimeUtc;
    private float nextTimerRefresh;
    private Transform giftButtonTransform;
    private Transform rewardTextTransform;

    private const string LastGiftTicksKey = "FreeGiftTicksUtc";
    private static readonly Color DisabledGlowColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private void Awake()
    {
        Instance = this;

        ConfigureTextLayout();

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
            rt.anchoredPosition = new Vector2(0f, 80f);
            rt.localScale = Vector3.one * 1.5f;
            rewardText.transform.SetAsLastSibling();
        }

        LoadGiftTime();
        nextTimerRefresh = Time.unscaledTime;
        giftButtonTransform = getGiftButton != null ? getGiftButton.transform : null;
        rewardTextTransform = rewardText != null ? rewardText.transform : null;
    }

    private void ConfigureTextLayout()
    {
        if (timerText != null)
        {
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.enableWordWrapping = false;
        }

        if (rewardText != null)
        {
            rewardText.alignment = TextAlignmentOptions.Center;
            rewardText.enableWordWrapping = false;
            rewardText.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private static DateTime UtcNow() => DateTime.UtcNow;

    private void LoadGiftTime()
    {
        long ticks = Convert.ToInt64(PlayerPrefs.GetString(LastGiftTicksKey, "0"));
        if (ticks <= 0)
        {
            nextGiftTimeUtc = null;
            return;
        }

        DateTime lastGiftUtc = new DateTime(ticks, DateTimeKind.Utc);
        nextGiftTimeUtc = lastGiftUtc.AddHours(giftCooldownHours);
    }

    private void SaveGiftTime(DateTime utc)
    {
        PlayerPrefs.SetString(LastGiftTicksKey, utc.Ticks.ToString());
    }

    private void Update()
    {
        if (Time.unscaledTime < nextTimerRefresh)
            return;

        nextTimerRefresh = Time.unscaledTime + 1f;
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        if (getGiftButton == null || timerText == null)
            return;

        if (!nextGiftTimeUtc.HasValue)
        {
            if (!isReadyStateActive)
                SetReady();
            return;
        }

        TimeSpan remaining = nextGiftTimeUtc.Value - UtcNow();
        if (remaining.TotalSeconds <= 0)
        {
            nextGiftTimeUtc = null;
            if (!isReadyStateActive)
                SetReady();
            return;
        }

        isReadyStateActive = false;
        getGiftButton.interactable = false;

        int totalHours = Mathf.Max(0, (int)remaining.TotalHours);
        timerText.SetText("{0:D2}:{1:D2}:{2:D2}", totalHours, remaining.Minutes, remaining.Seconds);

        if (buttonGlow != null)
            buttonGlow.color = DisabledGlowColor;

        getGiftButton.transform.DOKill();
    }

    private void SetReady()
    {
        if (getGiftButton == null)
            return;

        isReadyStateActive = true;
        getGiftButton.interactable = true;
        timerText.SetText("CLAIM GIFT");

        getGiftButton.transform.DOKill();
        getGiftButton.transform.localScale = Vector3.one;
        getGiftButton
            .transform.DOPunchScale(Vector3.one * 0.12f, 0.8f, 4)
            .SetLoops(-1)
            .SetUpdate(true);
    }

    public void CheckGiftStatus()
    {
        if (giftPanel == null)
            return;

        giftPanel.gameObject.SetActive(true);
        giftPanel.DOKill();
        giftPanel.DOAnchorPosX(0f, 0.6f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void OnGetButtonClick()
    {
        isReadyStateActive = false;

        if (getGiftButton != null)
        {
            getGiftButton.interactable = false;
            getGiftButton.transform.DOKill();
            getGiftButton.transform.localScale = Vector3.one;
        }

        UISoundManager.Instance?.PlayStarSFX();

        DateTime nowUtc = UtcNow();
        SaveGiftTime(nowUtc);
        nextGiftTimeUtc = nowUtc.AddHours(giftCooldownHours);

        StartStarAnimationWithRandomReward(10, 20, getGiftButton != null ? getGiftButton.transform : null);
    }

    public void StartStarAnimationWithRandomReward(int visualAmount, int realReward, Transform customStart = null)
    {
        if (visualAmount <= 0)
            return;

        starsReachedTarget = 0;
        displayedRewardValue = 0;

        float starPerDelay = totalDelay / visualAmount;
        int rewardPerStar = realReward / visualAmount;
        int extraReward = realReward % visualAmount;

        Transform startPos = customStart != null ? customStart : starStart;

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

        GameObject star = Instantiate(starPrefab, starParent);
        star.transform.position = actualStart.position;
        star.transform.localScale = Vector3.zero;

        Sequence s = DOTween.Sequence().SetUpdate(true);
        s.Append(star.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack));
        s.Join(star.transform.DOMove(actualStart.position + (Vector3)UnityEngine.Random.insideUnitCircle * 120f, 0.3f));
        s.AppendInterval(0.08f);
        s.Append(star.transform.DOMove(starEnd.position, moveDuration).SetEase(moveEase));
        s.Join(star.transform.DOScale(Vector3.one * 0.6f, moveDuration));
        s.SetDelay(delay);
        s.OnComplete(() =>
        {
            starsReachedTarget++;

            if (starEnd != null)
            {
                starEnd.DOKill(true);
                starEnd.DOPunchScale(Vector3.one * 0.16f, 0.18f).SetUpdate(true);
            }

            StarManager.Instance?.AddStar(rewardAmount);
            UpdateRewardDisplay(rewardAmount);

            if (starsReachedTarget >= totalStars)
                FinalizeRewardDisplay();

            Destroy(star);
        });
    }

    public void StartStarAnimation_NoTimer(int visualAmount, int realReward, Transform customStart = null)
    {
        if (visualAmount <= 0)
            return;

        starsReachedTarget = 0;
        displayedRewardValue = 0;

        float starPerDelay = totalDelay / visualAmount;
        int rewardPerStar = realReward / visualAmount;
        int extraReward = realReward % visualAmount;

        Transform startPos = customStart != null ? customStart : starStart;

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
        rewardText.SetText("+{0}", displayedRewardValue);
        rewardText.transform.DOKill(true);
        rewardText.transform.DOPunchScale(Vector3.one * 0.18f, 0.1f).SetUpdate(true);
    }

    private void FinalizeRewardDisplay()
    {
        if (rewardText == null)
            return;

        DOVirtual.DelayedCall(0.9f, () =>
        {
            if (rewardText != null)
                rewardText.gameObject.SetActive(false);
        }).SetUpdate(true);
    }

    private void OnDisable()
    {
        if (giftPanel != null)
            giftPanel.DOKill();

        if (giftButtonTransform != null)
            giftButtonTransform.DOKill();

        if (rewardTextTransform != null)
            rewardTextTransform.DOKill();

        isReadyStateActive = false;
    }
}



