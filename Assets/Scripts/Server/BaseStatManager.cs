using System.Collections.Generic;
using UnityEngine;

public abstract class BaseStatManager<T> : MonoBehaviorSingleton<T> where T : BaseStatManager<T>
{
    protected Dictionary<ulong, PlayerStat> _playerStats = new();

    // 공통: 딕셔너리 업데이트 및 이벤트 호출
    public virtual void UpdateStat(ulong playerId, int hp, float oxygen)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            stat.ChangeData(hp, oxygen);
            stat.CallOnHpChanged();
            stat.CallOnOxygenChanged();
        }
        else
        {
            Debug.LogWarning($"[StatManager] Player {playerId} not found!");
        }
    }

    public PlayerStat GetPlayerStat(ulong playerId)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
        {
            return stat;
        }
        // Log check;
        string currentKeys = string.Join(", ", _playerStats.Keys);
        Debug.LogError($"[GetPlayerStat] Player {playerId} not found! Current IDs in dict: [{currentKeys}]");
        return null;
    }

    public void AddPlayer(ulong playerId)
    {
        if (!_playerStats.ContainsKey(playerId))
        {
            _playerStats.Add(playerId, new PlayerStat());
        }
    }

    public void RemovePlayer(ulong playerId) => _playerStats.Remove(playerId);
}