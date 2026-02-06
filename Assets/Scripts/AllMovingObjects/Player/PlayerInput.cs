using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public float axisX { get; private set; } = 0;
    public float axisY { get; private set; } = 0;
    public Vector3 axisResultDir { get; private set; } = Vector3.zero;

    private bool isJumping = false;

    private PlayerInputSystem inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputSystem();
        inputActions.Player.Enable();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        inputActions.Player.Jump.performed -= OnJump;
        inputActions.Player.Disable();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        isJumping = true;
    }

    public void InputProcess()
    {
        Vector2 move = inputActions.Player.Move.ReadValue<Vector2>();

        axisX = move.x;
        axisY = move.y;

        axisResultDir = new Vector3(axisX, 0, axisY).normalized;
    }

    public bool GetIsJumping()
    {
        return isJumping;
    }

    public void MakeIsJumpingFalse()
    {
        isJumping = false;
    }
}