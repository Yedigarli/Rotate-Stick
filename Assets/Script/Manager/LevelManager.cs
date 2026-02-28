using System.Collections;
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
    private readonly string[] perfectWords = { "PERFECT!", "AMAZING!", "FANTASTIC!", "BULLSEYE!" };
    private readonly string[] niceWords = { "NICE!", "GOOD!", "COOL!", "NOT BAD!" };
    private readonly string[] insaneWords = { "INSANE!", "GODLIKE!", "UNSTOPPABLE!", "MONSTER!" };

    [Header("Level Dynamic Colors")]
    public Image currentLevelCircle;
    public Image nextLevelCircle;
    public Color[] levelColors;

    private Color currentLevelThemeColor;
    private bool isBestScoreReachedThisSession = false;

    [Header("Reward Settings")]
    public int levelUpStarReward = 20;
    public int bestScoreStarReward = 10;

    private TaskManager taskManager;

    // private Coroutine statusRoutine; // Status animasiyalarini idare etmek ucun

    // String Caching
    private static readonly string LevelKey = "level";
    private static readonly string PointsKey = "currentPoints";
    private static readonly string BestScoreKey = "BestScore";
    private static readonly string BallTag = "Ball";

    private void Awake()
    {
        Instance = this;

        level = PlayerPrefs.GetInt(LevelKey, 1);
        pointsToNextLevel = 10 + (level * 2);

        if (statusText != null)
            statusText.gameObject.SetActive(false);
        if (scoreText != null)
            scoreText.SetText("0");

        UpdateLevelTexts();
        if (progressBarFill != null)
            progressBarFill.fillAmount = 0;
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

        int currentColorIndex = (level - 1) % levelColors.Length;
        currentLevelThemeColor = levelColors[currentColorIndex];

        int nextColorIndex = level % levelColors.Length;
        Color nextLevelThemeColor = levelColors[nextColorIndex];

        // UI Rənglənməsi
        if (currentLevelCircle != null)
            currentLevelCircle.color = currentLevelThemeColor;
        if (nextLevelCircle != null)
            nextLevelCircle.color = nextLevelThemeColor;
        if (progressBarFill != null)
            progressBarFill.color = currentLevelThemeColor;

        // GameManager ilə inteqrasiya
        if (GameManager.Instance != null)
        {
            GameManager.Instance.baseBallColor = currentLevelThemeColor;
            GameManager.Instance.ballGlowColor = currentLevelThemeColor;

            Camera.main.DOKill();
            Camera.main.DOColor(currentLevelThemeColor * 0.15f, 1.5f).SetEase(Ease.Linear);

            // Səhnədəki mövcud topu tapıb rəngləyirik (TryGetComponent daha sürətlidir)
            GameObject ball = GameObject.FindGameObjectWithTag(BallTag);
            if (ball != null && ball.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.color = currentLevelThemeColor;
            }
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
            progressBarFill.DOFillAmount(fillRatio, 0.4f).SetEase(Ease.OutCubic);
        }

        // PlayerPrefs.SetInt(PointsKey, currentPoints); // <--- BU SƏTRİ SİL!
        // Yalnız LevelUp və ya Oyun Bitəndə yadda saxla.

        if (currentPoints >= pointsToNextLevel)
            LevelUp();
    }

    void CheckBestScore()
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

    void LevelUp()
    {
        level++;
        currentPoints = 0;
        pointsToNextLevel = Mathf.Min(10 + (level * 2), 60);

        PlayerPrefs.SetInt(LevelKey, level);
        PlayerPrefs.SetInt(PointsKey, 0);
        PlayerPrefs.Save();

        MissionManager.Instance?.AddLevel();

        // Speed Progression (Daha təmiz yoxlama)
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.FirstSpeed < 350f)
                GameManager.Instance.FirstSpeed += 2.5f;

            GameManager.Instance.currentSpeed = GameManager.Instance.FirstSpeed;
            PlayerPrefs.SetFloat("firstspeed", GameManager.Instance.FirstSpeed);
        }

        UISoundManager.Instance?.PlayLevelUpSFX();

        UpdateLevelTexts();
        UpdateThemeColor();
        SpawnLevelUpEffect();
        ShowStatus("LEVEL UP!", levelUpColor);
        LevelUpSlowMo();

        progressBarFill?.DOFillAmount(0f, 0.3f).SetDelay(0.5f).SetEase(Ease.InOutSine);

        if (taskManager != null)
        {
            taskManager.starAmount = levelUpStarReward;
            taskManager.StartStarAnimation_NoTimer(levelUpStarReward, levelUpStarReward);
        }
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

        // Coroutine əvəzinə DOTween (Daha yüngül və hamar)
        RectTransform rect = statusText.rectTransform;
        rect.DOKill(); // Köhnə animasiyanı dayandır
        rect.localScale = Vector3.zero;

        // AAA Pop Animasiyası
        rect.DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                DOVirtual
                    .DelayedCall(
                        0.8f,
                        () =>
                        {
                            statusText.gameObject.SetActive(false);
                        }
                    )
                    .SetUpdate(true);
            });
    }

    IEnumerator StatusAnimationRoutine()
    {
        RectTransform rect = statusText.rectTransform;
        rect.localScale = Vector3.zero;

        // AAA Pop Effect (Time.unscaledDeltaTime istifadə olunur ki, slow-mo zamanı donmasın)
        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 6f;
            float curve =
                (t < 0.6f)
                    ? Mathf.Lerp(0, 1.4f, t / 0.6f)
                    : Mathf.Lerp(1.4f, 1f, (t - 0.6f) / 0.4f);
            rect.localScale = Vector3.one * curve;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.8f);
        statusText.gameObject.SetActive(false);
        // statusRoutine = null;
    }

    void UpdateLevelTexts()
    {
        if (currentLevelText != null)
        {
            currentLevelText.SetText("{0}", level);
            AnimateTextPulse(currentLevelText.rectTransform);
        }
        nextLevelText?.SetText("{0}", level + 1);
    }

    void AnimateTextPulse(RectTransform rt)
    {
        rt.DOKill();
        rt.localScale = Vector3.one;
        rt.DOPunchScale(Vector3.one * 0.3f, 0.4f, 2, 0.5f).SetUpdate(true);
    }

    public string GetCurrentLevelPercentage()
    {
        if (pointsToNextLevel <= 0)
            return "0%";
        float percentage = ((float)currentPoints / pointsToNextLevel) * 100f;
        return Mathf.Clamp(Mathf.RoundToInt(percentage), 0, 100).ToString();
    }

    public void SpawnLevelUpEffect()
    {
        if (levelUpParticlePrefab == null)
            return;

        // Viewport-dan koordinat hesablanması (Unity 6 Optimized)
        Vector3 leftPos = Camera.main.ViewportToWorldPoint(new Vector3(0.15f, -0.1f, 10f));
        Vector3 rightPos = Camera.main.ViewportToWorldPoint(new Vector3(0.85f, -0.1f, 10f));

        GameObject l = Instantiate(levelUpParticlePrefab, leftPos, Quaternion.Euler(-60, 30, 0));
        GameObject r = Instantiate(levelUpParticlePrefab, rightPos, Quaternion.Euler(-60, -30, 0));

        // CameraShake.Instance?.ShakeCamera(0.4f, 0.4f);

        Destroy(l, 5f);
        Destroy(r, 5f);
    }

    public void LevelUpSlowMo()
    {
        Time.timeScale = 0.4f;
        DOVirtual
            .DelayedCall(
                0.8f,
                () =>
                {
                    if (Time.timeScale < 1f)
                        Time.timeScale = 1f;
                }
            )
            .SetUpdate(true);
    }

    public float GetLevelProgressValue()
    {
        if (pointsToNextLevel <= 0)
            return 0f;
        float progress = ((float)currentPoints / pointsToNextLevel) * 100f;
        return Mathf.Clamp(progress, 0f, 100f);
    }
}
