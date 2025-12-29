using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 150f; // UI üçün daha böyük rəqəm lazımdır
    public float fadeDuration = 1f;
    private TMP_Text textMesh;

    [ColorUsage(showAlpha: true, hdr: true)]
    private Color startColor;
    private float timer = 0f;

    void Start()
    {
        textMesh = GetComponentInChildren<TMP_Text>();
        if (textMesh != null)
            startColor = textMesh.color;
        Destroy(gameObject, fadeDuration);
    }

    void Update()
    {
        // Yuxarı doğru hərəkət
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // Yavaşca şəffaflaşma
        timer += Time.deltaTime;
        if (textMesh != null)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        }
    }
}
