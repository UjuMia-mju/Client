using System.Collections.Generic;
using UnityEngine;
using Protocol;

public class PeerStatManager : BaseStatManager<PeerStatManager>
{
    void Start()
    {
        // 호스트 플레이어 초기화
        if (!_playerStats.ContainsKey(0))
            _playerStats.Add(0, new PlayerStat());

        // 자기 자신의 playerId는 S_PLAYER_ENTER 수신 후 확정되므로
        // 이벤트 구독으로 처리
        HostPacketHandler.Instance.OnPlayerEnterEvent += OnPlayerEnterReceived;
    }

    private void OnDestroy()
    {
        if (HostPacketHandler.Instance != null)
            HostPacketHandler.Instance.OnPlayerEnterEvent -= OnPlayerEnterReceived;
    }

    // S_PLAYER_ENTER에서 첫 번째 패킷이 자기 자신의 ID
    // HostPacketHandler에서 _playerId=0일 때만 ID를 갱신하고 이벤트를 올리지 않음
    // 즉 이 이벤트가 처음 발생하는 시점 = _playerId가 이미 확정된 이후
    // 따라서 여기서 자기 자신을 등록
    private void OnPlayerEnterReceived(S_PLAYER_ENTER packet)
    {
        ulong myId = NetManager.Instance._playerId;
        if (myId != 0 && !_playerStats.ContainsKey(myId))
        {
            _playerStats.Add(myId, new PlayerStat());
            Debug.Log($"[PeerStatManager] 자기 자신 등록: playerId={myId}");
        }

        // 새로 입장한 다른 플레이어도 등록
        ulong remoteId = (ulong)packet.Player.PlayerId;
        if (!_playerStats.ContainsKey(remoteId))
        {
            _playerStats.Add(remoteId, new PlayerStat());
            Debug.Log($"[PeerStatManager] 원격 플레이어 등록: playerId={remoteId}");
        }
    }

    public IReadOnlyDictionary<ulong, PlayerStat> GetAllRemoteStats()
    {
        return _playerStats;
    }

    #region each player stat update methods

    public void DecreaseHp(ulong playerId, int amount)
    {
        var Damage = new Protocol.DamageEventData { DamageAmount = amount };
        PacketSender.Instance.SendPlayerStatEvent(StatEventType.DamageTaken, playerId, Damage);
    }

    public void IncreaseHp(ulong playerId, int amount)
    {
        var Heal = new HealEventData { HealAmount = amount };
        PacketSender.Instance.SendPlayerStatEvent(StatEventType.Healed, playerId, null, Heal);
    }   

    public void DecreaseOxygen(ulong playerId)
    {
        var Oxygen = new OxygenEventData
        {
            ChangeType = OxygenChangeType.ConsumeNatural,
            Amount = 0.01f
        };
        PacketSender.Instance.SendPlayerStatEvent(StatEventType.OxygenChanged, playerId, null, null, Oxygen);
    }

    public void IncreaseOxygen(ulong playerId)
    {
        var Oxygen = new OxygenEventData
        {
            ChangeType = OxygenChangeType.RestoreArea,
            Amount = 0.02f
        };
        PacketSender.Instance.SendPlayerStatEvent(StatEventType.OxygenChanged, playerId, null, null, Oxygen);
    }

    public IReadOnlyDictionary<ulong, PlayerStat> GetAllStats()
    {
        return _playerStats;
    }
    #endregion
}
