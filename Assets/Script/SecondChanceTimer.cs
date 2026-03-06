using System.Collections;
using DG.Tweening;
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

    public Button noThanksButton;
    public float noThanksDelay = 1.5f;

    [HideInInspector]
    public bool canStart = false;

    private Tween useStarPulseTween;

    private void OnEnable()
    {
        if (!canStart)
        {
            gameObject.SetActive(false);
            return;
        }

        if (noThanksButton != null)
            noThanksButton.interactable = false;

        ConfigureTexts();
        UpdateButtonStyle();
        StopAllCoroutines();

        StartCoroutine(AnimatePanel(true));
        StartCoroutine(CountdownRoutine());
        StartCoroutine(EnableNoThanksAfterDelay());
        StartUseStarPulse();
    }

    private void OnDisable()
    {
        useStarPulseTween?.Kill();
        useStarPulseTween = null;
    }

    private void ConfigureTexts()
    {
        if (timerText != null)
        {
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.enableWordWrapping = false;
        }

        if (buttonText != null)
        {
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.enableWordWrapping = true;
            buttonText.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    private void StartUseStarPulse()
    {
        if (GameManager.Instance == null || GameManager.Instance.useStarBtn == null)
            return;

        Transform t = GameManager.Instance.useStarBtn.transform;
        t.DOKill();
        t.localScale = Vector3.one;
        useStarPulseTween = t.DOScale(1.05f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
    }

    private IEnumerator EnableNoThanksAfterDelay()
    {
        yield return new WaitForSecondsRealtime(noThanksDelay);

        if (noThanksButton != null)
        {
            noThanksButton.interactable = true;
            noThanksButton.transform.DOKill();
            noThanksButton.transform.localScale = Vector3.one;
            noThanksButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 5, 1f).SetUpdate(true);
        }
    }

    private void UpdateButtonStyle()
    {
        int currentStars = PlayerPrefs.GetInt("Stars", 0);
        int currentLives = PlayerPrefs.GetInt("PlayerLives", 0);

        if (GameManager.Instance != null && GameManager.Instance.useStarBtn != null)
        {
            if (currentLives > 0)
            {
                buttonText.SetText("USE 1 LIFE");
                GameManager.Instance.useStarBtn.interactable = true;
            }
            else
            {
                buttonText.SetText("BUY 5 LIVES (50 STARS)");
                GameManager.Instance.useStarBtn.interactable = currentStars >= 50;
            }

            buttonText.ForceMeshUpdate();
        }
    }

    private IEnumerator CountdownRoutine()
    {
        float currentTime = duration;
        while (currentTime > 0f)
        {
            currentTime -= Time.unscaledDeltaTime;
            if (timerFillImage != null)
                timerFillImage.fillAmount = currentTime / duration;
            if (timerText != null)
                timerText.SetText("{0}", Mathf.CeilToInt(currentTime));
            yield return null;
        }

        yield return StartCoroutine(AnimatePanel(false));

        if (GameManager.Instance != null)
            GameManager.Instance.CloseSecondChanceAndShowGameOver();
    }

    private IEnumerator AnimatePanel(bool opening)
    {
        float t = 0f;
        Vector3 startS = opening ? Vector3.zero : Vector3.one;
        Vector3 endS = opening ? Vector3.one : Vector3.zero;
        float startA = opening ? 0f : 1f;
        float endA = opening ? 1f : 0f;

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
    }
}
