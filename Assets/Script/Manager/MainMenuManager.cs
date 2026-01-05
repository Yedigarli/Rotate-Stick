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
}

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    [Header("UI & Appearance")]
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

    [Header("Movement")]
    public float currentSpeed = 50f;
    public float radius = 1.2f;
    private Vector3 speedDirection;

    [HideInInspector]
    public bool isSkinsOpen = false;
    public List<GameObject> activeBalls = new List<GameObject>();

    [Header("HOP Style Mode Selection")]
    public GameMode currentMode = GameMode.Levels;
    public RectTransform highlightBox;
    public TMP_Text levelsText;
    public TMP_Text classicText;

    [Header("Stretch Compatible Mode Selection")]
    public RectTransform levelsBtnRect;
    public RectTransform classicBtnRect;
    public float slideSpeed = 10f;

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color ballGlowColor = Color.white;

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color playerGlowColor = Color.cyan;

    [Header("Advanced Color Palette (Background)")]
    public Camera mainCamera;
    public Color levelsBG = new Color32(21, 11, 45, 255); // #150B2D
    public Color classicBG = new Color32(15, 15, 15, 255); // #0F0F0F
    public float colorTransitionSpeed = 5f;

    private void Awake()
    {
        Instance = this;
        speedDirection = Vector3.forward;

        PLayButton.onClick.AddListener(() =>
        {
            if (currentMode == GameMode.Levels)
                TransitionManager.Instance.LoadLevel("Game");
            else
                TransitionManager.Instance.LoadLevel("ClassicGame");
        });
    }

    private void Start()
    {
        // 1. Yaddaşdan rejimi oxu
        currentMode = (GameMode)PlayerPrefs.GetInt("SavedGameMode", 0);

        int level = PlayerPrefs.GetInt("level", 1);
        int currentPoints = PlayerPrefs.GetInt("currentPoints", 0);
        UpdateLevelUI(level, currentPoints, level + 5);

        ApplyTargetGlow();
        Invoke(nameof(SpawnBall), 0f);

        // 2. Vizual nizamlamalar
        UpdateModeVisuals();

        // 3. Açılışda highlight-ı birbaşa yerinə qoy
        if (highlightBox != null && levelsBtnRect != null && classicBtnRect != null)
        {
            highlightBox.anchoredPosition =
                (currentMode == GameMode.Levels)
                    ? levelsBtnRect.anchoredPosition
                    : classicBtnRect.anchoredPosition;
        }

        if (StarUI.Instance != null)
            StarUI.Instance.UpdateUI();
    }

    private void Update()
    {
        // QAYTARILAN HİSSƏ: Hədəfin fırlanma hərəkəti
        if (target != null)
        {
            transform.RotateAround(
                target.transform.position,
                speedDirection,
                currentSpeed * Time.deltaTime
            );
        }

        // Background rəng keçidi
        if (mainCamera != null)
        {
            Color targetBG = (currentMode == GameMode.Levels) ? levelsBG : classicBG;
            mainCamera.backgroundColor = Color.Lerp(
                mainCamera.backgroundColor,
                targetBG,
                Time.deltaTime * colorTransitionSpeed
            );
        }
    }

    public void SetLevelsMode()
    {
        if (currentMode == GameMode.Levels)
            return;
        currentMode = GameMode.Levels;
        SaveAndRefresh();
    }

    public void SetClassicMode()
    {
        if (currentMode == GameMode.Classic)
            return;
        currentMode = GameMode.Classic;
        SaveAndRefresh();
    }

    private void SaveAndRefresh()
    {
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
            int best =
                (currentMode == GameMode.Classic)
                    ? PlayerPrefs.GetInt("ClassicBestScore", 0)
                    : PlayerPrefs.GetInt("BestScore", 0);
            bestScoreText.text = best.ToString();
        }

        if (levelsText != null)
            levelsText.color = (currentMode == GameMode.Levels) ? Color.black : Color.white;
        if (classicText != null)
            classicText.color = (currentMode == GameMode.Classic) ? Color.black : Color.white;

        StopAllCoroutines();
        StartCoroutine(SlideHighlight());
    }

    IEnumerator SlideHighlight()
    {
        if (highlightBox == null || levelsBtnRect == null || classicBtnRect == null)
            yield break;

        Vector2 targetPos =
            (currentMode == GameMode.Levels)
                ? levelsBtnRect.anchoredPosition
                : classicBtnRect.anchoredPosition;

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
