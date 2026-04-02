using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
public struct PlayerStatData
{
    public float oxygen;
    public int hp;
    public readonly int maxHp;
    public PlayerStatData(int maxHp)
    {
        this.maxHp = maxHp;
        this.hp = maxHp;
        this.oxygen = 1f;
    }

    public void Reset()
    {
        hp = maxHp;
        oxygen = 1f;
    }

    public void IncreaseHp(int amount)
    {
        hp = Mathf.Clamp(hp + amount, 0, maxHp);
    }

    public void DecreaseHp(int amount)
    {
        hp = Mathf.Clamp(hp - amount, 0, maxHp);
    }

    public void IncreaseOxygen(float amount)
    {
        float rawOxygen = oxygen + amount;
        // 소수점 4번째 자리에서 반올림하여 오차 보정
        oxygen = Mathf.Round(rawOxygen * 10000f) / 10000f;
        oxygen = Mathf.Clamp01(oxygen);
    }

    public void DecreaseOxygen(float amount)
    {
        float rawOxygen = oxygen - amount;
        // 소수점 4번째 자리에서 반올림하여 오차 보정
        oxygen = Mathf.Round(rawOxygen * 10000f) / 10000f;
        oxygen = Mathf.Clamp01(oxygen);
    }
}

/// <summary>
/// 플레이어의 HP와 산소 상태를 관리하는 클래스
/// </summary>
public class PlayerStat : MonoBehaviour
{
    public PlayerStatData statData = new (5);
    public ulong playerId = NetManager.Instance._playerId; // NetManager에서 playerId 가져오기

    public event Action<float> OnOxygenChanged;
    public event Action<int> OnHpChanged;
    public event Action OnPlayerDead; // 사망 이벤트
    public event Action OnPlayerRevive; // 부활 이벤트

    protected Coroutine oxygenRoutine;
    protected Coroutine oxygenHpDrainRoutine;
    protected float oxygenHpDrainInterval = 5f;

    public float GetOxygen() => statData.oxygen;
    public int GetHp() => statData.hp;

    private GameObject RespawnPos;  // 플레이어가 부활할 위치
    protected float respawnDelay = 5f; // 부활까지의 지연 시간
    protected bool isRespawning = false; // 현재 부활 중인지 여부

    protected virtual void Awake()
    {
        statData = new PlayerStatData(5);
    }

    void Start()
    {
        StartOxygenDecrease(); // 게임 시작과 동시에 산소 감소 시작
    }

    public void ChangeData(int hp, float oxygen)
    {
        statData.hp = hp;
        statData.oxygen = oxygen;
    }

    #region HP 증/감소 로직
    public void CallOnHpChanged() => OnHpChanged?.Invoke(statData.hp);
    public virtual void DecreaseHp(int damage)
    {
        statData.DecreaseHp(damage);
        CallOnHpChanged();
        if (statData.hp <= 0)
        {
            OnPlayerDead?.Invoke();
        }
    }

    public virtual void IncreaseHp(int amount)
    {
        statData.IncreaseHp(amount);
        CallOnHpChanged();
    }
    #endregion

    #region Oxygen 증/감소 로직
    public void CallOnOxygenChanged() => OnOxygenChanged?.Invoke(statData.oxygen);
    public void StartOxygenDecrease()
    {
        if (oxygenRoutine != null) StopCoroutine(oxygenRoutine);
        oxygenRoutine = StartCoroutine(DecreaseOxygen());
    }
    public virtual IEnumerator DecreaseOxygen() { yield break; }

    public virtual IEnumerator IncreaseOxygen() { yield break; }

    public virtual IEnumerator OxygenHpDrainCoroutine()
    {
        while (statData.oxygen <= 0f && !isRespawning && statData.hp > 0)
        {
            DecreaseHp(1);
            if (isRespawning || statData.hp <= 0)
            {
                break;
            }
            yield return new WaitForSeconds(oxygenHpDrainInterval);
        }
        oxygenHpDrainRoutine = null;
    }

    public virtual void StartOxygenRecovery()
    {
        if (oxygenRoutine != null) 
        {
            StopCoroutine(oxygenRoutine);
        }
        oxygenRoutine = StartCoroutine(IncreaseOxygen());
    }

    public virtual void StopOxygenRecovery()
    {
        if (oxygenRoutine != null) 
        {
            StopCoroutine(oxygenRoutine);
        }
        oxygenRoutine = StartCoroutine(DecreaseOxygen());
    }
    #endregion
}