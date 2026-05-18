using System.Collections;
using UnityEngine;
using Protocol;

public class DesertWorm : Monster
{
    [Header("Attack Settings")]
    public GameObject damageBox;
    [SerializeField] private float detectRadius = 5f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float damageBoxLifetime = 0.3f;

    [Header("Animation Durations")]
    [SerializeField] private float spawnAnimDuration = 1.5f;
    [SerializeField] private float biteAnimDuration = 0.6f;
    [SerializeField] private float takeDamageAnimDuration = 0.4f;
    [SerializeField] private float dieAnimDuration = 1.2f;

    [Header("Gizmo")]
    [SerializeField] private bool alwaysDrawGizmo = false;
    [SerializeField] private Color detectGizmoColor = new Color(1f, 0.2f, 0.2f, 0.25f);
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

        SetStateHostAndBroadcast(WormAnimState.BiteAttack);

        if (targetPlayer != null)
            SpawnDamageBox(targetPlayer.position);

        yield return new WaitForSeconds(biteAnimDuration);

        if (!isDying && !isSpawning && !isTakingDamage)
            SetStateHostAndBroadcast(WormAnimState.Idle);

        isBiting = false;
        biteCo = null;
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
