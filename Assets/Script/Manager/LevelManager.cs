using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("UI Elements")]
    public Image progressBarFill;
    public TMP_Text currentLevelText;
    public TMP_Text nextLevelText;
    public TMP_Text comboText;
    public TMP_Text statusText;

    [Header("Status Colors (HDR)")]
    [ColorUsage(showAlpha: true, hdr: true)]
    public Color comboColor = new Color(2f, 0.5f, 0f); // HDR Neon Narıncı

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color perfectColor = new Color(1.5f, 1.2f, 0f); // HDR Sarı

    [ColorUsage(true, true)]
    public Color niceColor = new Color(0f, 1.2f, 1.5f); // HDR Mavi

    [ColorUsage(true, true)]
    public Color levelUpColor = new Color(0.2f, 2f, 0.2f); // HDR Yaşıl

    [Header("Settings")]
    public int pointsToNextLevel;
    public float smoothSpeed = 5f; // Barın dolma sürəti
    private int currentPoints = 0;
    private int level;
    private float targetFillAmount;

    [Header("Randomized Words")]
    private string[] perfectWords = { "PERFECT!", "AMAZING!", "FANTASTIC!", "BULLSEYE!" };
    private string[] niceWords = { "NICE!", "GOOD!", "COOL!", "NOT BAD!" };
    private string[] insaneWords = { "INSANE!", "GODLIKE!", "UNSTOPPABLE!", "MONSTER!" };

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        level = PlayerPrefs.GetInt("level", 1);
        pointsToNextLevel = level + 5;

        statusText?.gameObject.SetActive(false);
        comboText?.gameObject.SetActive(false);

        // ⭐ İlk açılışda UI-ı yeniləyirik
        UpdateLevelTexts();
        targetFillAmount = 0;
        if (progressBarFill != null)
            progressBarFill.fillAmount = 0;
    }

    private void Update()
    {
        if (progressBarFill != null)
        {
            // Smooth keçid
            progressBarFill.fillAmount = Mathf.Lerp(
                progressBarFill.fillAmount,
                targetFillAmount,
                Time.deltaTime * smoothSpeed
            );

            // HDR Rəng keçidi (Qırmızıdan Yaşıla)
            Color baseColor = Color.Lerp(Color.red, Color.green, progressBarFill.fillAmount);

            // ⭐ Parıltını (Intensity) tətbiq edirik
            progressBarFill.color = new Color(
                baseColor.r * 2f,
                baseColor.g * 2f,
                baseColor.b * 2f,
                1f
            );
        }
    }

    public void AddProgress(int amount)
    {
        currentPoints += amount;
        // Hədəfi təyin edirik, Update bunu smooth şəkildə dolduracaq
        targetFillAmount = (float)currentPoints / pointsToNextLevel;

        if (currentPoints >= pointsToNextLevel)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        level++;
        currentPoints = 0;
        targetFillAmount = 0;
        // Qeyd: progressBarFill.fillAmount-ı 0 etmirik ki, Update-dəki Lerp onu smooth boşaltsın.

        pointsToNextLevel = level + 5;
        PlayerPrefs.SetInt("level", level);
        PlayerPrefs.Save();

        UpdateLevelTexts();
        ShowStatus("LEVEL UP!", levelUpColor);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentSpeed = GameManager.Instance.FirstSpeed;
            GameManager.Instance.FirstSpeed += 5f;
            PlayerPrefs.SetFloat("firstspeed", GameManager.Instance.FirstSpeed);
        }
    }

    public void ShowStatusByType(string type, int combo = 0)
    {
        string selectedWord = "";
        Color selectedColor = Color.white;

        if (type == "Perfect")
        {
            if (combo >= 5)
            {
                selectedWord = insaneWords[Random.Range(0, insaneWords.Length)];
                selectedColor = new Color(2f, 0f, 2f); // HDR Magenta
            }
            else
            {
                selectedWord = perfectWords[Random.Range(0, perfectWords.Length)];
                selectedColor = perfectColor;
            }
        }
        else if (type == "Nice")
        {
            selectedWord = niceWords[Random.Range(0, niceWords.Length)];
            selectedColor = niceColor;
        }

        ShowStatus(selectedWord, selectedColor);
    }

    public void ShowStatus(string message, Color col)
    {
        if (statusText == null)
            return;
        statusText.text = message;
        statusText.color = col;
        statusText.gameObject.SetActive(true);

        StopCoroutine(nameof(StatusAnimationRoutine));
        StartCoroutine(nameof(StatusAnimationRoutine));
    }

    IEnumerator StatusAnimationRoutine()
    {
        RectTransform rect = statusText.GetComponent<RectTransform>();
        rect.localScale = Vector3.zero;

        float t = 0;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            rect.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.5f, t / 0.15f);
            yield return null;
        }

        t = 0;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            rect.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, t / 0.1f);
            yield return null;
        }

        yield return new WaitForSeconds(0.7f);
        statusText.gameObject.SetActive(false);
    }

    void UpdateLevelTexts()
    {
        if (currentLevelText != null)
            currentLevelText.text = level.ToString();
        if (nextLevelText != null)
            nextLevelText.text = (level + 1).ToString();
    }

    public void ShowCombo(int combo, Color color)
    {
        if (comboText == null)
            return;

        if (combo > 1)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = "COMBO X" + combo;

            // Burada mərkəzi comboColor-u istifadə edirik
            comboText.color = comboColor;

            StopCoroutine(nameof(ComboAnimationRoutine));
            StartCoroutine(nameof(ComboAnimationRoutine));
        }
        else
        {
            comboText.gameObject.SetActive(false);
        }
    }

    IEnumerator ComboAnimationRoutine()
    {
        RectTransform rect = comboText.GetComponent<RectTransform>();
        Vector2 originalPos = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y);

        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            rect.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one * 1.3f, elapsed / 0.15f);
            yield return null;
        }

        float timer = 0f;
        while (timer < 0.8f)
        {
            timer += Time.deltaTime;
            float offset = Mathf.Sin(Time.time * 15f) * 5f;
            rect.anchoredPosition = new Vector2(originalPos.x + offset, originalPos.y);
            yield return null;
        }

        rect.anchoredPosition = originalPos;
        comboText.gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        if (comboText != null)
        {
            SpriteRenderer comboSr = comboText.GetComponent<SpriteRenderer>();
            if (comboSr != null)
                comboSr.color = comboColor;
        }
    }
}
