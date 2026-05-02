using Unity.VisualScripting;
using UnityEngine;
using Protocol;
using System.Collections;

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
            PacketDispatcher.Instance.SendLeaveRoom();

        // 1. 실제 빌드된 게임 종료
        Application.Quit();
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