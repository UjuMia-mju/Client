using UnityEngine;
using Protocol;

/// <summary>
/// Main 씬에서 멀티플레이 버튼으로 SendCreateRoom() 후,
/// S_CREATE_ROOM 성공 시 C_ENTER_ROOM을 보내고 로비 씬으로 이동한다.
/// </summary>
public class MainMultiPlayHandler : MonoBehaviour
{
    private void OnEnable()
    {
        PacketHandler.Instance.OnCreateRoomEvent += OnCreateRoomResult;
    }

    private void OnDisable()
    {
        if (PacketHandler.Instance != null)
            PacketHandler.Instance.OnCreateRoomEvent -= OnCreateRoomResult;
    }

    private void OnCreateRoomResult(S_CREATE_ROOM packet)
    {
        if (!packet.Success)
        {
            Debug.LogWarning($"[MainMultiPlayHandler] 방 생성 실패: {packet.ErrorMsg}");
            return;
        }

        // 로비 씬 로드 타이밍 때문에 S_ENTER_ROOM 이벤트를 놓칠 수 있어,
        // 최소 1명(본인) 상태는 캐시로 보장해 둔다. (LobbyRoomClient가 씬 로드 후 적용)
        var synthetic = new S_ENTER_ROOM
        {
            Success = true,
            Room = packet.Room
        };
        synthetic.Members.Add(new RoomMemberInfo
        {
            Player = new Protocol.Player
            {
                Id = NetManager.Instance._playerId,
                Name = NetManager.Instance.PlayerName ?? "",
                Tag = NetManager.Instance.PlayerTag
            },
            IsReady = false
        });
        PacketManager.SetCachedEnterRoom(synthetic);


        PacketDispatcher.Instance.SendEnterRoom(packet.Room.RoomId);

        SceneLoader.Instance.LoadScene(Define.Scene.LOBBY);
    }
}
