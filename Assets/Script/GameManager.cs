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

    public Button settingsButton;
    public Button closeSettingsButton;

    public float FirstSpeed = 60f;
    public float currentSpeed;
    public float raycastDistance = 5f;
    public LayerMask detectableLayers;

    public GameObject target;
    public GameObject ball;
    public GameObject bg;

    public Color newBackgroundColor = new Color(0.91f, 0.35f, 0.40f, 1f);
    public Color firstBackgroundColor = new Color(50 / 255f, 160 / 255f, 201 / 255f, 1f);

    public bool isSettingsOpen = false;
    public bool isSettingbtnPressed = false;

    private bool hasScoredOnce = false;
    private Vector3 speedDirection;
    public float radius = 1.2f;

    private void Awake()
    {
        Instance = this;

        bg.GetComponent<SpriteRenderer>().color = firstBackgroundColor;

        FirstSpeed = PlayerPrefs.GetFloat("firstspeed", 90);
        currentSpeed = FirstSpeed;
        speedDirection = Vector3.forward;

        Invoke(nameof(SpawnBall), 0.1f);

        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayBackgroundMusic();

        settingsButton.onClick.AddListener(() => SettingsManager.Instance.OpenSettings());

        closeSettingsButton.onClick.AddListener(() => SettingsManager.Instance.CloseSettings());
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

        VibrationManager.SoftVibration();

        if (!hasScoredOnce)
        {
            CameraShake.Instance.ShakeCamera(1.1f, 0.3f);
            yield return new WaitForSeconds(0.3f);
            ResetGameSoft();
        }
        else
        {
            CameraShake.Instance.ShakeCamera(1.1f, 1.2f);
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.StopBackgroundMusic();
                MusicManager.Instance.PlayLoseSoundSnippet(1.2f);
            }
            yield return new WaitForSeconds(1.2f);
            TransitionManager.Instance.LoadLevel("Game");
        }
    }

    void ResetGameSoft()
    {
        bg.GetComponent<SpriteRenderer>().color = firstBackgroundColor;

        currentSpeed = FirstSpeed;
        speedDirection = Vector3.forward;
        hasScoredOnce = false;

        foreach (GameObject b in GameObject.FindGameObjectsWithTag("Ball"))
            Destroy(b);

        Invoke(nameof(SpawnBall), 0.1f);
    }

    public static class VibrationManager
    {
        public static void SoftVibration()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (
                AndroidJavaClass unityPlayer = new AndroidJavaClass(
                    "com.unity3d.player.UnityPlayer"
                )
            )
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>(
                    "currentActivity"
                );
                AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>(
                    "getSystemService",
                    "vibrator"
                );

                if (vibrator != null)
                    vibrator.Call("vibrate", 80L); // 🔑 80 ms (ideal)
            }
#endif
        }
    }
}
