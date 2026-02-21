using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GlowEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Breathing Effect (Düymənin böyüyüb-kiçilməsi)")]
    public float breatheScale = 1.05f;
    public float breatheDuration = 0.8f;

    [Header("Back Glow (Arxa Fondakı İşıq)")]
    public Image glowImage;
    public float glowMaxAlpha = 0.6f;

    void Start()
    {
        // 1. Düymənin özünün nəfəs alması (Hop stili)
        // transform.DOScale(breatheScale, breatheDuration)
        //     .SetEase(Ease.InOutSine)
        //     .SetLoops(-1, LoopType.Yoyo)
        //     .SetUpdate(true);

        // 2. Arxa fonun parlaması
        if (glowImage != null)
        {
            glowImage
                .DOFade(glowMaxAlpha, breatheDuration)
                .From(0.1f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }
    }

    // --- Hop stilində klik reaksiyası ---
    public void OnPointerDown(PointerEventData eventData)
    {
        // Basanda bir az kiçilsin
        transform.DOScale(0.9f, 0.1f).SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Buraxanda əvvəlki nəfəs alma rejiminə qayıtsın
        transform.DOScale(breatheScale, 0.1f).SetUpdate(true);
    }
}
