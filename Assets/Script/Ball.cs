using UnityEngine;

public class Ball : MonoBehaviour
{
    public BallType ballType;

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
        // Pulse (Nəfəs alma) animasiyası - Oyunu canlı göstərən kiçik detallardır
        float pulse = Mathf.Sin(Time.time * 3f) * 0.05f;
        transform.localScale = Vector3.one * (0.27f + pulse);
    }
}
