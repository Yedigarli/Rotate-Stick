using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
    public AudioSource bgmusic;
    public AudioSource loseSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void PlayBackgroundMusic()
    {
        if (!bgmusic.isPlaying)
            bgmusic.Play();
    }

    public void StopBackgroundMusic()
    {
        if (bgmusic.isPlaying)
            bgmusic.Stop();
    }

    // 🔹 loseSound-dan yalnız müəyyən hissəni çalmaq
    public void PlayLoseSoundSnippet(float snippetDuration = 1.7f)
    {
        if (loseSound != null)
        {
            StartCoroutine(PlaySnippetCoroutine(snippetDuration));
        }
    }

    private IEnumerator PlaySnippetCoroutine(float duration)
    {
        loseSound.time = 0f; // başlanğıcdan oynat
        loseSound.Play();
        yield return new WaitForSeconds(duration); // yalnız duration qədər gözlə
        loseSound.Stop(); // dayandır
    }
}
