using System.Collections;
using MaskTransitions;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public float FirstSpeed = 60f;
    public float currentSpeed;
    public float raycastDistance = 5f;
    public LayerMask detectableLayers;
    public GameObject target;
    public GameObject ball;
    public Color newBackgroundColor = new Color(0.91f, 0.35f, 0.40f, 1f); // soft red
    public Color firstBackgroundColor = new Color(50 / 255f, 160 / 255f, 201 / 255f, 1f);

    private bool hasScoredOnce = false;
    private Vector3 speedDirection;
    public float radius = 1.2f;
    public GameObject bg;

    private void Awake()
    {
        Instance = this;

        bg.GetComponent<SpriteRenderer>().color = firstBackgroundColor;

        FirstSpeed = PlayerPrefs.GetFloat("firstspeed", 90);
        currentSpeed = FirstSpeed;
        speedDirection = Vector3.forward;
        hasScoredOnce = false;

        Invoke(nameof(SpawnBall), 0.1f);

        // Music
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayBackgroundMusic();
    }

    private void Update()
    {
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
                Destroy(hit.collider.gameObject);
                hasScoredOnce = true;

                speedDirection *= -1;
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

        if (target != null)
            transform.RotateAround(
                target.transform.position,
                speedDirection,
                currentSpeed * Time.deltaTime
            );
    }

    void SpawnBall()
    {
        if (target == null || ball == null)
            return;

        Vector2 pos2D = Random.insideUnitCircle.normalized * radius;

        Vector3 spawnPos = new Vector3(
            target.transform.position.x + pos2D.x,
            target.transform.position.y + pos2D.y,
            0f
        );

        Instantiate(ball, spawnPos, Quaternion.identity);
    }

    IEnumerator GameOver()
    {
        bg.GetComponent<SpriteRenderer>().color = newBackgroundColor;

        CameraShake.Instance.ShakeCamera(1.1f, 1.2f);

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.StopBackgroundMusic();
            MusicManager.Instance.PlayLoseSoundSnippet(1.2f);
        }

        yield return new WaitForSeconds(1.2f);

        if (!hasScoredOnce)
        {
            // İlk hit yoxdursa → soft reset
            ResetGameSoft();
        }
        else
        {
            // Hit olub → scene reload
            TransitionManager.Instance.LoadLevel("Game");
        }
    }

    void ResetGameSoft()
    {
        bg.GetComponent<SpriteRenderer>().color = firstBackgroundColor;

        currentSpeed = FirstSpeed;
        speedDirection = Vector3.forward;
        hasScoredOnce = false;

        // Bütün topları sil
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        foreach (GameObject b in balls)
            Destroy(b);

        Invoke(nameof(SpawnBall), 0.1f);

        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayBackgroundMusic();
    }
}
