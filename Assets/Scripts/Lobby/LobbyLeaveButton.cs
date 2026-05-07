using Protocol;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 뒤로가기 버튼: 방 나가기 요청 후 메인 씬으로 이동.
/// </summary>
public class LobbyLeaveButton : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private string targetSceneName = Define.Scene.MAIN;

    private void OnEnable()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnClickBack);
            backButton.interactable = true;
        }

        if (PacketHandler.Instance != null)
            PacketHandler.Instance.OnLeaveRoomEvent += OnLeaveRoomResult;
    }

    private void OnDisable()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(OnClickBack);

        if (PacketHandler.Instance != null)
            PacketHandler.Instance.OnLeaveRoomEvent -= OnLeaveRoomResult;
    }

    private void OnClickBack()
    {
        if (NetManager.Instance == null || !NetManager.Instance.IsConnected)
        {
            Debug.LogWarning("[LobbyLeaveButton] 서버에 연결되어 있지 않습니다.");
            return;
        }

        if (backButton != null)
            backButton.interactable = false;

        // 방장이 나갈 때는 남은 피어가 즉시 메인으로 돌아가도록 relay 신호를 먼저 보낸다.
        if (RoomMembershipTracker.Instance != null && RoomMembershipTracker.Instance.AmIFirst())
            PacketSender.Instance.BroadcastReturnToStageSelect();

        PacketDispatcher.Instance.SendLeaveRoom();
    }

    private void OnLeaveRoomResult(S_LEAVE_ROOM packet)
    {
        if (backButton != null)
            backButton.interactable = true;

        if (!packet.Success)
        {
            Debug.LogWarning($"[LobbyLeaveButton] 방 떠나기 실패: playerId={packet.PlayerId}");
            return;
        }

        SceneLoader.Instance.LoadScene(targetSceneName);
    }
}
