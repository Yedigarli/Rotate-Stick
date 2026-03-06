using DG.Tweening;
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public float moveDistance = 150f;
    public float fadeDuration = 1f;

    private TMP_Text textMesh;
    private RectTransform rectTransform;
    private Vector2 startAnchoredPos;

    private void Awake()
    {
        textMesh = GetComponentInChildren<TMP_Text>();
        rectTransform = transform as RectTransform;
        if (rectTransform != null)
            startAnchoredPos = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        if (textMesh == null || rectTransform == null)
            return;

        textMesh.alpha = 1f;
        rectTransform.anchoredPosition = startAnchoredPos;
        rectTransform.localScale = Vector3.one;

        rectTransform
            .DOAnchorPosY(startAnchoredPos.y + moveDistance, fadeDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);

        textMesh
            .DOFade(0f, fadeDuration)
            .SetUpdate(true)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void OnDisable()
    {
        if (rectTransform != null)
            rectTransform.DOKill();
        if (textMesh != null)
            textMesh.DOKill();
    }
}
