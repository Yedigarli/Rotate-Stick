using UnityEngine;
using UnityEngine.UI;

public class SkinsScrollAnimator : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float centerScale = 1.05f;
    public float edgeScale = 1f;
    public float centerAlpha = 1f;
    public float edgeAlpha = 1f;
    public float maxDistance = 420f;
    public float lerpSpeed = 12f;

    private RectTransform viewport;
    private RectTransform content;
    private RectTransform[] itemRects = new RectTransform[0];
    private CanvasGroup[] itemGroups = new CanvasGroup[0];

    public void Setup(ScrollRect sr)
    {
        scrollRect = sr;
        if (scrollRect == null)
            return;

        if (scrollRect.viewport == null)
            scrollRect.viewport = scrollRect.GetComponent<RectTransform>();

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 25f;

        viewport = scrollRect.viewport;
        content = scrollRect.content;

        Refresh();
    }

    public void Refresh()
    {
        if (content == null)
            return;

        SkinButton[] buttons = content.GetComponentsInChildren<SkinButton>(true);
        itemRects = new RectTransform[buttons.Length];
        itemGroups = new CanvasGroup[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            itemRects[i] = buttons[i].GetComponent<RectTransform>();

            CanvasGroup cg = buttons[i].GetComponent<CanvasGroup>();
            if (cg == null)
                cg = buttons[i].gameObject.AddComponent<CanvasGroup>();

            itemGroups[i] = cg;
            // Obyekt yarananda və ya refresh olanda şəffaflığı birbaşa 1-ə bərabər edirik
            itemGroups[i].alpha = 1f;
        }
    }

    private void LateUpdate()
    {
        if (viewport == null || itemRects == null || itemRects.Length == 0)
            return;

        Vector3 center = viewport.TransformPoint(viewport.rect.center);
        float dt = Time.unscaledDeltaTime;

        for (int i = 0; i < itemRects.Length; i++)
        {
            RectTransform rt = itemRects[i];
            if (rt == null)
                continue;

            float dist = Mathf.Abs(rt.position.y - center.y);
            float t = Mathf.Clamp01(dist / Mathf.Max(1f, maxDistance));

            // Sadece Scale animasiyası qalır
            float targetScale = Mathf.Lerp(centerScale, edgeScale, t);
            targetScale = Mathf.Max(1f, targetScale);
            rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one * targetScale, dt * lerpSpeed);

            // Şəffaflıq (Alpha) kodunu burdan sildim ki, hər zaman 1 qalsın.
        }
    }
}
