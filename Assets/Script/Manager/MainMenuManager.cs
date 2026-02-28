using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Oyun rejimləri
public enum GameMode
{
    Levels,
    Classic,
}

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    [Header("UI & Appearance (Cached Text)")]
    public TMP_Text levelText;
    public TMP_Text nextLevelText;
    public TMP_Text percentageText;
    public TMP_Text bestScoreText;

    [Header("Level Bar Elements")]
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

    [Header("Movement Settings")]
    public float currentSpeed = 50f;
    public float radius = 1.2f;
    private Vector3 _speedDirection = Vector3.forward;

    [Header("Game Mode Selection")]
    public GameMode currentMode = GameMode.Levels;
    public RectTransform highlightBox;
    public TMP_Text levelsText;
    public TMP_Text classicText;
    public RectTransform levelsPos;
    public RectTransform classicPos;
    public float slideDuration = 0.45f;

    [Header("Performance Colors")]
    public Camera mainCamera;
    public Color levelsBG = new Color32(21, 11, 45, 255);
    public Color classicBG = new Color32(15, 15, 15, 255);

    [Header("Ball & Skins Settings")]
    public bool isSkinsOpen = false;
    public List<GameObject> activeBalls = new List<GameObject>();
    public Color ballGlowColor = Color.white;
    public Color playerGlowColor = Color.cyan;

    [Header("Audio Clips")]
    public AudioSource uiAudioSource;
    public AudioClip buttonClickSFX;
    public AudioClip modeChangeSFX;
    public AudioClip sceneLoadSFX;

    // String Caching
    private static readonly string ComingSoonStr = "Coming Soon";
    private static readonly string PlayStr = "Play";
    private static readonly string BestScoreKey = "BestScore";
    private static readonly string ClassicBestScoreKey = "ClassicBestScore";

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;

        if (PLayButton != null)
            PLayButton.onClick.AddListener(OnPlayButtonClick);
    }

    private void Start()
    {
        currentMode = (GameMode)PlayerPrefs.GetInt("SavedGameMode", 0);

        InitializeUI();
        UpdateLevelUI();
        ApplyTargetGlow();
        SpawnBall();
    }

    private void Update()
    {
        // PERFORMANS: Hər kadrda List təmizləmək olmaz (UpdateScene-i ağırlaşdıran bu idi)
        // Sadəcə hərəkət məntiqi qalır
        if (target != null)
        {
            transform.RotateAround(
                target.transform.position,
                _speedDirection,
                currentSpeed * Time.deltaTime
            );
        }
    }

    private void InitializeUI()
    {
        if (highlightBox != null && levelsPos != null && classicPos != null)
        {
            Vector2 targetPos = (currentMode == GameMode.Levels)
                    ? levelsPos.anchoredPosition
                    : classicPos.anchoredPosition;
            highlightBox.anchoredPosition = targetPos;
        }

        if (mainCamera != null)
            mainCamera.backgroundColor = (currentMode == GameMode.Levels) ? levelsBG : classicBG;

        RefreshModeVisuals(true);
    }

    public void ToggleGameMode()
    {
        currentMode = (currentMode == GameMode.Levels) ? GameMode.Classic : GameMode.Levels;

        PlayerPrefs.SetInt("SavedGameMode", (int)currentMode);
        PlayerPrefs.Save();

        if (mainCamera != null)
            mainCamera.DOColor((currentMode == GameMode.Levels) ? levelsBG : classicBG, 0.6f).SetEase(Ease.InOutQuad);

        RefreshModeVisuals(false);
    }

    private void RefreshModeVisuals(bool immediate)
    {
        if (levelBarContainer != null)
            levelBarContainer.SetActive(currentMode == GameMode.Levels);

        int best = PlayerPrefs.GetInt(currentMode == GameMode.Classic ? ClassicBestScoreKey : BestScoreKey, 0);
        AnimateScore(bestScoreText, best);

        if (levelsText != null)
            levelsText.DOColor(currentMode == GameMode.Levels ? Color.green : Color.white, 0.3f);
        if (classicText != null)
            classicText.DOColor(currentMode == GameMode.Classic ? Color.red : Color.white, 0.3f);

        bool isClassic = (currentMode == GameMode.Classic);
        if (PlayButtonText != null)
            PlayButtonText.SetText(isClassic ? ComingSoonStr : PlayStr);

        if (PLayButton != null)
            PLayButton.interactable = !isClassic;

        if (highlightBox != null)
        {
            Vector2 targetPos = isClassic ? classicPos.anchoredPosition : levelsPos.anchoredPosition;
            highlightBox.DOKill();
            if (immediate)
                highlightBox.anchoredPosition = targetPos;
            else
                highlightBox.DOAnchorPos(targetPos, slideDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    public void UpdateLevelUI()
    {
        int level = PlayerPrefs.GetInt("level", 1);
        int currentIndex = (level - 1) % levelColors.Length;
        int nextIndex = level % levelColors.Length;

        // Garbage-Free Rendering
        levelText?.SetText("{0}", level);
        nextLevelText?.SetText("{0}", level + 1);

        if (levelCircle != null) levelCircle.color = levelColors[currentIndex];
        if (nextLevelCircle != null) nextLevelCircle.color = levelColors[nextIndex];
        if (progressBarFill != null) progressBarFill.color = levelColors[currentIndex];

        ballGlowColor = levelColors[currentIndex];

        // LevelManager ilə elaqeni float metodu ile edirik (Xətasız)
        if (percentageText != null)
        {
            if (LevelManager.Instance != null)
            {
                // GetCurrentLevelPercentage() evezine GetLevelProgressValue() istifade edirik (Float qaytardığı üçün)
                float prog = LevelManager.Instance.GetLevelProgressValue();
                percentageText.SetText("{0:0}%", prog);
            }
            else
            {
                // Fallback: LevelManager hazır deyilsə
                percentageText.SetText("0%");
            }
        }
    }

    public void SpawnBall()
    {
        if (isSkinsOpen || target == null || ball == null) return;

        // PERFORMANS: List təmizləməsini Update-dən bura gətirdik (Lazım olanda işləsin)
        activeBalls.RemoveAll(item => item == null);

        Vector2 pos2D = Random.insideUnitCircle.normalized * radius;
        Vector3 spawnPos = target.transform.position + new Vector3(pos2D.x, pos2D.y, 0f);

        GameObject spawnedBall = Instantiate(ball, spawnPos, Quaternion.identity);
        spawnedBall.tag = "Ball";
        activeBalls.Add(spawnedBall);

        if (spawnedBall.TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
            sr.color = ballGlowColor;
    }

    private void ApplyTargetGlow()
    {
        if (player != null && player.TryGetComponent<SpriteRenderer>(out SpriteRenderer playerSr))
        {
            playerSr.color = playerGlowColor;
        }
    }

    private void AnimateScore(TMP_Text textElement, int targetValue)
    {
        if (textElement == null) return;
        textElement.DOKill();
        int startValue = 0;
        DOTween.To(() => startValue, x =>
        {
            startValue = x;
            textElement.SetText("{0}", startValue);
        }, targetValue, 1.0f).SetEase(Ease.OutExpo).SetUpdate(true);
    }

    private void OnPlayButtonClick()
    {
        if (currentMode == GameMode.Levels)
        {
            if (MaskTransitions.TransitionManager.Instance != null)
                MaskTransitions.TransitionManager.Instance.LoadLevel("Game");
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        }
    }
}
