using UnityEngine;
using System;
using System.Collections;
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
        oxygen = Mathf.Clamp01(oxygen + amount);
    }

    public void DecreaseOxygen(float amount)
    {
        oxygen = Mathf.Clamp01(oxygen - amount);
    }
}

public class PlayerStat : MonoBehaviour
{
    protected PlayerStatData statData = new (5);

    public event Action<float> OnOxygenChanged;
    public event Action<int> OnHpChanged;
    public event Action OnPlayerDead; // 사망 이벤트
    public event Action OnPlayerRevive; // 부활 이벤트

    protected Coroutine oxygenRoutine;
    protected Coroutine oxygenHpDrainRoutine;
    protected float oxygenHpDrainInterval = 5f;

    protected float GetOxygen() => statData.oxygen;
    public int GetHp() => statData.hp;

    private GameObject RespawnPos;  // 플레이어가 부활할 위치
    protected float respawnDelay = 5f; // 부활까지의 지연 시간
    protected bool isRespawning = false; // 현재 부활 중인지 여부

    protected virtual void Awake()
    {
        statData = new PlayerStatData(5);
    }

    #region HP 증/감소 로직
    public virtual void DecreaseHp(int damage)
    {
        statData.DecreaseHp(damage);
        OnHpChanged?.Invoke(statData.hp);
        if (statData.hp <= 0)
        {
            OnPlayerDead?.Invoke();
        }
    }

    public virtual void IncreaseHp(int amount)
    {
        statData.IncreaseHp(amount);
        OnHpChanged?.Invoke(statData.hp);
    }
    #endregion

    #region Oxygen 증/감소 로직
    public virtual IEnumerator OxygenDecrease()
    {
        while (statData.oxygen > 0)
        {
            statData.DecreaseOxygen(0.01f);
            OnOxygenChanged?.Invoke(statData.oxygen);
            yield return new WaitForSeconds(1.0f);
        }
        if (!isRespawning)
        {
            if (oxygenHpDrainRoutine == null)
            {
                oxygenHpDrainRoutine = StartCoroutine(OxygenHpDrainCoroutine());
            }
        }
    }

    public virtual IEnumerator OxygenIncrease()
    {
        while (statData.oxygen < 1f)
        {
            statData.IncreaseOxygen(0.02f);
            OnOxygenChanged?.Invoke(statData.oxygen);
            yield return new WaitForSeconds(1.0f);
        }
        statData.oxygen = 1f;
    }

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
        oxygenRoutine = StartCoroutine(OxygenIncrease());
    }

    public virtual void StopOxygenRecovery()
    {
        if (oxygenRoutine != null) 
        {
            StopCoroutine(oxygenRoutine);
        }
        oxygenRoutine = StartCoroutine(OxygenDecrease());
    }
    #endregion
}