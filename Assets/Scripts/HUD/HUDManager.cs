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
    private GameObject _settingsOverlay;
    private bool isTransitioning = false;
    private bool _overlayTransitioning;
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

    /// <summary>설정이 열려 있거나 닫는 연출 중이면 true.</summary>
    public bool IsSettingsOverlayActive => _settingsOverlay != null || _overlayTransitioning;

    /// <summary>ExitPopup 거절 등 외부에서 HUD 일시정지 패널을 제거할 때.</summary>
    public void DismissPausePanelCompletely()
    {
        if (currentActivePanel == null)
            return;

        if (!currentActivePanel.CompareTag(Define.Tag.PAUSE_PANEL))
            return;

        StopAllCoroutines();
        isTransitioning = false;
        ReleasePauseMenuHoldIfApplied();
        Destroy(currentActivePanel);
        currentActivePanel = null;
    }
    
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

        if (Keyboard.current == null || !Keyboard.current[closeKey].wasPressedThisFrame)
            return;

        if (IsSettingsOverlayActive)
        {
            if (_settingsOverlay != null && !_overlayTransitioning)
                CloseSettingsOverlay();
            return;
        }

        if (isTransitioning || _overlayTransitioning)
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

        currentActivePanel = Instantiate(prefab);
        StartCoroutine(OpenSequence(currentActivePanel, customScale));
    }

    public void OpenPanel(GameObject prefab)
    {
        OpenPanel(prefab, Vector3.one);
    }

    /// <summary>설정 오버레이를 연다. PausePanel Canvas는 유지하고 메뉴(Panel)만 숨깁니다.</summary>
    public void OpenSettingsOverlay(GameObject prefab, Vector3 customScale, PausePanelController pauseUi = null)
    {
        if (prefab == null || _overlayTransitioning)
            return;

        if (_settingsOverlay != null)
            Destroy(_settingsOverlay);

        pauseUi ??= currentActivePanel != null
            ? currentActivePanel.GetComponentInChildren<PausePanelController>(true)
            : null;

        pauseUi?.HidePauseShellForSettings();

        Transform spawnParent = currentActivePanel != null ? currentActivePanel.transform : null;
        _settingsOverlay = Instantiate(prefab, spawnParent);
        StartCoroutine(OpenSettingsOverlaySequence(_settingsOverlay, customScale));
    }

    public void CloseSettingsOverlay()
    {
        if (!IsSettingsOverlayActive || _overlayTransitioning)
            return;

        StartCoroutine(CloseSettingsOverlaySequence());
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
        if (_settingsOverlay != null)
            Destroy(_settingsOverlay);

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

    IEnumerator OpenSettingsOverlaySequence(GameObject panel, Vector3 target)
    {
        _overlayTransitioning = true;

        EnsureAnimator();
        if (animator == null)
        {
            _overlayTransitioning = false;
            if (panel != null)
            {
                panel.SetActive(true);
                panel.transform.localScale = target;
            }
            yield break;
        }

        yield return StartCoroutine(animator.FadeIn(panel, target));
        _overlayTransitioning = false;
    }

    IEnumerator CloseSettingsOverlaySequence()
    {
        _overlayTransitioning = true;
        GameObject panel = _settingsOverlay;
        _settingsOverlay = null;

        EnsureAnimator();
        if (animator != null && panel != null)
            yield return StartCoroutine(animator.FadeOut(panel));
        else if (panel != null)
            Destroy(panel);

        // 설정이 완전히 사라진 뒤에만 Pause 메뉴(Panel)를 다시 켭니다.
        yield return null;

        if (currentActivePanel != null)
        {
            var pauseUi = currentActivePanel.GetComponentInChildren<PausePanelController>(true);
            if (pauseUi != null)
            {
                pauseUi.ShowPauseShellAfterSettings();
                pauseUi.NotifyHudSettingsOverlayClosed();
            }
        }

        _overlayTransitioning = false;
    }
    #endregion
}