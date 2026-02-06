using UnityEngine;


public class PlayerTPCamera : MonoBehaviour
{
    private const float Y_ANGLE_MIN = 0.0f;
    private const float Y_ANGLE_MAX = 50.0f;

    public float distance = 5.0f;

    private float currentX = 0.0f;
    private float currentY = 45.0f;

    // 마우스 민감도, 추후 설정에 민감도 옵션 추가 시 이 상수는 제거하거나 리팩토링해야 합니다. 상의 필요합니다.
    private const float SENSITIVITY = 0.2f;

    private PlayerInputSystem inputActions;

    public Transform cameraOffset;
    public Transform planet;


    private void Awake()
    {
        inputActions = new PlayerInputSystem();
        inputActions.Player.Enable();
    }

    private void Update()
    {
        Vector2 look = inputActions.Player.Look.ReadValue<Vector2>();

        currentX += look.x * SENSITIVITY;
        currentY += -look.y * SENSITIVITY;

        currentY = Mathf.Clamp(currentY, Y_ANGLE_MIN, Y_ANGLE_MAX);

        Debug.Log(this.transform.eulerAngles);
    }

    private void LateUpdate()
    {
        if (cameraOffset == null || planet == null) return;

        // =========================
        //  X축 연산 (Yaw)
        // =========================
        Vector3 gravityUp = (cameraOffset.position - planet.position).normalized;

        Quaternion yawRot = Quaternion.AngleAxis(currentX, gravityUp);

        Vector3 baseForward = Vector3.ProjectOnPlane(cameraOffset.forward, gravityUp).normalized;
        if (baseForward.sqrMagnitude < 0.01f)
            baseForward = Vector3.ProjectOnPlane(transform.forward, gravityUp).normalized;
        if (baseForward.sqrMagnitude < 0.01f)
            baseForward = Vector3.ProjectOnPlane(Vector3.forward, gravityUp).normalized;

        Vector3 right = Vector3.Cross(gravityUp, baseForward).normalized;
        Vector3 forward = Vector3.Cross(right, gravityUp).normalized;

        // --- yaw 적용 offset ---
        Vector3 offset = yawRot * (-forward * distance);


        // =========================
        //  Y축 연산 (Pitch 추가!)
        // =========================

        // pitch 회전축 = yaw 적용 후의 right 축
        Vector3 pitchAxis = yawRot * right;
        pitchAxis.Normalize();

        // pitch 회전 쿼터니언
        Quaternion pitchRot = Quaternion.AngleAxis(currentY, pitchAxis);

        // offset 에 pitch 적용
        offset = pitchRot * offset;


        // =========================
        //  카메라 위치 및 회전 적용
        // =========================
        transform.position = cameraOffset.position + offset;

        Vector3 newForward = (cameraOffset.position - transform.position).normalized;
        Vector3 newRight = Vector3.Cross(gravityUp, newForward).normalized;
        Vector3 newUp = Vector3.Cross(newRight, newForward).normalized;

        transform.rotation = Quaternion.LookRotation(newForward, -newUp);
    }
}