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
    public event Action OnPlayerRevive; // 부활 이벤트

    private Coroutine oxygenRoutine;
    private Coroutine oxygenHpDrainRoutine;

    // 산소가 0일 때 체력이 감소하는 간격 (초 단위이며 산소보다는 느리게 소모되어야 할 것입니다.)
    // 이것도 레벨 디자인 단계에서 조절되어야 합니다.
    private float oxygenHpDrainInterval = 5f;


    public float GetOxygen() => oxygen;
    public int GetHp() => hp;

    private GameObject RespawnPos;  // 플레이어가 부활할 위치
    [SerializeField] private float respawnDelay = 5f; // 부활까지의 지연 시간
    private bool isRespawning = false; // 현재 부활 중인지 여부

    private void Start()
    {
        OnOxygenChanged?.Invoke(oxygen);
        OnHpChanged?.Invoke(hp);

        RespawnPos = GameObject.FindWithTag(Define.Tag.RESPAWN_SPOT); // "RespawnPos" 태그를 가진 오브젝트를 찾아서 RespawnPos에 할당

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
            BeginRespawn();
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
    }

    // 상태 초기화
    private void ResetStats()
    {
        hp = MAX_HP;
        oxygen = 1f;
        OnHpChanged?.Invoke(hp);
        OnOxygenChanged?.Invoke(oxygen);
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

        // 산소가 0이 되면 HP를 바로 소모하지 않고, 별도 코루틴으로 주기적 소모 시작
        if (!isRespawning)
        {
            oxygen = 0f;
            OnOxygenChanged?.Invoke(oxygen);
            Debug.Log("산소 고갈: HP를 주기적으로 소모 시작");
            // 이미 실행중이면 다시 시작하지 않음
            if (oxygenHpDrainRoutine == null)
            {
                oxygenHpDrainRoutine = StartCoroutine(OxygenHpDrainCoroutine());
            }
        }
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

    private IEnumerator OxygenHpDrainCoroutine()
    {
        // 산소 고갈 상태 동안 주기적으로 HP 소모
        while (oxygen <= 0f && !isRespawning && hp > 0)
        {
            // HP 소모
            DecreaseHp(1);

            // 소모 후 즉시 죽었는지 확인 (DecreaseHp가 BeginRespawn을 호출함)
            if (isRespawning || hp <= 0)
            {
                break;
            }

            yield return new WaitForSeconds(oxygenHpDrainInterval);
        }

        // 코루틴 종료 시 리셋
        oxygenHpDrainRoutine = null;
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

    #region 부활 로직
    private void BeginRespawn()
    {
        if (isRespawning) return;
        isRespawning = true;

        // 멈춰야 할 것들 정리
        if (oxygenRoutine != null)
        {
            try { StopCoroutine(oxygenRoutine); } catch { }
            oxygenRoutine = null;
        }



        GameObject playerGO = this.gameObject;
        

        //playerGO.SetActive(false);

        // MainThreadDispatcher는 씬에 항상 존재하도록 설계되어 있으므로 예외처리 없이 사용
        if (MainThreadDispatcher.Instance != null)
        {
            MainThreadDispatcher.Instance.StartCoroutine(RespawnCoroutine(playerGO));
        }
    }

    private IEnumerator RespawnCoroutine(GameObject playerGO)
    {
        float remaining = respawnDelay;
        while (remaining > 0f)
        {
            // 1초 단위로 대기
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        // 리스폰 위치 결정
        Vector3 spawnPos = Vector3.zero;
        if (RespawnPos != null)
            spawnPos = RespawnPos.transform.position;

        // 위치 복구
        playerGO.transform.position = spawnPos;

        // 상태 리셋
        ResetStats();

        // 활성화
        OnPlayerRevive?.Invoke();
        //playerGO.SetActive(true);

        // 리스폰 완료
        isRespawning = false;

        StopOxygenRecovery();
    }

    #endregion
}