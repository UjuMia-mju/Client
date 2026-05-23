using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Protocol;

public class PeerStatManager : BaseStatManager<PeerStatManager>
{
    void Start()
    {
        if (!_playerStats.ContainsKey(0))
            _playerStats.Add(0, new PlayerStatState(5));

        HostPacketHandler.Instance.OnPlayerEnterEvent += OnPlayerEnterReceived;
        HostPacketHandler.Instance.OnEnterGameEvent += OnEnterGameReceived;
    }

    protected override void OnDestroy()
    {
        if (HostPacketHandler.Instance != null)
        {
            HostPacketHandler.Instance.OnPlayerEnterEvent -= OnPlayerEnterReceived;
            HostPacketHandler.Instance.OnEnterGameEvent -= OnEnterGameReceived;
        }
        base.OnDestroy();
    }

    /// <summary>S_ENTER_GAME 수신 시 전체 플레이어 목록을 스탯 딕셔너리에 등록합니다.</summary>
    private void OnEnterGameReceived(S_ENTER_GAME packet)
    {
        // 자기 자신 등록
        ulong myId = NetManager.Instance._playerId;
        if (myId != 0 && !_playerStats.ContainsKey(myId))
        {
            _playerStats.Add(myId, new PlayerStatState(5));
            Debug.Log($"[PeerStatManager] 자기 자신 등록(S_ENTER_GAME): playerId={myId}");
        }

        // 전체 플레이어 등록
        foreach (var playerInfo in packet.Players)
        {
            ulong id = (ulong)playerInfo.PlayerId;
            if (!_playerStats.ContainsKey(id))
            {
                _playerStats.Add(id, new PlayerStatState(5));
                Debug.Log($"[PeerStatManager] 플레이어 등록(S_ENTER_GAME): playerId={id}");
            }
        }
    }

    private void OnPlayerEnterReceived(S_PLAYER_ENTER packet)
    {
        ulong myId = NetManager.Instance._playerId;
        if (myId != 0 && !_playerStats.ContainsKey(myId))
        {
            _playerStats.Add(myId, new PlayerStatState(5));
            Debug.Log($"[PeerStatManager] 자기 자신 등록: playerId={myId}");
        }

        ulong remoteId = (ulong)packet.Player.PlayerId;
        if (!_playerStats.ContainsKey(remoteId))
        {
            _playerStats.Add(remoteId, new PlayerStatState(5));
            Debug.Log($"[PeerStatManager] 원격 플레이어 등록: playerId={remoteId}");
        }
    }

    /// <summary>
    /// 로컬 피어 본인의 PlayerStat을 stat 슬롯에 바인딩한다.
    /// - boundPlayer 연결: PlayerStatState.CallOnHpChanged() → PlayerStat.OnHpChanged 까지 전파.
    /// - statData 공유: 호스트가 S_PLAYER_STAT으로 내려준 hp/oxygen이 PlayerStat.statData에 즉시 반영.
    /// 호스트의 HostStatManager.RegisterPlayer와 동일한 역할.
    /// </summary>
    public void RegisterPlayer(ulong playerId, PlayerStat stat)
    {
        if (stat == null) return;

        // 동일 stat이 다른 키로 남아 있으면 정리 (id가 늦게 확정되는 경우 대비)
        var staleKeys = _playerStats
            .Where(kv => kv.Value != null && kv.Value.boundPlayer == stat && kv.Key != playerId)
            .Select(kv => kv.Key)
            .ToList();
        foreach (var k in staleKeys)
            _playerStats.Remove(k);

        if (_playerStats.TryGetValue(playerId, out var existing) && existing != null)
        {
            existing.statData = stat.statData;
            existing.boundPlayer = stat;
        }
        else
        {
            _playerStats[playerId] = new PlayerStatState(5) { statData = stat.statData, boundPlayer = stat };
        }
        Debug.Log($"[PeerStatManager] RegisterPlayer: {playerId}");
    }

    public IReadOnlyDictionary<ulong, PlayerStatState> GetAllRemoteStats() => _playerStats;

    #region stat update methods

    public void DecreaseHp(ulong playerId, int amount)
    {
        var damage = new Protocol.DamageEventData { DamageAmount = amount };
        PacketSender.Instance.SendPlayerStatEvent(StatEventType.DamageTaken, playerId, damage);
    }

    public void IncreaseHp(ulong playerId, int amount)
    {
        var heal = new HealEventData { HealAmount = amount };
        PacketSender.Instance.SendPlayerStatEvent(StatEventType.Healed, playerId, null, heal);
    }

    public void DecreaseOxygen(ulong playerId)
    {
        var oxygen = new OxygenEventData
        {
            ChangeType = OxygenChangeType.ConsumeNatural,
            Amount = 0.01f
        };
        PacketSender.Instance.SendPlayerStatEvent(StatEventType.OxygenChanged, playerId, null, null, oxygen);
    }

    public void IncreaseOxygen(ulong playerId)
    {
        var oxygen = new OxygenEventData
        {
            ChangeType = OxygenChangeType.RestoreArea,
            Amount = 0.02f
        };
        PacketSender.Instance.SendPlayerStatEvent(StatEventType.OxygenChanged, playerId, null, null, oxygen);
    }

    public IReadOnlyDictionary<ulong, PlayerStatState> GetAllStats() => _playerStats;

    #endregion
}
