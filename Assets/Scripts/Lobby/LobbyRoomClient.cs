using System.Collections.Generic;
using Protocol;
using UnityEngine;

/// <summary>
/// 로비에서 방 관련 패킷을 받아 LobbyManager(스폰/디스폰)로 연결하는 브리지.
/// - S_ENTER_ROOM: 방 입장 결과/현재 멤버 리스트 → 기존 스폰 클리어 후 멤버별 스폰
/// - S_ROOM_MEMBER_ENTER: 새 멤버 입장 → 해당 플레이어 스폰
/// - S_ROOM_MEMBER_LEAVE: 멤버 퇴장 → 해당 플레이어 디스폰
/// </summary>
public class LobbyRoomClient : MonoBehaviour
{
    [Header("Optional")]
    [SerializeField] private LobbyManager lobbyManager; // 로비 캐릭터 스폰/디스폰 담당. 비어 있으면 스폰 연출 없음.

    /// <summary>현재 방에 있는 멤버 ID 집합 (중복 스폰 방지, 퇴장 시 디스폰 대상 확인)</summary>
    private readonly HashSet<ulong> _members = new HashSet<ulong>();

    private void OnEnable()
    {
        PacketManager.Instance.OnEnterRoomEvent += OnEnterRoom;
        PacketManager.Instance.OnRoomMemberEnterEvent += OnRoomMemberEnter;
        PacketManager.Instance.OnRoomMemberLeaveEvent += OnRoomMemberLeave;

        // 방 생성 후 로비 씬 전환 시, S_ENTER_ROOM이 로비 로드 전에 도착해 이벤트를 놓친 경우 캐시에서 복구
        S_ENTER_ROOM cached = PacketManager.GetAndClearCachedEnterRoom();
        if (cached != null)
        {
            Debug.Log("[LobbyRoomClient] 캐시된 S_ENTER_ROOM 적용 (방 생성 후 로비 전환)");
            ApplyEnterRoomResult(cached);
        }
    }

    private void OnDisable()
    {
        if (PacketManager.Instance == null)
            return;
        PacketManager.Instance.OnEnterRoomEvent -= OnEnterRoom;
        PacketManager.Instance.OnRoomMemberEnterEvent -= OnRoomMemberEnter;
        PacketManager.Instance.OnRoomMemberLeaveEvent -= OnRoomMemberLeave;
    }

    /// <summary>방 입장 결과 수신. 성공 시 기존 스폰 전부 제거 후 현재 멤버 목록으로 이름 표시 스폰.</summary>
    private void OnEnterRoom(S_ENTER_ROOM packet)
    {
        if (!packet.Success)
        {
            Debug.LogWarning($"[LobbyRoomClient] 방 입장 실패: {packet.ErrorMsg}");
            return;
        }
        ApplyEnterRoomResult(packet);
    }

    /// <summary>S_ENTER_ROOM(성공) 패킷으로 멤버 목록 스폰. OnEnterRoom과 캐시 복구에서 공통 사용.</summary>
    private void ApplyEnterRoomResult(S_ENTER_ROOM packet)
    {
        _members.Clear();
        Debug.Log($"[LobbyRoomClient] 방 입장 성공: {packet.Room.RoomName} (roomId={packet.Room.RoomId})");

        if (lobbyManager == null)
        {
            Debug.LogWarning("[LobbyRoomClient] LobbyManager가 할당되지 않았습니다.");
            return;
        }

        lobbyManager.ClearSpawnedPlayers();
        foreach (RoomMemberInfo member in packet.Members)
        {
            _members.Add(member.Player.Id);
            Debug.Log($"[LobbyRoomClient] 멤버: {member.Player.Name}#{member.Player.Tag} (id={member.Player.Id}) ready={member.IsReady}");
            lobbyManager.SpawnNewPlayer(member.Player.Name, member.Player.Id);
        }
    }

    /// <summary>다른 플레이어가 방에 입장했을 때. 해당 플레이어용 LobbyAstronut 스폰.</summary>
    private void OnRoomMemberEnter(S_ROOM_MEMBER_ENTER packet)
    {
        ulong id = packet.Member.Player.Id;
        if (_members.Add(id))
        {
            Debug.Log($"[LobbyRoomClient] 멤버 입장: {packet.Member.Player.Name}#{packet.Member.Player.Tag} (id={id})");
            lobbyManager?.SpawnNewPlayer(packet.Member.Player.Name, id);
        }
    }

    /// <summary>플레이어가 방을 나갔을 때. 해당 플레이어용 LobbyAstronut 디스폰.</summary>
    private void OnRoomMemberLeave(S_ROOM_MEMBER_LEAVE packet)
    {
        if (_members.Remove(packet.PlayerId))
        {
            Debug.Log($"[LobbyRoomClient] 멤버 퇴장: {packet.PlayerName} (id={packet.PlayerId}), newOwnerId={packet.NewOwnerId}");
            lobbyManager?.DespawnPlayer(packet.PlayerId);
        }
    }
}

