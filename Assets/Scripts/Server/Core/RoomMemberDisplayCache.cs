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

    /// <summary>인게임 입장 패킷 등에 넣을 표시 이름. 로비 캐시가 없으면 fallback.</summary>
    public static string GetDisplayNameOrFallback(ulong playerId, string fallback)
    {
        var c = Instance;
        c?.WarmUp();
        if (c != null && c.TryGet(playerId, out var e) && !string.IsNullOrWhiteSpace(e.DisplayName))
            return e.DisplayName;
        return fallback;
    }

    /// <summary>로비·카운트다운 UI에 레디 표시가 의미 있는 세션(멀티, 로비 경유)인지.</summary>
    public static bool IsLobbyReadyDisplayRelevant =>
        !SinglePlaySession.IsActive && !SinglePlaySession.IsAwaitingRoomBootstrap;

    static bool StoreReadyForDisplay(bool isReady) =>
        isReady && IsLobbyReadyDisplayRelevant;

    public void SetReady(ulong playerId, bool isReady)
    {
        TryWire();
        if (!_byId.TryGetValue(playerId, out var e))
            return;
        _byId[playerId] = new Entry(e.DisplayName, StoreReadyForDisplay(isReady));
        NotifyChanged();
    }

    static string StripTagSuffix(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        int h = s.IndexOf('#');
        return h >= 0 ? s.Substring(0, h).TrimEnd() : s.TrimEnd();
    }

    /// <summary>인게임 등 표시용: <c>이름#태그</c> 형태가 오면 태그 접미사를 뗍니다.</summary>
    public static string WithoutDiscriminatorTag(string raw) => StripTagSuffix(raw?.Trim() ?? "");

    static string FormatPlayerLabel(string name, int idForFallback)
    {
        name = StripTagSuffix(name?.Trim() ?? "");
        if (!string.IsNullOrEmpty(name))
            return name;
        if (idForFallback != 0)
            return $"Player {idForFallback}";
        return "Player";
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
        string name = member.Player.Name ?? "";

        var nm = NetManager.Instance;
        if (nm != null && id == nm._playerId &&
            string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(nm.PlayerName))
            name = nm.PlayerName;

        string label = FormatPlayerLabel(name, member.Player.Id);
        _byId[id] = new Entry(label, StoreReadyForDisplay(member.IsReady));
    }

    /// <summary>로그인 직후 등, 이미 캐시된 로컬 멤버 행에 이름을 다시 반영합니다.</summary>
    public void RefreshLocalMemberFromNetManager()
    {
        TryWire();
        var nm = NetManager.Instance;
        if (nm == null || nm._playerId == 0)
            return;

        ulong id = nm._playerId;
        if (!_byId.TryGetValue(id, out var prev))
            return;

        string name = nm.PlayerName ?? "";
        if (string.IsNullOrEmpty(name))
            name = StripTagSuffix(prev.DisplayName);

        string label = FormatPlayerLabel(name, (int)id);
        _byId[id] = new Entry(label, prev.IsReady);
        NotifyChanged();
    }

    private void OnReady(S_READY packet)
    {
        if (packet == null)
            return;

        ulong id = packet.PlayerId;
        bool ready = StoreReadyForDisplay(packet.IsReady);
        if (_byId.TryGetValue(id, out var prev))
            _byId[id] = new Entry(prev.DisplayName, ready);
        else
            _byId[id] = new Entry($"Player {id}", ready);

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
