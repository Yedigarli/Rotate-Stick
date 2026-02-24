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
    private string[] perfectWords = { "PERFECT!", "AMAZING!", "FANTASTIC!", "BULLSEYE!" };
    private string[] niceWords = { "NICE!", "GOOD!", "COOL!", "NOT BAD!" };
    private string[] insaneWords = { "INSANE!", "GODLIKE!", "UNSTOPPABLE!", "MONSTER!" };

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
    private GameObject ballReference; // Topu hər dəfə axtarmamaq üçün referans

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        level = PlayerPrefs.GetInt("level", 1);
        pointsToNextLevel = 10 + (level * 2); // Başlanğıc hesabı

        statusText?.gameObject.SetActive(false);
        if (scoreText != null)
            scoreText.text = "0";

        UpdateLevelTexts();
        if (progressBarFill != null)
            progressBarFill.fillAmount = 0;
    }

    private void Start()
    {
        // TaskManager.Instance varsa birbaşa istifadə etmək daha yaxşıdır
        taskManager = FindFirstObjectByType<TaskManager>();
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

            Camera.main.DOKill(); // Köhnə animasiyanı dayandır
            Camera.main.DOColor(currentLevelThemeColor * 0.2f, 2f);

            // FindWithTag yerinə referans yoxlaması
            if (ballReference == null)
                ballReference = GameObject.FindWithTag("Ball");

            if (ballReference != null)
            {
                SpriteRenderer sr = ballReference.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.color = currentLevelThemeColor;
            }
        }
    }

    public void AddProgress(int amount)
    {
        currentPoints += amount;
        totalScore += amount;

        if (scoreText != null)
            scoreText.text = totalScore.ToString();

        // Barı DOTween ilə doldururuq (Axıcı və performanslı)
        float fillRatio = (float)currentPoints / pointsToNextLevel;
        progressBarFill.DOFillAmount(fillRatio, 0.3f).SetEase(Ease.OutQuad);

        if (amount >= 2 && MissionManager.Instance != null)
            MissionManager.Instance.AddPerfect();

        MissionManager.Instance?.AddScore(amount);
        CheckBestScore();

        // Hər xalda yox, yalnız vacib anlarda Save etmək olar.
        // Amma mütləq lazımdırsa burda qalsın.
        PlayerPrefs.SetInt("currentPoints", currentPoints);

        if (currentPoints >= pointsToNextLevel)
            LevelUp();
    }

    // Yeni köməkçi funksiya
    void CheckBestScore()
    {
        int currentBest = PlayerPrefs.GetInt("BestScore", 0);
        if (totalScore > currentBest)
        {
            if (!isBestScoreReachedThisSession && currentBest > 0)
            {
                isBestScoreReachedThisSession = true;
                if (taskManager != null)
                    taskManager.StartStarAnimation_NoTimer(
                        bestScoreStarReward,
                        bestScoreStarReward
                    );

                SpawnLevelUpEffect();
                ShowStatus("NEW BEST!", perfectColor);
            }
            PlayerPrefs.SetInt("BestScore", totalScore);
        }
    }

    void LevelUp()
    {
        level++;
        currentPoints = 0;
        pointsToNextLevel = Mathf.Min(10 + (level * 2), 50);

        PlayerPrefs.SetInt("level", level);
        PlayerPrefs.SetInt("currentPoints", 0);
        PlayerPrefs.Save(); // Yalnız level artanda diskə yazırıq

        if (MissionManager.Instance != null)
            MissionManager.Instance.AddLevel();

        // Sürət artımı
        if (GameManager.Instance.FirstSpeed < 350f)
            GameManager.Instance.FirstSpeed += 2f;

        GameManager.Instance.currentSpeed = GameManager.Instance.FirstSpeed;
        PlayerPrefs.SetFloat("firstspeed", GameManager.Instance.FirstSpeed);

        UISoundManager.Instance?.PlayLevelUpSFX();

        UpdateLevelTexts();
        UpdateThemeColor();
        SpawnLevelUpEffect();
        ShowStatus("LEVEL UP!", levelUpColor);
        LevelUpSlowMo();

        StartCoroutine(SmoothLevelTransition());

        if (taskManager != null)
        {
            taskManager.starAmount = levelUpStarReward;
            taskManager.StartStarAnimation_NoTimer(levelUpStarReward, levelUpStarReward);
        }
    }

    // Bu funksiya LevelUp-dan xaricdə, aşağıda olmalıdır
    IEnumerator SmoothLevelTransition()
    {
        yield return new WaitForSecondsRealtime(0.4f);
        progressBarFill.fillAmount = 0f;
    }

    public void ShowStatusByType(string type, int combo = 0)
    {
        string selectedWord = "";
        Color selectedColor = Color.white;

        if (type == "Perfect")
        {
            // ⭐ MISSİYA BURADA ÇAĞIRILMALIDIR
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.AddPerfect();
            }

            selectedWord =
                combo >= 5
                    ? insaneWords[UnityEngine.Random.Range(0, insaneWords.Length)]
                    : perfectWords[UnityEngine.Random.Range(0, perfectWords.Length)];

            selectedColor = combo >= 5 ? new Color(2f, 0f, 2f) : perfectColor;
        }
        else if (type == "Nice")
        {
            selectedWord = niceWords[UnityEngine.Random.Range(0, niceWords.Length)];
            selectedColor = niceColor;
        }

        ShowStatus(selectedWord, selectedColor);
    }

    public void ShowStatus(string message, Color col)
    {
        if (statusText == null)
            return;
        statusText.text = message;
        statusText.color = col;
        statusText.gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(StatusAnimationRoutine());
    }

    IEnumerator StatusAnimationRoutine()
    {
        RectTransform rect = statusText.GetComponent<RectTransform>();
        rect.localScale = Vector3.zero;

        float t = 0;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            rect.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.5f, t / 0.15f);
            yield return null;
        }

        t = 0;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            rect.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, t / 0.1f);
            yield return null;
        }

        yield return new WaitForSeconds(0.7f);
        statusText.gameObject.SetActive(false);
    }

    void UpdateLevelTexts()
    {
        if (currentLevelText != null)
        {
            currentLevelText.text = level.ToString();
            StartCoroutine(TextPunchScale(currentLevelText.rectTransform));
        }
        if (nextLevelText != null)
            nextLevelText.text = (level + 1).ToString();
    }

    IEnumerator TextPunchScale(RectTransform rt)
    {
        float t = 0;
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = Vector3.one * 1.8f; // 1.8 qat böyüsün

        while (t < 0.15f)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(originalScale, targetScale, t / 0.15f);
            yield return null;
        }

        t = 0;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(targetScale, originalScale, t / 0.2f);
            yield return null;
        }
    }

    public string GetCurrentLevelPercentage()
    {
        // pointsToNextLevel 0 olarsa, bölmə xətası verməməsi üçün yoxlama
        if (pointsToNextLevel <= 0)
            return "0%";

        float percentage = ((float)currentPoints / pointsToNextLevel) * 100f;

        // 100-dən yuxarı çıxmaması üçün Clamp əlavə edirik
        int finalValue = Mathf.Clamp(Mathf.RoundToInt(percentage), 0, 100);

        return finalValue.ToString() + "%";
    }

    public void SpawnLevelUpEffect()
    {
        if (levelUpParticlePrefab == null)
            return;

        // 1. Mövqeləri təyin edirik (Z oxu 10f kameranın qarşısında görünməsi üçündür)
        // Viewport: (0,0) sol alt, (1,1) sağ üst küncdür.
        Vector3 leftCorner = Camera.main.ViewportToWorldPoint(new Vector3(0.15f, -0.1f, 10f));
        Vector3 rightCorner = Camera.main.ViewportToWorldPoint(new Vector3(0.85f, -0.1f, 10f));

        // 2. Sol küncdən atəş (Yuxarı və sağa doğru - Euler: -60, 30, 0)
        GameObject leftConfetti = Instantiate(
            levelUpParticlePrefab,
            leftCorner,
            Quaternion.Euler(-60, 30, 0)
        );

        // 3. Sağ küncdən atəş (Yuxarı və sola doğru - Euler: -60, -30, 0)
        GameObject rightConfetti = Instantiate(
            levelUpParticlePrefab,
            rightCorner,
            Quaternion.Euler(-60, -30, 0)
        );

        // 4. Vizual təsir: Kamera titrəməsi
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCamera(0.4f, 0.4f);
        }

        // 5. Təmizlik: Effektləri müəyyən müddətdən sonra yox edirik
        Destroy(leftConfetti, 5f);
        Destroy(rightConfetti, 5f);
    }

    public void LevelUpSlowMo()
    {
        Time.timeScale = 0.4f;
        DOVirtual
            .DelayedCall(
                0.8f,
                () =>
                {
                    Time.timeScale = 1f;
                }
            )
            .SetUpdate(true); // DOTween tələb olunur
    }
}
