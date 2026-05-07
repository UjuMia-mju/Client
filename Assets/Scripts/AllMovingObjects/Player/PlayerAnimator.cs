using Unity.VisualScripting;
using UnityEngine;

public enum AnimState
{
    Idle,
    Run,
    Jump,
    Falling,
    Mining,
    Throw_Ready,
    Throw_Release,
    Suprise
}

public class PlayerAnimator : MonoBehaviour
{
    private AnimState state = new AnimState();
    private Animator anim;

    public void Initialize()
    {
        anim = gameObject.GetComponentInChildren<Animator>();
    }

    public void PlayerAnimation(
        Vector3 moveDir,
        bool isJumping,
        bool isGrounded,
        bool inputFreeze,
        bool isMining,
        bool isHoldingThrow,
        bool isReleaseThrow)
    {
        // 기존 AnimationPar 구조를 유지하면서 던지기 상태만 우선 적용합니다.
        if (isReleaseThrow && !isMining)
        {
            state = AnimState.Throw_Release;
            anim.SetInteger("AnimationPar", (int)state);
            return;
        }

        if (isHoldingThrow && !isMining)
        {
            state = AnimState.Throw_Ready;
            anim.SetInteger("AnimationPar", (int)state);
            return;
        }

        if (inputFreeze || isGrounded && moveDir == Vector3.zero && !isMining)
        {
            state = AnimState.Idle;
        }

        else if (isJumping && isGrounded)
        {
            state = AnimState.Jump;
        }

        else if (isMining && isGrounded)
        {
            state = AnimState.Mining;
        }

        else if (moveDir != Vector3.zero && isGrounded)
        {
            state = AnimState.Run;
        }

        else if (!isGrounded)
        {
            state = AnimState.Falling;
        }


        anim.SetInteger("AnimationPar", (int)state);
    }

    public AnimState GetAnimState()
    {
        return state;
    }

    public void SurpriseAnimation(bool isSurprise)
    {
        if (isSurprise)
        {
            state = AnimState.Suprise;
            anim.SetInteger("AnimationPar", (int)state);
        }
    }
}
