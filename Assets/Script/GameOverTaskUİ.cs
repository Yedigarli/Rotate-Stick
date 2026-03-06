using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverTaskDisplay : MonoBehaviour
{
    public TMP_Text taskNameText;
    public TMP_Text progressText;
    public Image progressBarFill;
    public Color completedColor = Color.green;

    private void Awake()
    {
        if (taskNameText != null)
        {
            taskNameText.enableWordWrapping = true;
            taskNameText.overflowMode = TextOverflowModes.Ellipsis;
            taskNameText.alignment = TextAlignmentOptions.MidlineLeft;
        }

        if (progressText != null)
        {
            progressText.enableWordWrapping = false;
            progressText.alignment = TextAlignmentOptions.MidlineRight;
        }
    }

    public void Setup(GameTask task)
    {
        if (task == null)
            return;

        taskNameText.SetText(task.description);

        if (task.currentProgress >= task.targetAmount)
        {
            progressText.SetText("DONE!");
            progressText.color = completedColor;
            progressBarFill.fillAmount = 1f;
            progressBarFill.color = completedColor;
        }
        else
        {
            progressText.SetText("{0} / {1}", task.currentProgress, task.targetAmount);
            progressBarFill.fillAmount = task.GetProgress();
        }
    }
}
