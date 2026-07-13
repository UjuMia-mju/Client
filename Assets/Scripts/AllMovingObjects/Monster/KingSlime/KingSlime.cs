using UnityEngine;
using System.Collections;
using Protocol;

public class KingSlime : Monster
{

    private KingSlimeAnimator kingSlimeAnimator;

    private KingSlimeAnimState? lastAppliedState;


    private bool isBiting;
    private bool isTakingDamage;
    private bool isDying;

    private bool IsHost =>
        ConnectManager.Instance != null && ConnectManager.Instance.isHost;

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

        //if (hp <= 0)
        //{
        //    if (!isTakingDamage && hurtCo == null)
        //        EnterDying();
        //    return;
        //}

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
}
