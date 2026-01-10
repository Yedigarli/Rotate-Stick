using System.Collections;
using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;

    public AudioSource audioSource;
    public AudioClip clickSFX;
    public AudioClip sceneSFX;
    public AudioClip overSFX;

    // Hər səs tipi üçün ayrı Coroutine referansı (qarışmaması üçün)
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

    // --- KLİK SƏSİ ---
    public void PlayClick()
    {
        if (audioSource != null && clickSFX != null)
            audioSource.PlayOneShot(clickSFX);
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

    // --- GAME OVER SƏSİ ---
    public void PlayOverSFX()
    {
        if (overSFX == null || audioSource == null) return;

        // Əgər artıq bir səs gözləyirsə, onu dayandırırıq
        if (overSFXCoroutine != null)
            StopCoroutine(overSFXCoroutine);

        overSFXCoroutine = StartCoroutine(DelayedOverSFX(0.05f)); // GameOver üçün bir az daha uzun gecikmə (istəyə görə)
    }

    private IEnumerator DelayedOverSFX(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        audioSource.PlayOneShot(overSFX);
        overSFXCoroutine = null;
    }
}
