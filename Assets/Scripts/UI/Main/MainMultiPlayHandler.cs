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

        // 서버가 "방에 들어간 상태"로 인식하도록 EnterRoom을 명시적으로 보낸다.
        // (초대 기능에서 Not in a room 방지)
        NetManager.Instance.SendEnterRoom(packet.Room.RoomId);

        SceneLoader.Instance.LoadScene(Define.Scene.LOBBY);
    }
}
