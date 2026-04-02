using System.Collections.Generic;
using UnityEngine;

public class HostStatManager : BaseStatManager<HostStatManager>
{
    void Start()
    {
        // 임시 코드임. 나중에 방에 참여한 플레이어들의 ID정보들을 받아와서 전부다 초기화 해주고,
        // 추후에 새로운 플레이어가 참여하면 새롭게 추가해줘야함.
        _playerStats.Add(NetManager.Instance._playerId, new PlayerStat()); // 호스트 플레이어 초기화
    }
    
    // 플레이어 입장
    public void AddPlayer(ulong playerId, int maxHp = 5)
    {
        if (!_playerStats.ContainsKey(playerId))
        {
            _playerStats[playerId] = new PlayerStat();
        }
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
    #endregion
}
