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
        HostPlayerStatManager.Instance.DecreaseHp((int)playerId, damage);
    }

    public override void IncreaseHp(int amount)
    {
        HostPlayerStatManager.Instance.IncreaseHp((int)playerId, amount);
    }
    #endregion

    #region Oxygen 증/감소 로직
    public override IEnumerator OxygenDecrease()
    {
        while (statData.oxygen > 0)
        {
            HostPlayerStatManager.Instance.DecreaseOxygen((int)playerId, 0.01f);
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

    public override IEnumerator OxygenIncrease()
    {
        while (statData.oxygen < 1f)
        {
            HostPlayerStatManager.Instance.IncreaseOxygen((int)playerId, 0.02f);

            yield return new WaitForSeconds(1.0f);
        }
    }

    public override IEnumerator OxygenHpDrainCoroutine()
    {
        while (statData.oxygen <= 0f && !isRespawning && statData.hp > 0)
        {
            DecreaseHp(1);

            yield return new WaitForSeconds(oxygenHpDrainInterval);
        }
        oxygenHpDrainRoutine = null;
    }

    public override void StartOxygenRecovery()
    {
        if (oxygenRoutine != null) StopCoroutine(oxygenRoutine);
        oxygenRoutine = StartCoroutine(OxygenIncrease());
    }

    public override void StopOxygenRecovery()
    {
        if (oxygenRoutine != null) StopCoroutine(oxygenRoutine);
        oxygenRoutine = StartCoroutine(OxygenDecrease());
    }
    #endregion
}
