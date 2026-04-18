using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// 순수 데이터 및 기본 연산
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
    protected ulong GetMyPlayerId() => NetManager.Instance._playerId;

    public event Action<float> OnOxygenChanged;
    public event Action<int> OnHpChanged;
    protected Coroutine oxygenRoutine;
    protected Coroutine oxygenHpDrainRoutine;
    protected float oxygenHpDrainInterval = 5f;

    public float GetOxygen() => statData.oxygen;
    public int GetHp() => statData.hp;

    // 부활 관련 변수 및 이벤트
    public event Action OnPlayerDead; // 사망 이벤트
    public event Action OnPlayerRevive; // 부활 이벤트
    private GameObject RespawnPos;  // 플레이어가 부활할 위치
    protected float respawnDelay = 5f; // 부활까지의 지연 시간
    protected bool isRespawning = false; // 현재 부활 중인지 여부

    protected virtual void Awake()
    {
        statData = new PlayerStatData(5);
    }

    void Start()
    {
        // StartOxygenDecrease()는 OnNetworkReady() 이후에 호출됩니다.
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

    public virtual IEnumerator OxygenHpDrainCoroutine() { yield break; }

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

    public void CallOnPlayerDead() => OnPlayerDead?.Invoke();
}