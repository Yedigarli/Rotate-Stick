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

    [Header("Settings")]
    public int pointsToNextLevel;
    private int currentPoints = 0;
    private int level;

    private void Awake()
    {
        Instance = this;
        level = PlayerPrefs.GetInt("level", 1);
        pointsToNextLevel = level + 5; // Hər leveldə tələb olunan xal artır
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

        // Hər level artanda sürəti bir az sıfırla ki, oyunçu nəfəs alsın
        GameManager.Instance.currentSpeed = GameManager.Instance.FirstSpeed;
        GameManager.Instance.FirstSpeed += 5f;
        PlayerPrefs.SetFloat("firstspeed", GameManager.Instance.FirstSpeed);

        PlayerPrefs.Save();
    }

    // LevelManager.cs daxilinə bu funksiyanı əlavə et
    public void ShowCombo(int combo, Color color)
    {
        if (comboText == null)
            return;

        if (combo > 1)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = "COMBO X" + combo;
            comboText.color = color;

            // Əvvəlki animasiyanı dayandır və yenisini başlat
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

        // 1. PUNCH EFFEKTİ (Böyümə)
        float elapsed = 0f;
        float duration = 0.15f;
        Vector3 startScale = Vector3.one * 0.5f;
        Vector3 targetScale = Vector3.one * 1.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rect.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            yield return null;
        }

        // 2. GERİ QAYIDIŞ (Sabitlənmə)
        elapsed = 0f;
        duration = 0.1f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rect.localScale = Vector3.Lerp(targetScale, Vector3.one, elapsed / duration);
            yield return null;
        }

        // 3. YELLƏNMƏ VƏ YOX OLMA (Idle & Fade)
        float idleTime = 0f;
        while (idleTime < 0.8f)
        {
            idleTime += Time.deltaTime;
            // Yüngül yuxarı-aşağı yellənmə
            rect.anchoredPosition += new Vector2(0, Mathf.Sin(Time.time * 10f) * 0.5f);
            yield return null;
        }

        comboText.gameObject.SetActive(false);
    }

    IEnumerator ScaleDown(Transform t)
    {
        while (t.localScale.x > 1.0f)
        {
            t.localScale -= Vector3.one * Time.deltaTime * 3f;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    void UpdateUI()
    {
        float fillAmount = (float)currentPoints / pointsToNextLevel;
        progressBarFill.fillAmount = fillAmount;

        // Bar dolduqca rəngi yaşıllaşsın (Opsional)
        progressBarFill.color = Color.Lerp(Color.red, Color.green, fillAmount);

        currentLevelText.text = level.ToString();
        nextLevelText.text = (level + 1).ToString();
    }
}
