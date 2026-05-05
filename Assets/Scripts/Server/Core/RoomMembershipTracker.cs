using System.Collections.Generic;
using UnityEngine;
using Protocol;
using UnityEngine.SceneManagement;

/// <summary>
/// 클라이언트에서 방 입장 순서를 자체 추적합니다.
/// S_GAME_READY_TO_START.IdOrder가 도착하지 않을 때의 폴백 호스트 결정 등에 사용.
/// 0번째(가장 먼저 입장한) 멤버를 호스트로 간주합니다.
/// </summary>
public class RoomMembershipTracker : Singleton<RoomMembershipTracker>
{
    private readonly List<ulong> _orderedIds = new List<ulong>();
    private bool _wired;

    /// <summary>앱 시작 시 자동으로 인스턴스를 만들고 이벤트를 즉시 구독합니다.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // _ = Instance만으로는 EnsureWired가 안 돈다. 명시적으로 워이어업.
        Instance.EnsureWired();
    }

    public IReadOnlyList<ulong> OrderedIds
    {
        get { EnsureWired(); return _orderedIds; }
    }

    /// <summary>본인이 방의 0번째(가장 먼저 입장)인지 여부.</summary>
    public bool AmIFirst()
    {
        EnsureWired();
        if (_orderedIds.Count == 0) return false;
        if (NetManager.Instance == null) return false;
        return _orderedIds[0] == NetManager.Instance._playerId;
    }

    public void Reset()
    {
        _orderedIds.Clear();
        Debug.Log("[RoomMembershipTracker] Reset");
    }

    public void EnsureWired()
    {
        if (_wired) return;
        _wired = true;

        PacketHandler.Instance.OnEnterRoomEvent += OnEnterRoom;
        PacketHandler.Instance.OnRoomMemberEnterEvent += OnMemberEnter;
        PacketHandler.Instance.OnRoomMemberLeaveEvent += OnMemberLeave;
        PacketHandler.Instance.OnLeaveRoomEvent += OnLeaveRoom;

        Debug.Log("[RoomMembershipTracker] Wired to PacketHandler events.");

        // 부팅 이전에 도착해 캐시된 S_ENTER_ROOM(synthetic 포함)을 즉시 소비
        var cached = PacketHandler.PeekCachedEnterRoom();
        if (cached != null)
        {
            Debug.Log("[RoomMembershipTracker] 캐시된 S_ENTER_ROOM 소비 → 초기 멤버 반영");
            OnEnterRoom(cached);
        }
    }

    private void OnEnterRoom(S_ENTER_ROOM packet)
    {
        if (packet == null || !packet.Success) return;

        _orderedIds.Clear();
        foreach (var member in packet.Members)
        {
            if (member?.Player == null) continue;
            ulong id = (ulong)member.Player.Id;
            if (!_orderedIds.Contains(id))
                _orderedIds.Add(id);
        }
        Debug.Log($"[RoomMembershipTracker] OnEnterRoom. orderedIds=[{string.Join(",", _orderedIds)}]");
    }

    private void OnMemberEnter(S_ROOM_MEMBER_ENTER packet)
    {
        if (packet?.Member?.Player == null) return;
        ulong id = (ulong)packet.Member.Player.Id;
        if (!_orderedIds.Contains(id))
            _orderedIds.Add(id);
        Debug.Log($"[RoomMembershipTracker] OnMemberEnter. id={id}, orderedIds=[{string.Join(",", _orderedIds)}]");
    }

    private void OnMemberLeave(S_ROOM_MEMBER_LEAVE packet)
    {
        bool wasHostLeaving = _orderedIds.Count > 0 && _orderedIds[0] == packet.PlayerId;
        ulong id = packet.PlayerId;
        if (_orderedIds.Remove(id))
            Debug.Log($"[RoomMembershipTracker] OnMemberLeave. id={id}, orderedIds=[{string.Join(",", _orderedIds)}]");

        // TODO(Server): 스테이지 선택에서 “전원 메인”이면 방장 퇴장 시 new_owner_id=0·방 해산과 맞춰야
        // 아래 else if (wasHostLeaving) → GoMainIfInGame() 이 실행됨. (방장만 교체하면 여기 안 탈 수 있음.)
        // 서버가 새 방장 ID를 명시했다면 그 ID를 0번째로 보정
        if (packet.NewOwnerId != 0UL)
        {
            ulong newOwner = packet.NewOwnerId;
            _orderedIds.Remove(newOwner);
            _orderedIds.Insert(0, newOwner);
            Debug.Log($"[RoomMembershipTracker] NewOwner 적용. orderedIds=[{string.Join(",", _orderedIds)}]");
        }
        else if (wasHostLeaving)
        {
            // 인게임·스테이지 선택 등 로비가 아닐 때는 메인으로 돌립니다.
            GoMainIfInGame();
        }
    }

    private void OnLeaveRoom(S_LEAVE_ROOM packet)
    {
        _orderedIds.Clear();
        Debug.Log("[RoomMembershipTracker] OnLeaveRoom -> 클리어");
        GoMainIfInGame();
    }

    private void GoMainIfInGame()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == Define.Scene.MAIN || scene == Define.Scene.LOBBY)
            return;

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(Define.Scene.MAIN);
    }
}