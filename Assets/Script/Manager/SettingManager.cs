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
        settingsPanel.SetActive(false);

        // PlayClick() çağırışları silindi
        settingbutton.onClick.AddListener(OpenSettings);
        closeSettingButton.onClick.AddListener(CloseSettings);

        vibrationToggleButton.onClick.AddListener(ToggleVibrationAction);
        soundToggleButton.onClick.AddListener(ToggleSoundAction);
        musicToggleButton.onClick.AddListener(ToggleMusicAction);
    }

    private void Start()
    {
        UpdateAllUI();
    }

    public void ToggleSoundAction()
    {
        UISoundManager.Instance?.ToggleSound();
        UpdateAllUI();
    }

    public void ToggleMusicAction()
    {
        UISoundManager.Instance?.ToggleMusic();
        UISoundManager.Instance?.PlayClick();
        UpdateAllUI();
    }

    public void ToggleVibrationAction()
    {
        UISoundManager.Instance?.ToggleVibration();
        UISoundManager.Instance?.PlayClick();
        UpdateAllUI();
        if (UISoundManager.Instance.isVibrationOn) UISoundManager.Instance.TriggerLightVibration();
    }

    private void UpdateAllUI()
    {
        var sm = UISoundManager.Instance;
        if (sm == null) return;

        if (vibrationIcon) vibrationIcon.sprite = sm.isVibrationOn ? vibOn : vibOff;
        if (soundIcon) soundIcon.sprite = sm.isSoundOn ? soundOn : soundOff;
        if (musicIcon) musicIcon.sprite = sm.isMusicOn ? musicOn : musicOff;
    }

    public void OpenSettings()
    {
        if (isAnimating) return;
        UpdateAllUI();
        settingsPanel.SetActive(true);
        StartCoroutine(Animate(true));
    }

    public void CloseSettings()
    {
        if (isAnimating) return;
        StartCoroutine(Animate(false));
    }

    IEnumerator Animate(bool open)
    {
        isAnimating = true;
        float t = 0f;
        float startAlpha = open ? 0 : 1;
        float endAlpha = open ? 1 : 0;
        Vector3 startScale = open ? Vector3.one * 0.8f : Vector3.one;
        Vector3 endScale = open ? Vector3.one : Vector3.one * 0.8f;

        canvasGroup.alpha = startAlpha;
        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / animDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, p);
            settingsPanel.transform.localScale = Vector3.Lerp(startScale, endScale, p);
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
        settingsPanel.transform.localScale = endScale;

        if (!open) settingsPanel.SetActive(false);
        isAnimating = false;
    }
}
