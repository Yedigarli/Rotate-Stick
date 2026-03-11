using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Speeds")]
    public float FirstSpeed = 180f;
    public float currentSpeed;
    private Vector3 speedDirection = Vector3.forward;

    [Header("Speed Tuning")]
    public float minSpeed = 120f;
    public float maxSpeed = 360f;
    public float acceleration = 4.5f;

    [Header("Raycast Settings")]
    public float raycastDistance = 10f;
    public LayerMask detectableLayers;

    [Header("Pooling Tags")]
    public string normalBallTag = "Ball";
    public string starBallTag = "StarBall";
    public string ballParticleTag = "BallParticle";

    [Header("Objects References")]
    public GameObject target;
    public GameObject player;

    [Header("Spawning")]
    public float radius = 1.3f;
    public float starBallChance = 0.08f;

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
    public Button resStartBTN, menuBTN, skinsButton, useStarBtn, noThanksBtn;

    [Header("Colors (HDR)")]
    [ColorUsage(true, true)]
    public Color ballGlowColor = Color.white;

    [ColorUsage(true, true)]
    public Color baseBallColor = Color.white;

    [ColorUsage(true, true)]
    public Color playerGlowColor = Color.cyan;

    private float animDuration = 0.25f;

    [Header("Optimized References")]
    private Canvas mainCanvas;

    [Header("Floating Text")]
    public bool enableFloatingText = false;
    private float _defaultFixedDeltaTime;

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;

        _defaultFixedDeltaTime = Time.fixedDeltaTime;
        mainCanvas = FindFirstObjectByType<Canvas>();

        if (levelProgressPercentText != null)
            levelProgressPercentText.enabled = false;
        if (gameoverPOPUP != null)
            gameoverPOPUP.SetActive(false);
        if (secondChancePanel != null)
            secondChancePanel.SetActive(false);

        FirstSpeed = Mathf.Clamp(
            PlayerPrefs.GetFloat("firstspeed", FirstSpeed),
            minSpeed,
            maxSpeed * 0.9f
        );
        currentSpeed = FirstSpeed;
        starBallChance = Mathf.Clamp(starBallChance, 0.03f, 0.12f);

        SetupButtons();
    }

    private void Start()
    {
        if (target != null)
        {
            target.transform.localScale = Vector3.zero;
            target.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }

        if (mainCanvas == null)
            mainCanvas = FindFirstObjectByType<Canvas>();

        StartCoroutine(SafeSpawn(0.2f));
    }

    private void Update()
    {
        if (isGameOver || isSettingsOpen || Time.timeScale <= 0f || target == null)
            return;

        float speedLerp = Mathf.Lerp(currentSpeed, FirstSpeed, 0.08f * Time.deltaTime);
        currentSpeed = Mathf.Clamp(speedLerp, minSpeed, maxSpeed);

        float step = currentSpeed * Time.deltaTime;
        transform.RotateAround(target.transform.position, speedDirection, step);

        if (Input.GetMouseButtonDown(0))
            HandleShoot();
    }

    private void SetupButtons()
    {
        resStartBTN?.onClick.AddListener(() => RestartLevel("Game"));
        menuBTN?.onClick.AddListener(() => RestartLevel("Menu"));
    }

    private void HandleShoot()
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
        {
            LevelManager.Instance?.ShowStatus("MISS!", Color.red);
            StartCoroutine(GameOver());
        }
    }

    private void ProcessHit(RaycastHit2D hit)
    {
        Vector3 hitPos = hit.collider.transform.position;
        float distToCenter = Vector2.Distance(hit.point, hitPos);
        bool isPerfect = distToCenter < 0.18f;

        if (isPerfect)
        {
            comboCount++;
            ApplyPerfectEffects(hitPos);
            LevelManager.Instance?.AddProgress(1);
            LevelManager.Instance?.ShowStatusByType("Perfect", comboCount);
            MissionManager.Instance?.AddPerfect();
            MissionManager.Instance?.AddScore(1);
        }
        else
        {
            comboCount = 0;
            ApplyNormalHitEffects(hitPos);
            LevelManager.Instance?.AddProgress(1);
            LevelManager.Instance?.ShowStatusByType("Nice");
            MissionManager.Instance?.AddScore(1);
        }

        if (
            hit.collider.TryGetComponent<Ball>(out Ball ballComp)
            && ballComp.ballType == BallType.Star
        )
            StarManager.Instance?.AddStar(1);

        hit.collider.gameObject.SetActive(false);
        currentBall = null;

        speedDirection *= -1f;
        float accel = Mathf.Lerp(acceleration, 1.25f, currentSpeed / maxSpeed);
        currentSpeed = Mathf.Clamp(currentSpeed + accel, minSpeed, maxSpeed);

        UISoundManager.Instance?.UpdateMusicPitch(currentSpeed, FirstSpeed);
        StartCoroutine(SafeSpawn(0.12f));
    }

    private void ApplyPerfectEffects(Vector3 pos)
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.DOKill();
            cam.DOFieldOfView(63f, 0.06f)
                .OnComplete(() =>
                {
                    if (cam != null)
                        cam.DOFieldOfView(60f, 0.12f);
                });
        }

        HitStop();
        UISoundManager.Instance?.PlayHandleSFX(comboCount);
        PlayBallEffect(pos, ballGlowColor);
    }

    private void ApplyNormalHitEffects(Vector3 pos)
    {
        UISoundManager.Instance?.PlayHandleSFX(0);
        PlayBallEffect(pos, Color.white);
    }

    public void HitStop()
    {
        Time.timeScale = 0.15f;
        DOVirtual
            .DelayedCall(
                0.035f,
                () =>
                {
                    if (!isGameOver)
                        Time.timeScale = 1f;
                }
            )
            .SetUpdate(true);
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

    private void SpawnBallAction()
    {
        if (currentBall != null)
            currentBall.SetActive(false);

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

    private IEnumerator SafeSpawn(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentBall == null || !currentBall.activeInHierarchy)
        {
            bool hasActiveNormal =
                ObjectPooler.Instance != null
                && ObjectPooler.Instance.HasActiveObject(normalBallTag);
            bool hasActiveStar =
                ObjectPooler.Instance != null && ObjectPooler.Instance.HasActiveObject(starBallTag);

            if (!hasActiveNormal && !hasActiveStar)
                SpawnBallAction();
        }
    }

    private void ResumeFromSecondChance()
    {
        secondChancePanel.SetActive(false);
        if (levelProgressPercentText != null)
            levelProgressPercentText.enabled = false;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = _defaultFixedDeltaTime;
        isSettingsOpen = false;
        isGameOver = false;

        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.DeactivateAll(normalBallTag);
            ObjectPooler.Instance.DeactivateAll(starBallTag);
        }

        currentBall = null;
        StartCoroutine(SafeSpawn(0.2f));
    }

    private IEnumerator GameOver()
    {
        if (isGameOver)
            yield break;

        isGameOver = true;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = _defaultFixedDeltaTime;

        Camera cam = Camera.main;
        if (cam != null)
            cam.DOFieldOfView(52f, 0.18f).SetUpdate(true).SetEase(Ease.OutQuad);

        LoseWiggle.Instance?.PlayLoseAnimation();
        UISoundManager.Instance?.PlayOverSFX();

        yield return new WaitForSecondsRealtime(0.55f);
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
        Time.fixedDeltaTime = _defaultFixedDeltaTime;
        isSettingsOpen = false;
        isGameOver = false;
        TransitionManager.Instance?.LoadLevel(sceneName);
    }

    private void GameOverPopUp()
    {
        gameoverPOPUP.SetActive(true);
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (levelProgressPercentText != null)
        {
            string percent =
                LevelManager.Instance != null
                    ? LevelManager.Instance.GetCurrentLevelPercentage()
                    : "0";

            levelProgressPercentText.enabled = true;
            levelProgressPercentText.text = percent + "%";
            if (int.TryParse(percent, out int p))
                PlayerPrefs.SetInt("LastRunPercent", p);
            levelProgressPercentText.rectTransform.DOKill();
            levelProgressPercentText.rectTransform.localScale = Vector3.one * 0.85f;
            levelProgressPercentText
                .rectTransform.DOScale(Vector3.one, 0.22f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        ResetButtons();
        AnimateButtonsSequentially();
        UISoundManager.Instance?.PlaySceneSFX();
        StartCoroutine(ShowGiftAfterGameOver());
        StartCoroutine(FreezeTimeDelayed());
    }

    private void AnimateButtonsSequentially()
    {
        Button[] buttons = { resStartBTN, menuBTN };
        RectTransform[] rects = new RectTransform[buttons.Length];
        Vector2[] finalPositions = new Vector2[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            rects[i] = buttons[i].GetComponent<RectTransform>();
            if (rects[i] != null)
                finalPositions[i] = rects[i].anchoredPosition;
        }

        if (rects[0] != null
            && rects[1] != null
            && Mathf.Abs(finalPositions[0].x - finalPositions[1].x) < 20f)
        {
            float y = finalPositions[0].y;
            finalPositions[0] = new Vector2(-170f, y);
            finalPositions[1] = new Vector2(170f, y);
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null || rects[i] == null)
                continue;

            RectTransform rt = rects[i];
            CanvasGroup cg = buttons[i].GetComponent<CanvasGroup>()
                ?? buttons[i].gameObject.AddComponent<CanvasGroup>();

            rt.DOKill();
            cg.DOKill();

            Vector2 finalPos = finalPositions[i];
            rt.anchoredPosition = new Vector2(finalPos.x, finalPos.y - 180f);
            rt.localScale = Vector3.one * 0.75f;
            rt.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? -6f : 6f);
            cg.alpha = 0f;

            float delay = i * 0.08f;
            rt.DOAnchorPos(finalPos, 0.5f).SetDelay(delay).SetEase(Ease.OutCubic).SetUpdate(true);
            rt.DOScale(Vector3.one, 0.42f).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true);
            rt.DORotate(Vector3.zero, 0.42f).SetDelay(delay).SetEase(Ease.OutCubic).SetUpdate(true);
            cg.DOFade(1f, 0.3f).SetDelay(delay).SetUpdate(true);
        }
    }

    private IEnumerator ShowGiftAfterGameOver()
    {
        yield return new WaitForSecondsRealtime(0.35f);
        MissionManager.Instance?.OpenPanel();
    }

    private void ResetButtons()
    {
        Button[] buttons = { resStartBTN, menuBTN };
        for (int i = 0; i < buttons.Length; i++)
        {
            Button btn = buttons[i];
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
        Time.fixedDeltaTime = _defaultFixedDeltaTime;
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

    private IEnumerator FreezeTimeDelayed()
    {
        yield return new WaitForSecondsRealtime(animDuration);
        Time.fixedDeltaTime = _defaultFixedDeltaTime;
        Time.timeScale = 0f;
        isSettingsOpen = true;
    }

    private string GetComboText()
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







