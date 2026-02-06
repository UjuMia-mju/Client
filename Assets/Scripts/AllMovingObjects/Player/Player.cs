using UnityEngine;
using System.Collections;
using UnityEngine.Animations;

public class Player : MovingObject
{
    private bool inputFreeze = false;

    private PlayerInput playerInput;
    private PlayerAnimator playerAnimator;
    public PlayerRaycastCollisionSystem playerRaycastCollisionControl { get; private set; }
    private PlayerGravityController playerGravityController;



    // 새롭게 리팩토링된 버전
    protected override void Awake()
    {
        base.Awake();

        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<PlayerAnimator>();
        playerRaycastCollisionControl = GetComponent<PlayerRaycastCollisionSystem>();
        playerGravityController = GetComponent<PlayerGravityController>();

        playerAnimator.Initialize();
    }

    private void Update()
    {
        playerInput.InputProcess();
        inputFreeze = playerRaycastCollisionControl.CollisionDetectWithRaycast(playerInput.axisResultDir, wallMask);

        if (!inputFreeze)
        {
            playerRaycastCollisionControl.GroundDetectingWithRaycast(groundMask);

            playerAnimator.PlayerAnimation(playerInput.axisResultDir,
                playerInput.GetIsJumping(),
                playerRaycastCollisionControl.GetIsGrounded(),
                inputFreeze);
        }

        playerRaycastCollisionControl.GetGroundNormal(groundMask);
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
