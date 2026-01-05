using System.Collections;
using System.Collections.Generic;
using MaskTransitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GameMode
{
    Levels,
    Classic,
} // Enum mütləq bura əlavə edilməlidir

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    [Header("UI & Appearance")]
    public TMP_Text levelText;
    public TMP_Text nextLevelText;
    public TMP_Text percentageText;
    public TMP_Text bestScoreText;

    [Header("Level Bar Elements")]
    public GameObject levelBarContainer; // Barın özü
    public Image levelCircle;
    public Image nextLevelCircle;
    public Image progressBarFill;
    public Color[] levelColors;

    [Header("References")]
    public GameObject player;
    public GameObject target;
    public GameObject ball;
    public Button PLayButton;

    [Header("Movement")]
    public float currentSpeed = 50f;
    public float radius = 1.2f;
    private Vector3 speedDirection;

    [HideInInspector]
    public bool isSkinsOpen = false;
    public List<GameObject> activeBalls = new List<GameObject>();

    [Header("HOP Style Mode Selection")]
    public GameMode currentMode = GameMode.Levels;
    public RectTransform highlightBox; // Ağ seçim qutusu
    public TMP_Text levelsText; // "LEVELS" yazısı
    public TMP_Text classicText; // "CLASSIC" yazısı
    public Vector2 levelsPos; // Highlight X: örnək -100
    public Vector2 classicPos; // Highlight X: örnək 100
    public float slideSpeed = 10f;

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color ballGlowColor = Color.white;

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color playerGlowColor = Color.cyan;

    private void Awake()
    {
        Instance = this;
        speedDirection = Vector3.forward;

        // Play düyməsinin məntiqi
        PLayButton.onClick.AddListener(() =>
        {
            if (currentMode == GameMode.Levels)
                TransitionManager.Instance.LoadLevel("Game"); // Level səhnəsi
            else
                TransitionManager.Instance.LoadLevel("ClassicGame"); // Classic səhnəsi
        });
    }

    private void Start()
    {
        // 1. Ən son seçilmiş modu yaddaşdan oxu (Default olaraq 0 yəni Levels)
        currentMode = (GameMode)PlayerPrefs.GetInt("SavedGameMode", 0);

        int level = PlayerPrefs.GetInt("level", 1);
        int currentPoints = PlayerPrefs.GetInt("currentPoints", 0);
        UpdateLevelUI(level, currentPoints, level + 5);

        ApplyTargetGlow();
        Invoke(nameof(SpawnBall), 0f);

        // 2. Vizualı yadda qalan moda görə nizamla
        UpdateModeVisuals();

        // Highlight-ı animasiyasız birbaşa yerinə qoy (Açılışda sürüşməsin)
        highlightBox.anchoredPosition = (currentMode == GameMode.Levels) ? levelsPos : classicPos;

        if (StarUI.Instance != null)
            StarUI.Instance.UpdateUI();
    }

    // --- MODE SELECTION ---
    public void SetLevelsMode()
    {
        if (currentMode == GameMode.Levels)
            return;
        currentMode = GameMode.Levels;

        // 3. Seçimi yaddaşa yaz
        PlayerPrefs.SetInt("SavedGameMode", (int)currentMode);
        PlayerPrefs.Save();

        UpdateModeVisuals();
    }

    public void SetClassicMode()
    {
        if (currentMode == GameMode.Classic)
            return;
        currentMode = GameMode.Classic;

        // 3. Seçimi yaddaşa yaz
        PlayerPrefs.SetInt("SavedGameMode", (int)currentMode);
        PlayerPrefs.Save();

        UpdateModeVisuals();
    }

    void UpdateModeVisuals()
    {
        if (levelBarContainer != null)
            levelBarContainer.SetActive(currentMode == GameMode.Levels);

        if (bestScoreText != null)
        {
            // Rejimə görə fərqli rekordları göstər
            int best =
                (currentMode == GameMode.Classic)
                    ? PlayerPrefs.GetInt("ClassicBestScore", 0)
                    : PlayerPrefs.GetInt("BestScore", 0);
            bestScoreText.text = best.ToString();
        }

        // Yazı rəngləri (Highlight üstündə olan qara, digəri ağ)
        if (levelsText != null)
            levelsText.color = (currentMode == GameMode.Levels) ? Color.black : Color.white;
        if (classicText != null)
            classicText.color = (currentMode == GameMode.Classic) ? Color.black : Color.white;

        StopAllCoroutines();
        StartCoroutine(SlideHighlight());
    }

    IEnumerator SlideHighlight()
    {
        Vector2 targetPos = (currentMode == GameMode.Levels) ? levelsPos : classicPos;
        while (Vector2.Distance(highlightBox.anchoredPosition, targetPos) > 0.1f)
        {
            highlightBox.anchoredPosition = Vector2.Lerp(
                highlightBox.anchoredPosition,
                targetPos,
                Time.deltaTime * slideSpeed
            );
            yield return null;
        }
        highlightBox.anchoredPosition = targetPos;
    }

    // --- GAME LOGIC ---
    void UpdateLevelUI(int level, int points, int totalRequired)
    {
        if (levelColors == null || levelColors.Length == 0)
            return;

        int currentIndex = (level - 1) % levelColors.Length;
        int nextIndex = level % levelColors.Length;

        if (levelText != null)
            levelText.text = level.ToString();
        if (nextLevelText != null)
            nextLevelText.text = (level + 1).ToString();

        float fillAmount = (float)points / totalRequired;
        if (percentageText != null)
            percentageText.text = Mathf.RoundToInt(fillAmount * 100f) + "%";

        if (levelCircle != null)
            levelCircle.color = levelColors[currentIndex];
        if (nextLevelCircle != null)
            nextLevelCircle.color = levelColors[nextIndex];
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = fillAmount;
            progressBarFill.color = levelColors[currentIndex];
        }
        ballGlowColor = levelColors[currentIndex];
    }

    private void Update()
    {
        if (target != null)
            transform.RotateAround(
                target.transform.position,
                speedDirection,
                currentSpeed * Time.deltaTime
            );
    }

    public void SpawnBall()
    {
        if (isSkinsOpen)
            return;
        activeBalls.RemoveAll(item => item == null);

        Vector2 pos2D = Random.insideUnitCircle.normalized * radius;
        Vector3 spawnPos = target.transform.position + new Vector3(pos2D.x, pos2D.y, 0f);

        GameObject spawnedBall = Instantiate(ball, spawnPos, Quaternion.identity);
        spawnedBall.tag = "Ball";
        activeBalls.Add(spawnedBall);

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
