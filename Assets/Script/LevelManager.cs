using System.Collections;
using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public TMP_Text ScoreText;
    public TMP_Text LevelText;

    private int score;
    private int level = 1;

    private void Awake()
    {
        Instance = this;
        score = level;
        ScoreText.text = score.ToString();
        LevelText.text = "Level: " + level.ToString();
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
            score = level;
        }
        ScoreText.text = score.ToString();
        LevelText.text = "Level: " + level.ToString();
    }
}
