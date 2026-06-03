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

    /// <summary>싱글플레이: 본인만 방에 있는 것처럼 입장 순서를 설정해 AmIFirst()·행성 선택이 동작하게 합니다.</summary>
    public void SetSoloHost(ulong localPlayerId)
    {
        EnsureWired();
        _orderedIds.Clear();
        if (localPlayerId != 0)
            _orderedIds.Add(localPlayerId);
        Debug.Log($"[RoomMembershipTracker] SetSoloHost id={localPlayerId}");
    }

    public void EnsureWired()
    {
        if (_wired) return;
        _wired = true;

        PacketHandler.Instance.OnEnterRoomEvent += OnEnterRoom;
        PacketHandler.Instance.OnRoomMemberEnterEvent += OnMemberEnter;
        PacketHandler.Instance.OnRoomMemberLeaveEvent += OnMemberLeave;
        PacketHandler.Instance.OnLeaveRoomEvent += OnLeaveRoom;
        PacketHandler.Instance.OnRoomDestroyEvent += OnRoomDestroy;

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
        if (packet == null) return;
        ulong localId = NetManager.Instance != null ? NetManager.Instance._playerId : 0UL;
        ulong id = packet.PlayerId;
        ulong hostIdBefore = _orderedIds.Count > 0 ? _orderedIds[0] : 0UL;
        bool wasHostLeaving = _orderedIds.Count > 0 && _orderedIds[0] == id;
        bool removed = _orderedIds.Remove(id);
        Debug.Log(
            $"[RoomMembershipTracker] S_ROOM_MEMBER_LEAVE id={id}, name='{packet.PlayerName}', removed={removed}, " +
            $"wasHostLeaving={wasHostLeaving}, localId={localId}, hostIdBefore={hostIdBefore}, " +
            $"orderedIds=[{string.Join(",", _orderedIds)}]");

        // 방장(입장 순 0번) 퇴장이면 방 해산으로 보고 메인으로.
        // 로비/스테이지/인게임 어디에 있든 모두 메인으로 보냅니다.
        if (wasHostLeaving)
        {
            GoMainIfNeeded();
        }
    }

    private void OnLeaveRoom(S_LEAVE_ROOM packet)
    {
        _orderedIds.Clear();
        Debug.Log($"[RoomMembershipTracker] OnLeaveRoom -> clear, success={packet?.Success}, localId={NetManager.Instance?._playerId}");
        // 씬 전환은 LobbyLeaveButton·PausePanelController 등 요청한 UI가 담당합니다.
        // 여기서 GoMainIfNeeded()까지 하면 LoadScene이 중복 호출되어 로딩이 90%에서 멈춥니다.
    }

    private void OnRoomDestroy(S_ROOM_DESTROY packet)
    {
        if (packet == null || !packet.Success)
            return;

        _orderedIds.Clear();
        PacketHandler.ClearCachedEnterRoom();
        Debug.Log(
            $"[RoomMembershipTracker] S_ROOM_DESTROY -> clear, roomId={packet.RoomId}, localId={NetManager.Instance?._playerId}");
        GoMainIfNeeded();
    }

    private void GoMainIfNeeded()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == Define.Scene.MAIN)
            return;

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(Define.Scene.MAIN);
    }
}