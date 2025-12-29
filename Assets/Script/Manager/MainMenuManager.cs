using MaskTransitions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI & Appearance")]
    [ColorUsage(showAlpha: true, hdr: true)]
    public TMP_Text levelText;

    // ⭐ Topun HDR rəngi üçün dəyişən
    [ColorUsage(showAlpha: true, hdr: true)]
    public Color ballGlowColor = Color.white;

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color playerGlowColor = Color.cyan; // Çubuğun parıltı rəngi

    [Header("References")]
    public GameObject player;
    public GameObject target;
    public GameObject ball;
    public Button PLayButton;
    public LayerMask detectableLayers;

    [Header("Movement")]
    public float currentSpeed;
    public float raycastDistance = 5f;
    public float radius = 1.2f;
    private Vector3 speedDirection;

    private void Awake()
    {
        speedDirection = Vector3.forward;
        ApplyTargetGlow();
        Invoke(nameof(SpawnBall), 0f);

        PLayButton.onClick.AddListener(() =>
        {
            TransitionManager.Instance.LoadLevel("Game");
        });

        int level = PlayerPrefs.GetInt("level", 1);
        if (levelText != null)
            levelText.text = "Level: " + level.ToString();

        if (StarUI.Instance != null)
            StarUI.Instance.UpdateUI();
    }

    private void Update()
    {
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

        // Topu yaradırıq
        GameObject spawnedBall = Instantiate(ball, spawnPos, Quaternion.identity);

        // ⭐ Topun SpriteRenderer-ini tapırıq və HDR rəngini veririk
        SpriteRenderer sr = spawnedBall.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = ballGlowColor;

            // Əgər topun xüsusi glow materialı varsa, onu da burda təyin edə bilərsən
            // sr.material.SetColor("_EmissionColor", ballGlowColor);
        }
    }

    void ApplyTargetGlow()
    {
        if (target != null)
        {
            SpriteRenderer playerSr = player.GetComponent<SpriteRenderer>();
            if (playerSr != null)
            {
                playerSr.color = playerGlowColor;
            }
        }
    }

    private void OnValidate()
    {
        ApplyTargetGlow();
    }
}
