using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GlowEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Breathing")]
    public float breatheScale = 1.05f;
    public float breatheDuration = 0.8f;

    [Header("Background Glow")]
    public Image glowImage;
    public float glowMaxAlpha = 0.6f;

    private Tween glowTween;
    private Tween scaleTween;

    private void Start()
    {
        if (glowImage != null)
        {
            glowImage.DOKill();
            glowTween = glowImage
                .DOFade(glowMaxAlpha, breatheDuration)
                .From(0.1f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(glowImage.gameObject, LinkBehaviour.KillOnDestroy);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOKill();
        scaleTween = transform
            .DOScale(0.9f, 0.1f)
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOKill();
        scaleTween = transform
            .DOScale(breatheScale, 0.1f)
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void OnDisable()
    {
        if (scaleTween != null && scaleTween.IsActive())
            scaleTween.Kill(false);

        if (glowTween != null && glowTween.IsActive())
            glowTween.Kill(false);

        transform.DOKill();
        if (glowImage != null)
            glowImage.DOKill();
    }
}
