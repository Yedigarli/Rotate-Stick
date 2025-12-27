using UnityEngine;

public class Ball : MonoBehaviour
{
    public BallType ballType;

    [Header("Movement Settings")]
    public bool isMoving;
    public float moveSpeed = 2f;
    public float moveRange = 0.5f;

    [Header("Visual Juice")]
    public float pulseSpeed = 3f;
    public float pulseAmount = 0.1f;

    private Vector3 startPos;
    private Vector3 startScale;

    private void Start()
    {
        startPos = transform.position;
        startScale = transform.localScale;
    }

    private void Update()
    {
        // 1. Canlılıq effekti (Scale pulse) - bu həmişə qalsın
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = startScale + new Vector3(pulse, pulse, 0);

        // 2. Ədalətli hərəkət: Top dairəvi xətt boyunca (sağa-sola) fırlanır
        if (isMoving)
        {
            // Top mərkəz ətrafında kiçik bir bucaqla yellənir (pendulum kimi)
            float angle = Mathf.Sin(Time.time * moveSpeed) * 30f; // 30 dərəcə sağa-sola
            transform.RotateAround(
                GameManager.Instance.target.transform.position,
                Vector3.forward,
                angle * Time.deltaTime
            );
        }
    }
}
