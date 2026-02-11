using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어의 입력을 담당하는 클래스
public class PlayerInput : MonoBehaviour
{
    // x,y, 그리고 그것으로 만들어낸 벡터
    // 외부에서 참조가 필요하므로 get은 public으로 정하였습니다.
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

    // 입력받은 값으로 초기화
    public void InputProcess()
    {
        Vector2 move = inputActions.Player.Move.ReadValue<Vector2>();

        axisX = move.x;
        axisY = move.y;

        axisResultDir = new Vector3(axisX, 0, axisY).normalized;
    }

    // 현재 점프 상태 반환
    public bool GetIsJumping()
    {
        return isJumping;
    }

    // 점프는 외부에서 false로만 전환시킵니다. 그외 외부에서의 수정은 허락하지 않습니다. (OnJump 함수로 발동되는 점프만 제외)
    public void MakeIsJumpingFalse()
    {
        isJumping = false;
    }
}