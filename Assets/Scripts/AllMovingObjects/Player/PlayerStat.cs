using UnityEngine;
using System;
using System.Collections;

public class PlayerStat : MonoBehaviour
{
    private float oxygen = 1f;
    private int hp = 5;
    private const int MAX_HP = 5;

    public event Action<float> OnOxygenChanged;
    public event Action<int> OnHpChanged;
    public event Action OnPlayerDead; // 사망 이벤트

    private Coroutine oxygenRoutine;
    
    public float GetOxygen() => oxygen;
    public int GetHp() => hp;

    private void Start()
    {
        OnOxygenChanged?.Invoke(oxygen);
        OnHpChanged?.Invoke(hp);

        // 게임 시작 시 기본적으로 산소가 줄어들도록 설정
        StopOxygenRecovery(); 
    }
    
    #region HP 증/감소 로직
    public void DecreaseHp(int damage)
    {
        if (hp <= 0) return; // 이미 죽었다면 무시

        hp = Mathf.Clamp(hp - damage, 0, MAX_HP);
        OnHpChanged?.Invoke(hp);

        // [추가] 사망 판정
        if (hp <= 0)
        {
            OnPlayerDead?.Invoke();
            Debug.Log("플레이어 사망");
        }
    }
    
    public void IncreaseHp(int amount)
    {
        // 이미 최대 체력이면 무시
        if (hp >= MAX_HP) return;

        // 체력 증가 (최대치 MAX_HP를 넘지 않도록 Clamp)
        hp = Mathf.Clamp(hp + amount, 0, MAX_HP);

        // UI 갱신
        OnHpChanged?.Invoke(hp);

        Debug.Log($"체력 회복: {hp}");
    }
    #endregion

    #region Oxygen 증/감소 로직
    public IEnumerator OxygenDecrease()
    {
        while (oxygen > 0) // 0이 되면 멈춤
        {
            oxygen = Mathf.Clamp01(oxygen - 0.01f);
            OnOxygenChanged?.Invoke(oxygen);
            yield return new WaitForSeconds(1.0f);
        }
        
        // TODO: 산소가 0이 되었을 때 우주선으로 되돌아가는 로직
    }

    public IEnumerator OxygenIncrease()
    {
        while (oxygen < 1f)
        {
            oxygen = Mathf.Clamp01(oxygen + 0.02f);
            OnOxygenChanged?.Invoke(oxygen);
            yield return new WaitForSeconds(1.0f);
        }
        oxygen = 1f;
    }

    public void StartOxygenRecovery()
    {
        if (oxygenRoutine != null) StopCoroutine(oxygenRoutine);
        oxygenRoutine = StartCoroutine(OxygenIncrease());
    }

    public void StopOxygenRecovery()
    {
        if (oxygenRoutine != null) StopCoroutine(oxygenRoutine);
        oxygenRoutine = StartCoroutine(OxygenDecrease());
    }
    #endregion
}