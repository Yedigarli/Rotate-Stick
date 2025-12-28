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

    [Header("Status Colors")]
    // Rəngləri buradakı kimi dəqiqləşdir:
    public Color perfectColor = new Color(1f, 0.84f, 0f); // Qızılı/Sarı
    public Color niceColor = new Color(0f, 0.8f, 1f); // Mavi
    public Color levelUpColor = new Color(0.2f, 1f, 0.2f); // Yaşıl

    [Header("Settings")]
    public int pointsToNextLevel;
    private int currentPoints = 0;
    private int level;

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

        if (statusText != null)
            statusText.gameObject.SetActive(false);
        if (comboText != null)
            comboText.gameObject.SetActive(false);

        UpdateUI();
    }

    public void AddProgress(int amount)
    {
        currentPoints += amount;
        if (currentPoints >= pointsToNextLevel)
        {
            LevelUp();
        }
        UpdateUI();
    }

    void LevelUp()
    {
        level++;
        currentPoints = 0;
        pointsToNextLevel = level + 5;
        PlayerPrefs.SetInt("level", level);

        ShowStatus("LEVEL UP!", levelUpColor);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentSpeed = GameManager.Instance.FirstSpeed;
            GameManager.Instance.FirstSpeed += 5f;
            PlayerPrefs.SetFloat("firstspeed", GameManager.Instance.FirstSpeed);
        }

        PlayerPrefs.Save();
    }

    public void ShowStatusByType(string type, int combo = 0)
    {
        string selectedWord = "";
        Color selectedColor = Color.white;

        if (type == "Perfect")
        {
            // Əgər kombo 5-dən çoxdursa, daha "ağır" sözlər çıxsın
            if (combo >= 5)
            {
                selectedWord = insaneWords[Random.Range(0, insaneWords.Length)];
                selectedColor = Color.magenta; // Bənövşəyi/Parlaq rəng
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
        else if (type == "LevelUp")
        {
            selectedWord = "LEVEL UP!";
            selectedColor = levelUpColor;
        }

        ShowStatus(selectedWord, selectedColor);
    }

    public void ShowStatus(string message, Color col)
    {
        if (statusText == null)
            return;

        statusText.text = message;
        statusText.color = col; // Rəng burada təyin olunur
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

    public void ShowCombo(int combo, Color color)
    {
        if (comboText == null)
            return;

        if (combo > 1)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = "COMBO X" + combo;
            comboText.color = color; // Rəng burada təyin olunur

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

    void UpdateUI()
    {
        if (progressBarFill != null)
        {
            float fillAmount = (float)currentPoints / pointsToNextLevel;
            progressBarFill.fillAmount = fillAmount;
            // Progress bar rəngi qırmızıdan yaşıla keçir
            progressBarFill.color = Color.Lerp(Color.red, Color.green, fillAmount);
        }

        if (currentLevelText != null)
            currentLevelText.text = level.ToString();
        if (nextLevelText != null)
            nextLevelText.text = (level + 1).ToString();
    }
}
