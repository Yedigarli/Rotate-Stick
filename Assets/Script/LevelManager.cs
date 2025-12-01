using System.Collections;
using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public TMP_Text ScoreText;
    public TMP_Text LevelText;

    private int score;
    private int level;

    private void Awake()
    {
        Instance = this;
        level = PlayerPrefs.GetInt("level", 1);
        score = level;

        UpdateScoreUI();
    }

    public IEnumerator ScoreChanger()
    {
        score--;
        if (score <= 0)
        {
            yield return new WaitForSeconds(0.1f);
            level++;
            GameManager.Instance.currentSpeed = GameManager.Instance.FirstSpeed;
            GameManager.Instance.FirstSpeed += 5.5f;
            float speed = GameManager.Instance.FirstSpeed;
            PlayerPrefs.SetFloat("firstspeed", speed);
            score = level;
            PlayerPrefs.SetInt("level", level);
            PlayerPrefs.Save();
        }
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (ScoreText != null)
        {
            ScoreText.text = score.ToString();
        }
        if (LevelText.text != null)
        {
            LevelText.text = "Level: " + level.ToString();
        }
    }
}
