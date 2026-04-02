using System.Collections.Generic;
using UnityEngine;

public class HostPlayerStatManager : MonoBehaviorSingleton<HostPlayerStatManager>
{
    Dictionary<int, PlayerStatData> _playerStats = new();

    void Start()
    {
        _playerStats.Add((int)NetManager.Instance._playerId, new PlayerStatData(5)); // 호스트 플레이어 초기화
    }

    // 플레이어 입장
    public void AddPlayer(int playerId, int maxHp = 5)
    {
        if (!_playerStats.ContainsKey(playerId))
        {
            _playerStats[playerId] = new PlayerStatData(maxHp);
        }
    }

    // 플레이어 퇴장
    public void RemovePlayer(int playerId)
    {
        _playerStats.Remove(playerId);
    }

    // 개별 플레이어 상태 조회
    public PlayerStatData? GetPlayerStat(int playerId)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            return stat;
        }
        return null;
    }

    // 개별 플레이어 상태 변경 (예: HP 감소)
    public void DecreaseHp(int playerId, int amount)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            stat.DecreaseHp(amount);
            _playerStats[playerId] = stat; // struct라면 다시 할당 필요
        }

        PacketSender.Instance?.BroadcastStatResult((ulong)playerId, stat.hp, stat.oxygen);
    }

    public void IncreaseHp(int playerId, int amount)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            stat.IncreaseHp(amount);
            _playerStats[playerId] = stat;
        }

        PacketSender.Instance?.BroadcastStatResult((ulong)playerId, stat.hp, stat.oxygen);
    }

    public void DecreaseOxygen(int playerId, float amount)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            stat.DecreaseOxygen(amount);
            _playerStats[playerId] = stat;
        }

        PacketSender.Instance?.BroadcastStatResult((ulong)playerId, stat.hp, stat.oxygen);
    }

    public void IncreaseOxygen(int playerId, float amount)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            stat.IncreaseOxygen(amount);
            _playerStats[playerId] = stat;
        }

        PacketSender.Instance?.BroadcastStatResult((ulong)playerId, stat.hp, stat.oxygen);
    }

    // 전체 플레이어 상태 반환 (동기화용)
    public IReadOnlyDictionary<int, PlayerStatData> GetAllStats()
    {
        return _playerStats;
    }
}
