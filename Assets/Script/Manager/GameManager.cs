using System.Collections;
using MaskTransitions;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Speeds")]
    public float FirstSpeed = 90f;
    public float currentSpeed;
    private Vector3 speedDirection = Vector3.forward;

    [Header("Raycast Settings")]
    public float raycastDistance = 10f; // Daha uzun raycast
    public LayerMask detectableLayers;

    [Header("Objects")]
    public GameObject target;
    public GameObject ballParticlePrefab;
    public GameObject floatingTextPrefab; // "+1" yazısı üçün

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

    [Header("Visuals")]
    public Camera mainCamera;
    public Color[] bgColors;
    private int colorIndex = 0;

    private void Awake()
    {
        Instance = this;
        FirstSpeed = PlayerPrefs.GetFloat("firstspeed", 90);
        currentSpeed = FirstSpeed;
        Invoke(nameof(SpawnBall), 0.1f);
    }

    private void Update()
    {
        if (isSettingsOpen)
            return;

        // Oxun fırlanması
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
            float distToCenter = Vector2.Distance(hit.point, hit.collider.transform.position);
            bool isPerfect = distToCenter < 0.18f; // Bir az daha geniş limit

            // Rəng Ayarları
            Color perfectColor = new Color(1f, 0.84f, 0f); // Parlaq Qızılı/Sarı
            Color normalColor = Color.white;

            if (isPerfect)
            {
                comboCount++;
                LevelManager.Instance.ShowCombo(comboCount, perfectColor);
                CreateFloatingText(hit.collider.transform.position, "+2", perfectColor);
                PlayBallEffect(hit.collider.transform.position, perfectColor);
                ChangeBGColor(); // Yalnız Perfect olanda rəng dəyişsin ki, mükafat hissi versin
            }
            else
            {
                comboCount = 0;
                LevelManager.Instance.ShowCombo(0, normalColor);
                CreateFloatingText(hit.collider.transform.position, "+1", normalColor);
                PlayBallEffect(hit.collider.transform.position, normalColor);
            }

            // Topu məhv et və yenisini yarat
            Destroy(hit.collider.gameObject);
            currentBall = null;
            hasScoredOnce = true;

            LevelManager.Instance.AddProgress(isPerfect ? 2 : 1);

            speedDirection *= -1;
            currentSpeed += 2.5f;
            Invoke(nameof(SpawnBall), 0.05f);
        }
        else
        {
            StartCoroutine(GameOver());
        }
    }

    void CreateFloatingText(Vector3 pos, string text, Color color)
    {
        if (floatingTextPrefab == null)
            return;
        GameObject fText = Instantiate(floatingTextPrefab, pos, Quaternion.identity);
        TMP_Text tmp = fText.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = text;
            tmp.color = color;
            // Yazıya bir az "Glow" (parıltı) vermək üçün
            tmp.fontSharedMaterial.SetColor(ShaderUtilities.ID_GlowColor, color);
        }
    }

    void SpawnBall()
    {
        // 1. Səhnədə hələ də top varmı deyə təkrar yoxla (Sığorta)
        if (currentBall != null)
            return;

        // Əgər tag ilə axtarışda nəsə qalıbsa, spawn etmə (Ekran təmiz olmalıdır)
        if (GameObject.FindGameObjectsWithTag("Ball").Length > 0)
            return;

        Vector2 pos2D = Random.insideUnitCircle.normalized * radius;
        Vector3 spawnPos = new Vector3(
            target.transform.position.x + pos2D.x,
            target.transform.position.y + pos2D.y,
            0f
        );

        GameObject prefabToSpawn =
            (Random.value < starBallChance) ? starBallPrefab : normalBallPrefab;

        // Topu yaradırıq
        currentBall = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        // Ball komponentini tənzimləyirik
        Ball ballScript = currentBall.GetComponent<Ball>();
        int currentLevel = PlayerPrefs.GetInt("level", 1);

        if (currentLevel > 5 && Random.value < 0.3f)
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
            yield return new WaitForSeconds(0.5f);
            TransitionManager.Instance.LoadLevel("Game");
        }
    }

    void ResetGameSoft()
    {
        // 1. Bütün Invoke-ları dayandır (Çox vacibdir!)
        CancelInvoke();

        // 2. Mövcud top referansını sıfırla
        currentBall = null;

        // 3. Səhnədəki BÜTÜN topları dərhal məhv et
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        foreach (GameObject b in balls)
        {
            Destroy(b);
        }

        // 4. Parametrləri sıfırla
        currentSpeed = FirstSpeed;
        speedDirection = Vector3.forward;
        hasScoredOnce = false;

        // 5. Bir az gözlə və yeni top yarat (Yalnız bir dəfə)
        Invoke(nameof(SpawnBall), 0.15f);
    }

    //elaveler
    public void PlayBallEffect(Vector3 pos, Color ballColor)
    {
        if (ballParticlePrefab == null)
            return;
        GameObject effect = Instantiate(ballParticlePrefab, pos, Quaternion.identity);
        var main = effect.GetComponent<ParticleSystem>().main;
        main.startColor = ballColor;
        Destroy(effect, 1f);
    }

    public void ChangeBGColor()
    {
        if (bgColors.Length == 0)
            return;
        colorIndex = (colorIndex + 1) % bgColors.Length;
        StopCoroutine("LerpColor");
        StartCoroutine(LerpColor(bgColors[colorIndex]));
    }

    IEnumerator LerpColor(Color targetColor)
    {
        float t = 0;
        Color startColor = mainCamera.backgroundColor;
        while (t < 1)
        {
            t += Time.deltaTime * 0.8f;
            mainCamera.backgroundColor = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
    }
}
