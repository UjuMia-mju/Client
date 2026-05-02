using UnityEngine;
using System.Collections;
using Protocol;

public class HostPlayerStat : PlayerStat
{
    #region HP 증/감소 로직
    public override void DecreaseHp(int damage)
    {
        // ... 호스트 측 추가 로직 ...
        base.DecreaseHp(damage);  // ← 이게 빠져있으면 ReportDeathToHost가 안 탐
    }

    public override void IncreaseHp(int amount)
    {
        HostStatManager.Instance.IncreaseHp(GetMyPlayerId(), amount);
    }
    #endregion

    #region Oxygen 증/감소 로직
    public override IEnumerator DecreaseOxygen()
    {
        while (statData.oxygen > 0)
        {
            HostStatManager.Instance.DecreaseOxygen(GetMyPlayerId());
            yield return new WaitForSeconds(1.0f);
        }

        if (!isRespawning)
        {
            if (oxygenHpDrainRoutine == null)
                oxygenHpDrainRoutine = StartCoroutine(OxygenHpDrainCoroutine());
        }
    }

    public override IEnumerator IncreaseOxygen()
    {
        float oxygen = HostStatManager.Instance.GetPlayerStat(GetMyPlayerId()).statData.oxygen;
        while (oxygen < 1f)
        {
            HostStatManager.Instance.IncreaseOxygen(GetMyPlayerId());
            yield return new WaitForSeconds(1.0f);
        }
    }

    public override IEnumerator OxygenHpDrainCoroutine()
    {
        float oxygen = HostStatManager.Instance.GetPlayerStat(GetMyPlayerId()).statData.oxygen;
        int hp = HostStatManager.Instance.GetPlayerStat(GetMyPlayerId()).statData.hp;

        while (oxygen <= 0f && !isRespawning && hp > 0)
        {
            DecreaseHp(1);
            yield return new WaitForSeconds(oxygenHpDrainInterval);
        }
        oxygenHpDrainRoutine = null;
    }
    #endregion
}
