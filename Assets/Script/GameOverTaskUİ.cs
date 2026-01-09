using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverTaskDisplay : MonoBehaviour
{
    public TMP_Text taskNameText;
    public TMP_Text progressText;
    public Image progressBarFill;
    public Color completedColor = Color.green;

    public void Setup(GameTask task)
    {
        taskNameText.text = task.description;
        // "15 / 50" formatında yazır, əgər tamamlanıbsa "DONE" yazır
        if (task.currentProgress >= task.targetAmount)
        {
            progressText.text = "DONE!";
            progressText.color = completedColor;
            progressBarFill.fillAmount = 1f;
            progressBarFill.color = completedColor;
        }
        else
        {
            progressText.text = task.currentProgress + " / " + task.targetAmount;
            progressBarFill.fillAmount = task.GetProgress();
        }
    }
}
