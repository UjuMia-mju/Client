using UnityEngine;
using System;
using System.Collections;
using Protocol;
public class HostPlayerStat : PlayerStat
{
    #region HP 증/감소 로직
    public override void DecreaseHp(int damage)
    {
        base.DecreaseHp(damage);
        HostStatManager.Instance.DecreaseHp(playerId, damage);
    }

    public override void IncreaseHp(int amount)
    {
        HostStatManager.Instance.IncreaseHp(playerId, amount);
    }
    #endregion

    #region Oxygen 증/감소 로직
    public override IEnumerator DecreaseOxygen()
    {
        while (statData.oxygen > 0)
        {
            HostStatManager.Instance.DecreaseOxygen(playerId);
            yield return new WaitForSeconds(1.0f);
        }

        if (!isRespawning)
        {
            // 산소 고갈 시 HP 소모 코루틴 시작
            if (oxygenHpDrainRoutine == null)
            {
                oxygenHpDrainRoutine = StartCoroutine(OxygenHpDrainCoroutine());
            }
        }
    }

    public override IEnumerator IncreaseOxygen()
    {
        float oxygen = HostStatManager.Instance.GetPlayerStat(playerId).statData.oxygen;
        while (oxygen < 1f)
        {
            HostStatManager.Instance.IncreaseOxygen(playerId);
            yield return new WaitForSeconds(1.0f);
        }
    }

    public override IEnumerator OxygenHpDrainCoroutine()
    {
        float oxygen = HostStatManager.Instance.GetPlayerStat(playerId).statData.oxygen;
        int hp = HostStatManager.Instance.GetPlayerStat(playerId).statData.hp;

        while (oxygen <= 0f && !isRespawning && hp > 0)
        {
            DecreaseHp(1);
            yield return new WaitForSeconds(oxygenHpDrainInterval);
        }
        oxygenHpDrainRoutine = null;
    }
    
    #endregion
}
