using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HostStatManager : BaseStatManager<HostStatManager>
{
    void Start()
    {
        if (!_playerStats.ContainsKey(NetManager.Instance._playerId))
            _playerStats.Add(NetManager.Instance._playerId, new PlayerStatState(5));
    }

    public void DecreaseHp(ulong playerId, int amount)
    {
        if (!_playerStats.TryGetValue(playerId, out var stat)) return;

        stat.statData.DecreaseHp(amount);
        stat.CallOnHpChanged();
        if (stat.statData.hp <= 0)
            stat.CallOnPlayerDead();

        PacketSender.Instance?.BroadcastStatResult(playerId, stat.GetHp(), stat.GetOxygen());
        PlayManager.Instance?.UpdateRemotePlayerStat(playerId, stat.GetHp(), stat.GetOxygen());
    }

    public void IncreaseHp(ulong playerId, int amount)
    {
        if (!_playerStats.TryGetValue(playerId, out var stat)) return;

        stat.statData.IncreaseHp(amount);
        stat.CallOnHpChanged();

        PacketSender.Instance?.BroadcastStatResult(playerId, stat.GetHp(), stat.GetOxygen());
        PlayManager.Instance?.UpdateRemotePlayerStat(playerId, stat.GetHp(), stat.GetOxygen());
    }

    public void DecreaseOxygen(ulong playerId)
    {
        if (!_playerStats.TryGetValue(playerId, out var stat)) return;

        stat.statData.DecreaseOxygen(0.01f);
        stat.CallOnOxygenChanged();

        PacketSender.Instance?.BroadcastStatResult(playerId, stat.GetHp(), stat.GetOxygen());
        PlayManager.Instance?.UpdateRemotePlayerStat(playerId, stat.GetHp(), stat.GetOxygen());
    }

    public void IncreaseOxygen(ulong playerId)
    {
        if (!_playerStats.TryGetValue(playerId, out var stat)) return;

        stat.statData.IncreaseOxygen(0.02f);
        stat.CallOnOxygenChanged();

        PacketSender.Instance?.BroadcastStatResult(playerId, stat.GetHp(), stat.GetOxygen());
        PlayManager.Instance?.UpdateRemotePlayerStat(playerId, stat.GetHp(), stat.GetOxygen());
    }

    public void RegisterPlayer(ulong playerId, PlayerStat stat)
    {
        if (stat == null) return;
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
        Debug.Log($"[HostStatManager] RegisterPlayer: {playerId}");
    }
}
