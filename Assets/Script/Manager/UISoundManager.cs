using System.Collections;
using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("Audio Clips")]
    public AudioClip clickSFX;
    public AudioClip sceneSFX;
    public AudioClip overSFX;
    public AudioClip handleSFX;
    public AudioClip upstarSFX;
    public AudioClip levelUpSFX;

    [Header("Pitch Settings")]
    public float maxMusicPitch = 1.15f;

    [Header("Settings State")]
    public bool isVibrationOn = true;
    public bool isSoundOn = true;
    public bool isMusicOn = true;

    private Coroutine sceneLoadSFXCoroutine;
    private Coroutine overSFXCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        isVibrationOn = PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
        isSoundOn = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
        isMusicOn = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;

        ApplyMusicSettings();
    }

    private void ApplyMusicSettings()
    {
        if (musicSource != null)
        {
            musicSource.mute = !isMusicOn;
            if (isMusicOn && !musicSource.isPlaying)
                musicSource.Play();
        }
    }

    // --- 🔹 SFX SƏSLƏRİ (Mute Yoxlaması ilə) ---
    public void PlayClick()
    {
        if (!isSoundOn || sfxSource == null || clickSFX == null)
            return;
        sfxSource.PlayOneShot(clickSFX);
    }

    public void PlayLevelUpSFX()
    {
        if (!isSoundOn || sfxSource == null || levelUpSFX == null)
            return;
        sfxSource.PlayOneShot(levelUpSFX);
    }

    public void PlayStarSFX()
    {
        if (!isSoundOn || sfxSource == null || upstarSFX == null)
            return;
        sfxSource.PlayOneShot(upstarSFX);
    }

    public void PlayHandleSFX(float comboCount)
    {
        if (!isSoundOn || sfxSource == null || handleSFX == null)
            return;

        float newPitch = 1.0f + (comboCount * 0.05f);
        sfxSource.pitch = Mathf.Clamp(newPitch, 1f, 1.6f);
        sfxSource.PlayOneShot(handleSFX);
        StartCoroutine(ResetPitchAfterSound());
    }

    public void PlayOverSFX()
    {
        if (musicSource != null)
            musicSource.Pause();

        if (!isSoundOn || sfxSource == null || overSFX == null)
            return;

        if (overSFXCoroutine != null)
            StopCoroutine(overSFXCoroutine);
        overSFXCoroutine = StartCoroutine(DelayedOverSFX(0.1f));
    }

    private IEnumerator DelayedOverSFX(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        sfxSource.PlayOneShot(overSFX);
    }

    public void PlaySceneSFX()
    {
        if (!isSoundOn || sfxSource == null || sceneSFX == null)
            return;
        if (sceneLoadSFXCoroutine != null)
            StopCoroutine(sceneLoadSFXCoroutine);
        sceneLoadSFXCoroutine = StartCoroutine(DelayedSceneLoadSFX(0.25f));
    }

    private IEnumerator DelayedSceneLoadSFX(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        sfxSource.PlayOneShot(sceneSFX);
    }

    private IEnumerator ResetPitchAfterSound()
    {
        yield return null;
        if (sfxSource != null)
            sfxSource.pitch = 1.0f;
    }

    // --- 🔹 MUSİQİ SÜRƏTİ ---
    public void UpdateMusicPitch(float currentSpeed, float firstSpeed)
    {
        if (musicSource == null)
            return;
        float speedDiff = currentSpeed - firstSpeed;
        float pitchIncr = speedDiff / 400f;
        musicSource.pitch = Mathf.Clamp(1f + pitchIncr, 1f, maxMusicPitch);
    }

    // --- 🔹 AYARLARI DƏYİŞDİR ---
    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        PlayerPrefs.SetInt("SoundEnabled", isSoundOn ? 1 : 0);
    }

    public void ToggleMusic()
    {
        isMusicOn = !isMusicOn;
        PlayerPrefs.SetInt("MusicEnabled", isMusicOn ? 1 : 0);
        ApplyMusicSettings();
    }
}
