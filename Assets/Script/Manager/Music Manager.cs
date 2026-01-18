using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
    
    [Header("Audio Sources")]
    public AudioSource bgmusic;
    public AudioSource loseSound;

    [Header("Pitch Settings")]
    public float maxPitch = 1.15f; // Maksimum sürət həddi
    private float initialPitch = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Səhnə dəyişəndə musiqi kəsilməsin
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (bgmusic != null)
        {
            initialPitch = bgmusic.pitch;
        }
    }

    // --- 🔹 Musiqi Sürətini Yenilə (GameManager-dən çağırılacaq) ---
    public void UpdateMusicPitch(float currentSpeed, float firstSpeed)
    {
        if (bgmusic == null) return;

        // Sürət artdıqca pitch-i 1.0-dan 1.15-ə doğru qaldırır
        // Hər 150 sürət artımında maksimum həddə çatır
        float speedDifference = currentSpeed - firstSpeed;
        float newPitch = initialPitch + (speedDifference / 500f); 
        
        bgmusic.pitch = Mathf.Clamp(newPitch, initialPitch, maxPitch);
    }

    // --- 🔹 Lose Sound Snippet ---
    public void PlayLoseSoundSnippet(float snippetDuration = 1.7f)
    {
        if (loseSound != null)
        {
            StartCoroutine(PlaySnippetCoroutine(snippetDuration));
        }
    }

    private IEnumerator PlaySnippetCoroutine(float duration)
    {
        if (bgmusic != null) bgmusic.Pause(); // Uduzanda arxa fon musiqisini saxla

        loseSound.time = 0f;
        loseSound.Play();
        yield return new WaitForSecondsRealtime(duration); // Zaman dayansa belə səs çalınsın
        loseSound.Stop();
    }
}
