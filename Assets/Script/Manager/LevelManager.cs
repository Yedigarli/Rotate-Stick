using System.Collections;
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
    [ColorUsage(showAlpha: true, hdr: true)]
    public Color comboColor = new Color(2f, 0.5f, 0f); // HDR Neon Narıncı

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color perfectColor = new Color(1.5f, 1.2f, 0f); // HDR Sarı

    [ColorUsage(true, true)]
    public Color niceColor = new Color(0f, 1.2f, 1.5f); // HDR Mavi

    [ColorUsage(true, true)]
    public Color levelUpColor = new Color(0.2f, 2f, 0.2f); // HDR Yaşıl

    [Header("Settings")]
    public int pointsToNextLevel;
    public float smoothSpeed = 5f; // Barın dolma sürəti
    private int currentPoints = 0;
    private int totalScore = 0; // Ümumi xal üçün yeni dəyişən
    private int level;
    private float targetFillAmount;

    [Header("Level Up Effects")]
    public GameObject levelUpParticlePrefab; // Inspector-dan bura ulduz və ya konfeti effekti atacaqsan

    [Header("Randomized Words")]
    private string[] perfectWords = { "PERFECT!", "AMAZING!", "FANTASTIC!", "BULLSEYE!" };
    private string[] niceWords = { "NICE!", "GOOD!", "COOL!", "NOT BAD!" };
    private string[] insaneWords = { "INSANE!", "GODLIKE!", "UNSTOPPABLE!", "MONSTER!" };

    [Header("Level Dynamic Colors")]
    public Image currentLevelCircle; // Inspector-da sol dairəni bura at
    public Image nextLevelCircle; // Inspector-da sağ dairəni bura at
    public Color[] levelColors; // Müxtəlif rəngləri bura əlavə et (Göy, Yaşıl, Bənövşəyi və s.)

    private Color currentLevelThemeColor;
    private bool isBestScoreReachedThisSession = false;

    [Header("Reward Settings")]
    public int levelUpStarReward = 20; // Level keçəndə verilən ulduz
    public int bestScoreStarReward = 10; // Rekord qıranda verilən ulduz

    // TaskManager referansı (Animasiyanı başlatmaq üçün)
    private TaskManager taskManager;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        level = PlayerPrefs.GetInt("level", 1);
        pointsToNextLevel = level + 5;

        statusText?.gameObject.SetActive(false);

        // Score başlanğıcda 0 görünsün
        if (scoreText != null)
            scoreText.text = "0";

        UpdateLevelTexts();
        if (progressBarFill != null)
            progressBarFill.fillAmount = 0;
    }

    private void Start()
    {
        // Oyun tam başlayanda rəngləri tətbiq et
        UpdateThemeColor();

        taskManager = FindFirstObjectByType<TaskManager>();
    }

    private void Update()
    {
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = Mathf.Lerp(
                progressBarFill.fillAmount,
                targetFillAmount,
                Time.deltaTime * smoothSpeed
            );

            // Barın rəngi seçdiyimiz mövzu rəngində olsun
            progressBarFill.color = currentLevelThemeColor;
        }
    }

    public void UpdateThemeColor()
    {
        if (levelColors == null || levelColors.Length == 0)
            return;

        // 1. Cari levelin rəng indeksi
        int currentColorIndex = (level - 1) % levelColors.Length;
        currentLevelThemeColor = levelColors[currentColorIndex];

        // 2. Növbəti levelin rəng indeksi (Dairənin arxa rəngi üçün)
        int nextColorIndex = level % levelColors.Length;
        Color nextLevelThemeColor = levelColors[nextColorIndex];

        // UI-ları rənglə
        if (currentLevelCircle != null)
            currentLevelCircle.color = currentLevelThemeColor;

        // ⭐ BURANI DƏYİŞDİK: Sağ dairə artıq növbəti rəngdə olacaq
        if (nextLevelCircle != null)
            nextLevelCircle.color = nextLevelThemeColor;

        // ProgressBar rəngi cari levelin rəngində qalsın (və ya keçid rəngi edə bilərsən)
        if (progressBarFill != null)
            progressBarFill.color = currentLevelThemeColor;

        // GameManager-i yenilə
        if (GameManager.Instance != null)
        {
            GameManager.Instance.baseBallColor = currentLevelThemeColor;
            GameManager.Instance.ballGlowColor = currentLevelThemeColor;

            // Aktiv topun rəngini dəyiş
            GameObject activeBall = GameObject.FindWithTag("Ball");
            if (activeBall != null)
            {
                SpriteRenderer sr = activeBall.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.color = currentLevelThemeColor;
            }
        }
    }

    public void AddProgress(int amount)
    {
        currentPoints += amount;
        totalScore += amount;
        // Əgər vuruş Perfect-dirsə (GameManager-dən 2 xal gəlirsə)
        if (amount >= 2)
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.AddPerfect(); // Bu MissionManager-dəki 1-ci missiyanı artıracaq
            }
        }

        if (scoreText != null)
            scoreText.text = totalScore.ToString();

        MissionManager.Instance.AddScore(amount);

        // ⭐ REKORDU YADDA SAXLA
        CheckBestScore();

        PlayerPrefs.SetInt("currentPoints", currentPoints);
        PlayerPrefs.Save();

        targetFillAmount = (float)currentPoints / pointsToNextLevel;

        if (currentPoints >= pointsToNextLevel)
        {
            LevelUp();
        }
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

                // ⭐ REKORD ÜÇÜN ULDUZ VER
                if (StarManager.Instance != null)
                {
                    // Artıq AddStar-ı burada çağırmırıq, çünki StartStarAnimationWithRandomReward özü artırır
                    if (taskManager != null)
                    {
                        // DÜZƏLİŞ: OnGetButtonClick yerinə birbaşa animasiya funksiyasını çağırırıq
                        // Bu funksiya taymeri sıfırlamır!
                        taskManager.StartStarAnimation_NoTimer(
                            bestScoreStarReward,
                            bestScoreStarReward
                        );
                    }
                }

                SpawnLevelUpEffect();
                ShowStatus("NEW BEST!", perfectColor);
            }

            PlayerPrefs.SetInt("BestScore", totalScore);
            PlayerPrefs.Save();
        }
    }

    void LevelUp()
    {
        level++;
        currentPoints = 0;
        targetFillAmount = 0;

        pointsToNextLevel = level + 5;
        PlayerPrefs.SetInt("level", level);
        PlayerPrefs.Save();

        PlayerPrefs.SetInt("currentPoints", currentPoints);
        PlayerPrefs.Save();

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.AddLevel(); // Bu 2-ci indeksdəki missiyanı 1 artıracaq
        }

        // ⭐ ULDUZ ARTIMINI BURADAN SİLDİK (StarManager.Instance.AddStar hissəsini)
        // Sadəcə animasiyanı çağırırıq, ulduzlar uçub hədəfə çatanda özü artacaq.
        if (taskManager != null)
        {
            taskManager.starAmount = levelUpStarReward;

            // Animasiyanı başla, amma Free Gift vaxtını sıfırlama!
            // Bunun üçün aşağıdakı yeni funksiyanı çağıracağıq:
            taskManager.StartStarAnimation_NoTimer(bestScoreStarReward, bestScoreStarReward);
        }

        UpdateLevelTexts();
        ShowStatus("LEVEL UP!", levelUpColor);
        UpdateThemeColor();
        SpawnLevelUpEffect();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentSpeed = GameManager.Instance.FirstSpeed;
            GameManager.Instance.FirstSpeed += 5f;
            PlayerPrefs.SetFloat("firstspeed", GameManager.Instance.FirstSpeed);
        }
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
        float percentage = ((float)currentPoints / pointsToNextLevel) * 100f;
        return Mathf.RoundToInt(percentage).ToString() + "%";
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
}
