using System.Collections.Generic;
using UnityEngine;

public class HostPlayerStatManager : MonoBehaviorSingleton<HostPlayerStatManager>
{
    // 참여자 플레어들의 스탯
    Dictionary<ulong, PlayerStat> _playerStats = new();
    void Start()
    {
        _playerStats.Add(NetManager.Instance._playerId, new PlayerStat()); // 호스트 플레이어 초기화
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
    
    //
    public void UpdateStat(ulong playerId, int hp, float oxygen)
    {
        _playerStats[playerId].ChangeData(hp, oxygen);
        _playerStats[playerId].CallOnHpChanged();
        _playerStats[playerId].CallOnOxygenChanged();

        //Debug.Log($"[HostPlayerStatManager] Updated stat for Player {playerId}: HP={hp}, Oxygen={oxygen}");

        //TODO 전체 관리하는 부분은 바뀌는데 실질적으로 Plyaer객체에 붙어있는 stat이 바뀌고 있지 않음.
    }

    // 플레이어 입장
    public void AddPlayer(ulong playerId, int maxHp = 5)
    {
        if (!_playerStats.ContainsKey(playerId))
        {
            _playerStats[playerId] = new PlayerStat();
        }
    }

    // 플레이어 퇴장
    public void RemovePlayer(ulong playerId)
    {
        _playerStats.Remove(playerId);
    }

    public PlayerStat GetPlayerStat(ulong playerId)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            return stat;
        }

        string currentKeys = string.Join(", ", _playerStats.Keys);
        Debug.LogError($"[GetPlayerStat] Player {playerId} not found! Current IDs in dict: [{currentKeys}]");
        return null;
    }

    #region each player stat update methods

    // ==========================체력============================
    public void DecreaseHp(ulong playerId, int amount)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            stat.DecreaseHp(amount);
            _playerStats[playerId] = stat; // struct라면 다시 할당 필요
        }

        int hp = stat.GetHp();
        float oxygen = stat.GetOxygen();
        
        UpdateStat(playerId, hp, oxygen);

        PacketSender.Instance?.BroadcastStatResult((ulong)playerId, stat.GetHp(), stat.GetOxygen());
    }

    public void IncreaseHp(ulong playerId, int amount)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            stat.IncreaseHp(amount);
            _playerStats[playerId] = stat;
        }

        int hp = stat.GetHp();
        float oxygen = stat.GetOxygen();

        UpdateStat(playerId, hp, oxygen);

        PacketSender.Instance?.BroadcastStatResult((ulong)playerId, stat.GetHp(), stat.GetOxygen());
    }

    // =========================산소============================
    public void DecreaseOxygen(ulong playerId)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            stat.statData.DecreaseOxygen(0.01f); // 매 초마다 0.01씩 감소
            _playerStats[playerId] = stat;
        }
        
        int hp = stat.GetHp();
        float oxygen = stat.GetOxygen();

        UpdateStat(playerId, hp, oxygen);

        PacketSender.Instance?.BroadcastStatResult((ulong)playerId, stat.GetHp(), stat.GetOxygen());
    }
    public void IncreaseOxygen(ulong playerId)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            stat.statData.IncreaseOxygen(0.02f); // 매 초마다 0.02씩 증가
            _playerStats[playerId] = stat;
        }

        int hp = stat.GetHp();
        float oxygen = stat.GetOxygen();

        UpdateStat(playerId, hp, oxygen);

        PacketSender.Instance?.BroadcastStatResult((ulong)playerId, stat.GetHp(), stat.GetOxygen());
    }

    // 전체 플레이어 상태 반환 (동기화용)
    public IReadOnlyDictionary<ulong, PlayerStat> GetAllStats()
    {
        return _playerStats;
    }
    #endregion
}
