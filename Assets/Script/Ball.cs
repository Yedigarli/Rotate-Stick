using UnityEngine;

public class Ball : MonoBehaviour
{
    public BallType ballType;

    [Header("Asanlaşdırılmış Hərəkət")]
    public bool isMoving;
    public float moveSpeed = 1.5f; // Sürəti azaltdıq
    public float moveRange = 0.8f; // Hərəkət məsafəsi

    private Vector3 startPos;
    private float randomOffset;

    private void Start()
    {
        startPos = transform.position;
        // Bütün toplar eyni anda eyni tərəfə getməsin deyə random başlanğıc
        randomOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    private void Update()
    {
        // 1. Kiçik nəfəs alma effekti (Scale)
        float pulse = Mathf.Sin(Time.time * 3f) * 0.05f;
        transform.localScale = Vector3.one * (0.27f + pulse);
    }
}
