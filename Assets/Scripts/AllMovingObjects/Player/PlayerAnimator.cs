using Unity.VisualScripting;
using UnityEngine;

public enum AnimState
{
    Idle,
    Run,
    Jump,
    Falling
}

public class PlayerAnimator : MonoBehaviour
{
    private AnimState state = new AnimState();
    private Animator anim;

    public void Initialize()
    {
        anim = gameObject.GetComponentInChildren<Animator>();
    }

    public void PlayerAnimation(Vector3 moveDir, bool isJumping, bool isGrounded, bool inputFreeze)
    {
        if (inputFreeze || isGrounded && moveDir == Vector3.zero)
        {
            state = AnimState.Idle;
        }

        else if (isJumping && isGrounded)
        {
            state = AnimState.Jump;
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
