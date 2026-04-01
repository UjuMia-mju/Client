using UnityEngine;
using System;
using System.Collections;
using Protocol;
public class HostPlayerStat : PlayerStat
{
    public ulong PlayerId { get; set; } // 네트워크 playerId
    #region HP 증/감소 로직
    public override void DecreaseHp(int damage)
    {
        base.DecreaseHp(damage);
        var Damage = new DamageEventData
        {
            DamageAmount = damage
        };

        PacketSender.Instance.SendPlayerStatEvent(StatEventType.DamageTaken, PlayerId, Damage);
    }

    public override void IncreaseHp(int amount)
    {
        var Heal = new HealEventData
        {
            HealAmount = amount
        };

        PacketSender.Instance.SendPlayerStatEvent(StatEventType.Healed, PlayerId, null, Heal);
    }
    #endregion

    #region Oxygen 증/감소 로직
    public override IEnumerator OxygenDecrease()
    {
        while (statData.oxygen > 0)
        {
            // 산소 감소 패킷 전송
            var Oxygen = new OxygenEventData
            {
                ChangeType = OxygenChangeType.ConsumeNatural,
                Amount = 0.01f
            };
            PacketSender.Instance.SendPlayerStatEvent(StatEventType.OxygenChanged, PlayerId, null, null, Oxygen);
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
            var Oxygen = new OxygenEventData
            {
                ChangeType = OxygenChangeType.RestoreArea,
                Amount = 0.02f
            };
            PacketSender.Instance.SendPlayerStatEvent(StatEventType.OxygenChanged, PlayerId, null, null, Oxygen);

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
