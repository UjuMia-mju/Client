using UnityEngine;
using System;
using System.Collections;
using Protocol;
public class PeerPlayerStat : PlayerStat
{
    [SerializeField] private int maxHp = 5;
    ulong currPlayerId = 1; // 이 부분 바꿔야 함.
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
        base.DecreaseHp(damage);
        PeerStatManager.Instance.DecreaseHp(currPlayerId, damage);
    }

    public override void IncreaseHp(int amount)
    {
        base.IncreaseHp(amount);
        PeerStatManager.Instance.IncreaseHp(currPlayerId, amount);
    }
    #endregion

    #region Oxygen 증/감소 로직
    public override IEnumerator DecreaseOxygen() 
    {
        while (statData.oxygen > 0)
        {
            PeerStatManager.Instance.DecreaseOxygen(currPlayerId);
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
        float oxygen = PeerStatManager.Instance.GetPlayerStat(currPlayerId).statData.oxygen;
        while (oxygen < 1f)
        {
            PeerStatManager.Instance.IncreaseOxygen(currPlayerId);
            yield return new WaitForSeconds(1.0f);
        }
    }

    public override IEnumerator OxygenHpDrainCoroutine()
    {
        float oxygen = PeerStatManager.Instance.GetPlayerStat(currPlayerId).statData.oxygen;
        int hp = PeerStatManager.Instance.GetPlayerStat(currPlayerId).statData.hp;
        while (oxygen <= 0f && !isRespawning && hp > 0)
        {
            DecreaseHp(1);

            yield return new WaitForSeconds(oxygenHpDrainInterval);
        }
        oxygenHpDrainRoutine = null;
    }

    public override void StartOxygenRecovery()
    {
        if (oxygenRoutine != null) StopCoroutine(oxygenRoutine);
        oxygenRoutine = StartCoroutine(IncreaseOxygen());
    }

    public override void StopOxygenRecovery()
    {
        if (oxygenRoutine != null) StopCoroutine(oxygenRoutine);
        oxygenRoutine = StartCoroutine(DecreaseOxygen());
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
