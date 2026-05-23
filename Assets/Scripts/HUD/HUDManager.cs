using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// 게임 내 HUD 및 UI 패널들의 생성, 제거, 관리를 담당 (연출은 UIPanelAnimator에 위임)
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("UI Containers")]
    [SerializeField] private GameObject pausePanel; 

    [Header("Dependencies")]
    [SerializeField] private UIPanelAnimator animator;
    
    // 상태 관리 변수
    private GameObject currentActivePanel;
    private bool isTransitioning = false; 
    private Key closeKey = Key.Escape;
    /// <summary><see cref="InputManager.PushPauseMenuHold"/>를 이번 패널에 대해 걸었는지(교체·언로드 시 짝 맞춤).</summary>
    private bool _pauseMenuHoldApplied;
    
    private void Awake() 
    {
        // 인스펙터에서 할당하지 않았을 경우를 대비한 자동 할당
        if (animator == null) animator = GetComponent<UIPanelAnimator>();
        if (animator == null) animator = UIPanelAnimator.Instance;
    }

    public bool IsPanelOpen => currentActivePanel != null; 
    
    void EnsureAnimator()
    {
        if (animator != null)
            return;
        animator = GetComponent<UIPanelAnimator>();
        if (animator != null)
            return;
        animator = UIPanelAnimator.Instance;
        if (animator == null)
            Debug.LogWarning($"{nameof(HUDManager)}: {nameof(UIPanelAnimator)} 싱글톤이 없습니다. SceneLoader Fade가 먼저 로드되어야 합니다.", this);
    }

    private void Update()
    {
        // 일시정지 중에는 ESC로 닫아야 하므로, Ready/코디네이터 게이트만 여기서 차단합니다.
        if (InputManager.IsEscBlockedForHud)
            return;

        if (Keyboard.current == null || !Keyboard.current[closeKey].wasPressedThisFrame || isTransitioning)
            return;
        HandleTogglePanel();
    }

    private void HandleTogglePanel()
    {
        if (currentActivePanel == null)
            OpenPanel(pausePanel);
        else
            ClosePanel();
    }

    public void OpenPanel(GameObject prefab, Vector3 customScale)
    {
        if (prefab == null || isTransitioning) return;
        if (InputManager.IsEscBlockedForHud) return;

        if (currentActivePanel != null)
        {
            ReleasePauseMenuHoldIfApplied();
            Destroy(currentActivePanel);
        }

        currentActivePanel = Instantiate(prefab, transform);
        StartCoroutine(OpenSequence(currentActivePanel, customScale));
    }

    public void OpenPanel(GameObject prefab)
    {
        OpenPanel(prefab, Vector3.one);
    }

    public void ClosePanel()
    {
        if (currentActivePanel != null && !isTransitioning)
        {
            StartCoroutine(CloseSequence());
        }
    }

    private void OnDestroy()
    {
        ReleasePauseMenuHoldIfApplied();
    }

    void ReleasePauseMenuHoldIfApplied()
    {
        if (!_pauseMenuHoldApplied)
            return;
        _pauseMenuHoldApplied = false;
        InputManager.PopPauseMenuHold();
    }

    #region Wrapper Coroutines
    // Animator의 코루틴을 실행하고 상태(isTransitioning)를 관리하는 래퍼 함수

    private IEnumerator OpenSequence(GameObject panel, Vector3 target)
    {
        isTransitioning = true;

        EnsureAnimator();
        if (animator == null)
        {
            isTransitioning = false;
            if (panel != null)
                Destroy(panel);
            currentActivePanel = null;
            yield break;
        }

        InputManager.PushPauseMenuHold();
        _pauseMenuHoldApplied = true;
        
        // UIPanelAnimator의 FadeIn 코루틴이 끝날 때까지 대기
        yield return StartCoroutine(animator.FadeIn(panel, target));
        
        isTransitioning = false;
    }

    private IEnumerator CloseSequence()
    {
        isTransitioning = true;

        EnsureAnimator();

        if (animator != null)
            yield return StartCoroutine(animator.FadeOut(currentActivePanel));
        else if (currentActivePanel != null)
            Destroy(currentActivePanel);

        ReleasePauseMenuHoldIfApplied();
        currentActivePanel = null;
        isTransitioning = false;
    }
    #endregion
}