using Unity.VisualScripting;
using UnityEngine;

enum AnimState
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
            Debug.Log(1);
            state = AnimState.Idle;
        }

        else if (isJumping && isGrounded)
        {
            Debug.Log(2);
            state = AnimState.Jump;
        }

        else if (moveDir != Vector3.zero && isGrounded)
        {

            Debug.Log(3);
            state = AnimState.Run;
        }

        else if (!isGrounded)
        {

            Debug.Log(4);
            state = AnimState.Falling;
        }


        anim.SetInteger("AnimationPar", (int)state);
    }
}
