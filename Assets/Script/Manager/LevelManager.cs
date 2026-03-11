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
    public TMP_Text milestoneText;

    [Header("Milestone Text")]
    public Vector2 milestoneTextOffset = new Vector2(0f, -120f);
    public Vector2 levelUpTextOffset = Vector2.zero;
    public Vector2 bestScoreTextOffset = new Vector2(0f, -20f);
    [ColorUsage(true, true)]
    public Color levelUpTextColor = new Color(0.3f, 2.2f, 0.3f);
    [ColorUsage(true, true)]
    public Color bestScoreTextColor = new Color(2.2f, 1.1f, 0.2f);
    public float levelUpTextDuration = 1.3f;
    public float bestScoreTextDuration = 1.6f;
    public float levelUpTextScale = 1.15f;
    public float bestScoreTextScale = 1.28f;
    public float levelUpTextSize = 62f;
    public float bestScoreTextSize = 70f;

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
    public GameObject levelUpParticlePrefab;    [Header("Milestone Feedback")]
    public float milestoneStatusDuration = 1.4f;
    public float milestoneStatusScale = 1.15f;
    public float milestoneCameraPunch = 0.12f;
    public float milestoneCameraPunchDuration = 0.28f;
    public float milestonePlayerFxYOffset = 0.2f;

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
    private float baseMilestoneFontSize = -1f;


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

        if (nextLevelText != null)
            nextLevelText.gameObject.SetActive(false);
        if (nextLevelCircle != null)
            nextLevelCircle.gameObject.SetActive(false);
        if (currentLevelCircle != null)
            currentLevelCircle.gameObject.SetActive(false);
        if (progressBarFill != null)
            progressBarFill.gameObject.SetActive(false);
        if (milestoneText != null)
        {
            milestoneText.gameObject.SetActive(false);
            baseMilestoneFontSize = milestoneText.fontSize;
        }

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
                UISoundManager.Instance?.PlayBestScoreSFX();
                PlayMilestoneFeedback("NEW BEST!", perfectColor, true);
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
        PlayMilestoneFeedback("LEVEL UP!", levelUpColor, false);

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
        ShowStatus(message, col, 0.87f, 1f);
    }

    public void ShowStatus(string message, Color col, float duration, float scale)
    {
        if (statusText == null)
            return;

        statusText.SetText(message);
        statusText.color = col;
        statusText.gameObject.SetActive(true);

        RectTransform rect = statusText.rectTransform;
        rect.DOKill();
        rect.localScale = Vector3.zero;

        rect.DOScale(Vector3.one * scale, 0.22f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);

        statusHideTween?.Kill();
        statusHideTween = DOVirtual.DelayedCall(duration, HideStatusText)
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
            currentLevelText.SetText("LEVEL: {0}", level);
            AnimateTextPulse(currentLevelText.rectTransform);
        }
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

    private void PlayMilestoneFeedback(string message, Color color, bool isBestScore)
    {
        ShowStatus(message, color, milestoneStatusDuration, milestoneStatusScale);
        ShowMilestoneText(message, isBestScore);
        SpawnLevelUpEffect();
        SpawnPlayerMilestoneEffect(color, isBestScore);
        PulseCamera(isBestScore);
        PulsePlayer(color);
    }

    private void ShowMilestoneText(string message, bool isBestScore)
    {
        if (milestoneText == null)
            return;

        if (baseMilestoneFontSize <= 0f)
            baseMilestoneFontSize = milestoneText.fontSize;

        float size = isBestScore ? bestScoreTextSize : levelUpTextSize;
        if (size <= 0f)
            size = baseMilestoneFontSize;

        milestoneText.fontSize = size;
        milestoneText.SetText(message);
        milestoneText.color = isBestScore ? bestScoreTextColor : levelUpTextColor;
        milestoneText.gameObject.SetActive(true);

        Vector2 baseOffset = milestoneTextOffset;
        Vector2 typeOffset = isBestScore ? bestScoreTextOffset : levelUpTextOffset;
        Vector2 startPos = baseOffset + typeOffset;
        Vector2 endPos = startPos + new Vector2(0f, isBestScore ? 34f : 26f);

        RectTransform rect = milestoneText.rectTransform;
        rect.DOKill();
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * 0.55f;
        rect.anchoredPosition = startPos;

        float scale = isBestScore ? bestScoreTextScale : levelUpTextScale;
        float duration = isBestScore ? bestScoreTextDuration : levelUpTextDuration;

        Sequence seq = DOTween.Sequence();
        seq.Append(rect.DOScale(Vector3.one * scale, 0.18f).SetEase(Ease.OutBack));
        seq.Join(rect.DOAnchorPos(endPos, 0.28f).SetEase(Ease.OutCubic));

        if (isBestScore)
            seq.Append(rect.DOPunchRotation(new Vector3(0f, 0f, 8f), 0.32f, 10, 0.7f));
        else
            seq.Append(rect.DOPunchScale(Vector3.one * 0.08f, 0.28f, 8, 0.6f));

        seq.AppendInterval(duration);
        seq.Append(rect.DOScale(Vector3.one * 0.7f, 0.18f).SetEase(Ease.InBack));
        seq.OnComplete(() =>
        {
            if (milestoneText != null)
                milestoneText.gameObject.SetActive(false);
        });
        seq.SetUpdate(true);
        seq.SetLink(milestoneText.gameObject, LinkBehaviour.KillOnDestroy);
    }
    private void PulseCamera(bool isBestScore)
    {
        if (cachedCamera == null)
            cachedCamera = Camera.main;
        if (cachedCamera == null)
            return;

        float punch = milestoneCameraPunch * (isBestScore ? 1.25f : 1f);
        cachedCamera.transform.DOKill();
        cachedCamera.transform.DOPunchPosition(
            new Vector3(punch, punch, 0f),
            milestoneCameraPunchDuration,
            10,
            0.6f
        ).SetUpdate(true);
    }

    private void PulsePlayer(Color color)
    {
        GameObject playerObj = GameManager.Instance != null ? GameManager.Instance.player : null;
        if (playerObj == null)
            return;
        if (!playerObj.TryGetComponent<SpriteRenderer>(out var sr))
            return;

        sr.DOKill();
        sr.DOColor(color, 0.12f)
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void SpawnPlayerMilestoneEffect(Color color, bool isBestScore)
    {
        if (levelUpParticlePrefab == null)
            return;

        GameObject playerObj = GameManager.Instance != null ? GameManager.Instance.player : null;
        if (playerObj == null)
            return;

        Vector3 pos = playerObj.transform.position + new Vector3(0f, milestonePlayerFxYOffset, 0f);
        float tilt = isBestScore ? -25f : -35f;
        GameObject fx = Instantiate(levelUpParticlePrefab, pos, Quaternion.Euler(tilt, 0f, 0f));
        if (fx.TryGetComponent<ParticleSystem>(out var ps))
        {
            var main = ps.main;
            main.startColor = color;
        }

        Destroy(fx, 3f);
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

























