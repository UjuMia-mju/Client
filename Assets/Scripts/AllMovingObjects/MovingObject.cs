using UnityEngine;
using UnityEngine.InputSystem.XR;

// 모든 움직이는 오브젝트의 기본 클래스
public class MovingObject : MonoBehaviour
{
    // 이동 관련 변수
    public Rigidbody rb { get; protected set; }
    protected Vector3 moveAmount;

    public float walkSpeed;
    public float jumpForce;
    public float rotationSpeed;

    protected float currentAngle;

    // 레이어마스크
    protected LayerMask groundMask;
    protected LayerMask wallMask;


    // 초기화
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        groundMask = LayerMask.GetMask("Ground");
        wallMask = LayerMask.GetMask("Wall");
    }

    // 이동 처리
    protected virtual void Moving(Vector3 movDir)
    {
        Vector3 targetMoveAmount = movDir * walkSpeed;
        moveAmount = Vector3.MoveTowards(moveAmount, targetMoveAmount, walkSpeed);
        rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
    }

    // 점프 처리
    protected virtual void Jump()
    {
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    // 캐릭터가 바라보는 방향을 입력 방향으로 회전시킴
    protected virtual void RotateToDirection(Transform t, float h, float v)
    {
        if (v < 0) { return; }
        Vector3 localInput = new Vector3(h, 0f, v);
        if (localInput.sqrMagnitude < 0.0001f) return;

        Vector3 worldDir = t.TransformDirection(localInput);

        Vector3 up = t.up;
        worldDir = Vector3.ProjectOnPlane(worldDir, up);
        if (worldDir.sqrMagnitude < 0.0001f) return;
        worldDir.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(worldDir, up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
    }
}