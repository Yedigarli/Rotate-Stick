using DG.Tweening;
using TMPro;
using UnityEngine;

public class StarUI : MonoBehaviour
{
    public static StarUI Instance;
    public TMP_Text starText;

    private void Awake()
    {
        // Singleton nizamlanmasÄ±
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        UpdateUI();
    }

    private void OnDisable()
    {
        if (starText != null)
            starText.transform.DOKill();
    }

    public void UpdateUI()
    {
        if (starText != null)
        {
            // 1. PERFORMANS: PlayerPrefs oxumaq É™vÉ™zinÉ™ Manager-dÉ™ki hazÄ±r rÉ™qÉ™mi gÃ¶tÃ¼r
            int starCount = 0;
            if (StarManager.Instance != null)
            {
                starCount = StarManager.Instance.stars;
            }
            else
            {
                starCount = PlayerPrefs.GetInt("Stars", 0);
            }

            // 2. PERFORMANS: ToString() É™vÉ™zinÉ™ SetText("{0}") istifadÉ™ et (No Garbage)
            starText.SetText("{0}", starCount);

            // 3. JUICY EFFECT: Hamar animasiya
            starText.transform.DOKill();
            starText.transform.localScale = Vector3.one;
            starText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 1, 0.1f).SetUpdate(true).SetLink(starText.gameObject, LinkBehaviour.KillOnDestroy);
        }
    }
}


