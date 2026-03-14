using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SkinsScrollAnimator : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float centerScale = 1.05f;
    public float edgeScale = 1f;
    public float maxDistance = 420f;
    public float lerpSpeed = 12f;

    private RectTransform viewport;
    private RectTransform content;
    private RectTransform[] itemRects = new RectTransform[0];

    public void Setup(ScrollRect sr)
    {
        scrollRect = sr;
        if (scrollRect == null) return;
        viewport = scrollRect.viewport;
        content = scrollRect.content;
        Refresh();
    }

    public void Refresh()
    {
        if (content == null) return;
        SkinButton[] buttons = content.GetComponentsInChildren<SkinButton>(true);
        itemRects = new RectTransform[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
            itemRects[i] = buttons[i].GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        if (viewport == null || itemRects == null || itemRects.Length == 0) return;

        Vector3 center = viewport.TransformPoint(viewport.rect.center);
        float dt = Time.unscaledDeltaTime;

        for (int i = 0; i < itemRects.Length; i++)
        {
            RectTransform rt = itemRects[i];
            if (rt == null) continue;

            // Əgər bu düymə hazırda DOTween (Click animasiyası) ilə məşğuldursa, Scroll Animator qarışmasın
            if (DOTween.IsTweening(rt)) continue;

            float dist = Mathf.Abs(rt.position.y - center.y);
            float t = Mathf.Clamp01(dist / Mathf.Max(1f, maxDistance));

            float targetScale = Mathf.Lerp(centerScale, edgeScale, t);
            rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one * targetScale, dt * lerpSpeed);
        }
    }
}
