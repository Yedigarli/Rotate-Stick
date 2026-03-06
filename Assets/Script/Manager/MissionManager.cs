using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [Header("UI Elements")]
    public RectTransform missionPanel;
    public TMP_Text missionNameText;
    public TMP_Text progressText;
    public TMP_Text claimButtonText;
    public TMP_Text timerText;
    public Image progressBarFill;
    public Button claimButton;

    [Header("Cooldown Settings")]
    public float hoursBetweenMissions = 2f;
    private bool isWaiting;

    [Header("Mission Data")]
    private int currentIdx;
    private int currentProg;
    private bool isDone;

    private readonly string[] names = { "SCORE 120", "8 PERFECT", "2 LEVEL UP" };
    private readonly int[] goals = { 120, 8, 2 };
    private readonly int[] rewards = { 20, 18, 25 };

    private DateTime? nextMissionTimeUtc;
    private float nextTimerRefresh;

    private const string NextMissionTicksKey = "NextMissionTicksUtc";

    private void Awake()
    {
        Instance = this;

        ConfigureTextLayout();

        if (missionPanel != null)
        {
            missionPanel.gameObject.SetActive(false);
            missionPanel.anchoredPosition = new Vector2(-1200f, missionPanel.anchoredPosition.y);
        }

        CheckDailyReset();
        LoadState();
        LoadNextMissionTime();
        UpdateUI(false);
        nextTimerRefresh = Time.unscaledTime;
    }

    private void Update()
    {
        if (!isWaiting || currentIdx >= names.Length)
            return;

        if (Time.unscaledTime < nextTimerRefresh)
            return;

        nextTimerRefresh = Time.unscaledTime + 1f;
        UpdateTimerUI();
    }

    private void ConfigureTextLayout()
    {
        if (missionNameText != null)
        {
            missionNameText.enableWordWrapping = true;
            missionNameText.overflowMode = TextOverflowModes.Ellipsis;
            missionNameText.alignment = TextAlignmentOptions.Center;
        }

        if (progressText != null)
        {
            progressText.enableWordWrapping = false;
            progressText.alignment = TextAlignmentOptions.Center;
        }

        if (claimButtonText != null)
        {
            claimButtonText.enableWordWrapping = false;
            claimButtonText.alignment = TextAlignmentOptions.Center;
        }

        if (timerText != null)
        {
            timerText.enableWordWrapping = false;
            timerText.alignment = TextAlignmentOptions.Center;
        }
    }

    private static DateTime UtcNow() => DateTime.UtcNow;

    private void LoadNextMissionTime()
    {
        long ticks = Convert.ToInt64(PlayerPrefs.GetString(NextMissionTicksKey, "0"));
        nextMissionTimeUtc = ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : (DateTime?)null;
        isWaiting = nextMissionTimeUtc.HasValue && UtcNow() < nextMissionTimeUtc.Value;
    }

    private void SaveNextMissionTime(DateTime utcTime)
    {
        PlayerPrefs.SetString(NextMissionTicksKey, utcTime.Ticks.ToString());
    }

    private void ClearNextMissionTime()
    {
        nextMissionTimeUtc = null;
        PlayerPrefs.DeleteKey(NextMissionTicksKey);
    }

    private void UpdateTimerUI()
    {
        if (!nextMissionTimeUtc.HasValue)
            return;

        TimeSpan diff = nextMissionTimeUtc.Value - UtcNow();

        if (diff.TotalSeconds <= 0)
        {
            isWaiting = false;
            ClearNextMissionTime();
            if (timerText != null)
                timerText.gameObject.SetActive(false);
            UpdateUI(true);
            return;
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            int totalHours = Mathf.Max(0, (int)diff.TotalHours);
            timerText.SetText("{0:D2}:{1:D2}:{2:D2}", totalHours, diff.Minutes, diff.Seconds);
        }
    }

    public void OpenPanel()
    {
        if (missionPanel == null)
            return;

        missionPanel.gameObject.SetActive(true);
        missionPanel.DOKill();
        missionPanel.DOAnchorPosX(0f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void ClosePanel()
    {
        if (missionPanel == null)
            return;

        missionPanel.DOKill();
        missionPanel
            .DOAnchorPosX(1500f, 0.4f)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => missionPanel.gameObject.SetActive(false));
    }

    public void AddScore(int amt)
    {
        if (!isWaiting && currentIdx == 0)
            ProgressLogic(amt);
    }

    public void AddPerfect()
    {
        if (!isWaiting && currentIdx == 1)
            ProgressLogic(1);
    }

    public void AddLevel()
    {
        if (!isWaiting && currentIdx == 2)
            ProgressLogic(1);
    }

    private void ProgressLogic(int amt)
    {
        if (currentIdx >= names.Length || isDone || isWaiting)
            return;

        currentProg += amt;
        if (currentProg >= goals[currentIdx])
        {
            currentProg = goals[currentIdx];
            isDone = true;
            AnimateClaimButton();
        }

        UpdateUI(true);
        SaveState();
    }

    private void UpdateUI(bool animate)
    {
        bool allFinished = currentIdx >= names.Length;

        if (allFinished)
        {
            missionNameText.SetText("ALL MISSIONS DONE");
            progressText.SetText("100%");
            progressBarFill.fillAmount = 1f;
            claimButton.interactable = false;
            claimButtonText.SetText("DONE");
            if (timerText != null)
                timerText.gameObject.SetActive(false);
            return;
        }

        if (isWaiting)
        {
            missionNameText.SetText("NEXT MISSION LOCKED");
            progressText.SetText("WAIT");
            progressBarFill.fillAmount = 0f;
            claimButton.interactable = false;
            claimButtonText.SetText("WAIT");
            return;
        }

        missionNameText.SetText(names[currentIdx]);
        progressText.SetText("{0}/{1}", currentProg, goals[currentIdx]);

        float fill = (float)currentProg / goals[currentIdx];
        if (animate)
        {
            progressBarFill.DOKill();
            progressBarFill.DOFillAmount(fill, 0.25f).SetUpdate(true);
        }
        else
        {
            progressBarFill.fillAmount = fill;
        }

        claimButton.interactable = isDone;
        claimButtonText.SetText(isDone ? "CLAIM" : "GO");
        if (timerText != null)
            timerText.gameObject.SetActive(false);
    }

    public void OnClaimClick()
    {
        if (!isDone)
            return;

        TaskManager.Instance?.StartStarAnimation_NoTimer(8, rewards[currentIdx], claimButton.transform);
        UISoundManager.Instance?.PlayStarSFX();

        currentIdx++;
        currentProg = 0;
        isDone = false;
        isWaiting = true;

        DateTime nextUtc = UtcNow().AddHours(hoursBetweenMissions);
        nextMissionTimeUtc = nextUtc;
        SaveNextMissionTime(nextUtc);

        SaveState();
        UpdateUI(true);
    }

    private void AnimateClaimButton()
    {
        claimButton.transform.DOKill();
        claimButton.transform.localScale = Vector3.one;
        claimButton
            .transform.DOPunchScale(Vector3.one * 0.1f, 0.45f, 4)
            .SetLoops(-1)
            .SetUpdate(true);
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt("M_Idx", currentIdx);
        PlayerPrefs.SetInt("M_Prog", currentProg);
        PlayerPrefs.SetInt("M_Done", isDone ? 1 : 0);
    }

    private void LoadState()
    {
        currentIdx = PlayerPrefs.GetInt("M_Idx", 0);
        currentProg = PlayerPrefs.GetInt("M_Prog", 0);
        isDone = PlayerPrefs.GetInt("M_Done", 0) == 1;
    }

    private void CheckDailyReset()
    {
        string today = DateTime.Now.ToString("yyyyMMdd");
        if (PlayerPrefs.GetString("M_Date", string.Empty) != today)
        {
            PlayerPrefs.SetString("M_Date", today);
            currentIdx = 0;
            currentProg = 0;
            isDone = false;
            isWaiting = false;
            ClearNextMissionTime();
            SaveState();
        }
    }
    private void OnDisable()
    {
        if (missionPanel != null)
            missionPanel.DOKill();

        if (progressBarFill != null)
            progressBarFill.DOKill();

        if (claimButton != null)
            claimButton.transform.DOKill();

        missionNameText?.DOKill();
        progressText?.DOKill();
        claimButtonText?.DOKill();
        timerText?.DOKill();
    }
}
