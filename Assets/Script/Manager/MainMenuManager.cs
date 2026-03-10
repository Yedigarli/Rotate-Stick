using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    [Header("UI & Appearance")]
    public TMP_Text levelText;
    public TMP_Text nextLevelText;
    public TMP_Text percentageText;
    public TMP_Text bestScoreText;

    [Header("Level Bar")]
    public GameObject levelBarContainer;
    public Image levelCircle;
    public Image nextLevelCircle;
    public Image progressBarFill;
    public Color[] levelColors;

    [Header("References")]
    public GameObject player;
    public GameObject target;
    public GameObject ball;
    public Button PLayButton;
    public TMP_Text PlayButtonText;

    [Header("Movement")]
    public float currentSpeed = 50f;
    public float radius = 1.2f;
    private Vector3 speedDirection = Vector3.forward;


    [Header("Background")]
    public Camera mainCamera;
    public Color levelsBG = new Color32(21, 11, 45, 255);

    [Header("Ball & Skins")]
    public bool isSkinsOpen;
    public List<GameObject> activeBalls = new List<GameObject>();
    public Color ballGlowColor = Color.white;
    public Color playerGlowColor = Color.cyan;

    [Header("Menu Action Fills")]
    public Button freeGiftButton;
    public Image skinsButtonFill;
    public Image challengesButtonFill;
    public Image freeGiftButtonFill;
    public TMP_Text freeGiftStatusText;
    public Color activeFillColor = new Color32(255, 153, 51, 255);
    public Color passiveFillColor = new Color32(255, 153, 51, 90);

    private static readonly string PlayStr = "PLAY";
    private static readonly string BestScoreKey = "BestScore";

    private float nextActionFillRefresh;
    private float prevSkinFill = -1f;
    private float prevChallengeFill = -1f;
    private float prevGiftFill = -1f;
    private bool prevGiftReady;

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;

        if (PLayButton != null)
            PLayButton.onClick.AddListener(OnPlayButtonClick);
        if (freeGiftButton != null)
            freeGiftButton.onClick.AddListener(OnFreeGiftClick);
    }

    private void Start()
    {
        InitializeUI();
        UpdateLevelUI();
        ApplyTargetGlow();
        Invoke(nameof(SpawnBall), 0.1f);
        UpdateMenuActionIndicators(true);
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextActionFillRefresh)
        {
            nextActionFillRefresh = Time.unscaledTime + 1f;
            UpdateMenuActionIndicators(false);
        }

        if (isSkinsOpen || target == null)
            return;

        transform.RotateAround(target.transform.position, speedDirection, currentSpeed * Time.deltaTime);
    }

    private void InitializeUI()
    {
        if (mainCamera != null)
            mainCamera.backgroundColor = levelsBG;

        if (PLayButton != null)
            PLayButton.interactable = true;
        if (PlayButtonText != null)
            PlayButtonText.SetText(PlayStr);

        AnimateScore(bestScoreText, PlayerPrefs.GetInt(BestScoreKey, 0));
    }

    public void ToggleGameMode() { }

    public void UpdateLevelUI()
    {
        if (levelColors == null || levelColors.Length == 0)
            return;

        int level = PlayerPrefs.GetInt("level", 1);
        int currentPoints = PlayerPrefs.GetInt("currentPoints", 0);
        int lastRunPercent = PlayerPrefs.GetInt("LastRunPercent", -1);
        int pointsToNextLevel = 10 + (level * 2);

        int currentIndex = (level - 1) % levelColors.Length;
        int nextIndex = level % levelColors.Length;

        levelText?.SetText("{0}", level);
        nextLevelText?.SetText("{0}", level + 1);

        if (levelCircle != null)
            levelCircle.color = levelColors[currentIndex];
        if (nextLevelCircle != null)
            nextLevelCircle.color = levelColors[nextIndex];

        float progressRatio = Mathf.Clamp01((float)currentPoints / pointsToNextLevel);
        float percentage = progressRatio * 100f;
        if (lastRunPercent >= 0)
        {
            percentage = Mathf.Clamp(lastRunPercent, 0, 100);
            progressRatio = percentage / 100f;
        }

        if (progressBarFill != null)
        {
            progressBarFill.DOKill();
            progressBarFill.color = levelColors[currentIndex];
            progressBarFill.fillAmount = progressRatio;
        }

        if (percentageText != null)
            percentageText.SetText("{0:0}%", percentage);

        ballGlowColor = levelColors[currentIndex];

        for (int i = 0; i < activeBalls.Count; i++)
        {
            GameObject b = activeBalls[i];
            if (b != null && b.TryGetComponent<SpriteRenderer>(out var sr))
                sr.color = ballGlowColor;
        }
    }

    public void SpawnBall()
    {
        if (isSkinsOpen || target == null || ball == null)
            return;

        activeBalls.RemoveAll(item => item == null);

        Vector2 pos2D = Random.insideUnitCircle.normalized * radius;
        Vector3 spawnPos = target.transform.position + (Vector3)pos2D;

        GameObject spawnedBall = Instantiate(ball, spawnPos, Quaternion.identity);
        spawnedBall.tag = "Ball";
        activeBalls.Add(spawnedBall);

        if (spawnedBall.TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
        {
            int level = PlayerPrefs.GetInt("level", 1);
            ballGlowColor = levelColors[(level - 1) % levelColors.Length];
            sr.color = ballGlowColor;
        }
    }

    private void UpdateMenuActionIndicators(bool immediate)
    {
        float skinFill = SkinsManager.Instance != null ? SkinsManager.Instance.GetUnlockedSkinsFill01() : 0f;
        float challengeFill = MissionManager.Instance != null ? MissionManager.Instance.GetMissionFill01() : 0f;

        bool giftReady = TaskManager.Instance != null && TaskManager.Instance.IsGiftReadyForClaim();
        float giftFill = TaskManager.Instance != null ? TaskManager.Instance.GetGiftCooldownFill01() : 0f;

        if (immediate || Mathf.Abs(prevSkinFill - skinFill) > 0.001f)
            UpdateFillImage(skinsButtonFill, skinFill, false);

        if (immediate || Mathf.Abs(prevChallengeFill - challengeFill) > 0.001f)
            UpdateFillImage(challengesButtonFill, challengeFill, false);

        if (immediate || Mathf.Abs(prevGiftFill - giftFill) > 0.001f || prevGiftReady != giftReady)
            UpdateFillImage(freeGiftButtonFill, giftFill, giftReady);

        if (freeGiftStatusText != null)
        {
            freeGiftStatusText.SetText(
                giftReady
                    ? "FREE GIFT"
                    : (TaskManager.Instance != null ? TaskManager.Instance.GetGiftRemainingText() : "00:00:00")
            );
        }

        if (freeGiftButton != null)
            freeGiftButton.interactable = giftReady;

        prevSkinFill = skinFill;
        prevChallengeFill = challengeFill;
        prevGiftFill = giftFill;
        prevGiftReady = giftReady;
    }

    private void UpdateFillImage(Image img, float value, bool isActive)
    {
        if (img == null)
            return;

        float fill = Mathf.Clamp01(value);
        if (isActive)
            fill = 1f;

        img.color = isActive ? activeFillColor : passiveFillColor;
        img.fillAmount = fill;
    }

    private void OnFreeGiftClick()
    {
        if (TaskManager.Instance == null)
            return;

        if (!TaskManager.Instance.IsGiftReadyForClaim())
            return;

        TaskManager.Instance.OnGetButtonClick();
        UpdateMenuActionIndicators(true);
    }

    private void ApplyTargetGlow()
    {
        if (player != null && player.TryGetComponent<SpriteRenderer>(out SpriteRenderer playerSr))
            playerSr.color = playerGlowColor;
    }

    private static void AnimateScore(TMP_Text textElement, int targetValue)
    {
        if (textElement != null)
            textElement.SetText("{0}", targetValue);
    }

    private void OnDisable()
    {
        progressBarFill?.DOKill();
        percentageText?.DOKill();
        skinsButtonFill?.DOKill();
        challengesButtonFill?.DOKill();
        freeGiftButtonFill?.DOKill();
    }

    private void OnPlayButtonClick()
    {
        if (PLayButton != null)
            PLayButton.interactable = false;

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadLevel("Game");
        else
            SceneManager.LoadScene("Game");
    }
}
