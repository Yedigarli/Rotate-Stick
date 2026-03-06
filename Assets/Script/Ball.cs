using UnityEngine;

public class Ball : MonoBehaviour
{
    public BallType ballType;

    [Header("Scene Settings")]
    public bool isMenuBall = false;

    [Header("Movement")]
    public bool isMoving;
    public float moveSpeed = 1.5f;
    public float moveRange = 0.8f;

    private Transform cachedTransform;
    private float baseScale;

    private void Awake()
    {
        cachedTransform = transform;
        baseScale = isMenuBall ? 0.21f : 0.27f;
    }

    private void OnEnable()
    {
        if (cachedTransform == null)
            cachedTransform = transform;

        cachedTransform.localScale = Vector3.one * baseScale;
    }

    private void Update()
    {
        float pulse = Mathf.Sin(Time.time * 3f) * 0.05f;
        cachedTransform.localScale = Vector3.one * (baseScale + pulse);
    }
}
