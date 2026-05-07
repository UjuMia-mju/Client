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

    private void OnEnable()
    {
        if (PacketHandler.Instance != null)
            PacketHandler.Instance.OnLeaveRoomEvent += OnLeaveRoomResult;
    }

    private void OnDisable()
    {
        if (PacketHandler.Instance != null)
            PacketHandler.Instance.OnLeaveRoomEvent -= OnLeaveRoomResult;
    }

    public void OnSettingsButtonClicked()
    {
        var hud = Object.FindFirstObjectByType<HUDManager>();
        if (hud != null)
        {
            // 생성과 동시에 목표 크기를 넘겨줍니다.
            hud.OpenPanel(SettingsPanel, new Vector3(2f, 2f, 1f));
        }
    }

    public void OnMainMenuButtonClicked()
    {
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
        if (NetManager.Instance != null && NetManager.Instance.IsConnected)
        {
            // TODO(Server): Quit 직후에는 C_LEAVE_ROOM 이 안 나갈 수 있음 — 끊김 시 퇴장 브로드캐스트는 서버 담당.
            if (IsStageSelectMultiplayerHost())
                StageManager.NotifyHostEndingStageSessionForAllPeers();
            PacketDispatcher.Instance.SendLeaveRoom();
        }

        // 1. 실제 빌드된 게임 종료
        Application.Quit();
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
        SceneLoader.Instance.LoadScene(targetSceneName);
    }
}