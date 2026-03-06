using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance;
    public CanvasGroup transitionCanvasGroup;

    private bool isLoading;
    private int transitionCount;
    private const float DefaultFixedDelta = 0.02f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.backgroundLoadingPriority = ThreadPriority.Low;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            DOTween.SetTweensCapacity(1200, 300);

            if (transitionCanvasGroup != null)
            {
                transitionCanvasGroup.alpha = 1f;
                transitionCanvasGroup.gameObject.SetActive(true);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start() => FadeOut();

    public void LoadLevel(string sceneName)
    {
        if (isLoading)
            return;

        transitionCanvasGroup?.DOKill();

        StopAllCoroutines();
        StartCoroutine(SeamlessTransition(sceneName));
    }

    private IEnumerator SeamlessTransition(string sceneName)
    {
        isLoading = true;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = DefaultFixedDelta;

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.gameObject.SetActive(true);
            yield return transitionCanvasGroup
                .DOFade(1f, 0.2f)
                .SetUpdate(true)
                .WaitForCompletion();
        }

        yield return null;

        transitionCount++;
        if (transitionCount % 2 == 0)
        {
            AsyncOperation unload = Resources.UnloadUnusedAssets();
            while (!unload.isDone)
                yield return null;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        op.priority = 1;
        while (!op.isDone)
            yield return null;

        yield return null;
        FadeOut();
        isLoading = false;
    }

    private void FadeOut()
    {
        if (transitionCanvasGroup == null)
            return;

        transitionCanvasGroup.DOKill();
        transitionCanvasGroup
            .DOFade(0f, 0.33f)
            .SetUpdate(true)
            .OnComplete(() => transitionCanvasGroup.gameObject.SetActive(false));
    }
}
