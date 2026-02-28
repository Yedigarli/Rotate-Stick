using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MaskTransitions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Speeds")]
    public float FirstSpeed = 250f;
    public float currentSpeed;
    private Vector3 speedDirection = Vector3.forward;

    [Header("Raycast Settings")]
    public float raycastDistance = 10f;
    public LayerMask detectableLayers;

    [Header("Pooling Tags")]
    public string normalBallTag = "Ball";
    public string starBallTag = "StarBall";
    public string ballParticleTag = "BallParticle";
    public string floatingTextTag = "FloatingText";

    [Header("Objects References")]
    public GameObject target;
    public GameObject player;

    [Header("Spawning")]
    public float radius = 1.3f;
    public float starBallChance = 0.2f;

    [Header("State")]
    public bool isSettingsOpen = false;
    private GameObject currentBall;
    private int comboCount = 0;
    private bool isGameOver = false;

    [Header("GameOver UI")]
    public GameObject gameoverPOPUP;
    public GameObject secondChancePanel;
    public TMP_Text levelProgressPercentText;
    public CanvasGroup canvasGroup;
    public Button resStartBTN,
        menuBTN,
        skinsButton,
        useStarBtn,
        noThanksBtn;

    [Header("Colors (HDR)")]
    [ColorUsage(true, true)]
    public Color ballGlowColor = Color.white;

    [ColorUsage(true, true)]
    public Color baseBallColor = Color.white;

    [ColorUsage(true, true)]
    public Color playerGlowColor = Color.cyan;

    private float animDuration = 0.25f;
    private static readonly string BallTagStr = "Ball";

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;

        if (levelProgressPercentText != null)
            levelProgressPercentText.enabled = false;
        if (gameoverPOPUP != null)
            gameoverPOPUP.SetActive(false);
        if (secondChancePanel != null)
            secondChancePanel.SetActive(false);

        FirstSpeed = PlayerPrefs.GetFloat("firstspeed", FirstSpeed);
        currentSpeed = FirstSpeed;

        SetupButtons();
    }

    private void Start()
    {
        ApplyGlobalColors();
        StartCoroutine(SafeSpawn(0.1f));
    }

    private void Update()
    {
        if (isGameOver || isSettingsOpen || Time.timeScale == 0 || target == null)
            return;

        transform.RotateAround(
            target.transform.position,
            speedDirection,
            currentSpeed * Time.deltaTime
        );

        if (Input.GetMouseButtonDown(0))
            HandleShoot();
    }

    void SetupButtons()
    {
        resStartBTN?.onClick.AddListener(() => RestartLevel("Game"));
        menuBTN?.onClick.AddListener(() => RestartLevel("Menu"));
        skinsButton?.onClick.AddListener(OpenSkinsMenu);
        noThanksBtn?.onClick.AddListener(CloseSecondChanceAndShowGameOver);
        useStarBtn?.onClick.AddListener(HandleContinueButton);
    }

    void ApplyGlobalColors()
    {
        if (player != null && player.TryGetComponent<SpriteRenderer>(out var sr))
            sr.color = playerGlowColor;
    }

    void HandleShoot()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            transform.right,
            raycastDistance,
            detectableLayers
        );

        if (hit.collider != null)
            ProcessHit(hit);
        else
            StartCoroutine(GameOver());
    }

    void ProcessHit(RaycastHit2D hit)
    {
        Vector3 hitPos = hit.collider.transform.position;
        float distToCenter = Vector2.Distance(hit.point, hitPos);
        bool isPerfect = distToCenter < 0.18f;

        if (isPerfect)
        {
            comboCount++;
            ApplyPerfectEffects(hitPos);
            LevelManager.Instance?.AddProgress(2);
        }
        else
        {
            comboCount = 0;
            ApplyNormalHitEffects(hitPos);
            LevelManager.Instance?.AddProgress(1);
        }

        if (hit.collider.TryGetComponent<Ball>(out Ball ballComp))
        {
            if (ballComp.ballType == BallType.Star)
                StarManager.Instance?.AddStar(1);
        }

        hit.collider.gameObject.SetActive(false);
        currentBall = null;

        speedDirection *= -1;
        currentSpeed = Mathf.Clamp(
            currentSpeed + (8f * (1000f / (1000f + currentSpeed))),
            100f,
            550f
        );

        StartCoroutine(SafeSpawn(0.15f));
    }

    private void ApplyPerfectEffects(Vector3 pos)
    {
        Camera.main.DOKill();
        Camera
            .main.DOFieldOfView(65f, 0.05f)
            .OnComplete(() => Camera.main.DOFieldOfView(60f, 0.15f));
        HitStop();
        UISoundManager.Instance?.PlayHandleSFX(comboCount);
        CreateFloatingText(pos, GetComboText(), ballGlowColor);
        PlayBallEffect(pos, ballGlowColor);
    }

    private void ApplyNormalHitEffects(Vector3 pos)
    {
        UISoundManager.Instance?.PlayHandleSFX(0);
        CreateFloatingText(pos, "+1", Color.white);
        PlayBallEffect(pos, Color.white);
    }

    public void HitStop()
    {
        Time.timeScale = 0.1f;
        DOVirtual
            .DelayedCall(
                0.05f,
                () =>
                {
                    if (!isGameOver)
                        Time.timeScale = 1f;
                }
            )
            .SetUpdate(true);
    }

    void CreateFloatingText(Vector3 pos, string text, Color color)
    {
        GameObject fText = ObjectPooler.Instance.SpawnFromPool(
            floatingTextTag,
            pos,
            Quaternion.identity
        );
        if (fText == null)
            return;

        fText.transform.SetParent(FindFirstObjectByType<Canvas>().transform, false);
        fText.transform.position = Camera.main.WorldToScreenPoint(pos);

        if (fText.TryGetComponent<TMP_Text>(out var tmp))
        {
            tmp.SetText(text);
            tmp.color = color;
        }
    }

    public void PlayBallEffect(Vector3 pos, Color ballColor)
    {
        GameObject effect = ObjectPooler.Instance.SpawnFromPool(
            ballParticleTag,
            pos,
            Quaternion.identity
        );
        if (effect != null && effect.TryGetComponent<ParticleSystem>(out var ps))
        {
            var main = ps.main;
            var emission = ps.emission;
            float intensity = Mathf.Clamp01(comboCount * 0.1f);
            main.startColor = Color.Lerp(ballColor, Color.white, intensity * 0.5f);
            short count = (short)Mathf.Clamp(15 + (comboCount * 3), 15, 45);
            emission.SetBurst(0, new ParticleSystem.Burst(0f, count));

            ps.Clear();
            ps.Play();
        }
    }

    void SpawnBallAction()
    {
        // Əgər əvvəlki top hələ də aktiv qalıbsa, onu söndür
        if (currentBall != null)
        {
            currentBall.SetActive(false);
        }

        Vector2 pos2D = Random.insideUnitCircle.normalized * radius;
        Vector3 spawnPos = target.transform.position + (Vector3)pos2D;

        string tagToSpawn = (Random.value < starBallChance) ? starBallTag : normalBallTag;
        currentBall = ObjectPooler.Instance.SpawnFromPool(
            tagToSpawn,
            spawnPos,
            Quaternion.identity
        );

        if (currentBall != null && currentBall.TryGetComponent<SpriteRenderer>(out var ballSr))
        {
            float comboFactor = Mathf.Clamp01(comboCount / 10f);
            Color lerpedColor = Color.Lerp(baseBallColor, ballGlowColor, comboFactor);
            ballSr.color = lerpedColor * (1f + (comboFactor * 0.7f));
            currentBall.transform.localScale =
                Vector3.one * (1f + (Mathf.Min(comboCount, 10) * 0.02f));
        }
    }

    IEnumerator SafeSpawn(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Əgər nəsə səbəbdən currentBall hələ də doludursa və ya
        // səhnədə "Ball" taglı aktiv obyekt varsa, yenisini çıxarma.
        if (currentBall == null || !currentBall.activeInHierarchy)
        {
            // Əlavə təhlükəsizlik: Səhnədə başqa aktiv top varmı?
            GameObject existingBall = GameObject.FindGameObjectWithTag(normalBallTag);
            if (existingBall == null || !existingBall.activeInHierarchy)
            {
                SpawnBallAction();
            }
        }
    }

    private void ResumeFromSecondChance()
    {
        secondChancePanel.SetActive(false);
        if (levelProgressPercentText != null)
            levelProgressPercentText.enabled = false;

        Time.timeScale = 1f;
        isSettingsOpen = false;
        isGameOver = false;

        GameObject[] balls = GameObject.FindGameObjectsWithTag(BallTagStr);
        for (int i = 0; i < balls.Length; i++)
            balls[i].SetActive(false);

        currentBall = null;
        StartCoroutine(SafeSpawn(0.2f));
    }

    IEnumerator GameOver()
    {
        if (isGameOver)
            yield break;
        isGameOver = true;
        LoseWiggle.Instance?.PlayLoseAnimation();
        UISoundManager.Instance?.PlayOverSFX();
        yield return new WaitForSecondsRealtime(0.75f);
        if (levelProgressPercentText != null && LevelManager.Instance != null)
        {
            levelProgressPercentText.enabled = true;
            levelProgressPercentText.SetText(
                "{0}% COMPLETE",
                LevelManager.Instance.GetLevelProgressValue()
            );
        }
        if (secondChancePanel != null)
        {
            Time.timeScale = 0f;
            isSettingsOpen = true;
            if (secondChancePanel.TryGetComponent<SecondChanceTimer>(out var timer))
                timer.canStart = true;
            secondChancePanel.SetActive(true);
        }
        else
            GameOverPopUp();
    }

    public void CloseSecondChanceAndShowGameOver()
    {
        secondChancePanel?.SetActive(false);
        GameOverPopUp();
    }

    public void RestartLevel(string sceneName)
    {
        Time.timeScale = 1f;
        isSettingsOpen = false;
        isGameOver = false;
        TransitionManager.Instance?.LoadLevel(sceneName);
    }

    private void GameOverPopUp()
    {
        gameoverPOPUP.SetActive(true);
        if (canvasGroup != null)
            canvasGroup.alpha = 1;
        ResetButtons();
        AnimateButtonsSequentially();
        UISoundManager.Instance?.PlaySceneSFX();
        StartCoroutine(ShowGiftAfterGameOver());
        StartCoroutine(FreezeTimeDelayed());
    }

    void AnimateButtonsSequentially()
    {
        Button[] buttons = { resStartBTN, menuBTN };
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;
            RectTransform rt = buttons[i].GetComponent<RectTransform>();
            CanvasGroup cg =
                buttons[i].GetComponent<CanvasGroup>()
                ?? buttons[i].gameObject.AddComponent<CanvasGroup>();
            Vector2 finalPos = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(finalPos.x, finalPos.y - 150f);
            rt.localScale = Vector3.zero;
            cg.alpha = 0f;
            float delay = i * 0.1f;
            rt.DOAnchorPos(finalPos, 0.6f).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true);
            rt.DOScale(Vector3.one, 0.5f).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true);
            cg.DOFade(1f, 0.4f).SetDelay(delay).SetUpdate(true);
        }
    }

    IEnumerator ShowGiftAfterGameOver()
    {
        yield return new WaitForSecondsRealtime(0.35f);
        TaskManager.Instance?.CheckGiftStatus();
        MissionManager.Instance?.OpenPanel();
    }

    void ResetButtons()
    {
        Button[] buttons = { resStartBTN, menuBTN, skinsButton };
        foreach (var btn in buttons)
        {
            if (btn == null)
                continue;
            btn.transform.localScale = Vector3.zero;
            if (btn.TryGetComponent<CanvasGroup>(out var cg))
                cg.alpha = 0f;
        }
    }

    public void OpenSkinsMenu()
    {
        isSettingsOpen = true;
        Time.timeScale = 0f;
        SkinsManager.Instance?.OpenSkins();
    }

    public void HandleContinueButton()
    {
        if (LifeManager.Instance != null && LifeManager.Instance.currentLives > 0)
        {
            LifeManager.Instance.SpendLife();
            ResumeFromSecondChance();
        }
        else if (StarManager.Instance != null && StarManager.Instance.SpendStars(50))
        {
            LifeManager.Instance?.AddLives(5);
            LifeManager.Instance?.SpendLife();
            ResumeFromSecondChance();
        }
    }

    IEnumerator FreezeTimeDelayed()
    {
        yield return new WaitForSecondsRealtime(animDuration);
        Time.timeScale = 0f;
        isSettingsOpen = true;
    }

    string GetComboText()
    {
        if (comboCount >= 10)
            return "ULTIMATE!!";
        if (comboCount >= 5)
            return "UNSTOPPABLE!";
        if (comboCount >= 3)
            return "AMAZING!";
        return "PERFECT!";
    }
}
