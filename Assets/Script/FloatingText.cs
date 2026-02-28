using DG.Tweening;
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public float moveDistance = 150f;
    public float fadeDuration = 1f;
    private TMP_Text textMesh;
    private Vector3 initialScale;

    void Awake()
    {
        textMesh = GetComponentInChildren<TMP_Text>();
        initialScale = transform.localScale;
    }

    // OnEnable obyekt hər dəfə hovuzdan çıxanda işləyir
    void OnEnable()
    {
        if (textMesh == null)
            return;

        // Reset: Köhnə animasiyadan qalan dəyərləri sıfırla
        textMesh.alpha = 1f;
        transform.localScale = initialScale;

        // Animasiya
        transform
            .DOMoveY(transform.position.y + moveDistance, fadeDuration)
            .SetEase(Ease.OutCubic);

        textMesh
            .DOFade(0, fadeDuration)
            .OnComplete(() =>
            {
                // Destroy YOX, SetActive(false) edirik ki, hovuza qayıtsın
                gameObject.SetActive(false);
            });
    }

    void OnDisable()
    {
        // Obyekt sönəndə üzərindəki bütün animasiyaları dayandırırıq
        transform.DOKill();
        textMesh.DOKill();
    }
}
