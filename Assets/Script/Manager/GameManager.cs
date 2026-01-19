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

    [Header("Second Chance Setup")]
    public GameObject secondChancePanel;
    public Button noThanksBtn;
    public Button useStarBtn; // 10 ulduzla davam etmək üçün

    [Header("GameOver")]
    public GameObject gameoverPOPUP;
    public Button resStartBTN;
    public Button settingBTN;
    public Button instaBTN;
    public Button skinsButton;
    public Button menuBTN;
    public float animDuration = 0.25f;
    private bool isAnimating;
    public CanvasGroup canvasGroup;
    public AnimationCurve popupCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Skins Panel Setup")]
    public GameObject skinsUIObject; // Bura Hierarchy-dəki SkinsUI-ni atacaqsan

    // ⭐ Topun HDR rəngi üçün dəyişən
    [ColorUsage(showAlpha: true, hdr: true)]
    public Color ballGlowColor = Color.white;

    [Header("Combo Glow Settings")]
    [ColorUsage(showAlpha: true, hdr: true)]
    public Color baseBallColor = Color.white; // Combo 0 olanda topun rəngi

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color playerGlowColor = Color.cyan; // Çubuğun parıltı rəngi

    [Header("GameOver UI")]
    public TMP_Text levelProgressPercentText; // Inspector-da bura yeni yaratdığın mətni at
    private Coroutine freezeCoroutine;


    private void Awake()
    {
        Instance = this;
        ApplyGlobalColors();

        levelProgressPercentText.enabled = false;

        if (gameoverPOPUP != null)
            gameoverPOPUP.SetActive(false);
        if (canvasGroup != null)
            canvasGroup.alpha = 0;

        FirstSpeed = PlayerPrefs.GetFloat("firstspeed", FirstSpeed);
        currentSpeed = FirstSpeed;
        Invoke(nameof(SpawnBall), 0.1f);

        // --- DÜYMƏLƏRİN FUNKSİYALARI ---
        resStartBTN.onClick.AddListener(() =>
 {
     if (freezeCoroutine != null)
         StopCoroutine(freezeCoroutine);

     Time.timeScale = 1f;
     isSettingsOpen = false;
     TransitionManager.Instance.LoadLevel("Game");
 });

        menuBTN.onClick.AddListener(() =>
        {
            if (freezeCoroutine != null)
                StopCoroutine(freezeCoroutine);

            Time.timeScale = 1f;
            isSettingsOpen = false;
            TransitionManager.Instance.LoadLevel("Menu");
        });


        // Skins düyməsinə basanda menyunu aç
        skinsButton.onClick.AddListener(() =>
        {
            OpenSkinsMenu();
        });

        // No Thanks düyməsi basılanda birbaşa GameOver-ə keç
        if (noThanksBtn != null)
            noThanksBtn.onClick.AddListener(() =>
            {
                CloseSecondChanceAndShowGameOver();
            });

        useStarBtn.onClick.AddListener(() =>
        {
            HandleContinueButton();
        });
    }

    private void Update()
    {
        if (isSettingsOpen || Time.timeScale == 0)
            return;

        if (target != null)
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
        // Çubuğun rəngini parlat
        if (player != null)
        {
            SpriteRenderer playerSr = player.GetComponent<SpriteRenderer>();
            if (playerSr != null)
                playerSr.color = playerGlowColor;
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
            Ball ball = hit.collider.GetComponent<Ball>();
            Vector3 hitPos = hit.collider.transform.position;
            float distToCenter = Vector2.Distance(hit.point, hitPos);
            bool isPerfect = distToCenter < 0.18f;

            if (isPerfect)
            {
                comboCount++;

                // 🔹 HƏZZ: Kamera effekti
                Camera.main.DOFieldOfView(65f, 0.1f).OnComplete(() => Camera.main.DOFieldOfView(60f, 0.2f));
                HitStop();

                // 🔹 HƏZZ: Kombo ilə artan səs tonu və Yüngül Vibrasiya
                UISoundManager.Instance?.PlayHandleSFX(comboCount);

                // 🔹 HƏZZ: Musiqinin sürətlənməsi
                UISoundManager.Instance?.UpdateMusicPitch(currentSpeed, FirstSpeed);

                CreateFloatingText(hitPos, "PERFECT!", ballGlowColor, comboCount);
                PlayBallEffect(hitPos, ballGlowColor);
                LevelManager.Instance.AddProgress(2);
            }
            else
            {
                comboCount = 0;
                // Normal vuruşda vibrasiyasız və ya daha sadə səs
                UISoundManager.Instance?.PlayHandleSFX(0);
                CreateFloatingText(hitPos, "+1", Color.white);
                PlayBallEffect(hitPos, Color.white);
                LevelManager.Instance.AddProgress(1);
            }

            if (ball != null && ball.ballType == BallType.Star)
            {
                if (StarManager.Instance != null) StarManager.Instance.AddStar(1);
            }

            Destroy(hit.collider.gameObject);
            currentBall = null;

            speedDirection *= -1;
            // Sürəti artırırıq
            currentSpeed += 5f / (1f + (currentSpeed * 0.01f));
            Invoke(nameof(SpawnBall), 0.05f);
        }
        else
        {
            StartCoroutine(GameOver());
        }
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

    void SpawnBall()
    {
        if (currentBall != null || GameObject.FindGameObjectsWithTag("Ball").Length > 0)
            return;

        // ... (Spawn mövqeyi kodları dəyişmir) ...
        Vector2 pos2D = Random.insideUnitCircle.normalized * radius;
        Vector3 spawnPos = new Vector3(
            target.transform.position.x + pos2D.x,
            target.transform.position.y + pos2D.y,
            0f
        );
        GameObject prefabToSpawn =
            (Random.value < starBallChance) ? starBallPrefab : normalBallPrefab;
        currentBall = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        SpriteRenderer ballSr = currentBall.GetComponent<SpriteRenderer>();
        if (ballSr != null)
        {
            if (LevelManager.Instance != null)
            {
                // LevelManager-dəki mövcud rəngi birbaşa götürürük
                baseBallColor = LevelManager.Instance.levelColors[
                    (PlayerPrefs.GetInt("level", 1) - 1) % LevelManager.Instance.levelColors.Length
                ];
            }
            float comboFactor = Mathf.Clamp01(comboCount / 10f);

            // 2. Rəng keçidi (Lerp)
            Color lerpedColor = Color.Lerp(baseBallColor, ballGlowColor, comboFactor);

            // 3. PARLAMA GÜCÜ (Intensity) - Əsas sirr buradadır!
            // Combo artdıqca rəngi 2-yə, 4-ə vuraraq onu HDR-da partladırıq
            // Ekspozisiya (Exposure) artımı simulyasiyası:
            float intensity = 1f + (comboFactor * 0.7f);

            // HDR rəngini formalaşdırırıq
            Color finalGlowColor = new Color(
                lerpedColor.r * intensity,
                lerpedColor.g * intensity,
                lerpedColor.b * intensity,
                lerpedColor.a
            );

            ballSr.color = finalGlowColor;
        }

        // Combo artdıqca top cüzi böyüyür (1.0-dan 1.2-yə qədər)
        float scaleFactor = 1f + (Mathf.Clamp(comboCount, 0, 10) * 0.02f);
        currentBall.transform.localScale = Vector3.one * scaleFactor;

        Ball ballScript = currentBall.GetComponent<Ball>();
        if (PlayerPrefs.GetInt("level", 1) > 5 && Random.value < 0.3f)
        {
            ballScript.isMoving = true;
            ballScript.moveSpeed = Random.Range(2f, 4f);
        }
    }

    IEnumerator GameOver()
    {
        LoseWiggle.Instance.PlayLoseAnimation();
        UISoundManager.Instance?.PlayOverSFX();
        CameraShake.Instance.ShakeCamera(1.1f, 0.5f);

        yield return new WaitForSecondsRealtime(0.75f);

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

            // ⭐ BURANI DİQQƏTLƏ YENİLƏ:
            SecondChanceTimer timerScript = secondChancePanel.GetComponent<SecondChanceTimer>();
            if (timerScript != null)
            {
                timerScript.canStart = true; // İcazəni ver
                secondChancePanel.SetActive(true); // Paneli aç (OnEnable tetiklenecek)
            }
        }
        else
        {
            GameOverPopUp();
        }
    }

    // GameManager.cs daxilində belə olmalıdır:
    public void CloseSecondChanceAndShowGameOver()
    {
        // Paneli burada söndürürük ki, skript işini tamamlamış olsun
        if (secondChancePanel != null)
            secondChancePanel.SetActive(false);

        // Əsas GameOver popup-ını açırıq
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
        levelProgressPercentText.enabled = false;
        Time.timeScale = 1f;
        isSettingsOpen = false;

        // Mövcud topları təmizləyib yenisini yaradırıq ki, çətinlik olmasın
        foreach (GameObject b in GameObject.FindGameObjectsWithTag("Ball"))
            Destroy(b);

        Invoke(nameof(SpawnBall), 0.2f);
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
        if (isAnimating)
            return;

        ResetButtons();
        
        gameoverPOPUP.SetActive(true); // Paneli açırıq

        StartCoroutine(Animate(true)); // Panelin öz animasiyası
        UISoundManager.Instance?.PlaySceneSFX();
        StartCoroutine(ShowGiftAfterGameOver());
        freezeCoroutine = StartCoroutine(FreezeTimeDelayed());
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
            if (btn == null) continue;

            RectTransform rt = btn.GetComponent<RectTransform>();
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();

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
            rt.DOAnchorPos(finalPos, 0.6f)
                .SetDelay(delay)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);

            // Böyümə (Scale)
            rt.DOScale(Vector3.one, 0.5f)
                .SetDelay(delay)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);

            // Görünmə (Fade)
            cg.DOFade(1f, 0.4f)
                .SetDelay(delay)
                .SetUpdate(true);
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

    IEnumerator Animate(bool open)
    {
        isAnimating = true;
        float t = 0f;

        float startA = open ? 0 : 1;
        float endA = open ? 1 : 0;

        // Popup kiçikdən böyüyə doğru açılsın
        Vector3 startS = open ? Vector3.one * 0.5f : Vector3.one;
        Vector3 endS = open ? Vector3.one : Vector3.one * 0.5f;

        while (t < animDuration)
        {
            // ⭐ Time.timeScale-dən asılı olmaması üçün 'unscaledDeltaTime'
            t += Time.unscaledDeltaTime;
            float p = t / animDuration;

            float curveValue = popupCurve.Evaluate(p);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startA, endA, p);
            gameoverPOPUP.transform.localScale = Vector3.Lerp(startS, endS, curveValue);

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = endA;
        gameoverPOPUP.transform.localScale = endS;

        if (open)
        {
            AnimateButtonsSequentially(); // sonra animasiya
        }

        if (!open)
            gameoverPOPUP.SetActive(false);
        isAnimating = false;
    }

    void ResetButtons()
    {
        Button[] buttons = { resStartBTN, menuBTN, skinsButton };
        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            btn.transform.DOKill();

            // Scale-i dərhal 0 et
            btn.transform.localScale = Vector3.zero;

            // CanvasGroup varsa dərhal alpha-nı 0 et
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
        }
    }

    public void OpenSkinsMenu()
    {
        if (isAnimating)
            return;

        isSettingsOpen = true; // Bunu əlavə etdik
        Time.timeScale = 0f;

        if (SkinsManager.Instance != null)
        {
            SkinsManager.Instance.OpenSkins();
        }
    }

    public void HandleContinueButton()
    {
        // 1. Əgər oyunçunun hazırda canı varsa, birini işlət və davam et
        if (LifeManager.Instance != null && LifeManager.Instance.currentLives > 0)
        {
            LifeManager.Instance.SpendLife();
            ResumeFromSecondChance();
            // Debug.Log("Can istifadə edildi.");
        }
        // 2. Canı yoxdursa, ulduzla 5 can al, birini dərhal işlət və davam et
        else if (StarManager.Instance != null && StarManager.Instance.SpendStars(50))
        {
            LifeManager.Instance.AddLives(5); // 5 can alırıq
            LifeManager.Instance.SpendLife(); // Birini indi istifadə edirik (4-ü qalır)
            ResumeFromSecondChance();
            // Debug.Log("5 can alındı və biri istifadə edildi.");
        }
        else
        {
            // Debug.Log("Nə can var, nə də kifayət qədər ulduz!");
        }
    }

    public int GetComboCount()
    {
        return comboCount;
    }

    IEnumerator FreezeTimeDelayed()
    {
        // Realtime istifadə edirik ki, əgər zaman artıq yavaşlayıbsa problem olmasın.
        yield return new WaitForSecondsRealtime(animDuration);

        Time.timeScale = 0f;
        isSettingsOpen = true;
    }
}
