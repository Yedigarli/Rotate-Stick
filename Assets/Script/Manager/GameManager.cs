using System.Collections;
using MaskTransitions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Speeds")]
    public float FirstSpeed = 90f;
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
    private bool hasScoredOnce = false;
    private GameObject currentBall;
    private int comboCount = 0;

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

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color playerGlowColor = Color.cyan; // Çubuğun parıltı rəngi

    private void Awake()
    {
        Instance = this;
        ApplyGlobalColors();

        if (gameoverPOPUP != null)
            gameoverPOPUP.SetActive(false);
        if (canvasGroup != null)
            canvasGroup.alpha = 0;

        FirstSpeed = PlayerPrefs.GetFloat("firstspeed", 90);
        currentSpeed = FirstSpeed;
        Invoke(nameof(SpawnBall), 0.1f);

        // --- DÜYMƏLƏRİN FUNKSİYALARI ---
        resStartBTN.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            TransitionManager.Instance.LoadLevel("Game");
        });

        menuBTN.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            TransitionManager.Instance.LoadLevel("Menu");
        });

        // Skins düyməsinə basanda menyunu aç
        if (skinsButton != null)
        {
            skinsButton.onClick.AddListener(OpenSkinsMenu);
        }

        // Skins düyməsinə basanda SkinsManager-dəki OpenSkins funksiyasını çağır
        if (skinsButton != null)
        {
            skinsButton.onClick.AddListener(OpenSkinsMenu);
        }
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
            float distToCenter = Vector2.Distance(hit.point, hit.collider.transform.position);

            // PERFECT limiti
            bool isPerfect = distToCenter < 0.18f;

            Color perfectColor = LevelManager.Instance.perfectColor;
            Color niceColor = LevelManager.Instance.niceColor;

            if (isPerfect)
            {
                // PERFECT MƏNTİQİ
                comboCount++;
                LevelManager.Instance.ShowStatusByType("Perfect", comboCount);
                LevelManager.Instance.ShowCombo(comboCount, LevelManager.Instance.perfectColor);

                CreateFloatingText(hit.collider.transform.position, "+2", perfectColor);
                PlayBallEffect(hit.collider.transform.position, perfectColor);
                LevelManager.Instance.AddProgress(2); // Perfect olanda 2 xal
            }
            else
            {
                // NICE MƏNTİQİ
                comboCount = 0;
                LevelManager.Instance.ShowStatusByType("Nice");
                LevelManager.Instance.ShowCombo(0, Color.white);

                CreateFloatingText(hit.collider.transform.position, "+1", Color.white);
                PlayBallEffect(hit.collider.transform.position, Color.white);

                LevelManager.Instance.AddProgress(1); // Normal vuruşda 1 xal
            }

            // --- ULDUZ ARTIMI ---
            if (ball != null && ball.ballType == BallType.Star)
            {
                if (StarManager.Instance != null)
                    StarManager.Instance.AddStar(1);
            }

            Destroy(hit.collider.gameObject);
            currentBall = null;
            hasScoredOnce = true;

            speedDirection *= -1;
            currentSpeed += 2.5f;
            Invoke(nameof(SpawnBall), 0.05f);
        }
        else
        {
            StartCoroutine(GameOver());
        }
    }

    // --- DİGƏR FUNKSİYALAR (DƏYİŞMƏDİ) ---

    void CreateFloatingText(Vector3 pos, string text, Color color)
    {
        if (floatingTextPrefab == null)
            return;
        GameObject fText = Instantiate(floatingTextPrefab, pos, Quaternion.identity);
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas != null && fText.GetComponent<RectTransform>() != null)
        {
            fText.transform.SetParent(mainCanvas.transform, false);
            Vector2 screenPoint = Camera.main.WorldToScreenPoint(pos);
            fText.transform.position = screenPoint;
        }
        TMP_Text tmp = fText.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = text;
            tmp.color = color;
        }
    }

    void SpawnBall()
    {
        if (currentBall != null || GameObject.FindGameObjectsWithTag("Ball").Length > 0)
            return;

        Vector2 pos2D = Random.insideUnitCircle.normalized * radius;
        Vector3 spawnPos = new Vector3(
            target.transform.position.x + pos2D.x,
            target.transform.position.y + pos2D.y,
            0f
        );

        GameObject prefabToSpawn =
            (Random.value < starBallChance) ? starBallPrefab : normalBallPrefab;
        currentBall = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        // --- TOPUN HDR RƏNGİNİ TƏTBİQ ET ---
        SpriteRenderer ballSr = currentBall.GetComponent<SpriteRenderer>();
        if (ballSr != null)
            ballSr.color = ballGlowColor;

        Ball ballScript = currentBall.GetComponent<Ball>();
        if (PlayerPrefs.GetInt("level", 1) > 5 && Random.value < 0.3f)
        {
            ballScript.isMoving = true;
            ballScript.moveSpeed = Random.Range(2f, 4f);
        }
    }

    IEnumerator GameOver()
    {
        if (!hasScoredOnce)
        {
            CameraShake.Instance.ShakeCamera(1.1f, 0.3f);
            LoseWiggle.Instance.PlayLoseAnimation();
            yield return new WaitForSeconds(0.3f);
            ResetGameSoft();
        }
        else
        {
            LoseWiggle.Instance.PlayLoseAnimation();
            CameraShake.Instance.ShakeCamera(1.1f, 0.5f);

            // Popup açılmazdan əvvəl bir az gözlə
            yield return new WaitForSecondsRealtime(0.5f);
            GameOverPopUp();
        }
    }

    void ResetGameSoft()
    {
        CancelInvoke();
        currentBall = null;
        foreach (GameObject b in GameObject.FindGameObjectsWithTag("Ball"))
            Destroy(b);
        currentSpeed = FirstSpeed;
        speedDirection = Vector3.forward;
        hasScoredOnce = false;
        Invoke(nameof(SpawnBall), 0.15f);
    }

    public void PlayBallEffect(Vector3 pos, Color ballColor)
    {
        if (ballParticlePrefab == null)
            return;
        GameObject effect = Instantiate(ballParticlePrefab, pos, Quaternion.identity);
        var main = effect.GetComponent<ParticleSystem>().main;
        main.startColor = ballGlowColor;
        Destroy(effect, 1f);
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

        gameoverPOPUP.SetActive(true);
        StartCoroutine(Animate(true));

        // ⭐ Zamanı dayandır ki, arxada oyun davam etməsin
        // Amma animasiyanın işləməsi üçün Coroutine-də 'WaitForSecondsRealtime' istifadə etməliyik
        StartCoroutine(FreezeTimeDelayed());
    }

    IEnumerator FreezeTimeDelayed()
    {
        yield return new WaitForSecondsRealtime(animDuration);
        Time.timeScale = 0f;
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

        if (!open)
            gameoverPOPUP.SetActive(false);
        isAnimating = false;
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
}
