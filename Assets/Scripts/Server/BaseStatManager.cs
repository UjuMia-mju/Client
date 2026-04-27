using System.Collections.Generic;
using UnityEngine;

public abstract class BaseStatManager<T> : MonoBehaviorSingleton<T> where T : BaseStatManager<T>
{
    protected Dictionary<ulong, PlayerStatState> _playerStats = new();

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

    public PlayerStatState GetPlayerStat(ulong playerId)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
            return stat;

        string currentKeys = string.Join(", ", _playerStats.Keys);
        Debug.LogError($"[GetPlayerStat] Player {playerId} not found! Current IDs in dict: [{currentKeys}]");
        return null;
    }

    /// <summary>에러 로그 없이 PlayerStatState를 가져옵니다. 없으면 null 반환.</summary>
    public bool TryGetPlayerStat(ulong playerId, out PlayerStatState stat)
    {
        return _playerStats.TryGetValue(playerId, out stat);
    }

    public void AddPlayer(ulong playerId)
    {
        if (!_playerStats.ContainsKey(playerId))
            _playerStats.Add(playerId, new PlayerStatState(5));
    }

    public void RemovePlayer(ulong playerId) => _playerStats.Remove(playerId);
}
