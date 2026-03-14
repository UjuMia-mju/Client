using UnityEngine;
using Protocol;

/// <summary>
/// Main 씬에서 멀티플레이 버튼으로 SendCreateRoom() 후,
/// S_CREATE_ROOM 성공 시 방 입장 요청하고 로비 씬으로 이동한다.
/// Main 씬의 어느 한 오브젝트에만 붙여 두면 됨.
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
        if (packet.Success)
        {
            NetManager.Instance.SendEnterRoom(packet.Room.RoomId);
            SceneLoader.Instance.LoadScene(Define.Scene.LOBBY);
        }
        else
        {
            Debug.LogWarning($"[MainMultiPlayHandler] 방 생성 실패: {packet.ErrorMsg}");
        }
    }
}
