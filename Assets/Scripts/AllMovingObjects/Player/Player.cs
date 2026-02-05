using UnityEngine;
using System.Collections;

public class Player : MovingObject
{
    private bool inputFreeze = false;

    private PlayerInput playerInput;
    private PlayerAnimator playerAnimator;
    private PlayerRaycastCollisionSystem playerCollisionControl;

    // 새롭게 리팩토링된 버전
    protected override void Awake()
    {
        base.Awake();

        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<PlayerAnimator>();
        playerCollisionControl = GetComponent<PlayerRaycastCollisionSystem>();

        playerAnimator.Initialize();

        groundMask = LayerMask.GetMask("Ground");
        wallMask = LayerMask.GetMask("Wall");
    }

    private void Update()
    {
        inputFreeze = playerCollisionControl.CollisionDetectWithRaycast(playerInput.axisResultDir, wallMask);

        if (!inputFreeze)
        {
            playerInput.InputProcess();

            playerCollisionControl.GroundDetectingWithRaycast(groundMask);

            playerAnimator.PlayerAnimation(playerInput.axisResultDir,
                playerInput.GetIsJumping(),
                playerCollisionControl.GetIsGrounded(),
                inputFreeze);
        }
    }

    private void FixedUpdate()
    {
        if (!inputFreeze)
        {
            Moving(playerInput.axisResultDir);

            if (playerInput.GetIsJumping())
            {
                Jump();
                playerInput.MakeIsJumpingFalse();
            }
        }
        else
        {
            rb.Sleep();
        }
    }

}
