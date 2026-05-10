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
    private bool isLeftClick = false;

    private PlayerInputSystem inputActions;

    private bool inputEnabled = true;
    private float scrambleMoveInputUntil;
    private int scramblePatternIndex;

    // x/y를 섞는 변환 행렬 패턴(a,b,c,d): (x', y') = (a*x + b*y, c*x + d*y)
    private static readonly Vector4[] MoveScramblePatterns =
    {
        new Vector4(-1f, 0f, 0f, 1f),  // A<->D 반전
        new Vector4(1f, 0f, 0f, -1f),  // W<->S 반전
        new Vector4(0f, 1f, 1f, 0f),   // x/y 스왑
        new Vector4(0f, -1f, -1f, 0f), // x/y 스왑 + 반전
        new Vector4(0f, -1f, 1f, 0f),  // 90도 회전
        new Vector4(0f, 1f, -1f, 0f),  // -90도 회전
        new Vector4(-1f, 0f, 0f, -1f)  // x/y 모두 반전
    };

    private void Start()
    {
        //string updatedKeys = DataManager.Instance.InputAsset.SaveBindingOverridesAsJson();
        //inputActions.asset.LoadBindingOverridesFromJson(updatedKeys);
    }

    private void OnEnable()
    {
        if (inputActions == null)
            inputActions = InputManager.Instance.Actions;

        inputActions.Player.Enable();
        inputActions.Player.Jump.performed += OnJump;
        inputActions.Player.Interact.performed += OnInteract;
        inputActions.Player.ThrowOrCancel.performed += OnThrowOrCancel;
        inputActions.Player.LeftClick.performed += OnLeftClick;
    }

    private void OnDisable()
    {
        inputActions.Player.Jump.performed -= OnJump;
        inputActions.Player.Interact.performed -= OnInteract;
        inputActions.Player.ThrowOrCancel.performed -= OnThrowOrCancel;
        inputActions.Player.LeftClick.performed -= OnLeftClick;
        inputActions.Player.Disable();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!inputEnabled) return;
        Debug.Log("점프 눌러짐");
        isJumping = true;
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!inputEnabled) return;
        isInteract = true;
    }

    private void OnThrowOrCancel(InputAction.CallbackContext ctx)
    {
        if (!inputEnabled) return;
        isThrowOrCancel = true;
    }

    private void OnLeftClick(InputAction.CallbackContext ctx)
    {
        if (!inputEnabled) return;
        isLeftClick = true;
    }

    // 입력받은 값으로 초기화
    public void InputProcess()
    {
        if (!inputEnabled)
        {
            // 사망 등으로 입력이 막힌 동안에는 이동 입력을 0으로 강제.
            // 액션맵 자체는 살아있어야 PlayerTPCamera의 Look이 동작함.
            axisX = 0f;
            axisY = 0f;
            axisResultDir = Vector3.zero;
            return;
        }

        Vector2 move = inputActions.Player.Move.ReadValue<Vector2>();

        if (Time.time < scrambleMoveInputUntil)
        {
            Vector4 m = MoveScramblePatterns[scramblePatternIndex];
            move = new Vector2(
                m.x * move.x + m.y * move.y,
                m.z * move.x + m.w * move.y);
        }

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

    public bool GetIsLeftClick()
    {
        return isLeftClick;
    }

    public void MakeIsLeftClickFalse()
    {
        isLeftClick = false;
    }

    // 외부에서 입력 전체 활성/비활성 토글
    // 액션맵은 끄지 않는다 — PlayerTPCamera가 같은 맵의 Look을 사용하므로 카메라가 죽어버림.
    // 대신 inputEnabled 플래그로 게이팅한다.
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!inputEnabled)
        {
            // 즉시 모든 입력 상태 초기화 (눌려있던 키가 잔존하지 않도록)
            isJumping = false;
            isInteract = false;
            isThrowOrCancel = false;
            isLeftClick = false;
            axisX = 0f;
            axisY = 0f;
            axisResultDir = Vector3.zero;
            scrambleMoveInputUntil = 0f;
        }

        Debug.Log($"[PlayerInput] SetInputEnabled -> {inputEnabled}");
    }

    /// <summary>
    /// 일정 시간 동안 이동 입력(WASD/스틱)을 랜덤 패턴으로 섞습니다.
    /// </summary>
    public void ApplyRandomMoveScramble(float durationSeconds)
    {
        if (durationSeconds <= 0f) return;

        // 이미 디버프가 걸린 상태에서 다시 닿으면 패턴은 유지하고 시간만 갱신합니다.
        if (Time.time < scrambleMoveInputUntil)
        {
            scrambleMoveInputUntil = Time.time + durationSeconds;
            return;
        }

        int newPattern = Random.Range(0, MoveScramblePatterns.Length);
        if (MoveScramblePatterns.Length > 1 && newPattern == scramblePatternIndex)
            newPattern = (newPattern + 1) % MoveScramblePatterns.Length;

        scramblePatternIndex = newPattern;
        scrambleMoveInputUntil = Time.time + durationSeconds;
    }
}