using UnityEngine;
using System.Collections;

/// <summary>
/// IntroPanel과 MenuPanel의 전환
/// </summary>
public class MainManager : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private CanvasGroup introPanel;
    [SerializeField] private CanvasGroup menuPanel;
    
    private float fadeDuration = 0.5f;

    private void Start()
    {
        SoundManager.Instance.PlayBGM("Intro");
        
        // 초기 UI 상태 설정 (introPanel: On / menuPanel: Off)
        if (introPanel != null) { introPanel.alpha = 1; introPanel.gameObject.SetActive(true); }
        if (menuPanel != null) { menuPanel.alpha = 0; menuPanel.gameObject.SetActive(false); }
    }

    /// <summary>
    ///  introPanel에서 menuPanel로 전환
    /// </summary>
    public void ChangeFromIntroToMenu()
    {
        StartCoroutine(SwitchPanelRoutine(introPanel, menuPanel));
    }

    private IEnumerator SwitchPanelRoutine(CanvasGroup from, CanvasGroup to)
    {
        // 1. 현재 패널 Fade Out
        if (from != null)
        {
            yield return StartCoroutine(FadeCanvas(from, 1, 0));
            from.gameObject.SetActive(false);
        }

        // 2. 다음 패널 활성화 및 Fade In
        if (to != null)
        {
            to.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvas(to, 0, 1));
            
            SoundManager.Instance.PlayBGM("Menu");
        }
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float start, float end)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / fadeDuration);
            yield return null;
        }
        cg.alpha = end;
    }
}