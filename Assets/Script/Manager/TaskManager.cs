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
    public float giftCooldownHours = 2f;

    private int starsReachedTarget;
    private bool isReadyStateActive;
    private DateTime? nextGiftTimeUtc;
    private float nextTimerRefresh;
    private Transform giftButtonTransform;
    private Transform rewardTextTransform;
    private string lastTimerLabel;

    private const string LastGiftTicksKey = "FreeGiftTicksUtc";
    private static readonly Color DisabledGlowColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private void Awake()
    {
        Instance = this;

        if (giftPanel != null)
            giftPanel.gameObject.SetActive(false);

        giftCooldownHours = 2f;
        LoadGiftTime();
        nextTimerRefresh = Time.unscaledTime;
        giftButtonTransform = getGiftButton != null ? getGiftButton.transform : null;
        rewardTextTransform = rewardText != null ? rewardText.transform : null;
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
        string timerLabel = string.Format("{0:D2}:{1:D2}:{2:D2}", totalHours, remaining.Minutes, remaining.Seconds);
        if (lastTimerLabel != timerLabel)
        {
            lastTimerLabel = timerLabel;
            timerText.SetText(lastTimerLabel);
        }

        if (buttonGlow != null)
            buttonGlow.color = DisabledGlowColor;

        giftButtonTransform?.DOKill();
    }

    private void SetReady()
    {
        if (getGiftButton == null)
            return;

        isReadyStateActive = true;
        getGiftButton.interactable = true;
        if (lastTimerLabel != "CLAIM GIFT")
        {
            lastTimerLabel = "CLAIM GIFT";
            timerText.SetText(lastTimerLabel);
        }

        giftButtonTransform?.DOKill();
        if (giftButtonTransform != null)
        {
            giftButtonTransform.localScale = Vector3.one;
            giftButtonTransform
                .DOScale(1.06f, 0.55f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(getGiftButton.gameObject, LinkBehaviour.KillOnDestroy);
        }
    }

    public void CheckGiftStatus()
    {
        if (giftPanel == null)
            return;

        giftPanel.gameObject.SetActive(true);
        giftPanel.DOKill();
        giftPanel
            .DOAnchorPosX(0f, 0.6f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .SetLink(giftPanel.gameObject, LinkBehaviour.KillOnDestroy);
    }

    public void OnGetButtonClick()
    {
        isReadyStateActive = false;

        if (getGiftButton != null)
        {
            getGiftButton.interactable = false;
            giftButtonTransform?.DOKill();
            if (giftButtonTransform != null)
                giftButtonTransform.localScale = Vector3.one;
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
        s.SetLink(star, LinkBehaviour.KillOnDestroy);
        s.OnComplete(() =>
        {
            starsReachedTarget++;

            if (starEnd != null)
            {
                starEnd.DOKill(true);
                starEnd
                    .DOPunchScale(Vector3.one * 0.16f, 0.18f)
                    .SetUpdate(true)
                    .SetLink(starEnd.gameObject, LinkBehaviour.KillOnDestroy);
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
        rewardText.transform
            .DOPunchScale(Vector3.one * 0.18f, 0.1f)
            .SetUpdate(true)
            .SetLink(rewardText.gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void FinalizeRewardDisplay()
    {
        if (rewardText == null)
            return;

        DOVirtual
            .DelayedCall(
                0.9f,
                () =>
                {
                    if (rewardText != null)
                        rewardText.gameObject.SetActive(false);
                }
            )
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    public bool IsGiftReadyForClaim()
    {
        if (!nextGiftTimeUtc.HasValue)
            return true;

        return UtcNow() >= nextGiftTimeUtc.Value;
    }

    public float GetGiftCooldownFill01()
    {
        if (!nextGiftTimeUtc.HasValue)
            return 1f;

        DateTime startUtc = nextGiftTimeUtc.Value.AddHours(-giftCooldownHours);
        double totalSeconds = Math.Max(1d, giftCooldownHours * 3600d);
        double elapsedSeconds = (UtcNow() - startUtc).TotalSeconds;
        return Mathf.Clamp01((float)(elapsedSeconds / totalSeconds));
    }

    public string GetGiftRemainingText()
    {
        if (!nextGiftTimeUtc.HasValue)
            return "READY";

        TimeSpan remaining = nextGiftTimeUtc.Value - UtcNow();
        if (remaining.TotalSeconds <= 0)
            return "READY";

        int totalHours = Mathf.Max(0, (int)remaining.TotalHours);
        return string.Format("{0:D2}:{1:D2}:{2:D2}", totalHours, remaining.Minutes, remaining.Seconds);
    }

    private void OnDestroy()
    {
        giftButtonTransform?.DOKill();
        rewardTextTransform?.DOKill();
        starEnd?.DOKill();
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
