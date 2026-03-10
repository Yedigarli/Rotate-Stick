using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("UI Elements")]
    public Image progressBarFill;
    public TMP_Text currentLevelText;
    public TMP_Text nextLevelText;
    public TMP_Text scoreText;
    public TMP_Text statusText;

    [Header("Status Colors (HDR)")]
    [ColorUsage(true, true)]
    public Color perfectColor = new Color(1.5f, 1.2f, 0f);

    [ColorUsage(true, true)]
    public Color niceColor = new Color(0f, 1.2f, 1.5f);

    [ColorUsage(true, true)]
    public Color levelUpColor = new Color(0.2f, 2f, 0.2f);

    [Header("Settings")]
    public int pointsToNextLevel;
    private int currentPoints = 0;
    private int totalScore = 0;
    private int level;

    [Header("Level Up Effects")]
    public GameObject levelUpParticlePrefab;

    [Header("Randomized Words")]
    private readonly string[] perfectWords = { "PERFECT!", "AMAZING!", "FANTASTIC!", "BULLSEYE!", "EXCELLENT!" };
    private readonly string[] niceWords = { "NICE!", "GOOD!", "COOL!", "NOT BAD!", "GREAT!" };
    private readonly string[] insaneWords = { "INSANE!", "INCREDIBLE!", "UNSTOPPABLE!", "MONSTER!", "AWESOME!" };

    [Header("Level Dynamic Colors")]
    public Image currentLevelCircle;
    public Image nextLevelCircle;
    public Color[] levelColors;

    private Color currentLevelThemeColor;
    private bool isBestScoreReachedThisSession = false;

    [Header("Reward Settings")]
    public int levelUpStarReward = 8;
    public int bestScoreStarReward = 4;

    private TaskManager taskManager;
    private Camera cachedCamera;
    private Tween statusHideTween;

    private static readonly string LevelKey = "level";
    private static readonly string PointsKey = "currentPoints";
    private static readonly string BestScoreKey = "BestScore";

    private void Awake()
    {
        Instance = this;

        level = PlayerPrefs.GetInt(LevelKey, 1);
        currentPoints = 0;
        PlayerPrefs.SetInt(PointsKey, 0);

        pointsToNextLevel = 10 + (level * 2);

        if (statusText != null)
            statusText.gameObject.SetActive(false);

        if (scoreText != null)
            scoreText.SetText("{0}", totalScore);

        UpdateLevelTexts();

        if (progressBarFill != null)
            progressBarFill.fillAmount = 0f;

        cachedCamera = Camera.main;
    }

    private void Start()
    {
        taskManager = TaskManager.Instance;
        UpdateThemeColor();
    }

    public void UpdateThemeColor()
    {
        if (levelColors == null || levelColors.Length == 0)
            return;

        if (cachedCamera == null)
            cachedCamera = Camera.main;

        int currentColorIndex = (level - 1) % levelColors.Length;
        currentLevelThemeColor = levelColors[currentColorIndex];

        int nextColorIndex = level % levelColors.Length;
        Color nextLevelThemeColor = levelColors[nextColorIndex];

        if (currentLevelCircle != null)
            currentLevelCircle.color = currentLevelThemeColor;
        if (nextLevelCircle != null)
            nextLevelCircle.color = nextLevelThemeColor;
        if (progressBarFill != null)
            progressBarFill.color = currentLevelThemeColor;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.baseBallColor = currentLevelThemeColor;
            GameManager.Instance.ballGlowColor = currentLevelThemeColor;
        }
    }

    public void AddProgress(int amount)
    {
        currentPoints += amount;
        totalScore += amount;

        scoreText?.SetText("{0}", totalScore);

        if (progressBarFill != null)
        {
            float fillRatio = (float)currentPoints / pointsToNextLevel;
            progressBarFill.DOKill();
            progressBarFill
                .DOFillAmount(fillRatio, 0.35f)
                .SetEase(Ease.OutCubic)
                .SetLink(progressBarFill.gameObject, LinkBehaviour.KillOnDestroy);
        }

        CheckBestScore();

        if (currentPoints >= pointsToNextLevel)
            LevelUp();
    }

    private void CheckBestScore()
    {
        int currentBest = PlayerPrefs.GetInt(BestScoreKey, 0);
        if (totalScore > currentBest)
        {
            if (!isBestScoreReachedThisSession && currentBest > 0)
            {
                isBestScoreReachedThisSession = true;
                taskManager?.StartStarAnimation_NoTimer(bestScoreStarReward, bestScoreStarReward);
                SpawnLevelUpEffect();
                ShowStatus("NEW BEST!", perfectColor);
            }

            PlayerPrefs.SetInt(BestScoreKey, totalScore);
        }
    }

    private void LevelUp()
    {
        level++;
        currentPoints = 0;
        pointsToNextLevel = Mathf.Min(10 + (level * 2), 60);

        PlayerPrefs.SetInt(LevelKey, level);
        PlayerPrefs.SetInt(PointsKey, 0);
        PlayerPrefs.Save();

        MissionManager.Instance?.AddLevel();

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.FirstSpeed < 300f)
                GameManager.Instance.FirstSpeed += 1.5f;

            GameManager.Instance.currentSpeed = GameManager.Instance.FirstSpeed;
            PlayerPrefs.SetFloat("firstspeed", GameManager.Instance.FirstSpeed);
        }

        UISoundManager.Instance?.PlayLevelUpSFX();

        UpdateLevelTexts();
        UpdateThemeColor();
        SpawnLevelUpEffect();
        ShowStatus("LEVEL UP!", levelUpColor);

        if (progressBarFill != null)
            progressBarFill
                .DOFillAmount(0f, 0.25f)
                .SetDelay(0.35f)
                .SetEase(Ease.InOutSine)
                .SetLink(progressBarFill.gameObject, LinkBehaviour.KillOnDestroy);

        if (taskManager != null)
            taskManager.StartStarAnimation_NoTimer(levelUpStarReward, levelUpStarReward);
    }

    public void ShowStatusByType(string type, int combo = 0)
    {
        string selectedWord;
        Color selectedColor;

        if (type == "Perfect")
        {
            bool isInsane = combo >= 5;
            selectedWord = isInsane
                ? insaneWords[Random.Range(0, insaneWords.Length)]
                : perfectWords[Random.Range(0, perfectWords.Length)];
            selectedColor = isInsane ? new Color(2f, 0.5f, 2f) : perfectColor;
        }
        else
        {
            selectedWord = niceWords[Random.Range(0, niceWords.Length)];
            selectedColor = niceColor;
        }

        ShowStatus(selectedWord, selectedColor);
    }

    public void ShowStatus(string message, Color col)
    {
        if (statusText == null)
            return;

        statusText.SetText(message);
        statusText.color = col;
        statusText.gameObject.SetActive(true);

        RectTransform rect = statusText.rectTransform;
        rect.DOKill();
        rect.localScale = Vector3.zero;

        rect.DOScale(Vector3.one, 0.22f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);

        statusHideTween?.Kill();
        statusHideTween = DOVirtual.DelayedCall(0.87f, HideStatusText)
            .SetUpdate(true)
            .SetLink(statusText.gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void HideStatusText()
    {
        if (statusText != null)
            statusText.gameObject.SetActive(false);
    }

    private void UpdateLevelTexts()
    {
        if (currentLevelText != null)
        {
            currentLevelText.SetText("{0}", level);
            AnimateTextPulse(currentLevelText.rectTransform);
        }

        nextLevelText?.SetText("{0}", level + 1);
    }

    private void AnimateTextPulse(RectTransform rt)
    {
        rt.DOKill();
        rt.localScale = Vector3.one;
        rt.DOPunchScale(Vector3.one * 0.22f, 0.35f, 2, 0.5f)
            .SetUpdate(true)
            .SetLink(rt.gameObject, LinkBehaviour.KillOnDestroy);
    }

    public string GetCurrentLevelPercentage()
    {
        if (pointsToNextLevel <= 0)
            return "0";

        float percentage = ((float)currentPoints / pointsToNextLevel) * 100f;
        return Mathf.Clamp(Mathf.RoundToInt(percentage), 0, 100).ToString();
    }

    public void SpawnLevelUpEffect()
    {
        if (levelUpParticlePrefab == null)
            return;

        if (cachedCamera == null)
            cachedCamera = Camera.main;
        if (cachedCamera == null)
            return;

        Vector3 leftPos = cachedCamera.ViewportToWorldPoint(new Vector3(0.15f, -0.1f, 10f));
        Vector3 rightPos = cachedCamera.ViewportToWorldPoint(new Vector3(0.85f, -0.1f, 10f));

        GameObject l = Instantiate(levelUpParticlePrefab, leftPos, Quaternion.Euler(-60f, 30f, 0f));
        GameObject r = Instantiate(levelUpParticlePrefab, rightPos, Quaternion.Euler(-60f, -30f, 0f));

        Destroy(l, 4f);
        Destroy(r, 4f);
    }

    public float GetLevelProgressValue()
    {
        if (pointsToNextLevel <= 0)
            return 0f;

        float progress = ((float)currentPoints / pointsToNextLevel) * 100f;
        return Mathf.Clamp(progress, 0f, 100f);
    }
}

