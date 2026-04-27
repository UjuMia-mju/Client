using System.Collections.Generic;
using UnityEngine;

public class HostStatManager : BaseStatManager<HostStatManager>
{
    void Start()
    {
        AddPlayer(NetManager.Instance._playerId);
    }

    public void DecreaseHp(ulong playerId, int amount)
    {
        if (!_playerStats.TryGetValue(playerId, out var stat)) return;

        stat.statData.DecreaseHp(amount);
        stat.CallOnHpChanged();
        if (stat.statData.hp <= 0)
            stat.CallOnPlayerDead();

        PacketSender.Instance?.BroadcastStatResult(playerId, stat.GetHp(), stat.GetOxygen());
        // 호스트 로컬 RemotePlayer UI 갱신
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
        //Debug.Log($"[HostStatManager] DecreaseOxygen: playerId={playerId}, oxygen={stat.GetOxygen()}");

        // 확인 필요
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
        _playerStats[playerId] = stat;
        Debug.Log($"[HostStatManager] RegisterPlayer: {playerId}");
    }
}
