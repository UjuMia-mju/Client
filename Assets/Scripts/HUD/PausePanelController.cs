using UnityEngine;
using Protocol;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// PausePanel의 버튼 기능
/// </summary>
public class PausePanelController : MonoBehaviour
{
    [SerializeField] private GameObject SettingsPanel;
    [SerializeField] private string targetSceneName = Define.Scene.MAIN;
    [SerializeField] private float leaveRoomTimeoutSeconds = 2f;

    private bool _isLeavingToMain;
    private Coroutine _leaveRoomTimeoutCoroutine;
    private GameObject _settingsOverlay;
    private Coroutine _openSettingsRoutine;
    private Coroutine _closeSettingsRoutine;
    private GameObject _hiddenPauseContentForSettings;
    private bool _settingsManagedByHud;

    private void OnEnable()
    {
        if (PacketHandler.Instance != null)
            PacketHandler.Instance.OnLeaveRoomEvent += OnLeaveRoomResult;
    }

    private void OnDisable()
    {
        if (PacketHandler.Instance != null)
            PacketHandler.Instance.OnLeaveRoomEvent -= OnLeaveRoomResult;

        DestroySettingsOverlay();
    }

    void OnDestroy()
    {
        DestroySettingsOverlay();
    }

    /// <summary>설정이 열려 있거나 닫는 연출 중.</summary>
    public bool IsSettingsFlowActive =>
        _settingsOverlay != null || _closeSettingsRoutine != null || _settingsManagedByHud;

    /// <summary>ESC 등: 설정 오버레이만 닫기. 닫았으면 true.</summary>
    public bool TryCloseSettingsOverlay()
    {
        if (_closeSettingsRoutine != null)
            return true;

        if (_settingsOverlay != null)
        {
            _closeSettingsRoutine = StartCoroutine(CloseSettingsOverlayRoutine());
            return true;
        }

        var hud = Object.FindFirstObjectByType<HUDManager>(FindObjectsInactive.Include);
        if (hud != null && hud.IsSettingsOverlayActive)
        {
            _settingsManagedByHud = true;
            hud.CloseSettingsOverlay();
            return true;
        }

        return false;
    }

    /// <summary>HUDManager가 설정 닫기 연출을 끝낸 뒤 호출.</summary>
    public void NotifyHudSettingsOverlayClosed()
    {
        _settingsManagedByHud = false;
    }

    void DestroySettingsOverlay(bool restorePauseShell = false)
    {
        if (_closeSettingsRoutine != null)
        {
            StopCoroutine(_closeSettingsRoutine);
            _closeSettingsRoutine = null;
        }

        if (_openSettingsRoutine != null)
        {
            StopCoroutine(_openSettingsRoutine);
            _openSettingsRoutine = null;
        }

        if (_settingsOverlay != null)
        {
            PanelTweenPresentation.Kill(_settingsOverlay);
            Destroy(_settingsOverlay);
            _settingsOverlay = null;
        }

        _settingsManagedByHud = false;

        if (restorePauseShell)
            ShowPauseContentAfterSettings();
    }

    public void OnSettingsButtonClicked()
    {
        SoundManager.Instance.PlaySFX("Click2");

        if (SettingsPanel == null)
        {
            Debug.LogWarning("[PausePanelController] SettingsPanel 프리팹이 비어 있습니다.", this);
            return;
        }

        HidePauseShellForSettings();

        var hud = Object.FindFirstObjectByType<HUDManager>(FindObjectsInactive.Include);
        if (hud != null)
        {
            _settingsManagedByHud = true;
            hud.OpenSettingsOverlay(SettingsPanel, new Vector3(2f, 2f, 1f), this);
            return;
        }

        OpenSettingsOverlayStandalone();
    }

    /// <summary>설정 표시 중 일시정지 메뉴(딤·버튼) 숨김. <see cref="ShowPauseShellAfterSettings"/>와 짝.</summary>
    public void HidePauseShellForSettings() => HidePauseContentForSettings();

    /// <summary>설정 닫은 뒤 일시정지 메뉴 복구.</summary>
    public void ShowPauseShellAfterSettings() => ShowPauseContentAfterSettings();

    void OpenSettingsOverlayStandalone()
    {
        if (_settingsOverlay != null)
            return;

        Transform parent = GetComponentInParent<Canvas>() != null
            ? GetComponentInParent<Canvas>().transform
            : transform;

        _settingsOverlay = Instantiate(SettingsPanel, parent);
        _openSettingsRoutine = StartCoroutine(OpenSettingsOverlayRoutine(_settingsOverlay));
    }

    /// <summary>설정 표시 전 일시정지 메뉴(딤·버튼)만 숨깁니다. Canvas 루트는 설정 오버레이용으로 유지합니다.</summary>
    void HidePauseContentForSettings()
    {
        ShowPauseContentAfterSettings();

        Transform pauseRoot = GetPausePanelRootTransform();
        if (pauseRoot == null)
            return;

        Transform panel = pauseRoot.Find("Panel");
        if (panel != null)
        {
            _hiddenPauseContentForSettings = panel.gameObject;
            _hiddenPauseContentForSettings.SetActive(false);
            return;
        }

        _hiddenPauseContentForSettings = pauseRoot.gameObject;
        _hiddenPauseContentForSettings.SetActive(false);
    }

    void ShowPauseContentAfterSettings()
    {
        if (_hiddenPauseContentForSettings == null)
            return;

        _hiddenPauseContentForSettings.SetActive(true);
        _hiddenPauseContentForSettings = null;
    }

    Transform GetPausePanelRootTransform()
    {
        Transform t = transform;
        while (t != null)
        {
            if (t.CompareTag(Define.Tag.PAUSE_PANEL))
                return t;
            t = t.parent;
        }

        return transform.root;
    }

    IEnumerator OpenSettingsOverlayRoutine(GameObject panel)
    {
        var animator = UIPanelAnimator.Instance;
        if (animator != null)
            yield return animator.FadeIn(panel, new Vector3(2f, 2f, 1f));
        else if (panel != null)
        {
            var cg = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            panel.transform.localScale = new Vector3(2f, 2f, 1f);
        }

        _openSettingsRoutine = null;
    }

    IEnumerator CloseSettingsOverlayRoutine()
    {
        GameObject panel = _settingsOverlay;
        _settingsOverlay = null;

        if (panel != null)
        {
            var animator = UIPanelAnimator.Instance;
            if (animator != null)
                yield return animator.FadeOut(panel);
            else
                Destroy(panel);
        }

        yield return null;

        ShowPauseContentAfterSettings();
        _closeSettingsRoutine = null;
    }

    public void OnMainMenuButtonClicked()
    {
        SoundManager.Instance.PlaySFX("Click3");

        if (_isLeavingToMain)
            return;

        // 인게임에서 바로 메인으로 이동하면 방 정리가 되지 않으므로
        // 연결 중일 때는 먼저 방 나가기 패킷을 보냅니다.
        if (NetManager.Instance != null && NetManager.Instance.IsConnected)
        {
            // TODO(Server): 스테이지 호스트만 — 잔여 멤버는 S_ROOM_MEMBER_LEAVE 등 수신 후 RoomMembershipTracker 가 메인 처리.
            if (IsStageSelectMultiplayerHost())
                StageManager.NotifyHostEndingStageSessionForAllPeers();

            _isLeavingToMain = true;
            PacketDispatcher.Instance.SendLeaveRoom();
            _leaveRoomTimeoutCoroutine = StartCoroutine(LeaveRoomTimeout());
            return;
        }

        LoadMainScene();
    }

    public void OnExitButtonClicked()
    {
        SoundManager.Instance.PlaySFX("Click2");
        ExitPopupManager.ShowQuitConfirm();
    }

    static bool IsStageSelectMultiplayerHost()
    {
        if (SceneManager.GetActiveScene().name != Define.Scene.STAGE_SELECT)
            return false;
        var t = RoomMembershipTracker.Instance;
        if (t == null) return false;
        t.EnsureWired();
        return t.OrderedIds.Count > 0 && t.AmIFirst();
    }

    private void OnLeaveRoomResult(S_LEAVE_ROOM packet)
    {
        if (!_isLeavingToMain)
            return;

        if (_leaveRoomTimeoutCoroutine != null)
        {
            StopCoroutine(_leaveRoomTimeoutCoroutine);
            _leaveRoomTimeoutCoroutine = null;
        }

        _isLeavingToMain = false;
        LoadMainScene();
    }

    private IEnumerator LeaveRoomTimeout()
    {
        yield return new WaitForSecondsRealtime(leaveRoomTimeoutSeconds);

        // 서버 응답이 늦거나 누락되어도 UI가 멈추지 않게 메인으로 이동.
        if (_isLeavingToMain)
        {
            _isLeavingToMain = false;
            LoadMainScene();
        }
    }

    private void LoadMainScene()
    {
        SinglePlaySession.End();
        SceneLoader.Instance.LoadScene(targetSceneName);
    }
}