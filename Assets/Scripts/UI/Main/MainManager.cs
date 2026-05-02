using UnityEngine;
using System.Collections;

/// <summary>
/// IntroPanel과 MenuPanel의 전환 (UIPanelAnimator 활용)
/// </summary>
public class MainManager : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject introPanel; // CanvasGroup 대신 GameObject로 참조
    [SerializeField] private GameObject menuPanel;
    
    [Header("Dependencies")]
    [SerializeField] private UIPanelAnimator animator;

    // 앱 실행 후 딱 한 번만 false이고 이후엔 계속 true 유지
    private static bool _hasSeenIntro = false;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<UIPanelAnimator>();
        if (animator == null) animator = UIPanelAnimator.Instance;
    }

    private void Start()
    {
        if (_hasSeenIntro)
        {
            // 인트로 스킵 상태
            if (introPanel != null) introPanel.SetActive(false);
            if (menuPanel != null)
            {
                SoundManager.Instance.PlayBGM("Menu");
                menuPanel.SetActive(true);
                // 즉시 알파를 1로 설정 (연출 없이 고정)
                var cg = menuPanel.GetComponent<CanvasGroup>() ?? menuPanel.AddComponent<CanvasGroup>();
                cg.alpha = 1f;
            }
        }
        else
        {
            // 처음 실행 시 인트로 활성화
            if (introPanel != null)
            {
                introPanel.SetActive(true);
                var cg = introPanel.GetComponent<CanvasGroup>() ?? introPanel.AddComponent<CanvasGroup>();
                cg.alpha = 1f;
            }
            if (menuPanel != null)
            {
                SoundManager.Instance.PlayBGM("Menu");
                menuPanel.SetActive(false);
            }

            _hasSeenIntro = true;
        }
    }

    /// <summary>
    /// introPanel에서 menuPanel로 전환
    /// </summary>
    public void ChangeFromIntroToMenu()
    {
        StartCoroutine(SwitchPanelRoutine(introPanel, menuPanel));
    }

    private IEnumerator SwitchPanelRoutine(GameObject from, GameObject to)
    {
        // 1. 현재 패널 Fade Out (Scale 연출 없이 사라지게 하려면 Animator 수정이 필요할 수 있음)
        if (from != null)
        {
            // 주의: 현재 Animator.FadeOut은 Destroy(panel)를 포함하므로, 
            // 만약 패널을 파괴하지 않고 비활성화만 하려면 Animator에 별도 메서드가 필요합니다.
            yield return StartCoroutine(animator.FadeOut(from));
            // Animator 내부에서 이미 Destroy 되었을 것이므로 SetActive(false)는 생략 가능하거나 
            // Animator의 설정을 따릅니다.
        }

        // 2. 다음 패널 활성화 및 Fade In
        if (to != null)
        {
            to.SetActive(true);
            yield return StartCoroutine(animator.FadeIn(to, Vector3.one));
        }
    }
}