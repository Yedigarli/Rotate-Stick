using System.Collections;
using UnityEngine;

public class LoseWiggle : MonoBehaviour
{
    public static LoseWiggle Instance;
    public Transform skinVisual;

    public void Awake()
    {
        Instance = this;
    }

    public void PlayLoseAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(WiggleRoutine());
    }

    IEnumerator WiggleRoutine()
    {
        Vector3 startPos = skinVisual.localPosition;
        Quaternion startRot = skinVisual.localRotation;

        float duration = 0.45f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;
            float wave = Mathf.Sin(t * Mathf.PI * 4); // sağ-sol dalğa

            float yOffset = wave * 0.08f;
            float rot = wave * 8f;

            skinVisual.localPosition = startPos + new Vector3(0, yOffset, 0);
            skinVisual.localRotation = Quaternion.Euler(0, 0, rot);

            yield return null;
        }

        // Reset
        skinVisual.localPosition = startPos;
        skinVisual.localRotation = startRot;
    }
}
