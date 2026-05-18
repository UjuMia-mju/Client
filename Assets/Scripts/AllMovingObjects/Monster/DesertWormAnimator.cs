using UnityEngine;

public enum WormAnimState
{
    Idle = 0,
    BiteAttack = 1,
    TakeDamage = 2,
    Die = 3,
    Spawn = 4
}

public class DesertWormAnimator : MonoBehaviour
{
    private const string ANIM_PAR = "AnimPar";

    private WormAnimState state = WormAnimState.Idle;
    private Animator anim;

    public void Initialize()
    {
        anim = gameObject.GetComponent<Animator>();
        // 초기 파라미터 동기화
        if (anim != null)
            anim.SetInteger(ANIM_PAR, (int)state);
    }

    // 외부에서 상태 설정 및 즉시 애니메이터에 반영
    public void SetState(WormAnimState newState)
    {
        state = newState;
        if (anim != null)
            anim.SetInteger(ANIM_PAR, (int)state);
    }

    public WormAnimState GetAnimState()
    {
        return state;
    }
}
