using UnityEngine;
using System.Collections;

public class UIPanelAnimator : MonoBehaviour
{
    [SerializeField] private float duration = 0.2f;

    public IEnumerator FadeIn(GameObject panel, Vector3 targetScale)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        panel.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            panel.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }
    }

    public IEnumerator FadeOut(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            cg.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }
        Destroy(panel);
    }
}