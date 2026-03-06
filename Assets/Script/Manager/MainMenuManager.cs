using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private static readonly string ComingSoonStr = "Coming Soon";
    private static readonly string PlayStr = "PLAY NOW";
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

        ConfigureTextLayout();
        InitializeUI();
        UpdateLevelUI();
        ApplyTargetGlow();
        Invoke(nameof(SpawnBall), 0.1f);
    }

    private void Update()
    {
        if (isSkinsOpen || target == null)
            return;

        transform.RotateAround(
            target.transform.position,
            _speedDirection,
            currentSpeed * Time.deltaTime
        );
    }

    private void ConfigureTextLayout()
    {
        if (levelText != null)
            levelText.alignment = TextAlignmentOptions.Center;

        if (nextLevelText != null)
            nextLevelText.alignment = TextAlignmentOptions.Center;

        if (percentageText != null)
        {
            percentageText.alignment = TextAlignmentOptions.Center;
            percentageText.enableWordWrapping = false;
        }

        if (bestScoreText != null)
        {
            bestScoreText.alignment = TextAlignmentOptions.Center;
            bestScoreText.enableWordWrapping = false;
        }

        if (PlayButtonText != null)
        {
            PlayButtonText.alignment = TextAlignmentOptions.Center;
            PlayButtonText.enableWordWrapping = false;
            PlayButtonText.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    private void InitializeUI()
    {
        if (highlightBox != null && levelsPos != null && classicPos != null)
        {
            Vector2 targetPos =
                (currentMode == GameMode.Levels)
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
            mainCamera
                .DOColor((currentMode == GameMode.Levels) ? levelsBG : classicBG, 0.6f)
                .SetEase(Ease.InOutQuad);

        RefreshModeVisuals(false);
    }

    private void RefreshModeVisuals(bool immediate)
    {
        if (levelBarContainer != null)
            levelBarContainer.SetActive(currentMode == GameMode.Levels);

        int best = PlayerPrefs.GetInt(
            currentMode == GameMode.Classic ? ClassicBestScoreKey : BestScoreKey,
            0
        );
        AnimateScore(bestScoreText, best);

        if (levelsText != null)
            levelsText.DOColor(currentMode == GameMode.Levels ? Color.green : Color.white, 0.3f);
        if (classicText != null)
            classicText.DOColor(currentMode == GameMode.Classic ? Color.red : Color.white, 0.3f);

        bool isClassic = currentMode == GameMode.Classic;
        if (PlayButtonText != null)
            PlayButtonText.SetText(isClassic ? ComingSoonStr : PlayStr);

        if (PLayButton != null)
            PLayButton.interactable = !isClassic;

        if (highlightBox != null)
        {
            Vector2 targetPos = isClassic
                ? classicPos.anchoredPosition
                : levelsPos.anchoredPosition;
            highlightBox.DOKill();
            if (immediate)
                highlightBox.anchoredPosition = targetPos;
            else
                highlightBox
                    .DOAnchorPos(targetPos, slideDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
        }

        for (int i = 0; i < activeBalls.Count; i++)
        {
            GameObject b = activeBalls[i];
            if (b != null && b.TryGetComponent<SpriteRenderer>(out var sr))
                sr.color = (currentMode == GameMode.Classic) ? Color.red : ballGlowColor;
        }
    }

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
            progressBarFill.fillAmount = 0f;
            progressBarFill.DOFillAmount(progressRatio, 1.2f).SetEase(Ease.OutCubic);
        }

        if (percentageText != null)
        {
            percentageText.DOKill();
            float startValue = 0f;
            DOTween
                .To(
                    () => startValue,
                    x =>
                    {
                        startValue = x;
                        percentageText.SetText("{0:0}%", startValue);
                    },
                    percentage,
                    1.2f
                )
                .SetEase(Ease.OutCubic);
        }

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
            if (currentMode == GameMode.Classic)
            {
                sr.color = Color.red;
                ballGlowColor = Color.red;
            }
            else
            {
                int level = PlayerPrefs.GetInt("level", 1);
                ballGlowColor = levelColors[(level - 1) % levelColors.Length];
                sr.color = ballGlowColor;
            }
        }
    }

    private void ApplyTargetGlow()
    {
        if (player != null && player.TryGetComponent<SpriteRenderer>(out SpriteRenderer playerSr))
            playerSr.color = playerGlowColor;
    }

    private void AnimateScore(TMP_Text textElement, int targetValue)
    {
        if (textElement == null)
            return;

        textElement.DOKill();
        int startValue = 0;
        DOTween
            .To(
                () => startValue,
                x =>
                {
                    startValue = x;
                    textElement.SetText("{0}", startValue);
                },
                targetValue,
                1.0f
            )
            .SetEase(Ease.OutExpo)
            .SetUpdate(true);
    }

    private void OnPlayButtonClick()
    {
        PLayButton.interactable = false;

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadLevel("Game");
        else
            SceneManager.LoadScene("Game");
    }
}





