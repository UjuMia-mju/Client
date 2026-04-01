using System.Collections.Generic;
using UnityEngine;

public class HostPlayerStatManager : Singleton<HostPlayerStatManager>
{
    Dictionary<ulong, PlayerStatData> _playerStats = new();

    // 플레이어 입장
    public void AddPlayer(ulong playerId, int maxHp = 5)
    {
        if (!_playerStats.ContainsKey(playerId))
        {
            _playerStats[playerId] = new PlayerStatData(maxHp);
        }
    }

    // 플레이어 퇴장
    public void RemovePlayer(ulong playerId)
    {
        _playerStats.Remove(playerId);
    }

    // 개별 플레이어 상태 조회
    public PlayerStatData? GetPlayerStat(ulong playerId)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            return stat;
        }
        return null;
    }

    // 개별 플레이어 상태 변경 (예: HP 감소)
    public void DecreaseHp(ulong playerId, int amount)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            stat.DecreaseHp(amount);
            _playerStats[playerId] = stat; // struct라면 다시 할당 필요
        }
    }

    public void IncreaseHp(ulong playerId, int amount)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            stat.IncreaseHp(amount);
            _playerStats[playerId] = stat;
        }
    }

    public void DecreaseOxygen(ulong playerId, float amount)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            stat.DecreaseOxygen(amount);
            _playerStats[playerId] = stat;
        }
    }

    public void IncreaseOxygen(ulong playerId, float amount)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            stat.IncreaseOxygen(amount);
            _playerStats[playerId] = stat;
        }
    }

    // 전체 플레이어 상태 반환 (동기화용)
    public IReadOnlyDictionary<ulong, PlayerStatData> GetAllStats()
    {
        return _playerStats;
    }
}
