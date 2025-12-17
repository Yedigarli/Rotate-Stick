using System.Collections;
using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public TMP_Text ScoreText;
    public TMP_Text LevelText;

    public int score;
    public int level;

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
            GameManager.Instance.FirstSpeed += 4.5f;
            PlayerPrefs.SetFloat("firstspeed", GameManager.Instance.FirstSpeed);
            score = level;
            PlayerPrefs.SetInt("level", level);
            PlayerPrefs.Save();
        }
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (ScoreText != null)
            ScoreText.text = score.ToString();
        if (LevelText != null)
            LevelText.text = "Level: " + level.ToString();
    }
}
