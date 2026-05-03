using UnityEngine;

/// <summary>
/// 네트워크에서 동기화되는 HP/산소만 반영하는 PlayerStat. 로컬 산소 감소 루프는 돌지 않습니다.
/// </summary>
public class RemotePlayerStat : PlayerStat
{
    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>호스트/피어에서 브로드캐스트된 체력·산소를 적용하고 UI 이벤트를 발생시킵니다.</summary>
    public void ApplyNetworkStat(int hp, float oxygen)
    {
        int prevHp = statData.hp;
        statData.hp = Mathf.Clamp(hp, 0, statData.maxHp);
        statData.oxygen = Mathf.Clamp01(oxygen);

        CallOnOxygenChanged();
        CallOnHpChanged();

        if (prevHp > 0 && statData.hp <= 0)
            CallOnPlayerDead();
        else if (prevHp <= 0 && statData.hp > 0)
            CallOnPlayerRevive();
    }

    public override void StartOxygenDecrease()
    {
        // 원격 플레이어는 서버 동기화만 사용
    }
}
