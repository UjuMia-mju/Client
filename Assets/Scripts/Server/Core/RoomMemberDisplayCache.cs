using System;
using System.Collections.Generic;
using Protocol;
using UnityEngine;

/// <summary>
/// 로비·스테이지 선택 등에서 공통으로 참조할 플레이어 표시용 이름·레디 상태.
/// S_ENTER_ROOM / 입장·퇴장 / S_READY로 갱신됩니다 (RoomMembershipTracker의 id_order와 함께 사용).
/// </summary>
public sealed class RoomMemberDisplayCache : Singleton<RoomMemberDisplayCache>
{
    public readonly struct Entry
    {
        public readonly string DisplayName;
        public readonly bool IsReady;

        public Entry(string displayName, bool isReady)
        {
            DisplayName = displayName;
            IsReady = isReady;
        }
    }

    private readonly Dictionary<ulong, Entry> _byId = new();
    private bool _wired;

    /// <summary>이름·레디 상태 캐시가 바뀌었을 때 (UI에서 구독)</summary>
    public event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        Instance.TryWire();
    }

    /// <summary>PacketHandler 준비 이후 패널 표시 등 직전에 호출 가능. 첫 번째 성공 때만 구독합니다.</summary>
    public void WarmUp()
    {
        TryWire();
    }

    private void TryWire()
    {
        if (_wired) return;

        var handler = PacketHandler.Instance;
        if (handler == null)
            return;

        _wired = true;
        handler.OnEnterRoomEvent += OnEnterRoom;
        handler.OnRoomMemberEnterEvent += OnMemberEnter;
        handler.OnRoomMemberLeaveEvent += OnMemberLeave;
        handler.OnLeaveRoomEvent += OnLeaveRoom;
        handler.OnReadyEvent += OnReady;
    }

    public bool TryGet(ulong playerId, out Entry entry)
    {
        TryWire();
        return _byId.TryGetValue(playerId, out entry);
    }

    public void SetReady(ulong playerId, bool isReady)
    {
        TryWire();
        if (!_byId.TryGetValue(playerId, out var e))
            return;
        _byId[playerId] = new Entry(e.DisplayName, isReady);
        NotifyChanged();
    }

    static string FormatPlayerLabel(global::Protocol.Player player)
    {
        if (player == null)
            return "Player";

        bool hasTag = player.Tag != 0;
        if (hasTag && !string.IsNullOrEmpty(player.Name))
            return $"{player.Name}#{player.Tag}";
        if (!string.IsNullOrEmpty(player.Name))
            return player.Name;

        return $"Player {player.Id}";
    }

    private void OnEnterRoom(S_ENTER_ROOM packet)
    {
        if (packet == null || !packet.Success)
            return;

        _byId.Clear();
        foreach (RoomMemberInfo m in packet.Members)
            UpsertMember(m);

        NotifyChanged();
    }

    private void OnMemberEnter(S_ROOM_MEMBER_ENTER packet)
    {
        if (packet?.Member == null)
            return;
        UpsertMember(packet.Member);
        NotifyChanged();
    }

    private void UpsertMember(RoomMemberInfo member)
    {
        if (member?.Player == null)
            return;

        ulong id = (ulong)member.Player.Id;
        string label = FormatPlayerLabel(member.Player);
        _byId[id] = new Entry(label, member.IsReady);
    }

    private void OnReady(S_READY packet)
    {
        if (packet == null)
            return;

        ulong id = packet.PlayerId;
        if (_byId.TryGetValue(id, out var prev))
            _byId[id] = new Entry(prev.DisplayName, packet.IsReady);
        else
            _byId[id] = new Entry($"Player {id}", packet.IsReady);

        NotifyChanged();
    }

    private void OnMemberLeave(S_ROOM_MEMBER_LEAVE packet)
    {
        if (packet == null)
            return;
        _byId.Remove(packet.PlayerId);
        NotifyChanged();
    }

    private void OnLeaveRoom(S_LEAVE_ROOM packet)
    {
        _byId.Clear();
        NotifyChanged();
    }

    void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
