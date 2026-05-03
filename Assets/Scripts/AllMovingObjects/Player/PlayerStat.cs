using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// 순수 데이터 및 기본 연산 (class: StatManager와 씬 PlayerStat이 동일 statData 인스턴스를 공유)
public class PlayerStatData
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
        oxygen = Mathf.Round(rawOxygen * 10000f) / 10000f;
        oxygen = Mathf.Clamp01(oxygen);
    }

    public void DecreaseOxygen(float amount)
    {
        float rawOxygen = oxygen - amount;
        oxygen = Mathf.Round(rawOxygen * 10000f) / 10000f;
        oxygen = Mathf.Clamp01(oxygen);
    }
}

/// <summary>씬에 붙이지 않는 HP/산소 상태. StatManager에서 new로 생성합니다 (MonoBehaviour는 new 불가).</summary>
public class PlayerStatState
{
    public PlayerStatData statData;
    public event Action<float> OnOxygenChanged;
    public event Action<int> OnHpChanged;
    public event Action OnPlayerDead;
    public PlayerStat boundPlayer;

    public PlayerStatState(int maxHp = 5)
    {
        statData = new PlayerStatData(maxHp);
    }

    public int GetHp() => statData.hp;
    public float GetOxygen() => statData.oxygen;

    public void ChangeData(int hp, float oxygen)
    {
        statData.hp = hp;
        statData.oxygen = oxygen;
    }

    public void CallOnHpChanged()
    {
        OnHpChanged?.Invoke(statData.hp);
        boundPlayer?.CallOnHpChanged();
    }

    public void CallOnOxygenChanged()
    {
        OnOxygenChanged?.Invoke(statData.oxygen);
        boundPlayer?.CallOnOxygenChanged();
    }

    public void CallOnPlayerDead()
    {
        OnPlayerDead?.Invoke();
        boundPlayer?.CallOnPlayerDead();
    }
}

/// <summary>
/// 플레이어의 HP와 산소 상태를 관리하는 클래스.
/// 사망/부활 결정권은 호스트의 PlayerLifeServerManager가 가짐. 여기서는 보고/적용만.
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

    // 사망/부활 이벤트 (UI/카메라/입력 등이 구독)
    public event Action OnPlayerDead;
    public event Action OnPlayerRevive;

    /// <summary>이미 사망 보고를 호스트에 보냈는지. 부활 적용 시 false로 리셋.</summary>
    protected bool isRespawning = false;

    /// <summary>ApplyDeath가 이미 적용됐는지. echo 중복 방지용.</summary>
    private bool _isDeadApplied = false;

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

        // 호스트 권위 stat 푸시(S_PLAYER_STAT) 결과 hp=0이면 즉시 사망 보고.
        // 피어 본인의 사망 감지가 이 경로로만 들어옴.
        if (hp <= 0 && !isRespawning)
        {
            ReportDeathToHost();
        }
    }

    #region HP 증/감소 로직
    public void CallOnHpChanged() => OnHpChanged?.Invoke(statData.hp);
    public virtual void DecreaseHp(int damage)
    {
        if (isRespawning) return; // 부활 대기 중에는 데미지 무시

        statData.DecreaseHp(damage);
        CallOnHpChanged();
        if (statData.hp <= 0)
        {
            ReportDeathToHost();
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
    public virtual void StartOxygenDecrease()
    {
        if (oxygenRoutine != null) StopCoroutine(oxygenRoutine);
        oxygenRoutine = StartCoroutine(DecreaseOxygen());
    }
    public virtual IEnumerator DecreaseOxygen() { yield break; }
    public virtual IEnumerator IncreaseOxygen() { yield break; }
    public virtual IEnumerator OxygenHpDrainCoroutine() { yield break; }

    public virtual void StartOxygenRecovery()
    {
        if (oxygenRoutine != null) StopCoroutine(oxygenRoutine);
        oxygenRoutine = StartCoroutine(IncreaseOxygen());
    }

    public virtual void StopOxygenRecovery()
    {
        if (oxygenRoutine != null) StopCoroutine(oxygenRoutine);
        oxygenRoutine = StartCoroutine(DecreaseOxygen());
    }
    #endregion

    public void CallOnPlayerDead() => OnPlayerDead?.Invoke();

    // ======================================================================
    // 사망/부활 — 네트워크 협업
    // ======================================================================

    /// <summary>로컬 HP가 0이 되었을 때 호출. 호스트에 사망을 보고만 한다.</summary>
    private void ReportDeathToHost()
    {
        if (isRespawning) return;
        isRespawning = true;

        ulong myId = GetMyPlayerId();
        Debug.Log($"[PlayerStat] 사망 감지 → 호스트 보고. playerId={myId}");

        if (ConnectManager.Instance != null && ConnectManager.Instance.isHost)
        {
            // 호스트 본인 사망: 매니저 직접 호출
            PlayerLifeServerManager.Instance.OnReceivePlayerDead(myId);
        }
        else
        {
            // 피어 사망: 호스트에게 패킷
            PacketSender.Instance.SendPlayerDead(myId);
        }
    }

    /// <summary>
    /// 호스트로부터 S_PLAYER_DEAD 수신 시 적용 (또는 호스트 본인의 사망 직후 매니저가 직접 호출).
    /// 산소/HP 코루틴 정지, 사망 이벤트 발화.
    /// </summary>
    public virtual void ApplyDeathFromNetwork()
    {
        if (_isDeadApplied) return;       // 멱등: echo가 두 번 와도 한 번만
        _isDeadApplied = true;
        isRespawning = true;

        if (oxygenRoutine != null) { StopCoroutine(oxygenRoutine); oxygenRoutine = null; }
        if (oxygenHpDrainRoutine != null) { StopCoroutine(oxygenHpDrainRoutine); oxygenHpDrainRoutine = null; }

        statData.hp = 0;
        CallOnHpChanged();

        var player = GetComponent<Player>();
        if (player != null && player.playerItemSystem != null && player.playerItemSystem.currentEquipItem != null)
        {
            player.isPlayerGetSomething = false;
            player.playerItemSystem.ThrowItem(0);
        }

        OnPlayerDead?.Invoke();
        Debug.Log("[PlayerStat] ApplyDeathFromNetwork");
    }

    /// <summary>
    /// 호스트로부터 S_PLAYER_REVIVE 수신 시 적용. 위치 이동 + 스탯 풀 복원 + 산소 코루틴 재시작.
    /// </summary>
    public virtual void ApplyReviveFromNetwork(Vector3 pos, Quaternion rot)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(pos, rot);

        statData.Reset();
        CallOnHpChanged();
        CallOnOxygenChanged();

        isRespawning = false;
        _isDeadApplied = false;          // 다음 사망을 위해 리셋
        OnPlayerRevive?.Invoke();

        StartOxygenDecrease();
        Debug.Log($"[PlayerStat] ApplyReviveFromNetwork. pos={pos}");
    }

    public void CallOnPlayerRevive() => OnPlayerRevive?.Invoke();
}