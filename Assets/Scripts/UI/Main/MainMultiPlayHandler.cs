using UnityEngine;
using Protocol;

/// <summary>
/// Main 씬에서 멀티플레이 버튼으로 SendCreateRoom() 후,
/// S_CREATE_ROOM 성공 시 C_ENTER_ROOM을 보내지 않고, 본인만 있는 S_ENTER_ROOM을 캐시한 뒤 로비 씬으로 이동한다.
/// (서버는 방 생성 시 이미 플레이어를 방에 넣으므로 C_ENTER_ROOM 시 "Already in a room" 실패를 반환함. 클라에서 우회.)
/// </summary>
public class MainMultiPlayHandler : MonoBehaviour
{
    private void OnEnable()
    {
        PacketManager.Instance.OnCreateRoomEvent += OnCreateRoomResult;
    }

    private void OnDisable()
    {
        if (PacketManager.Instance != null)
            PacketManager.Instance.OnCreateRoomEvent -= OnCreateRoomResult;
    }

    private void OnCreateRoomResult(S_CREATE_ROOM packet)
    {
        if (!packet.Success)
        {
            Debug.LogWarning($"[MainMultiPlayHandler] 방 생성 실패: {packet.ErrorMsg}");
            return;
        }

        // 방 생성 직후엔 C_ENTER_ROOM을 보내지 않음 (서버가 "Already in a room"으로 실패 응답하므로).
        // 대신 본인만 멤버로 넣은 S_ENTER_ROOM을 캐시해 두고, 로비 로드 후 LobbyRoomClient가 적용.
        var synthetic = new S_ENTER_ROOM
        {
            Success = true,
            Room = packet.Room
        };
        synthetic.Members.Add(new RoomMemberInfo
        {
            Player = new Player
            {
                Id = (ulong)NetManager.Instance._playerId,
                Name = NetManager.Instance.PlayerName ?? "",
                Tag = NetManager.Instance.PlayerTag
            },
            IsReady = false
        });
        PacketManager.SetCachedEnterRoom(synthetic);

        SceneLoader.Instance.LoadScene(Define.Scene.LOBBY);
    }
}
