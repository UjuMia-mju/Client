using Unity.VisualScripting;
using UnityEngine;

public enum AnimState
{
    Idle,
    Run,
    Jump,
    Falling,
    Mining
}

public class PlayerAnimator : MonoBehaviour
{
    private AnimState state = new AnimState();
    private Animator anim;

    public void Initialize()
    {
        anim = gameObject.GetComponentInChildren<Animator>();
    }

    public void PlayerAnimation(Vector3 moveDir, bool isJumping, bool isGrounded, bool inputFreeze, bool isMining)
    {
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
}
