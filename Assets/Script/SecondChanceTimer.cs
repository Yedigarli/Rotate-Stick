using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SecondChanceTimer : MonoBehaviour
{
    public Image timerFillImage;
    public TMP_Text timerText;
    public float duration = 5f;
    public TMP_Text buttonText;

    [Header("Colors")]
    public Color startColor = new Color(0.18f, 0.8f, 0.44f); // Yaşıl (#2ECC71)
    public Color endColor = new Color(0.9f, 0.2f, 0.2f); // Qırmızı

    [HideInInspector]
    public bool canStart = false;

    private void OnEnable()
    {
        if (!canStart)
        {
            gameObject.SetActive(false);
            return;
        }

        UpdateButtonStyle();

        StopAllCoroutines();
        StartCoroutine(CountdownRoutine());
    }

    void UpdateButtonStyle()
    {
        int currentStars = PlayerPrefs.GetInt("Stars", 0);
        int currentLives = PlayerPrefs.GetInt("PlayerLives", 0);

        if (GameManager.Instance.useStarBtn != null && buttonText != null)
        {
            // Düymənin ana rəngini yaşıl edək
            GameManager.Instance.useStarBtn.image.color = startColor;

            if (currentLives > 0)
            {
                buttonText.text = "USE 1 LIFE";
                GameManager.Instance.useStarBtn.interactable = true;
            }
            else
            {
                buttonText.text = "BUY 5 LIVES (50 Stars)";
                GameManager.Instance.useStarBtn.interactable = (currentStars >= 50);

                // Əgər pulu çatmırsa, düyməni bir az şəffaf (solğun) edək
                if (currentStars < 50)
                    GameManager.Instance.useStarBtn.image.color = new Color(
                        startColor.r,
                        startColor.g,
                        startColor.b,
                        0.5f
                    );
            }
        }
    }

    private void Update()
    {
        // 1. "Use Star" düyməsinin böyüyüb-balacalanması (Pulse)
        if (GameManager.Instance != null && GameManager.Instance.useStarBtn != null)
        {
            float scale = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.05f;
            GameManager.Instance.useStarBtn.transform.localScale = new Vector3(scale, scale, 1f);
        }

        // 2. "No Thanks" yazısının yanıb-sönməsi (Fade)
        if (GameManager.Instance != null && GameManager.Instance.noThanksBtn != null)
        {
            float alpha = 0.5f + (Mathf.Sin(Time.unscaledTime * 4f) + 1f) / 4f; // 0.5 - 1.0 arası
            TMP_Text noThanksText =
                GameManager.Instance.noThanksBtn.GetComponentInChildren<TMP_Text>();
            if (noThanksText != null)
                noThanksText.color = new Color(1, 1, 1, alpha);
        }
    }

    IEnumerator CountdownRoutine()
    {
        float currentTime = duration;
        while (currentTime > 0)
        {
            currentTime -= Time.unscaledDeltaTime;

            if (timerFillImage != null)
            {
                timerFillImage.fillAmount = currentTime / duration;
                // Vaxt azaldıqca dairənin rəngi yaşıldan qırmızıya keçir
                timerFillImage.color = Color.Lerp(endColor, startColor, currentTime / duration);
            }

            if (timerText != null)
                timerText.text = Mathf.CeilToInt(currentTime).ToString();

            yield return null;
        }
        TimeFinished();
    }

    void TimeFinished()
    {
        canStart = false;
        if (GameManager.Instance != null)
            GameManager.Instance.CloseSecondChanceAndShowGameOver();
    }

    private void OnDisable()
    {
        canStart = false;
        // Düymənin ölçüsünü normala qaytarırıq ki, digər panellərdə eybəcər qalmasın
        if (GameManager.Instance != null && GameManager.Instance.useStarBtn != null)
            GameManager.Instance.useStarBtn.transform.localScale = Vector3.one;

        StopAllCoroutines();
    }
}
