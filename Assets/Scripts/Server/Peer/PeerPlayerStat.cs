using UnityEngine;
using System;
using System.Collections;
using Protocol;
public class PeerPlayerStat : PlayerStat
{
    [SerializeField] private int maxHp = 5;
    public ulong PlayerId { get; set; } // 네트워크 playerId

    private void OnDestroy()
    {
        // if (HostPacketHandler.Instance != null)
        // {
        //     HostPacketHandler.Instance.OnStatEvent -= OnStatSync;
        // } 
    }
    
    #region HP 증/감소 로직
    public override void DecreaseHp(int damage)
    {
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

    #region 부활 로직
    // private void BeginRespawn()
    // {
    //     if (isRespawning) return;
    //     isRespawning = true;

    //     // 멈춰야 할 것들 정리
    //     if (oxygenRoutine != null)
    //     {
    //         try { StopCoroutine(oxygenRoutine); } catch { }
    //         oxygenRoutine = null;
    //     }



    //     GameObject playerGO = this.gameObject;


    //     //playerGO.SetActive(false);

    //     // MainThreadDispatcher는 씬에 항상 존재하도록 설계되어 있으므로 예외처리 없이 사용
    //     if (MainThreadDispatcher.Instance != null)
    //     {
    //         MainThreadDispatcher.Instance.StartCoroutine(RespawnCoroutine(playerGO));
    //     }
    // }

    // private IEnumerator RespawnCoroutine(GameObject playerGO)
    // {
    //     float remaining = respawnDelay;
    //     while (remaining > 0f)
    //     {
    //         // 1초 단위로 대기
    //         yield return new WaitForSeconds(1f);
    //         remaining -= 1f;
    //     }

    //     // 리스폰 위치 결정
    //     Vector3 spawnPos = Vector3.zero;
    //     if (RespawnPos != null)
    //         spawnPos = RespawnPos.transform.position;

    //     // 위치 복구
    //     playerGO.transform.position = spawnPos;

    //     // 상태 리셋
    //     ResetStats();

    //     // 활성화
    //     OnPlayerRevive?.Invoke();
    //     //playerGO.SetActive(true);

    //     // 리스폰 완료
    //     isRespawning = false;

    //     StopOxygenRecovery();
    // }

    #endregion
}
