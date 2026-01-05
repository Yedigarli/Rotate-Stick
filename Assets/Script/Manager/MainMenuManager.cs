using MaskTransitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI & Appearance")]
    public TMP_Text levelText; // Sol dairənin içindəki yazı
    public TMP_Text nextLevelText; // Sağ dairənin içindəki yazı
    public TMP_Text percentageText; // ⭐ Barın ortasındakı % yazısı
    public TMP_Text bestScoreText;

    [Header("Level Bar Elements")]
    public Image levelCircle;
    public Image nextLevelCircle;
    public Image progressBarFill;
    public Color[] levelColors;

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color ballGlowColor = Color.white;

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color playerGlowColor = Color.cyan;

    [Header("References")]
    public GameObject player;
    public GameObject target;
    public GameObject ball;
    public Button PLayButton;

    [Header("Movement")]
    public float currentSpeed = 50f;
    public float radius = 1.2f;
    private Vector3 speedDirection;

    private void Awake()
    {
        speedDirection = Vector3.forward;

        // 1. Məlumatları oxu (PlayerPrefs-dən)
        int level = PlayerPrefs.GetInt("level", 1);
        int currentPoints = PlayerPrefs.GetInt("currentPoints", 0);
        int pointsToNextLevel = level + 5; // LevelManager-dəki düsturla eyni olmalıdır

        // 2. UI-ı Yenilə
        UpdateLevelUI(level, currentPoints, pointsToNextLevel);

        ApplyTargetGlow();
        Invoke(nameof(SpawnBall), 0f);

        PLayButton.onClick.AddListener(() =>
        {
            TransitionManager.Instance.LoadLevel("Game");
        });

        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        if (bestScoreText != null)
            bestScoreText.text = bestScore.ToString();

        if (StarUI.Instance != null)
            StarUI.Instance.UpdateUI();
    }

    void UpdateLevelUI(int level, int points, int totalRequired)
    {
        if (levelColors == null || levelColors.Length == 0)
            return;

        int currentIndex = (level - 1) % levelColors.Length;
        int nextIndex = level % levelColors.Length;

        Color currentColor = levelColors[currentIndex];
        Color nextColor = levelColors[nextIndex];

        // Yazılar
        if (levelText != null)
            levelText.text = level.ToString();
        if (nextLevelText != null)
            nextLevelText.text = (level + 1).ToString();

        // Faiz Hesablanması
        float fillAmount = (float)points / totalRequired;
        if (percentageText != null)
        {
            int percent = Mathf.RoundToInt(fillAmount * 100f);
            percentageText.text = percent + "%";
        }

        // Dairələr və Bar
        if (levelCircle != null)
            levelCircle.color = currentColor;
        if (nextLevelCircle != null)
            nextLevelCircle.color = nextColor;

        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = fillAmount;
            progressBarFill.color = currentColor;
        }

        ballGlowColor = currentColor;
    }

    // --- Digər funksiyalar (Update, SpawnBall və s.) ---
    private void Update()
    {
        if (target != null)
            transform.RotateAround(
                target.transform.position,
                speedDirection,
                currentSpeed * Time.deltaTime
            );
    }

    void SpawnBall()
    {
        Vector2 pos2D = Random.insideUnitCircle.normalized * radius;
        Vector3 spawnPos = new Vector3(
            target.transform.position.x + pos2D.x,
            target.transform.position.y + pos2D.y,
            0f
        );
        GameObject spawnedBall = Instantiate(ball, spawnPos, Quaternion.identity);
        SpriteRenderer sr = spawnedBall.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = ballGlowColor;
    }

    void ApplyTargetGlow()
    {
        if (player != null)
        {
            SpriteRenderer playerSr = player.GetComponent<SpriteRenderer>();
            if (playerSr != null)
                playerSr.color = playerGlowColor;
        }
    }
}
