using System.Collections.Generic;
using UnityEngine;

public class PeerStatManager : Singleton<PeerStatManager>
{
    // 내 스탯
    public PlayerStatData MyStat { get; private set; }

    // 다른 플레이어 스탯 (호스트가 보내준 값만 저장)
    private Dictionary<ulong, PlayerStatData> _remoteStats = new();

    public void SetMyStat(PlayerStatData stat)
    {
        MyStat = stat;
    }

    public void UpdateRemoteStat(ulong playerId, PlayerStatData stat)
    {
        _remoteStats[playerId] = stat;
    }

    public PlayerStatData? GetRemoteStat(ulong playerId)
    {
        if (_remoteStats.TryGetValue(playerId, out var stat))
        {
            return stat;
        }   
        return null;
    }

    public IReadOnlyDictionary<ulong, PlayerStatData> GetAllRemoteStats()
    {
        return _remoteStats;
    }

    // packet to PlayerStatData
    public PlayerStatData ConvertToPlayerStatData(Protocol.S_PLAYER_STAT packet)
    {
        PlayerStatData statData = new PlayerStatData
        {
            oxygen = packet.Oxygen,
            hp = packet.Hp
        };

        return statData;
    }

    // recv oxygen packet
    public void OnStatChanged(Protocol.S_PLAYER_STAT packet)
    {
        if (packet.PlayerId == NetManager.Instance._playerId)
        {
            SetMyStat(ConvertToPlayerStatData(packet));
        }
        else
        {
            UpdateRemoteStat(packet.PlayerId, ConvertToPlayerStatData(packet));
        }
    }
}
