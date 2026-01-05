using UnityEngine;

public class Ball : MonoBehaviour
{
    public BallType ballType;

    [Header("Scene Settings")]
    public bool isMenuBall = false; // Əgər menyudakı topdursa, bunu Inspector-da işarələ

    [Header("Movement")]
    public bool isMoving;
    public float moveSpeed = 1.5f;
    public float moveRange = 0.8f;

    private Vector3 startPos;
    private float randomOffset;

    private void Start()
    {
        startPos = transform.position;
        randomOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    private void Update()
    {
        // Pulse (Nəfəs alma) animasiyası
        float pulse = Mathf.Sin(Time.time * 3f) * 0.05f;

        // ⭐ Şərtə görə ölçünü təyin edirik
        if (isMenuBall)
        {
            // Menyu üçün ölçü (Məsələn, daha böyük: 0.27)
            transform.localScale = Vector3.one * (0.21f + pulse);
        }
        else
        {
            // Oyun səhnəsi üçün ölçü (Məsələn, daha kiçik: 0.15)
            transform.localScale = Vector3.one * (0.27f + pulse);
        }
    }
}
