using System.Collections;
using System.Collections.Generic;
using MaskTransitions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class GameManager : MonoBehaviour
{
    public Canvas settingsCanvas;
    public static GameManager Instance;
    public float FirstSpeed = 60f;
    public float currentSpeed;
    public float raycastDistance = 5f;
    public LayerMask detectableLayers;

    public GameObject target;
    public GameObject bg;

    [Header("Ball Prefabs")]
    public GameObject normalBallPrefab;
    public GameObject starBallPrefab;

    [Header("Star Settings")]
    [Range(0f, 1f)]
    public float starBallChance = 0.2f;

    //public Color newBackgroundColor = new Color(0.91f, 0.35f, 0.40f, 1f);
    //public Color firstBackgroundColor = new Color(50 / 255f, 160 / 255f, 201 / 255f, 1f);

    public bool isSettingsOpen = false;
    public bool isSettingbtnPressed = false;

    private bool hasScoredOnce = false;
    private Vector3 speedDirection;
    public float radius = 1.2f;
    private GameObject currentBall;

    private void Awake()
    {
        Instance = this;

        //bg.GetComponent<SpriteRenderer>().color = firstBackgroundColor;

        FirstSpeed = PlayerPrefs.GetFloat("firstspeed", 90);
        currentSpeed = FirstSpeed;
        speedDirection = Vector3.forward;

        Invoke(nameof(SpawnBall), 0.1f);
    }

    private void Update()
    {
        // 🔒 SETTINGS AÇIQDIRSA → OYUN INPUT-U TAM BLOK
        if (isSettingsOpen)
            return;

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
                Ball ball = hit.collider.GetComponent<Ball>();
                if (ball != null && ball.ballType == BallType.Star)
                {
                    StarManager.Instance.AddStar(1);
                    if (StarUI.Instance != null)
                        StarUI.Instance.UpdateUI();
                }

                Destroy(hit.collider.gameObject);
                currentBall = null;
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
        {
            transform.RotateAround(
                target.transform.position,
                speedDirection,
                currentSpeed * Time.deltaTime
            );
        }
    }

    void SpawnBall()
    {
        // Əgər artıq top varsa → spawn ETMƏ
        if (currentBall != null)
            return;

        Vector2 pos2D = Random.insideUnitCircle.normalized * radius;

        Vector3 spawnPos = new Vector3(
            target.transform.position.x + pos2D.x,
            target.transform.position.y + pos2D.y,
            0f
        );

        GameObject prefabToSpawn = normalBallPrefab;

        if (Random.value < starBallChance)
            prefabToSpawn = starBallPrefab;

        currentBall = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
    }

    IEnumerator GameOver()
    {
        if (!hasScoredOnce)
        {
            CameraShake.Instance.ShakeCamera(1.1f, 0.3f);
            LoseWiggle.Instance.PlayLoseAnimation();
            yield return new WaitForSeconds(0.3f);
            ResetGameSoft();
        }
        else
        {
            LoseWiggle.Instance.PlayLoseAnimation();
            CameraShake.Instance.ShakeCamera(1.1f, 0.5f);
            yield return new WaitForSeconds(0.5f);
            TransitionManager.Instance.LoadLevel("Game");
        }
    }

    void ResetGameSoft()
    {
        // bg.GetComponent<SpriteRenderer>().color = firstBackgroundColor;

        currentSpeed = FirstSpeed;
        speedDirection = Vector3.forward;
        hasScoredOnce = false;

        foreach (GameObject b in GameObject.FindGameObjectsWithTag("Ball"))
            Destroy(b);

        currentBall = null;
        CancelInvoke();

        Invoke(nameof(SpawnBall), 0.1f);
    }
}
