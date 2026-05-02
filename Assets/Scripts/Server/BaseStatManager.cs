using System.Collections.Generic;
using UnityEngine;

public abstract class BaseStatManager<T> : MonoBehaviorSingleton<T> where T : BaseStatManager<T>
{
    protected Dictionary<ulong, PlayerStatState> _playerStats = new();

    public virtual void UpdateStat(ulong playerId, int hp, float oxygen)
    {
        if (!_playerStats.TryGetValue(playerId, out var stat))
        {
            // S_ENTER_GAME보다 S_PLAYER_STAT이 먼저 도착하는 경쟁 조건 대응.
            // 미등록 플레이어는 즉시 등록 후 처리.
            stat = new PlayerStatState(5);
            _playerStats[playerId] = stat;
            Debug.Log($"[StatManager] 미등록 플레이어 자동 등록: playerId={playerId}");
        }

        stat.ChangeData(hp, oxygen);
        stat.CallOnHpChanged();
        stat.CallOnOxygenChanged();
    }

    public PlayerStatState GetPlayerStat(ulong playerId)
    {
        if (_playerStats.TryGetValue(playerId, out var stat))
            return stat;

        string currentKeys = string.Join(", ", _playerStats.Keys);
        Debug.LogError($"[GetPlayerStat] Player {playerId} not found! Current IDs: [{currentKeys}]");
        return null;
    }

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
