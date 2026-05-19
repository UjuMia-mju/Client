using System.Collections;
using UnityEngine;
using Protocol;

public class DesertWorm : Monster
{
    [Header("Attack Settings")]
    public GameObject damageBox;
    [Tooltip("공격 예고 표시용 프리팹 (시각적 경고 마커). 비워두면 표시 안 함.")]
    [SerializeField] private GameObject telegraphPrefab;
    [Tooltip("예고 표시 후 실제 데미지박스가 생성되기까지의 시간(초). 회피 가능 시간.")]
    [SerializeField] private float telegraphDelay = 1f;
    [SerializeField] private float detectRadius = 5f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float damageBoxLifetime = 0.3f;
    
    [Tooltip("플레이어 방향으로 회전할 때 1초당 회전 각도(도). 클수록 빠르게 돈다.")]
    [SerializeField] private float turnSpeedDegPerSec = 540f;

    [Header("Animation Durations")]
    [SerializeField] private float spawnAnimDuration = 1.5f;
    [SerializeField] private float biteAnimDuration = 0.6f;
    [SerializeField] private float takeDamageAnimDuration = 0.4f;
    [SerializeField] private float dieAnimDuration = 1.2f;

    [Header("Gizmo")]
    [SerializeField] private bool alwaysDrawGizmo = false;
    [SerializeField] private Color detectGizmoColor = new Color(1f, 0.2f, 0.25f, 0.25f);
    [SerializeField] private Color detectGizmoWireColor = new Color(1f, 0f, 0f, 0.9f);

    private DesertWormAnimator wormAnimator;
    private Transform targetPlayer;
    private float attackTimer;

    // 상태 플래그
    private bool isSpawning = true;
    private bool isBiting;
    private bool isTakingDamage;
    private bool isDying;

    // 진행 중 코루틴 핸들 (피격 시 Bite 중단용)
    private Coroutine biteCo;
    private Coroutine hurtCo;

    private bool IsHost =>
        ConnectManager.Instance != null && ConnectManager.Instance.isHost;

    private IEnumerator Start()
    {
        wormAnimator = GetComponent<DesertWormAnimator>();
        wormAnimator?.Initialize();

        PlayLocalState(WormAnimState.Spawn);

        if (IsHost)
        {
            BroadcastAnimState(WormAnimState.Spawn);
            yield return new WaitForSeconds(spawnAnimDuration);

            isSpawning = false;

            // Spawn 도중 사망 처리가 시작됐다면 Idle 덮어쓰기 금지
            if (isDying) yield break;

            SetStateHostAndBroadcast(WormAnimState.Idle);
        }
        else
        {
            isSpawning = false;
        }
    }

    private void Update()
    {
        if (!IsHost) return;
        if (isDying) return;

        if (hp <= 0)
        {
            StartCoroutine(DieRoutine());
            return;
        }

        if (isSpawning) return;
        if (isTakingDamage) return; // 피격 경직 중엔 AI 정지

        DetectPlayer();
        TryAttack();
    }

    // ===== 데미지 받음 =====
    public override void TakeDamage(int amount)
    {
        if (!IsHost) return;     // 데미지 판정은 호스트만
        if (isDying) return;
        if (amount <= 0) return;

        base.TakeDamage(amount); // hp 감소

        // hp가 0 이하면 굳이 hurt 재생 없이 Update에서 DieRoutine으로 진입
        if (hp <= 0) return;

        // 진행 중 Bite는 중단하고 Hurt 재생
        if (biteCo != null)
        {
            StopCoroutine(biteCo);
            biteCo = null;
            isBiting = false;
        }
        if (hurtCo != null) StopCoroutine(hurtCo);
        hurtCo = StartCoroutine(TakeDamageRoutine());
    }

    private IEnumerator TakeDamageRoutine()
    {
        isTakingDamage = true;
        SetStateHostAndBroadcast(WormAnimState.TakeDamage);

        yield return new WaitForSeconds(takeDamageAnimDuration);

        isTakingDamage = false;
        hurtCo = null;

        if (!isDying && !isSpawning)
            SetStateHostAndBroadcast(WormAnimState.Idle);
    }

    // ===== AI =====
    private void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius);
        float closestDist = float.MaxValue;
        Transform closest = null;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag(Define.Tag.PLAYER)) continue;

            // 죽은 플레이어 제외 (로컬/원격 모두)
            Player localPlayer = hit.GetComponentInParent<Player>();
            if (localPlayer != null)
            {
                PlayerStat stat = localPlayer.GetComponent<PlayerStat>();
                if (stat != null && stat.GetHp() <= 0) continue;
            }
            else
            {
                OtherPlayers remotePlayer = hit.GetComponentInParent<OtherPlayers>();
                if (remotePlayer != null
                    && HostStatManager.Instance != null
                    && HostStatManager.Instance.TryGetPlayerStat(remotePlayer.PlayerId, out var remoteStat)
                    && remoteStat != null
                    && remoteStat.GetHp() <= 0)
                {
                    continue;
                }
            }

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hit.transform;
            }
        }

        targetPlayer = closest;
    }

    private void TryAttack()
    {
        if (targetPlayer == null)
        {
            attackTimer = 0f;
            return;
        }

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            biteCo = StartCoroutine(BiteOnceRoutine());
        }
    }

    private IEnumerator BiteOnceRoutine()
    {
        if (isBiting) yield break;
        isBiting = true;

        // 1) 공격 지점 미리 결정 + 예고 마커
        Vector3 attackPos = targetPlayer != null ? targetPlayer.position : transform.position;

        GameObject telegraph = null;
        if (telegraphPrefab != null)
        {
            telegraph = Instantiate(telegraphPrefab, attackPos, Quaternion.identity);
            Destroy(telegraph, telegraphDelay + damageBoxLifetime);
        }

        // 2) 회피 시간 동안 플레이어 방향으로 부드럽게 회전 + 대기
        if (targetPlayer != null)
            yield return StartCoroutine(FaceTargetHorizontally(targetPlayer.position));

        // 회전 완료 후 남은 시간 (회전이 telegraphDelay보다 빨리 끝났을 때 보정)
        // 단순화: telegraphDelay 통째로 더 기다리지 않고, 회전 + 추가 대기로 분리
        // 더 자연스럽게 하려면 아래 한 줄을 별도 wait으로 조정 가능
        yield return new WaitForSeconds(telegraphDelay);

        // 3) BiteAttack 애니 + 데미지박스
        SetStateHostAndBroadcast(WormAnimState.BiteAttack);
        SpawnDamageBox(attackPos);

        yield return new WaitForSeconds(biteAnimDuration);

        if (!isDying && !isSpawning && !isTakingDamage)
            SetStateHostAndBroadcast(WormAnimState.Idle);

        isBiting = false;
        biteCo = null;
    }

    /// <summary>
    /// 행성 표면 위에서 수평(transform.up 축 기준) 방향만 타겟을 향하도록 보간 회전.
    /// 회전이 끝나면 코루틴 종료.
    /// </summary>
    private IEnumerator FaceTargetHorizontally(Vector3 targetPos)
    {
        const float epsilonDeg = 0.5f;

        while (true)
        {
            Vector3 up = transform.up;
            Vector3 toTarget = targetPos - transform.position;
            Vector3 flatDir = Vector3.ProjectOnPlane(toTarget, up);
            if (flatDir.sqrMagnitude < 1e-6f) yield break;

            Quaternion targetRot = Quaternion.LookRotation(flatDir.normalized, up);
            float angle = Quaternion.Angle(transform.rotation, targetRot);
            if (angle < epsilonDeg) yield break;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, turnSpeedDegPerSec * Time.deltaTime);

            // 매 프레임 피어에게 회전 동기화
            if (PacketSender.Instance != null)
            {
                S_MONSTER_MOVE movePacket = new S_MONSTER_MOVE
                {
                    MonsterId = monsterId,
                    Pos = new PosInfo { X = transform.position.x, Y = transform.position.y, Z = transform.position.z },
                    Rot = new RotInfo { X = transform.rotation.x, Y = transform.rotation.y, Z = transform.rotation.z, W = transform.rotation.w }
                };
                PacketSender.Instance.BroadcastMonsterMove(movePacket);
            }

            yield return null;
        }
    }

    private IEnumerator DieRoutine()
    {
        isDying = true;

        // 진행 중 모든 부수 코루틴 중단
        if (biteCo != null) { StopCoroutine(biteCo); biteCo = null; isBiting = false; }
        if (hurtCo != null) { StopCoroutine(hurtCo); hurtCo = null; isTakingDamage = false; }

        SetStateHostAndBroadcast(WormAnimState.Die);

        yield return new WaitForSeconds(dieAnimDuration);

        MonsterManager.Instance.MonsterDead(monsterId);
    }

    private void SpawnDamageBox(Vector3 position)
    {
        if (damageBox == null) return;

        GameObject box = Instantiate(damageBox, position, Quaternion.identity);
        Destroy(box, damageBoxLifetime);
    }

    // ===== 상태/네트워크 헬퍼 =====
    private void SetStateHostAndBroadcast(WormAnimState newState)
    {
        PlayLocalState(newState);
        BroadcastAnimState(newState);
    }

    private void PlayLocalState(WormAnimState newState)
    {
        if (wormAnimator != null)
            wormAnimator.SetState(newState);
    }

    private void BroadcastAnimState(WormAnimState newState)
    {
        if (!IsHost) return;
        if (PacketSender.Instance == null) return;

        S_MONSTER_ANIMATION packet = new S_MONSTER_ANIMATION
        {
            MonsterId = monsterId,
            State = (int)newState
        };
        PacketSender.Instance.BroadcastMonsterAnimation(packet);
    }

    // ===== Gizmo =====
    private void OnDrawGizmos()
    {
        if (!alwaysDrawGizmo) return;
        DrawDetectGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (alwaysDrawGizmo) return;
        DrawDetectGizmo();
    }

    private void DrawDetectGizmo()
    {
        Gizmos.color = detectGizmoColor;
        Gizmos.DrawSphere(transform.position, detectRadius);

        Gizmos.color = detectGizmoWireColor;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        if (Application.isPlaying && targetPlayer != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPlayer.position);
        }
    }
}
