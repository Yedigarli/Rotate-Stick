using DG.Tweening;
using UnityEngine;

public class LoseWiggle : MonoBehaviour
{
    public static LoseWiggle Instance;
    public Transform skinVisual;

    Tween wiggleTween;

    public void Awake()
    {
        Instance = this;
    }

    public void PlayLoseAnimation()
    {
        // Əvvəlki animasiyanı dayandır
        if (wiggleTween != null && wiggleTween.IsActive())
            wiggleTween.Kill();

        skinVisual.localRotation = Quaternion.identity;

        wiggleTween = skinVisual
            .DOLocalRotate(
                new Vector3(0, 0, 12f), // sağa sola tilt
                0.12f,
                RotateMode.Fast
            )
            .SetLoops(6, LoopType.Yoyo) // sağ-sol-sağ-sol
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                skinVisual.localRotation = Quaternion.identity;
            });
    }
}
