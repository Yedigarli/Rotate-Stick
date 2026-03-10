using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    public float scaleDownTo = 0.92f;
    public float duration = 0.1f;
    public bool playClickSound = true;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform
            .DOScale(originalScale * scaleDownTo, duration)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad);

        if (playClickSound)
            UISoundManager.Instance?.PlayClick();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOScale(originalScale, duration).SetUpdate(true).SetEase(Ease.OutBack);
    }

    private void OnDisable()
    {
        transform.DOKill();
        transform.localScale = originalScale;
    }
}
