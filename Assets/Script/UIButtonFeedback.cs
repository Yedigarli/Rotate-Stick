using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening; // DOTween kitabxanası mütləqdir

public class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    public float scaleDownTo = 0.92f; // Basanda nə qədər kiçilsin
    public float duration = 0.1f;    // Animasiya sürəti
    public bool playClickSound = true;
    public bool triggerVibration = true;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    // Düyməyə toxunduğun an bura işləyir
    public void OnPointerDown(PointerEventData eventData)
    {
        // 1. Vizual: Kiçilmə effekti (TimeScale-dən asılı deyil)
        transform.DOScale(originalScale * scaleDownTo, duration).SetUpdate(true).SetEase(Ease.OutQuad);

        // 2. Səs: Klik səsi
        if (playClickSound)
        {
            UISoundManager.Instance?.PlayClick();
        }

        // 3. Hissiyyat: Vibrasiya
        if (triggerVibration)
        {
            UISoundManager.Instance?.TriggerLightVibration();
        }
    }

    // Barmağını düymədən çəkdiyin an bura işləyir
    public void OnPointerUp(PointerEventData eventData)
    {
        // Vizual: Öz ölçüsünə qayıtma
        transform.DOScale(originalScale, duration).SetUpdate(true).SetEase(Ease.OutBack);
    }

    // Düymə panel bağlananda kiçik qalsa, onu sıfırla
    private void OnDisable()
    {
        transform.localScale = originalScale;
    }
}
