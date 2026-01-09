[System.Serializable]
public class GameTask
{
    public string taskID; // Unikal ad (məs: "collect_stars")
    public string description; // Oyunçuya görünən mətn (məs: "Collect 10 Stars")
    public int targetAmount; // Lazım olan miqdar
    public int currentProgress; // Hazırda yığılan miqdar
    public bool isCompleted; // Tamamlanıb?

    public float GetProgress() => (float)currentProgress / targetAmount;
}
