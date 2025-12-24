using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    public GameObject settingsPanel;
    public CanvasGroup canvasGroup;

    public Button settingbutton;
    public Button closeSettingButton;
    public float animDuration = 0.25f;

    private bool isAnimating = false;

    private void Awake()
    {
        Instance = this;
        settingsPanel.SetActive(false);
        settingbutton.onClick.AddListener(() => OpenSettings());
        closeSettingButton.onClick.AddListener(() => CloseSettings());
    }

    // 🔘 SETTINGS AÇ
    public void OpenSettings()
    {
        if (isAnimating)
            return;

        settingsPanel.SetActive(true);
        StartCoroutine(Animate(true));
    }

    // ❌ SETTINGS BAĞLA
    public void CloseSettings()
    {
        if (isAnimating)
            return;

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

        if (!open)
        {
            settingsPanel.SetActive(false);
        }

        isAnimating = false;
    }
}
