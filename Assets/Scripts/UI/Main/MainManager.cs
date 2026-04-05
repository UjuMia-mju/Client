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

    // 앱 실행 후 딱 한 번만 false이고 이후엔 계속 true 유지
    private static bool _hasSeenIntro = false;

    private void Start()
    {
        SoundManager.Instance.PlayBGM("Menu");
        
        // 이미 인트로를 본 상태라면 (다른 씬에서 돌아온 경우)
        if (_hasSeenIntro)
        {
            // 인트로를 건너뛰고 바로 메뉴 패널을 활성화
            if (introPanel != null)
            {
                introPanel.alpha = 0;
                introPanel.gameObject.SetActive(false);
            }

            if (menuPanel != null)
            {
                menuPanel.alpha = 1;
                menuPanel.gameObject.SetActive(true);
            }
        }
        else // 소프트웨어를 처음 켰을 때
        {
            // 기존처럼 인트로 패널 활성화
            if (introPanel != null)
            {
                introPanel.alpha = 1;
                introPanel.gameObject.SetActive(true);
            }

            if (menuPanel != null)
            {
                menuPanel.alpha = 0;
                menuPanel.gameObject.SetActive(false);
            }

            // 다음 씬 진입부터는 인트로를 스킵하도록 상태 변경
            _hasSeenIntro = true;
        }
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