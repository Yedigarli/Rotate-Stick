using System.Collections;
using MaskTransitions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public AudioSource bgmusic;
    public AudioSource loseSound;
    public static GameManager Instance;
    public float FirstSpeed = 60f;
    public float currentSpeed;
    public float raycastDistance = 5f;
    public LayerMask detectableLayers;
    public GameObject target;
    public GameObject ball;
    public Color newBackgroundColor = new Color(255 / 255f, 0 / 255f, 0 / 255f, 255f);
    public Color firstBackgroundColor = new Color(50 / 255f, 160 / 255f, 201 / 255f, 255f);

    private Vector3 speedDirection;
    public float radius = 1.2f;
    public GameObject bg;

    private void Awake()
    {
        bg.GetComponent<SpriteRenderer>().color = firstBackgroundColor;
        FirstSpeed = PlayerPrefs.GetFloat("firstspeed", 90);
        currentSpeed = FirstSpeed;
        Instance = this;
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
                currentSpeed += 3.5f;
                CancelInvoke();
                Invoke(nameof(SpawnBall), 0f);
                StartCoroutine(LevelManager.Instance.ScoreChanger());
            }
            else
            {
                StartCoroutine(GameOver());
            }
        }

        transform.RotateAround(
            target.transform.position,
            speedDirection,
            currentSpeed * Time.deltaTime
        );
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

    IEnumerator GameOver()
    {
        bg.GetComponent<SpriteRenderer>().color = newBackgroundColor;
        CameraShake.Instance.ShakeCamera(1.1f, 0.5f);
        bgmusic.Stop();

        loseSound.Play();

        yield return new WaitForSeconds(0.5f);

        TransitionManager.Instance.LoadLevel("Game");
    }
}
