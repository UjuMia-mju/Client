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
    private bool isInteract = false;
    private bool isThrowOrCancel = false;

    private PlayerInputSystem inputActions; 

    private void Awake()
    {
        inputActions = new PlayerInputSystem();
        inputActions.Player.Enable();
    }

    private void Start()
    {
        //string updatedKeys = DataManager.Instance.InputAsset.SaveBindingOverridesAsJson();
        //inputActions.asset.LoadBindingOverridesFromJson(updatedKeys);
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Jump.performed += OnJump;
        inputActions.Player.Interact.performed += OnInteract;
        inputActions.Player.ThrowOrCancel.performed += OnThrowOrCancel;
    }

    private void OnDisable()
    {
        inputActions.Player.Jump.performed -= OnJump;
        inputActions.Player.Interact.performed -= OnInteract;
        inputActions.Player.ThrowOrCancel.performed -= OnThrowOrCancel;
        inputActions.Player.Disable();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        Debug.Log("점프 눌러짐");
        isJumping = true;
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        isInteract = true;
    }

    private void OnThrowOrCancel(InputAction.CallbackContext ctx)
    {
        isThrowOrCancel = true;
    }

    // 입력받은 값으로 초기화
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

    public void SetIsJumping(bool data)
    {
        isJumping = data;
    }

    public bool GetIsInteract()
    {
        return isInteract;
    }

    public void MakeIsInteractFalse()
    {
        isInteract = false;
    }

    public bool GetIsThrowOrCancel()
    {
        return isThrowOrCancel;
    }

    public void MakeIsThrowOrCancelFalse()
    {
        isThrowOrCancel = false;
    }
}