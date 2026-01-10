using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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

    public bool isSkinsOpen = false;
    public List<GameObject> activeBalls = new List<GameObject>();

    [Header("HOP Style Mode Selection")]
    public GameMode currentMode = GameMode.Levels;
    public RectTransform highlightBox;
    public TMP_Text levelsText;
    public TMP_Text classicText;

    [Header("Mode Interaction Settings")]
    public RectTransform levelsPos;
    public RectTransform classicPos;
    public float slideSpeed = 10f;

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color ballGlowColor = Color.white;

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color playerGlowColor = Color.cyan;

    [Header("Advanced Color Palette (Background)")]
    public Camera mainCamera;
    public Color levelsBG = new Color32(21, 11, 45, 255);
    public Color classicBG = new Color32(15, 15, 15, 255);
    public float colorTransitionSpeed = 5f;

    [Header("UI Sounds")]
    public AudioSource uiAudioSource;
    public AudioClip buttonClickSFX;
    public AudioClip levelscchangeSFX;
    public AudioClip sceneloadSFX;
    private Coroutine sceneLoadSFXCoroutine;
    private Coroutine slideCoroutine;


    private void Awake()
    {
        Instance = this;
        speedDirection = Vector3.forward;

        PLayButton.onClick.AddListener(() =>
{
    PlayClickSound(); // 🔊 SƏS

    if (currentMode == GameMode.Levels)
        TransitionManager.Instance.LoadLevel("Game");
    else
        TransitionManager.Instance.LoadLevel("ClassicGame");
});

    }

    private void Start()
    {
        currentMode = (GameMode)PlayerPrefs.GetInt("SavedGameMode", 0);
        UpdateModeVisuals();

        if (highlightBox != null && levelsPos != null && classicPos != null)
        {
            highlightBox.anchoredPosition =
                (currentMode == GameMode.Levels)
                    ? levelsPos.anchoredPosition
                    : classicPos.anchoredPosition;
        }

        int level = PlayerPrefs.GetInt("level", 1);
        UpdateLevelUI(level, PlayerPrefs.GetInt("currentPoints", 0), level + 5);
        ApplyTargetGlow();
        Invoke(nameof(SpawnBall), 0f);
        if (sceneLoadSFXCoroutine != null)
            StopCoroutine(sceneLoadSFXCoroutine);

        sceneLoadSFXCoroutine = StartCoroutine(DelayedSceneLoadSFX(0.25f));
    }

    private void Update()
    {
        // Hədəf ətrafında fırlanma
        if (target != null)
        {
            transform.RotateAround(
                target.transform.position,
                speedDirection,
                currentSpeed * Time.deltaTime
            );
        }

        // Kamera rəng keçidi
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

    public void ToggleGameMode()
    {
        PlayLevelsSFX(); // 🔊 dərhal çal

        if (sceneLoadSFXCoroutine != null)
            StopCoroutine(sceneLoadSFXCoroutine);

        sceneLoadSFXCoroutine = StartCoroutine(DelayedSceneLoadSFX(0.25f));

        if (currentMode == GameMode.Levels)
            currentMode = GameMode.Classic;
        else
            currentMode = GameMode.Levels;

        SaveAndRefresh();
    }


    IEnumerator DelayedSceneLoadSFX(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // Time.timeScale-dən asılı deyil
        PlaySceneLoadSFX(); // 🔊 gecikmiş səs
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
            levelsText.color = (currentMode == GameMode.Levels) ? Color.green : Color.white;
        if (classicText != null)
            classicText.color = (currentMode == GameMode.Classic) ? Color.red : Color.white;

        int level = PlayerPrefs.GetInt("level", 1);
        UpdateLevelUI(level, PlayerPrefs.GetInt("currentPoints", 0), level + 5);

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(SlideHighlight());

    }

    IEnumerator SlideHighlight()
    {
        Vector2 targetPos =
            (currentMode == GameMode.Levels)
                ? levelsPos.anchoredPosition
                : classicPos.anchoredPosition;
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

    // --- Digər köməkçi funksiyalar (Heç bir dəyişiklik edilməyib) ---
    void UpdateLevelUI(int level, int points, int totalRequired)
    {
        if (levelColors == null || levelColors.Length == 0)
            return;

        int currentIndex;
        int nextIndex;

        if (currentMode == GameMode.Classic)
        {
            currentIndex = 0;
            nextIndex = 0;
        }
        else
        {
            currentIndex = (level - 1) % levelColors.Length;
            nextIndex = level % levelColors.Length;
        }
        // ----------------------------

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

        // Topun rəngini də buna görə yeniləyirik
        ballGlowColor = levelColors[currentIndex];

        for (int i = activeBalls.Count - 1; i >= 0; i--)
        {
            if (activeBalls[i] != null)
            {
                SpriteRenderer sr = activeBalls[i].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.DOKill();
                    sr.DOColor(ballGlowColor, 0.5f).SetUpdate(true);
                }
            }
            else
            {
                activeBalls.RemoveAt(i);
            }
        }
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

    void PlayClickSound()
    {
        if (uiAudioSource != null && buttonClickSFX != null)
            uiAudioSource.PlayOneShot(buttonClickSFX);
    }

    void PlayLevelsSFX()
    {
        if (uiAudioSource != null && levelscchangeSFX != null)
            uiAudioSource.PlayOneShot(levelscchangeSFX);
    }

    void PlaySceneLoadSFX()
    {
        if (uiAudioSource != null && sceneloadSFX != null)
            uiAudioSource.PlayOneShot(sceneloadSFX);
    }
}
