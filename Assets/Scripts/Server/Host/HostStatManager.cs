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
        stat.boundPlayer?.CallOnHpChanged();

        if (stat.statData.hp <= 0)
        {
            stat.CallOnPlayerDead();
            PlayerLifeServerManager.Instance?.OnReceivePlayerDead(playerId);
        }

        PacketSender.Instance?.BroadcastStatResult(playerId, stat.GetHp(), stat.GetOxygen());
        PlayManager.Instance?.UpdateRemotePlayerStat(playerId, stat.GetHp(), stat.GetOxygen());
    }

    public void IncreaseHp(ulong playerId, int amount)
    {
        if (!_playerStats.TryGetValue(playerId, out var stat)) return;

        stat.statData.IncreaseHp(amount);
        stat.CallOnHpChanged();
        stat.boundPlayer?.CallOnHpChanged();

        PacketSender.Instance?.BroadcastStatResult(playerId, stat.GetHp(), stat.GetOxygen());
        PlayManager.Instance?.UpdateRemotePlayerStat(playerId, stat.GetHp(), stat.GetOxygen());
    }

    public void DecreaseOxygen(ulong playerId)
    {
        if (!_playerStats.TryGetValue(playerId, out var stat)) return;

        float amount = stat.boundPlayer != null ? stat.boundPlayer.OxygenDecreasePerTick : 0.01f;
        stat.statData.DecreaseOxygen(amount);
        stat.CallOnOxygenChanged();

        PacketSender.Instance?.BroadcastStatResult(playerId, stat.GetHp(), stat.GetOxygen());
        PlayManager.Instance?.UpdateRemotePlayerStat(playerId, stat.GetHp(), stat.GetOxygen());
    }

    public void IncreaseOxygen(ulong playerId)
    {
        if (!_playerStats.TryGetValue(playerId, out var stat)) return;

        float amount = stat.boundPlayer != null ? stat.boundPlayer.OxygenIncreasePerTick : 0.02f;
        stat.statData.IncreaseOxygen(amount);
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

    /// <summary>
    /// 스테이지 시작 시 호출. 모든 플레이어의 stat 을 풀 회복 상태로 초기화하고,
    /// 그 결과를 모든 피어에게 S_STAT_RESULT 로 브로드캐스트하여 클라이언트 측 PlayerStat 도 함께 동기화한다.
    /// </summary>
    public void ResetAndBroadcastAll()
    {
        ResetAllStats();

        if (PacketSender.Instance == null) return;
        foreach (var kv in _playerStats)
        {
            if (kv.Value == null) continue;
            PacketSender.Instance.BroadcastStatResult(kv.Key, kv.Value.GetHp(), kv.Value.GetOxygen());
            PlayManager.Instance?.UpdateRemotePlayerStat(kv.Key, kv.Value.GetHp(), kv.Value.GetOxygen());
        }
        Debug.Log("[HostStatManager] ResetAndBroadcastAll 완료");
    }
}
