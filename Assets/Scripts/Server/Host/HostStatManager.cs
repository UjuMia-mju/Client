using System.Collections.Generic;
using UnityEngine;

public class HostStatManager : BaseStatManager<HostStatManager>
{
    void Start()
    {
        // RegisterPlayer로 이미 등록된 경우 덮어쓰지 않음
        if (!_playerStats.ContainsKey(NetManager.Instance._playerId))
        {
            _playerStats.Add(NetManager.Instance._playerId, new PlayerStat());
        }
    }

    public void AddPlayer(ulong playerId, int maxHp = 5)
    {
        if (!_playerStats.ContainsKey(playerId))
            _playerStats[playerId] = new PlayerStat();
    }

    public void DecreaseHp(ulong playerId, int amount)
    {
        if (!_playerStats.TryGetValue(playerId, out var stat)) return;

        // stat.DecreaseHp() 호출 시 HostPlayerStat.DecreaseHp() → 무한루프 발생
        // statData만 직접 수정하고 이벤트만 호출
        stat.statData.DecreaseHp(amount);
        stat.CallOnHpChanged();
        if (stat.statData.hp <= 0)
            stat.CallOnPlayerDead();

        PacketSender.Instance?.BroadcastStatResult(playerId, stat.GetHp(), stat.GetOxygen());
    }

    public void IncreaseHp(ulong playerId, int amount)
    {
        if (!_playerStats.TryGetValue(playerId, out var stat)) return;

        stat.statData.IncreaseHp(amount);
        stat.CallOnHpChanged();

        PacketSender.Instance?.BroadcastStatResult(playerId, stat.GetHp(), stat.GetOxygen());
    }

    public void DecreaseOxygen(ulong playerId)
    {
        if (!_playerStats.TryGetValue(playerId, out var stat)) return;

        stat.statData.DecreaseOxygen(0.01f);
        stat.CallOnOxygenChanged();

        PacketSender.Instance?.BroadcastStatResult(playerId, stat.GetHp(), stat.GetOxygen());
    }

    public void IncreaseOxygen(ulong playerId)
    {
        if (!_playerStats.TryGetValue(playerId, out var stat)) return;

        stat.statData.IncreaseOxygen(0.02f);
        stat.CallOnOxygenChanged();

        PacketSender.Instance?.BroadcastStatResult(playerId, stat.GetHp(), stat.GetOxygen());
    }

    /// <summary>실제 PlayerStat 컴포넌트를 등록합니다.</summary>
    public void RegisterPlayer(ulong playerId, PlayerStat stat)
    {
        _playerStats[playerId] = stat;
        Debug.Log($"[HostStatManager] RegisterPlayer: {playerId}");
    }
}
