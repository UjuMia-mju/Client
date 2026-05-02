using System.Collections.Generic;
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
