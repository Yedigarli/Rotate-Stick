using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float fadeDuration = 1f;
    private TMP_Text textMesh;
    private Color startColor;

    void Start()
    {
        textMesh = GetComponentInChildren<TMP_Text>();
        startColor = textMesh.color;
        Destroy(gameObject, fadeDuration);
    }

    void Update()
    {
        // Yuxarı doğru hərəkət
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // Yavaşca şəffaflaşma (Fade out)
        float alpha = Mathf.Lerp(startColor.a, 0, (Time.time % fadeDuration) / fadeDuration);
        textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
    }
}
