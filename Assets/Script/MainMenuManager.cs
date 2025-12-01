using TMPro;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public TMP_Text levelText;

    public GameObject target;
    public GameObject ball;
    public Button PLayButton;
    public LayerMask detectableLayers;
    public float currentSpeed;

    public float raycastDistance = 5f;

    private Vector3 speedDirection;
    public float radius = 1.2f;

    private void Awake()
    {
        speedDirection = Vector3.forward;
        Invoke(nameof(SpawnBall), 0f);
        PLayButton.onClick.AddListener(playButton);
        int level = PlayerPrefs.GetInt("level", 1);
        if (levelText != null)
            levelText.text = "Level: " + level.ToString();
    }

    private void Update()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            transform.right,
            raycastDistance,
            detectableLayers
        );

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

    void playButton()
    {
        SceneManager.LoadScene("Game");
    }
}
