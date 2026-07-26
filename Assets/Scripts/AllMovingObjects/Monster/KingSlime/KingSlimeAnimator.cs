using UnityEngine;
using System.Collections;

public enum KingSlimeAnimState
{
    Idle = 0,
    JumpingAttack = 1,
    Die = 2
}

public class KingSlimeAnimator : MonoBehaviour
{
    private const string ANIM_PAR = "AnimationPar";

    private KingSlimeAnimState state = KingSlimeAnimState.Idle;
    private Animator anim;

    public void Initialize()
    {
        anim = gameObject.GetComponent<Animator>();
        // 초기 파라미터 동기화
        if (anim != null)
            anim.SetInteger(ANIM_PAR, (int)state);
    }
    public void SetState(KingSlimeAnimState newState)
    {
        state = newState;
        if (anim != null)
            anim.SetInteger(ANIM_PAR, (int)state);
    }

    public KingSlimeAnimState GetAnimState()
    {
        return state;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
