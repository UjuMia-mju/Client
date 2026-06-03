using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// StagePausePanel 프리팹을 ESC로 열고 닫습니다. Gacha·Lobby·StageSelect 등에서 사용.
/// </summary>
public class ScenePauseMenuController : MonoBehaviour
{
    public static ScenePauseMenuController Instance { get; private set; }

    [SerializeField] private GameObject stagePausePanelPrefab;

    GameObject _pauseInstance;
    bool _pauseMenuHoldActive;
    bool _pauseTransitioning;
    Coroutine _pauseTransitionRoutine;

    public bool IsPauseMenuOpen => _pauseInstance != null && _pauseInstance.activeInHierarchy;

    /// <summary>ExitPopup 거절 등 외부에서 일시정지 UI를 완전히 제거할 때.</summary>
    public void DismissPausePanelCompletely() => ClosePausePanel(destroyInstance: true);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        ClosePausePanelImmediate(destroyInstance: true);

        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (InputManager.IsEscBlockedForHud || _pauseTransitioning)
            return;

        if (Keyboard.current == null || !Keyboard.current[Key.Escape].wasPressedThisFrame)
            return;

        if (ExitPopupManager.IsOpen)
        {
            ExitPopupManager.Hide();
            return;
        }

        if (IsPauseMenuOpen)
        {
            var pauseUi = _pauseInstance != null
                ? _pauseInstance.GetComponentInChildren<PausePanelController>(true)
                : null;

            if (pauseUi != null && pauseUi.IsSettingsFlowActive)
            {
                pauseUi.TryCloseSettingsOverlay();
                return;
            }

            ClosePausePanel(destroyInstance: false);
            return;
        }

        if (TryCloseSceneOverlayFromEscape())
            return;

        OpenPausePanel();
    }

    /// <summary>씬별 ESC 우선 처리(초대 패널·가챠 결과 등). 처리했으면 true.</summary>
    static bool TryCloseSceneOverlayFromEscape()
    {
        var inviteUi = FindFirstObjectByType<LobbyInviteUI>(FindObjectsInactive.Include);
        if (inviteUi != null && inviteUi.TryCloseInvitePanelFromEscape())
            return true;

        var gachaResult = FindFirstObjectByType<GachaResultPopupUI>(FindObjectsInactive.Include);
        if (gachaResult != null && gachaResult.TryCloseFromEscape())
            return true;

        return false;
    }

    void OpenPausePanel()
    {
        if (stagePausePanelPrefab == null)
        {
            Debug.LogWarning("[ScenePauseMenuController] stagePausePanelPrefab이 비어 있습니다.");
            return;
        }

        if (_pauseTransitioning)
            return;

        if (_pauseInstance == null)
            _pauseInstance = Instantiate(stagePausePanelPrefab);

        if (!_pauseMenuHoldActive)
        {
            InputManager.PushPauseMenuHold();
            _pauseMenuHoldActive = true;
        }

        if (_pauseTransitionRoutine != null)
            StopCoroutine(_pauseTransitionRoutine);

        _pauseTransitionRoutine = StartCoroutine(OpenPausePanelRoutine());
    }

    IEnumerator OpenPausePanelRoutine()
    {
        _pauseTransitioning = true;

        if (UIPanelAnimator.Instance != null)
            yield return UIPanelAnimator.Instance.FadeIn(_pauseInstance, Vector3.one);
        else
            _pauseInstance.SetActive(true);

        _pauseTransitioning = false;
        _pauseTransitionRoutine = null;
    }

    void ClosePausePanel(bool destroyInstance)
    {
        if (_pauseInstance == null)
        {
            ReleasePauseMenuHoldIfNeeded();
            return;
        }

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            ClosePausePanelImmediate(destroyInstance);
            return;
        }

        if (_pauseTransitionRoutine != null)
            StopCoroutine(_pauseTransitionRoutine);

        _pauseTransitionRoutine = StartCoroutine(ClosePausePanelRoutine(destroyInstance));
    }

    void ClosePausePanelImmediate(bool destroyInstance)
    {
        if (_pauseTransitionRoutine != null)
        {
            StopCoroutine(_pauseTransitionRoutine);
            _pauseTransitionRoutine = null;
        }

        _pauseTransitioning = false;

        if (_pauseInstance != null)
        {
            PanelTweenPresentation.Kill(_pauseInstance);

            if (destroyInstance)
            {
                Destroy(_pauseInstance);
                _pauseInstance = null;
            }
            else
                _pauseInstance.SetActive(false);
        }

        ReleasePauseMenuHoldIfNeeded();
    }

    IEnumerator ClosePausePanelRoutine(bool destroyInstance)
    {
        _pauseTransitioning = true;

        if (_pauseInstance != null && _pauseInstance.activeInHierarchy)
        {
            if (UIPanelAnimator.Instance != null)
                yield return UIPanelAnimator.Instance.FadeOut(_pauseInstance, destroyOnEnd: destroyInstance);
            else if (destroyInstance)
                Destroy(_pauseInstance);
            else
                _pauseInstance.SetActive(false);
        }

        if (destroyInstance)
            _pauseInstance = null;

        ReleasePauseMenuHoldIfNeeded();
        _pauseTransitioning = false;
        _pauseTransitionRoutine = null;
    }

    void ReleasePauseMenuHoldIfNeeded()
    {
        if (!_pauseMenuHoldActive)
            return;

        InputManager.PopPauseMenuHold();
        _pauseMenuHoldActive = false;
    }
}
