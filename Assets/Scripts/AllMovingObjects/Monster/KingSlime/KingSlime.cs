using UnityEngine;
using System.Collections;
using Protocol;

public class KingSlime : Monster
{
    [SerializeField] private Rigidbody rigidbodyDragAndDrop;
    private KingSlimeAnimator kingSlimeAnimator;

    private KingSlimeAnimState? lastAppliedState;


    private bool isBiting;
    private bool isTakingDamage;
    private bool isDying;

    private Coroutine hurtCo;

    private bool IsHost =>
        ConnectManager.Instance != null && ConnectManager.Instance.isHost;


    [SerializeField] private int damage = 1;

    [SerializeField] private float dieAnimDuration = 1.2f;

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

            // Spawn 도중 사망 처리가 시작됐다면 Idle 덮어쓰기 금지
            if (isDying) yield break;

            SetStateHostAndBroadcast(KingSlimeAnimState.Idle);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsHost) return;
        if (isDying) return;

        if (hp <= 0)
        {
            if (!isTakingDamage)
                EnterDying();
            return;
        }

        //if (isSpawning || isTakingDamage) return;

        //DetectPlayer();
        //TryAttack();
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

        // 진행 중 모든 부수 코루틴 중단
        if (hurtCo != null) { StopCoroutine(hurtCo); hurtCo = null; isTakingDamage = false; }

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
}