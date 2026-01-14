using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [Header("UI Paneli")]
    public RectTransform missionPanel;
    public TMP_Text missionNameText;
    public TMP_Text progressText;
    public Image progressBarFill;
    public Button claimButton;
    public TMP_Text claimButtonText;

    [Header("Visual Settings")]
    private float offScreenX = 1500f;
    private float onScreenX = 0f;

    [Header("Görev Ayarları")]
    private int currentMissionIndex = 0;
    private int currentProgress = 0;
    private bool isCompleted = false;

    // Görəv siyahısı
    private string[] missionNames = { "SCORE 200 POINTS", "GET 10 PERFECTS", "REACH 3 LEVELS" };
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

    public void OpenPanel()
    {
        missionPanel.gameObject.SetActive(true);
        missionPanel.DOKill();
        missionPanel.DOAnchorPosX(onScreenX, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void ClosePanel()
    {
        missionPanel.DOKill();
        missionPanel
            .DOAnchorPosX(offScreenX, 0.5f)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => missionPanel.gameObject.SetActive(false));
    }

    // --- Görəv Artımı Funksiyaları ---
    public void AddScore(int amt)
    {
        if (currentMissionIndex == 0)
            ProgressLogic(amt);
    }

    public void AddPerfect()
    {
        // Cari missiya 1-ci (Perfect) missiyadırsa artır
        if (currentMissionIndex == 1)
        {
            ProgressLogic(1);
            Debug.Log("Perfect Missiyası Artırıldı: " + currentProgress); // Yoxlamaq üçün
        }
    }

    public void AddLevel()
    {
        if (currentMissionIndex == 2)
            ProgressLogic(1);
    }

    private void ProgressLogic(int amt)
    {
        // Əgər bütün missiyalar bitibsə və ya cari missiya tamamlanıbsa, işləmə
        if (currentMissionIndex >= missionNames.Length || isCompleted)
            return;

        currentProgress += amt;

        // Hədəfə çatdıqda
        if (currentProgress >= goals[currentMissionIndex])
        {
            currentProgress = goals[currentMissionIndex];
            isCompleted = true;
            AnimateClaimButton();
        }

        UpdateUI(true);
        SaveState();
    }

    private void UpdateUI(bool animate)
    {
        claimButton.transform.DOKill(true);
        claimButton.transform.localScale = Vector3.one;
        if (currentMissionIndex >= missionNames.Length)
        {
            missionNameText.text = "ALL MISSIONS DONE!";
            progressText.text = "100%";
            progressBarFill.fillAmount = 1;
            claimButton.interactable = false;
            claimButtonText.text = "COMPLETED";
            return;
        }

        // Cari görəv məlumatlarını yazdır
        missionNameText.text = missionNames[currentMissionIndex];
        progressText.text = currentProgress + "/" + goals[currentMissionIndex];
        float fill = (float)currentProgress / goals[currentMissionIndex];

        if (animate)
            progressBarFill.DOFillAmount(fill, 0.4f).SetUpdate(true);
        else
            progressBarFill.fillAmount = fill;

        claimButton.interactable = isCompleted;
        claimButtonText.text = isCompleted ? "CLAIM REWARD" : "IN PROGRESS";
    }

    public void OnClaimClick()
    {
        if (!isCompleted)
            return;

        // Mükafat animasiyası (isFreeGift = false olaraq gedəcək)
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.StartStarAnimation_NoTimer(
                20,
                rewards[currentMissionIndex],
                claimButton.transform
            );
        }
        UISoundManager.Instance?.PlayStarSFX();

        claimButton.transform.DOKill(true);
        claimButton.transform.localScale = Vector3.one;

        // 🔄 NÖVBƏTİ MISSİON MƏLUMATLARINI YENİLƏ
        isCompleted = false;
        currentProgress = 0;
        currentMissionIndex++;

        SaveState(); // Yeni indeksi dərhal yadda saxla

        // UI-ı dərhal yenilə ki, köhnə yazı qalmasın
        UpdateUI(true);

        missionPanel.DOKill();
        missionPanel.DOShakeAnchorPos(0.3f, 15, 10).SetUpdate(true);
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
        PlayerPrefs.SetInt("M_Idx", currentMissionIndex);
        PlayerPrefs.SetInt("M_Prog", currentProgress);
        PlayerPrefs.SetInt("M_Done", isCompleted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        currentMissionIndex = PlayerPrefs.GetInt("M_Idx", 0);
        currentProgress = PlayerPrefs.GetInt("M_Prog", 0);
        isCompleted = PlayerPrefs.GetInt("M_Done", 0) == 1;
    }

    private void CheckDailyReset()
    {
        string today = DateTime.Now.ToString("yyyyMMdd");
        if (PlayerPrefs.GetString("M_Date", "") != today)
        {
            PlayerPrefs.SetString("M_Date", today);
            currentMissionIndex = 0;
            currentProgress = 0;
            isCompleted = false;
            SaveState();
        }
    }

    [ContextMenu("Reset Missions")] // Inspector-da skriptin üzərinə sağ klikləyib seçə bilərsən
    public void ResetMissions()
    {
        PlayerPrefs.DeleteKey("M_Idx");
        PlayerPrefs.DeleteKey("M_Prog");
        PlayerPrefs.DeleteKey("M_Done");
        PlayerPrefs.DeleteKey("M_Date");

        currentMissionIndex = 0;
        currentProgress = 0;
        isCompleted = false;

        Debug.Log("Missiyalar sıfırlandı!");
        UpdateUI(false);
    }
}
