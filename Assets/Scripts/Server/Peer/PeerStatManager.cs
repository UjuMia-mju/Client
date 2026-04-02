using System.Collections.Generic;
using UnityEngine;
using Protocol;
public class PeerStatManager : BaseStatManager<PeerStatManager>
{
    ulong currPlayerId = 1; // 이 부분 바꿔야 함.
    void Start()
    {
        // 이 부분을 수정해야 하는데, 나중에 방에 참여를 하고 미리 참여한 플레이어들의 ID정보들을 받아와서 전부다 초기화 해주고,
        // 추후에 새로운 플레이어가 참여하면 새롭게 추가해줘야함.
        // 처음 인원수만큼 전부다 참여 완료하면 그때 카운트 다운을 하고 본격적으로 게임이 시작되어야 함.
        // 지금 player이나 playerManager등등 모든 코드 부분에서 start awake 부분에서 수정해야할 부분들이 많이 보임.
        // 전부 다 수정하려고 하다가 코드 수정부분이 너무 많아져서 일단 하드코딩으로 2명으로 해놓음. 나중에 수정요함.
        _playerStats.Add(0, new PlayerStat()); // 호스트 플레이어 초기화
        _playerStats.Add(currPlayerId, new PlayerStat()); // 자기 자신 초기화
    }

    public IReadOnlyDictionary<ulong, PlayerStat> GetAllRemoteStats()
    {
        return _playerStats;
    }

    #region each player stat update methods

    // ==========================체력============================
    public void DecreaseHp(ulong playerId, int amount)
    {
        var Damage = new Protocol.DamageEventData
        {
            DamageAmount = amount
        };
        PacketSender.Instance.SendPlayerStatEvent(StatEventType.DamageTaken, currPlayerId, Damage);
    }

    public void IncreaseHp(ulong playerId, int amount)
    {
        var Heal = new HealEventData
        {
            HealAmount = amount
        };
        PacketSender.Instance.SendPlayerStatEvent(StatEventType.Healed, currPlayerId, null, Heal);
    }

    // =========================산소============================
    public void DecreaseOxygen(ulong playerId)
    {
        // 산소 감소 패킷 전송
        var Oxygen = new OxygenEventData
        {
            ChangeType = OxygenChangeType.ConsumeNatural,
            Amount = 0.01f
        };
        PacketSender.Instance.SendPlayerStatEvent(StatEventType.OxygenChanged, currPlayerId, null, null, Oxygen);
    }
    public void IncreaseOxygen(ulong playerId)
    {
        Debug.Log($"fyck~~~~ 산소 증가 시작 + id: " + playerId);
        
        var Oxygen = new OxygenEventData
        {
            ChangeType = OxygenChangeType.RestoreArea,
            Amount = 0.02f
        };

        PacketSender.Instance.SendPlayerStatEvent(StatEventType.OxygenChanged, currPlayerId, null, null, Oxygen);
    }

    // 전체 플레이어 상태 반환 (동기화용)
    public IReadOnlyDictionary<ulong, PlayerStat> GetAllStats()
    {
        return _playerStats;
    }
    #endregion
}
