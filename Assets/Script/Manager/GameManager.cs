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

    [Header("Objects")]
    public GameObject target;
    public GameObject player;
    public GameObject ballParticlePrefab;
    public GameObject floatingTextPrefab;

    [Header("Spawning")]
    public GameObject normalBallPrefab;
    public GameObject starBallPrefab;
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

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Instance = this;

        // PROBLEM 1 HƏLLİ: Progress Text oyun başlayanda bağlı olmalıdır
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
        // İlk topu təhlükəsiz şəkildə yarat
        StartCoroutine(SafeSpawn(0.1f));
    }

    void SetupButtons()
    {
        resStartBTN.onClick.AddListener(() => RestartLevel("Game"));
        menuBTN.onClick.AddListener(() => RestartLevel("Menu"));
        skinsButton.onClick.AddListener(OpenSkinsMenu);
        noThanksBtn?.onClick.AddListener(CloseSecondChanceAndShowGameOver);
        useStarBtn.onClick.AddListener(HandleContinueButton);
    }

    private void Update()
    {
        if (isSettingsOpen || Time.timeScale == 0 || target == null || isGameOver)
            return;

        transform.RotateAround(
            target.transform.position,
            speedDirection,
            currentSpeed * Time.deltaTime
        );

        if (Input.GetMouseButtonDown(0))
        {
            HandleShoot();
        }
    }

    void ApplyGlobalColors()
    {
        if (player != null)
        {
            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = playerGlowColor;
        }
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
        {
            ProcessHit(hit);
        }
        else
        {
            StartCoroutine(GameOver());
        }
    }

    void ProcessHit(RaycastHit2D hit)
    {
        Vector3 hitPos = hit.collider.transform.position;
        float distToCenter = Vector2.Distance(hit.point, hitPos);
        bool isPerfect = distToCenter < 0.18f;

        if (isPerfect)
        {
            comboCount++;
            Camera.main.DOKill();
            Camera
                .main.DOFieldOfView(65f, 0.05f)
                .OnComplete(() => Camera.main.DOFieldOfView(60f, 0.15f));
            HitStop();
            UISoundManager.Instance?.PlayHandleSFX(comboCount);
            CreateFloatingText(hitPos, GetComboText(), ballGlowColor, comboCount);
            PlayBallEffect(hitPos, ballGlowColor);
            LevelManager.Instance?.AddProgress(2);
        }
        else
        {
            comboCount = 0;
            UISoundManager.Instance?.PlayHandleSFX(0);
            CreateFloatingText(hitPos, "+1", Color.white);
            PlayBallEffect(hitPos, Color.white);
            LevelManager.Instance?.AddProgress(1);
        }

        if (hit.collider.TryGetComponent<Ball>(out Ball ball))
        {
            if (ball.ballType == BallType.Star)
                StarManager.Instance?.AddStar(1);
        }

        // PROBLEM 2 HƏLLİ: Top dərhal məhv edilir və referans sıfırlanır
        Destroy(hit.collider.gameObject);
        currentBall = null;

        speedDirection *= -1;
        currentSpeed = Mathf.Clamp(
            currentSpeed + (8f * (1000f / (1000f + currentSpeed))),
            100f,
            550f
        );

        // SafeSpawn istifadə edərək ikiqat yaranmanın qarşısını alırıq
        StartCoroutine(SafeSpawn(0.05f));
    }

    public void HitStop()
    {
        Time.timeScale = 0.1f;
        DOVirtual.DelayedCall(0.05f, () => Time.timeScale = 1f).SetUpdate(true);
    }

    void CreateFloatingText(Vector3 pos, string text, Color color, int combo = 0)
    {
        if (floatingTextPrefab == null)
            return;

        GameObject fText = Instantiate(floatingTextPrefab, pos, Quaternion.identity);
        fText.transform.SetParent(FindFirstObjectByType<Canvas>().transform, false);
        fText.transform.position = Camera.main.WorldToScreenPoint(pos);

        TMP_Text tmp = fText.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            if (text == "PERFECT!")
            {
                if (combo >= 10)
                    text = "ULTIMATE!!";
                else if (combo >= 5)
                    text = "UNSTOPPABLE!";
                else if (combo >= 3)
                    text = "AMAZING!";
            }
            tmp.text = text;
            tmp.color = color;
        }

        // LeanTween əvəzinə Coroutine başlat
        StartCoroutine(AnimateFloatingText(fText));
    }

    IEnumerator AnimateFloatingText(GameObject obj)
    {
        float duration = 0.8f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;
        Vector3 startPos = obj.transform.position;
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();

        while (elapsed < duration)
        {
            // Zaman dayansa belə animasiya işləsin deyə 'unscaledDeltaTime'
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;

            // 1. Pop/Böyümə effekti (ilk 30%-də sürətlə böyüyür)
            if (progress < 0.3f)
                obj.transform.localScale = Vector3.Lerp(
                    startScale,
                    targetScale * 1.3f,
                    progress / 0.3f
                );
            else
                obj.transform.localScale = Vector3.Lerp(
                    targetScale * 1.3f,
                    targetScale,
                    (progress - 0.3f) / 0.7f
                );

            // 2. Yuxarı sürüşmə
            obj.transform.position = startPos + new Vector3(0, progress * 120f, 0);

            // 3. Şəffaflaşma (CanvasGroup lazımdır)
            if (cg != null)
                cg.alpha = 1f - Mathf.Pow(progress, 2); // Sona doğru daha sürətli itir

            yield return null;
        }
        Destroy(obj);
    }

    IEnumerator SafeSpawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Həm referans, həm də səhnədəki fiziki obyekt yoxlanılır
        if (currentBall == null && GameObject.FindGameObjectsWithTag("Ball").Length == 0)
        {
            SpawnBallAction();
        }
    }

    void SpawnBallAction()
    {
        Vector2 pos2D = Random.insideUnitCircle.normalized * radius;
        Vector3 spawnPos = target.transform.position + (Vector3)pos2D;

        GameObject prefab = (Random.value < starBallChance) ? starBallPrefab : normalBallPrefab;
        currentBall = Instantiate(prefab, spawnPos, Quaternion.identity);

        if (currentBall.TryGetComponent<SpriteRenderer>(out SpriteRenderer ballSr))
        {
            float comboFactor = Mathf.Clamp01(comboCount / 10f);
            float intensity = 1f + (comboFactor * 0.7f);
            Color lerpedColor = Color.Lerp(baseBallColor, ballGlowColor, comboFactor);
            ballSr.color = lerpedColor * intensity;
        }
        currentBall.transform.localScale = Vector3.one * (1f + (Mathf.Min(comboCount, 10) * 0.02f));
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

    IEnumerator GameOver()
    {
        if (isGameOver)
            yield break;
        isGameOver = true;

        LoseWiggle.Instance?.PlayLoseAnimation();
        UISoundManager.Instance?.PlayOverSFX();
        CameraShake.Instance?.ShakeCamera(1.1f, 0.5f);

        yield return new WaitForSecondsRealtime(0.75f);

        // Progress Text-i yalnız GameOver-da aktivləşdiririk
        if (levelProgressPercentText != null && LevelManager.Instance != null)
        {
            levelProgressPercentText.enabled = true;
            levelProgressPercentText.text =
                LevelManager.Instance.GetCurrentLevelPercentage() + " COMPLETE";
        }

        if (secondChancePanel != null)
        {
            Time.timeScale = 0f;
            isSettingsOpen = true;
            SecondChanceTimer timer = secondChancePanel.GetComponent<SecondChanceTimer>();
            if (timer != null)
                timer.canStart = true;
            secondChancePanel.SetActive(true);
        }
        else
        {
            GameOverPopUp();
        }
    }

    // GameManager.cs daxilində belə olmalıdır:
    public void CloseSecondChanceAndShowGameOver()
    {
        if (secondChancePanel != null)
            secondChancePanel.SetActive(false);
        GameOverPopUp();
    }

    public void ContinueGame(bool useLife)
    {
        if (useLife)
        {
            // Əgər oyunçunun ehtiyatda canı (Life) varsa
            if (LifeManager.Instance != null && LifeManager.Instance.SpendLife())
            {
                ResumeFromSecondChance();
            }
            else
            {
                // Canı yoxdursa, ulduzla almaq təklifi çıxa bilər (opsional)
            }
        }
    }

    // Oyunu qaldığı yerdən davam etdirən köməkçi funksiya
    private void ResumeFromSecondChance()
    {
        secondChancePanel.SetActive(false);
        if (levelProgressPercentText != null)
            levelProgressPercentText.enabled = false;
        Time.timeScale = 1f;
        isSettingsOpen = false;
        isGameOver = false;

        foreach (GameObject b in GameObject.FindGameObjectsWithTag("Ball"))
            Destroy(b);

        currentBall = null;
        StartCoroutine(SafeSpawn(0.2f));
    }

    public void RestartLevel(string sceneName)
    {
        Time.timeScale = 1f;
        isSettingsOpen = false;
        isGameOver = false;
        MaskTransitions.TransitionManager.Instance.LoadLevel(sceneName);
    }

    public void PlayBallEffect(Vector3 pos, Color ballColor)
    {
        if (ballParticlePrefab == null)
            return;

        GameObject effect = Instantiate(ballParticlePrefab, pos, Quaternion.identity);
        ParticleSystem ps = effect.GetComponent<ParticleSystem>();

        if (ps != null)
        {
            var main = ps.main;
            var emission = ps.emission;
            var trails = ps.trails; // Əgər trail istifadə edirsənsə

            // 1. Rəng və Parlaqlıq (Combo artdıqca HDR effektini simulyasiya edirik)
            // Combo artdıqca rəng daha ağappaq (parlaq) olur
            float intensity = Mathf.Clamp01(comboCount * 0.1f);
            Color finalColor = Color.Lerp(ballColor, Color.white, intensity * 0.5f);
            main.startColor = finalColor;

            // 2. Sayı (Həddi aşmadan artırırıq: 15-dən 40-a qədər)
            short count = (short)Mathf.Clamp(15 + (comboCount * 3), 15, 45);
            emission.SetBurst(0, new ParticleSystem.Burst(0f, count));

            // 3. Ömür və Sürət (Daha qısa ömür = daha kəskin vuruş hissi)
            // Hissəciklər sürətlə çıxıb tez yox olduqda "impact" daha güclü hiss olunur
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                8f + (comboCount * 0.4f),
                12f + (comboCount * 0.4f)
            );

            // 4. Ölçü (Kiçik hissəciklər həmişə daha premium görünür)
            float sizeFactor = Mathf.Clamp(0.15f + (comboCount * 0.01f), 0.15f, 0.3f);
            main.startSize = sizeFactor;

            ps.Play();
        }

        Destroy(effect, 1f);

        // 5. Peşəkar toxunuş: Kamera Titrəməsi (Screen Shake)
        // Combo artdıqca titrəməni də cüzi artıraq
        if (CameraShake.Instance != null)
        {
            float shakeAmount = 0.1f + (Mathf.Clamp(comboCount, 0, 10) * 0.02f);
            CameraShake.Instance.ShakeCamera(shakeAmount, 0.15f);
        }
    }

    private void OnValidate()
    {
        if (player != null)
        {
            SpriteRenderer playerSr = player.GetComponent<SpriteRenderer>();
            if (playerSr != null)
                playerSr.color = playerGlowColor;
        }
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
        // Animasiya olunacaq düymələr
        Button[] buttons = { resStartBTN, menuBTN, skinsButton };

        float delayBetweenButtons = 0.1f; // Düymələr arası gecikmə
        float slideAmount = 150f; // Nə qədər aşağıdan gəlsinlər

        for (int i = 0; i < buttons.Length; i++)
        {
            Button btn = buttons[i];
            if (btn == null)
                continue;

            RectTransform rt = btn.GetComponent<RectTransform>();
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = btn.gameObject.AddComponent<CanvasGroup>();

            // 1. İLKIN VƏZİYYƏTƏ GƏTİR
            // Əvvəlki animasiyaları dayandır
            rt.DOKill();
            cg.DOKill();

            // Final mövqeyi yadda saxla (əgər düymə yerindədirsə)
            Vector3 finalPos = rt.anchoredPosition;

            // Düyməni aşağı sürüşdür, şəffaflaşdır və kiçilt
            rt.anchoredPosition = new Vector2(finalPos.x, finalPos.y - slideAmount);
            rt.localScale = Vector3.zero;
            cg.alpha = 0f;

            float delay = i * delayBetweenButtons;

            // 2. ANİMASİYA BAŞLASIN
            // Yuxarı sürüşmə (Ease.OutBack düymənin yerinə "atılaraq" gəlməsini təmin edir)
            rt.DOAnchorPos(finalPos, 0.6f).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true);

            // Böyümə (Scale)
            rt.DOScale(Vector3.one, 0.5f).SetDelay(delay).SetEase(Ease.OutBack).SetUpdate(true);

            // Görünmə (Fade)
            cg.DOFade(1f, 0.4f).SetDelay(delay).SetUpdate(true);
        }
    }

    IEnumerator ShowGiftAfterGameOver()
    {
        // GameOver animasiyası (0.25s) bitənə qədər gözlə
        yield return new WaitForSecondsRealtime(animDuration + 0.1f);

        // 1. Öncə Free Gift (Hədiyyə) statusunu yoxla və aç
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.CheckGiftStatus();
        }

        // 2. Bir az fasilə ver ki, panellər üst-üstə minməsin (opsional)
        yield return new WaitForSecondsRealtime(0.2f);

        // 3. Missiya panelini aç
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OpenPanel();
        }
    }

    void ResetButtons()
    {
        Button[] buttons = { resStartBTN, menuBTN, skinsButton };
        foreach (var btn in buttons)
        {
            if (btn == null)
                continue;

            btn.transform.DOKill();

            // Scale-i dərhal 0 et
            btn.transform.localScale = Vector3.zero;

            // CanvasGroup varsa dərhal alpha-nı 0 et
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = btn.gameObject.AddComponent<CanvasGroup>();
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
            LifeManager.Instance.AddLives(5);
            LifeManager.Instance.SpendLife();
            ResumeFromSecondChance();
        }
    }

    public int GetComboCount()
    {
        return comboCount;
    }

    IEnumerator FreezeTimeDelayed()
    {
        yield return new WaitForSecondsRealtime(animDuration);
        Time.timeScale = 0f;
        isSettingsOpen = true;
    }
}
