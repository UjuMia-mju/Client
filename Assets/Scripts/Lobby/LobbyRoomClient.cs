using System.Collections.Generic;
using Protocol;
using UnityEngine;

/// <summary>
/// 로비에서 방 관련 패킷을 받아 UI/연출로 연결하기 위한 간단한 브리지.
/// - S_ENTER_ROOM: 방 입장 결과/현재 멤버 리스트 수신
/// - S_ROOM_MEMBER_ENTER/LEAVE: 멤버 변동 수신
/// </summary>
public class LobbyRoomClient : MonoBehaviour
{
    [Header("Optional")]
    [SerializeField] private LobbyManager lobbyManager;

    private readonly HashSet<ulong> _members = new HashSet<ulong>();

    private void OnEnable()
    {
        PacketManager.Instance.OnEnterRoomEvent += OnEnterRoom;
        PacketManager.Instance.OnRoomMemberEnterEvent += OnRoomMemberEnter;
        PacketManager.Instance.OnRoomMemberLeaveEvent += OnRoomMemberLeave;
    }

    private void OnDisable()
    {
        if (PacketManager.Instance == null)
            return;

        PacketManager.Instance.OnEnterRoomEvent -= OnEnterRoom;
        PacketManager.Instance.OnRoomMemberEnterEvent -= OnRoomMemberEnter;
        PacketManager.Instance.OnRoomMemberLeaveEvent -= OnRoomMemberLeave;
    }

    private void OnEnterRoom(S_ENTER_ROOM packet)
    {
        if (!packet.Success)
        {
            Debug.LogWarning($"[LobbyRoomClient] 방 입장 실패: {packet.ErrorMsg}");
            return;
        }

        _members.Clear();
        Debug.Log($"[LobbyRoomClient] 방 입장 성공: {packet.Room.RoomName} (roomId={packet.Room.RoomId})");

        foreach (RoomMemberInfo member in packet.Members)
        {
            _members.Add(member.Player.Id);
            Debug.Log($"[LobbyRoomClient] 멤버: {member.Player.Name}#{member.Player.Tag} (id={member.Player.Id}) ready={member.IsReady}");
        }

        // 간단 연출: 현재 멤버 수만큼 스폰 (실제 네트워크 플레이어 스폰 로직으로 대체 가능)
        if (lobbyManager != null)
        {
            for (int i = 0; i < packet.Members.Count; i++)
                lobbyManager.SpawnNewPlayer();
        }
    }

    private void OnRoomMemberEnter(S_ROOM_MEMBER_ENTER packet)
    {
        ulong id = packet.Member.Player.Id;
        if (_members.Add(id))
        {
            Debug.Log($"[LobbyRoomClient] 멤버 입장: {packet.Member.Player.Name}#{packet.Member.Player.Tag} (id={id})");
            lobbyManager?.SpawnNewPlayer();
        }
    }

    private void OnRoomMemberLeave(S_ROOM_MEMBER_LEAVE packet)
    {
        if (_members.Remove(packet.PlayerId))
        {
            Debug.Log($"[LobbyRoomClient] 멤버 퇴장: {packet.PlayerName} (id={packet.PlayerId}), newOwnerId={packet.NewOwnerId}");
        }
    }
}

