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
    public TMP_Text missionNameText,
        progressText,
        claimButtonText,
        timerText;
    public Image progressBarFill;
    public Button claimButton;

    [Header("Cooldown Settings")]
    public float hoursBetweenMissions = 2f;
    private bool isWaiting = false;

    [Header("Mission Data")]
    private int currentIdx = 0;
    private int currentProg = 0;
    private bool isDone = false;

    private string[] names = { "SCORE 200", "10 PERFECTS", "3 LEVELS" };
    private int[] goals = { 200, 10, 3 };
    private int[] rewards = { 100, 80, 150 };

    private void Awake()
    {
        Instance = this;
        missionPanel.gameObject.SetActive(false);
        missionPanel.anchoredPosition = new Vector2(-1200f, missionPanel.anchoredPosition.y);

        CheckDailyReset();
        LoadState();
        UpdateUI(false);
    }

    private void Update()
    {
        if (isWaiting && currentIdx < names.Length)
            UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        string nextStr = PlayerPrefs.GetString("NextMissionTime", "");
        if (string.IsNullOrEmpty(nextStr))
            return;

        TimeSpan diff = DateTime.Parse(nextStr) - DateTime.Now;

        if (diff.TotalSeconds <= 0)
        {
            isWaiting = false;
            if (timerText != null)
                timerText.gameObject.SetActive(false);
            UpdateUI(true);
        }
        else if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = string.Format(
                "{0:D2}:{1:D2}:{2:D2}",
                diff.Hours,
                diff.Minutes,
                diff.Seconds
            );
        }
    }

    public void OpenPanel()
    {
        missionPanel.gameObject.SetActive(true);
        missionPanel.DOAnchorPosX(0f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void ClosePanel()
    {
        missionPanel
            .DOAnchorPosX(1500f, 0.4f)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => missionPanel.gameObject.SetActive(false));
    }

    public void AddScore(int amt)
    {
        if (!isWaiting)
            ProgressLogic(amt);
    }

    public void AddPerfect()
    {
        if (currentIdx == 1 && !isWaiting)
            ProgressLogic(1);
    }

    public void AddLevel()
    {
        if (currentIdx == 2 && !isWaiting)
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
            missionNameText.text = "FINISHED!";
            progressText.text = "100%";
            progressBarFill.fillAmount = 1;
            claimButton.interactable = false;
            claimButtonText.text = "DONE";
            if (timerText != null)
                timerText.gameObject.SetActive(false);
            return;
        }

        if (isWaiting)
        {
            missionNameText.text = "????";
            progressText.text = "LOCKED";
            progressBarFill.fillAmount = 0;
            claimButton.interactable = false;
            claimButtonText.text = "WAIT";
        }
        else
        {
            missionNameText.text = names[currentIdx];
            progressText.text = currentProg + "/" + goals[currentIdx];
            float fill = (float)currentProg / goals[currentIdx];

            if (animate)
                progressBarFill.DOFillAmount(fill, 0.3f).SetUpdate(true);
            else
                progressBarFill.fillAmount = fill;

            claimButton.interactable = isDone;
            claimButtonText.text = isDone ? "CLAIM" : "GO!";
            if (timerText != null)
                timerText.gameObject.SetActive(false);
        }
    }

    public void OnClaimClick()
    {
        if (!isDone)
            return;

        TaskManager.Instance?.StartStarAnimation_NoTimer(
            20,
            rewards[currentIdx],
            claimButton.transform
        );
        UISoundManager.Instance?.PlayStarSFX();

        currentIdx++;
        currentProg = 0;
        isDone = false;
        isWaiting = true;

        PlayerPrefs.SetString(
            "NextMissionTime",
            DateTime.Now.AddHours(hoursBetweenMissions).ToString()
        );
        SaveState();
        UpdateUI(true);
    }

    private void AnimateClaimButton()
    {
        claimButton.transform.DOKill();
        claimButton
            .transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 5)
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

        string nextStr = PlayerPrefs.GetString("NextMissionTime", "");
        if (!string.IsNullOrEmpty(nextStr))
            isWaiting = DateTime.Now < DateTime.Parse(nextStr);
    }

    private void CheckDailyReset()
    {
        string today = DateTime.Now.ToString("yyyyMMdd");
        if (PlayerPrefs.GetString("M_Date", "") != today)
        {
            PlayerPrefs.SetString("M_Date", today);
            PlayerPrefs.DeleteKey("NextMissionTime");
            currentIdx = 0;
            currentProg = 0;
            isDone = false;
            isWaiting = false;
            SaveState();
        }
    }
}
