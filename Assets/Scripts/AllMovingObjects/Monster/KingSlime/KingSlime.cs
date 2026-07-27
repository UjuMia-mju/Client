using UnityEngine;
using System.Collections;
using Protocol;
using System.Collections.Generic;

public class KingSlime : Monster
{
    [SerializeField] private Rigidbody rigidbodyDragAndDrop;
    private KingSlimeAnimator kingSlimeAnimator;

    private KingSlimeAnimState? lastAppliedState;


    private bool isTakingDamage;
    private bool isDying;

    private Coroutine hurtCo;

    private bool IsHost =>
        ConnectManager.Instance != null && ConnectManager.Instance.isHost;

    [SerializeField] private GameObject damageBox;
    private const float DAMAGE_BOX_LIFE_TIME = 0.5f;

    [SerializeField] private int damage = 1;

    [SerializeField] private float dieAnimDuration = 1.2f;

    [SerializeField] private List<GameObject> meteorPosList;
    [SerializeField] private GameObject meteorPrefab;
    [SerializeField] private Transform meteorTargetPos;

    [SerializeField] private float meteorInterval = 2f;
    private Coroutine meteorCo;

    [SerializeField] private int meteorMinCount = 5;
    [SerializeField] private int meteorMaxCount = 6;   // exclusive upper bound로 쓸 예정 (Random.Range(min, max+1))


    [Header("Gizmo")]
    [SerializeField] private Color detectGizmoColor = new Color(1f, 0.2f, 0.25f, 0.25f);
    [SerializeField] private Color detectGizmoWireColor = new Color(1f, 0f, 0f, 0.9f);
    [SerializeField] private float detectRadius;


    private bool isJumpingAttack;
    private Coroutine jumpCo;
    private float jumpAnimDuration = 0.4f;

    public Transform actualThisTransform;

    [Header("Attack Settings")]
    [SerializeField] private float attackInterval = 1f; // 공격 속도(공격 간 최소 간격, 초)
    private float lastAttackTime = -Mathf.Infinity;
    private Quaternion targetRotation;
    private bool hasTargetRotation;


    protected override void Awake()
    {
        base.Awake();
        rb = rigidbodyDragAndDrop;
    }

    private IEnumerator Start()
    {
        kingSlimeAnimator = GetComponent<KingSlimeAnimator>();
        kingSlimeAnimator?.Initialize();

        PlayLocalState(KingSlimeAnimState.Idle);

        if (IsHost)
        {
            BroadcastAnimState(KingSlimeAnimState.Idle);

            if (isDying) yield break;

            SetStateHostAndBroadcast(KingSlimeAnimState.Idle);

            meteorCo = StartCoroutine(MeteorLoopRoutine());   // 추가
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 호스트 권위
        if (!IsHost) return;
        if (isDying) return;

        if (hp <= 0)
        {
            if (!isTakingDamage)
                EnterDying();
            return;
        }

        //if (isSpawning || isTakingDamage) return;

        DetectPlayerAndAttack();

        // 목표 방향으로 부드럽게 회전
        if (hasTargetRotation)
        {
            actualThisTransform.rotation = Quaternion.Slerp(
                actualThisTransform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }


    private void PlayLocalState(KingSlimeAnimState newState)
    {
        lastAppliedState = newState;
        if (kingSlimeAnimator != null)
            kingSlimeAnimator.SetState(newState);
    }


    private void BroadcastAnimState(KingSlimeAnimState newState)
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

    private void SetStateHostAndBroadcast(KingSlimeAnimState newState)
    {
        // 같은 상태 중복 적용 방지.
        // (Animator 의 Any State → Die transition 이 매 프레임 재평가되며 Die 클립을
        //  계속 0프레임으로 되감는 문제와 별도로, 패킷 송신 낭비도 함께 차단.)
        if (lastAppliedState.HasValue && lastAppliedState.Value == newState)
            return;

        lastAppliedState = newState;
        PlayLocalState(newState);
        BroadcastAnimState(newState);
    }

    public override void TakeDamage(int amount)
    {
        if (!IsHost) return;
        if (isDying) return;
        if (amount <= 0) return;

        base.TakeDamage(amount);
        PlayDamageTint(amount);

        if (hurtCo != null)
            StopCoroutine(hurtCo);

        if (hp <= 0)
            hurtCo = StartCoroutine(TakeDamageThenDieRoutine());
    }

    public void SlimeHit(int damage)
    {
        Debug.Log("KingSlime : 맞았어.");

        BroadcastMonsterHit(damage);
        TakeDamage(damage);
    }

    // 몬스터 Hit 패킷 송신
    private void BroadcastMonsterHit(int damage)
    {
        if (!IsHost) return;
        if (PacketSender.Instance == null) return;

        S_MONSTER_HIT packet = new S_MONSTER_HIT
        {
            MonsterId = monsterId,
            Damage = damage
        };

        PacketSender.Instance.BroadcastMonsterHit(packet);
    }

    private void EnterDying()
    {
        if (isDying) return;
        isDying = true;

        if (hurtCo != null) { StopCoroutine(hurtCo); hurtCo = null; isTakingDamage = false; }

        if (meteorCo != null) { StopCoroutine(meteorCo); meteorCo = null; }   // 추가

        SetStateHostAndBroadcast(KingSlimeAnimState.Die);
    }

    IEnumerator TakeDamageThenDieRoutine()
    {
        isTakingDamage = true;

        isTakingDamage = false;
        hurtCo = null;
        EnterDying();

        yield return null;
    }

    // 사망 애니메이션에 이벤트로 발동됨. 코드에서 참조하지 않는다. DesertWorm과 동일
    public void OnDieAnimationEnd()
    {
        if (IsHost)
        {
            // MonsterManager.MonsterDead 가 패킷 송신 + Destroy 위임 처리.
            // MonsterManager 가 PlayDeathAndDestroy() 를 다시 호출해도 isDying==true 이므로 즉시 Destroy 됨.
            if (MonsterManager.Instance != null)
                MonsterManager.Instance.MonsterDead(monsterId);
            else
                Destroy(gameObject);


            CompleteMonsterClear();
        }
        else
        {
            // 피어는 그냥 파괴만. DestroyFromNetwork 가 이미 호출돼 dic 에서 제거된 상태.
            Destroy(gameObject);
        }

    }

    public override void PlayDeathAndDestroy()
    {
        // 호스트: 이미 EnterDying 으로 Die 재생 중이고, 이 호출 자체가 OnDieAnimationEnd 에서 트리거된 것이므로 즉시 파괴.
        if (IsHost)
        {
            Destroy(gameObject);
            return;
        }

        // 피어: 중복 진입 차단
        if (isDying) return;
        isDying = true;

        // 패킷 순서/누락 보호용: 명시적으로 Die 클립 강제. 끝나면 OnDieAnimationEnd 가 Destroy.
        PlayLocalState(KingSlimeAnimState.Die);

        // 안전 가드: Animation Event 가 어떤 이유로든 호출되지 않을 경우를 대비한 백업 타임아웃.
        StartCoroutine(DieSafetyDestroy(dieAnimDuration + 2f));
    }

    private IEnumerator DieSafetyDestroy(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (this != null && gameObject != null)
            Destroy(gameObject);
    }


    public void CompleteMonsterClear()
    {
        Debug.Log("우주선 조립이 완료되었습니다!");

        // 호스트만 피어들에게 완료 브로드캐스트 (씬 단독 실행 시 ConnectManager 없을 수 있으므로 null 체크)
        if (ConnectManager.Instance != null && ConnectManager.Instance.isHost)  
        {
            PacketSender.Instance.BroadcastSpaceshipComplete(true);

            int mapId = StageManager.LastLoadedMapId;
            var grm = GameRuleManager.Instance;
            int elapsed = grm != null ? grm.GetMissionElapsedSecondsRounded() : 0;
            var stars = 3;
            if (mapId != 0 && PacketDispatcher.Instance != null)
                PacketDispatcher.Instance.SendGameClear(mapId, stars, elapsed);
            else if (mapId == 0)
                Debug.LogWarning("[SpaceshipAssembly] LastLoadedMapId가 0 — C_GAME_CLEAR 미전송");

            GameRuleManager.Instance.ReturnToStageSelectScene(true, stars);
            return;
        }

        var starsLocal = 3;
        GameRuleManager.Instance.ReturnToStageSelectScene(true, starsLocal);
    }

    // 공격 판정
    private void SpawnDamageBox(Vector3 position)
    {
        if (damageBox == null) return;

        GameObject box = Instantiate(damageBox, position, this.transform.rotation);
        Destroy(box, DAMAGE_BOX_LIFE_TIME);
    }

    private void SpawnMeteor()
    {
        if (meteorPrefab == null || meteorPosList == null || meteorPosList.Count == 0) return;

        List<GameObject> shuffled = new List<GameObject>(meteorPosList);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        int count = Random.Range(meteorMinCount, meteorMaxCount + 1);
        count = Mathf.Min(count, shuffled.Count);   // 리스트보다 많이 뽑는 것 방지

        for (int i = 0; i < count; i++)
        {
            GameObject posObj = shuffled[i];
            if (posObj == null) continue;

            Vector3 spawnPos = posObj.transform.position;

            GameObject meteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);
            meteor.GetComponent<Meteor>()?.SetTargetPosition(meteorTargetPos.position);

            BroadcastMeteorSpawn(spawnPos);
        }
    }

    private void BroadcastMeteorSpawn(Vector3 position)
    {
        if (!IsHost) return;
        if (PacketSender.Instance == null) return;

        S_METEOR_SPAWN packet = new S_METEOR_SPAWN
        {
            MonsterId = monsterId,
            Pos = new PosInfo { X = position.x, Y = position.y, Z = position.z }
        };

        PacketSender.Instance.BroadcastMeteorSpawn(packet);
    }

    public void SpawnMeteorFromNetwork(Vector3 position)
    {
        if (meteorPrefab == null) return;

        GameObject meteor = Instantiate(meteorPrefab, position, Quaternion.identity);
        meteor.GetComponent<Meteor>()?.SetTargetPosition(meteorTargetPos.position);
    }

    private IEnumerator MeteorLoopRoutine()
    {
        while (!isDying)
        {
            yield return new WaitForSeconds(meteorInterval);

            if (isDying) yield break;

            SpawnMeteor();
        }
    }


    // 탐지범위 Gizmo
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        DrawDetectGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        DrawDetectGizmo();
    }

    private void DrawDetectGizmo()
    {
        Gizmos.color = detectGizmoColor;
        Gizmos.DrawSphere(actualThisTransform.position, detectRadius);

        Gizmos.color = detectGizmoWireColor;
        Gizmos.DrawWireSphere(actualThisTransform.position, detectRadius);
    }

    private void DetectPlayerAndAttack()
    {
        // 아직 쿨타임이 안 지났거나 이미 공격 중이면 무시
        if (isJumpingAttack || jumpCo != null) return;
        if (Time.time - lastAttackTime < attackInterval) return;

        Collider[] hits = Physics.OverlapSphere(actualThisTransform.position, detectRadius);
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

            float dist = Vector3.Distance(actualThisTransform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hit.transform;
            }
        }

        if (closest != null)
        {
            // 가장 가까운 플레이어 방향을 목표 회전값으로 저장 (Y축만)
            Vector3 lookDir = closest.position - actualThisTransform.position;
            lookDir.y = 0f; // 위아래로 기울지 않도록 수평 방향만 사용

            if (lookDir.sqrMagnitude > 0.0001f)
            {
                targetRotation = Quaternion.LookRotation(lookDir);
                hasTargetRotation = true;
            }

            lastAttackTime = Time.time; // 공격 시작 시각 기록 -> 다음 공격은 attackInterval 이후 가능

            jumpCo = StartCoroutine(JumpAttack());
        }
    }

    private IEnumerator JumpAttack()
    {
        SetStateHostAndBroadcast(KingSlimeAnimState.JumpingAttack);

        yield return new WaitForSeconds(jumpAnimDuration);

        SpawnDamageBox(actualThisTransform.position);

        if (!isDying && !isTakingDamage)
            SetStateHostAndBroadcast(KingSlimeAnimState.Idle);

        yield return new WaitForSeconds(attackInterval); // Idle 상태로 1초 대기

        jumpCo = null; // 여기서 풀어줘야 DetectPlayerAndAttack이 다음 공격을 다시 시작함
    }
}