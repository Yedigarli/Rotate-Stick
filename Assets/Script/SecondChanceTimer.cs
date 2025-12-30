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
    public CanvasGroup canvasGroup;

    [Header("Settings")]
    public float animDuration = 0.3f;
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

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
        StartCoroutine(AnimatePanel(true));
        StartCoroutine(CountdownRoutine());
    }

    void UpdateButtonStyle()
    {
        int currentStars = PlayerPrefs.GetInt("Stars", 0);
        int currentLives = PlayerPrefs.GetInt("PlayerLives", 0);

        if (GameManager.Instance != null && GameManager.Instance.useStarBtn != null)
        {
            if (currentLives > 0)
            {
                buttonText.text = "USE 1 LIFE";
                GameManager.Instance.useStarBtn.interactable = true;
            }
            else
            {
                buttonText.text = "BUY 5 LIVES (50 Stars)";
                GameManager.Instance.useStarBtn.interactable = (currentStars >= 50);
            }
        }
    }

    private void Update()
    {
        if (GameManager.Instance.useStarBtn != null)
        {
            float scale = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.05f;
            GameManager.Instance.useStarBtn.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    IEnumerator CountdownRoutine()
    {
        float currentTime = duration;
        while (currentTime > 0)
        {
            currentTime -= Time.unscaledDeltaTime;
            if (timerFillImage != null)
                timerFillImage.fillAmount = currentTime / duration;
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(currentTime).ToString();
            yield return null;
        }

        // 1. Əvvəlcə animasiyanın bitməsini gözləyirik
        yield return StartCoroutine(AnimatePanel(false));

        // 2. İndi GameManager-ə xəbər veririk.
        // GameManager həm bu paneli söndürəcək, həm də yenisini açacaq.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CloseSecondChanceAndShowGameOver();
        }
    }

    IEnumerator AnimatePanel(bool opening)
    {
        float t = 0;
        Vector3 startS = opening ? Vector3.zero : Vector3.one;
        Vector3 endS = opening ? Vector3.one : Vector3.zero;
        float startA = opening ? 0 : 1;
        float endA = opening ? 1 : 0;

        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / animDuration;
            float curveValue = openCurve.Evaluate(p);

            transform.localScale = Vector3.Lerp(startS, endS, curveValue);
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startA, endA, p);

            yield return null;
        }

        transform.localScale = endS;
        if (canvasGroup != null)
            canvasGroup.alpha = endA;

        // BURADAKI SetActive(false) SƏTRİNİ SİLDİK!
    }
}
