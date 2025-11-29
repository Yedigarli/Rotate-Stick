using UnityEngine;

public class StickAroundRotate : MonoBehaviour
{
    public float speed = 50f;
    public float raycastDistance = 5f;
    public LayerMask detectableLayers;
    public GameObject target;
    public GameObject ball;

    private Vector3 speedDirection;
    public float radius = 1.2f;

    private void Awake()
    {
        speedDirection = Vector3.forward;
        Invoke(nameof(SpawnBall), 0f);
    }

    void Update()
    {
        // Example: Cast a ray forward from the object's position
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            transform.right,
            raycastDistance,
            detectableLayers
        );

        if (Input.GetMouseButtonDown(0))
        {
            if (hit.collider != null)
            {
                //Debug.Log("Raycast hit: " + hit.collider.gameObject.name);
                // Access information about the hit, e.g., hit.point, hit.normal
                Destroy(hit.collider.gameObject);
                speedDirection = speedDirection * -1;
                speed += 2.5f;
                CancelInvoke();
                Invoke(nameof(SpawnBall), 0f);
                StartCoroutine(LevelManager.Instance.ScoreChanger());
            }
        }

        transform.RotateAround(target.transform.position, speedDirection, speed * Time.deltaTime);
    }

    void SpawnBall()
    {
        Vector2 pos2D = Random.insideUnitCircle.normalized * radius;

        Vector3 spawnPos = new Vector3(
            target.transform.position.x + pos2D.x,
            target.transform.position.y + pos2D.y,
            0f
        );

        Instantiate(ball, spawnPos, Quaternion.identity);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Collision detected with: " + collision.gameObject.name);
        // Access information about the collision, e.g., collision.contacts, collision.relativeVelocity
    }
}
