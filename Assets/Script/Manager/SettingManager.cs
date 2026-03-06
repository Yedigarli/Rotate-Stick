using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("Panels")]
    public GameObject settingsPanel;
    public CanvasGroup canvasGroup;

    [Header("Buttons")]
    public Button settingbutton;
    public Button closeSettingButton;
    public Button vibrationToggleButton;
    public Button soundToggleButton;
    public Button musicToggleButton;

    [Header("Icons & Sprites")]
    public Image vibrationIcon;
    public Image soundIcon;
    public Image musicIcon;

    public Sprite vibOn, vibOff;
    public Sprite soundOn, soundOff;
    public Sprite musicOn, musicOff;

    public float animDuration = 0.25f;
    private bool isAnimating = false;

    private void Awake()
    {
        Instance = this;
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        settingbutton?.onClick.AddListener(OpenSettings);
        closeSettingButton?.onClick.AddListener(CloseSettings);
        vibrationToggleButton?.onClick.AddListener(ToggleVibrationAction);
        soundToggleButton?.onClick.AddListener(ToggleSoundAction);
        musicToggleButton?.onClick.AddListener(ToggleMusicAction);
    }

    private void Start()
    {
        UpdateAllUI();
    }

    public void ToggleVibrationAction()
    {
        UISoundManager.Instance?.ToggleVibration();
        UISoundManager.Instance?.PlayClick();
        UpdateAllUI();
    }

    public void ToggleSoundAction()
    {
        UISoundManager.Instance?.ToggleSound();

        // If there is no dedicated vibration button, route vibration toggle through sound button.
        if (vibrationToggleButton == null || !vibrationToggleButton.gameObject.activeInHierarchy)
            UISoundManager.Instance?.ToggleVibration();

        UISoundManager.Instance?.PlayClick();
        UpdateAllUI();
    }

    public void ToggleMusicAction()
    {
        UISoundManager.Instance?.ToggleMusic();
        UISoundManager.Instance?.PlayClick();
        UpdateAllUI();
    }

    private void UpdateAllUI()
    {
        var sm = UISoundManager.Instance;
        if (sm == null)
            return;

        if (vibrationIcon != null)
            vibrationIcon.sprite = sm.isVibrationOn ? vibOn : vibOff;
        if (soundIcon != null)
            soundIcon.sprite = sm.isSoundOn ? soundOn : soundOff;
        if (musicIcon != null)
            musicIcon.sprite = sm.isMusicOn ? musicOn : musicOff;
    }

    public void OpenSettings()
    {
        if (isAnimating || settingsPanel == null)
            return;

        UpdateAllUI();
        settingsPanel.SetActive(true);
        StartCoroutine(Animate(true));
    }

    public void CloseSettings()
    {
        if (isAnimating || settingsPanel == null)
            return;

        StartCoroutine(Animate(false));
    }

    private IEnumerator Animate(bool open)
    {
        isAnimating = true;
        float t = 0f;
        float startAlpha = open ? 0f : 1f;
        float endAlpha = open ? 1f : 0f;
        Vector3 startScale = open ? Vector3.one * 0.8f : Vector3.one;
        Vector3 endScale = open ? Vector3.one : Vector3.one * 0.8f;

        if (canvasGroup != null)
            canvasGroup.alpha = startAlpha;

        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / animDuration;

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, p);
            settingsPanel.transform.localScale = Vector3.Lerp(startScale, endScale, p);
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = endAlpha;
        settingsPanel.transform.localScale = endScale;

        if (!open)
            settingsPanel.SetActive(false);

        isAnimating = false;
    }
}


