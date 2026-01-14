using System.Collections;
using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;

    public AudioSource audioSource;
    public AudioClip clickSFX;
    public AudioClip sceneSFX;
    public AudioClip overSFX;
    public AudioClip handleSFX; // Handle səsi
    public AudioClip upstarSFX;


    private Coroutine sceneLoadSFXCoroutine;
    private Coroutine overSFXCoroutine;
    private Coroutine handleSFXCoroutine; // Yeni Coroutine referansı

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

    // --- KLİK SƏSİ ---
    public void PlayClick()
    {
        if (audioSource != null && clickSFX != null)
            audioSource.PlayOneShot(clickSFX);
    }

    public void PlayStarSFX()
    {
        if (audioSource != null && upstarSFX != null)
            audioSource.PlayOneShot(upstarSFX);
    }

    // --- SCENE LOAD SƏSİ ---
    public void PlaySceneSFX()
    {
        if (sceneSFX == null || audioSource == null) return;

        if (sceneLoadSFXCoroutine != null)
            StopCoroutine(sceneLoadSFXCoroutine);

        sceneLoadSFXCoroutine = StartCoroutine(DelayedSceneLoadSFX(0.25f));
    }

    private IEnumerator DelayedSceneLoadSFX(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        audioSource.PlayOneShot(sceneSFX);
        sceneLoadSFXCoroutine = null;
    }


    public void PlayOverSFX()
    {
        if (overSFX == null || audioSource == null) return;

        if (overSFXCoroutine != null)
            StopCoroutine(overSFXCoroutine);

        overSFXCoroutine = StartCoroutine(DelayedOverSFX(0.1f));
    }

    private IEnumerator DelayedOverSFX(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        audioSource.PlayOneShot(overSFX);
        overSFXCoroutine = null;
    }

    // public void PlayOverSFX()
    // {
    //     if (audioSource != null && overSFX != null)
    //         audioSource.PlayOneShot(overSFX);
    // }

    public void PlayHandleSFX(float comboCount)
    {
        if (audioSource != null && handleSFX != null)
        {
            // Əsas ton 1.0f-dir. 
            // Hər combo artdıqca tonu 0.05 vahid artırırıq (maksimum 1.5-ə qədər)
            float newPitch = 1.0f + (comboCount * 0.05f);
            audioSource.pitch = Mathf.Clamp(newPitch, 1f, 1.5f);

            audioSource.PlayOneShot(handleSFX);

            // Səsi çıxarandan sonra tonu normala qaytarırıq ki, digər səslər xarab olmasın
            // (PlayOneShot-dan dərhal sonra pitch-i sıfırlamaq olar)
            StartCoroutine(ResetPitchAfterSound());
        }
    }

    private IEnumerator ResetPitchAfterSound()
    {
        // Səs faylının bitməsini gözləməyə ehtiyac yoxdur, 
        // çünki PlayOneShot mövcud pitch ilə işə düşür.
        yield return null;
        audioSource.pitch = 1.0f;
    }
}
