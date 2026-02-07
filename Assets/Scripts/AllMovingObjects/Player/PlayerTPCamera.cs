using UnityEngine;

public class PlayerTPCamera : MonoBehaviour
{
    // Y축 회전 각도 제한
    private const float Y_ANGLE_MIN = 0.0f;
    private const float Y_ANGLE_MAX = 50.0f;

    // 인스펙터에서 조정 가능한 카메라 거리
    public float distance = 5.0f;

    // 마우스로 입력하는 회전 수치
    private float currentX = 0.0f;
    private float currentY = 45.0f;

    // 마우스 민감도, 추후 설정에 민감도 옵션 추가 시 이 상수는 제거하거나 리팩토링해야 합니다. 상의 필요합니다.
    private const float SENSITIVITY = 0.2f;

    private PlayerInputSystem inputActions;

    // 카메라 오프셋, 중력의 주체
    // 원래 카메라를 플레이어의 자식으로 두고 상대 위치로 처리하려 했으나, 플레이어의 입력과 분리시킬수가 없어 직접 인스펙터에서 참조하도록 변경하였습니다.
    public Transform cameraOffset;
    public Transform planet;

    private Vector3 baseForwardRef; // 기준 forward (월드 벡터)
    private bool inited;

    // 
    // 20260207 수정본
    private float lastX = 0.0f;

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
    }

    private void LateUpdate()
    {
        if (cameraOffset == null || planet == null) return;

        //  X축 연산 (Yaw)
        Vector3 gravityUp = (cameraOffset.position - planet.position).normalized;

        if (!inited)
        {
            // 카메라가 타겟을 바라보는 방향을 기준으로 잡기 (처음 1회)
            Vector3 toTarget = (cameraOffset.position - transform.position).normalized;

            baseForwardRef = Vector3.ProjectOnPlane(toTarget, gravityUp).normalized;
            if (baseForwardRef.sqrMagnitude < 0.01f)
                baseForwardRef = Vector3.ProjectOnPlane(Vector3.forward, gravityUp).normalized;

            inited = true;
        }

        // 수정본 추가, 이 코드 때문에 currentX같은 누적량을 쓰면 카메라가 빙글빙글 돕니다.
        // 영벡터를 정사영하면 이 코드 어딘가에서 아마 부호가 뒤바뀌는 문제가 생깁니다. 영벡터는 normalized로 방향을 정의할수가 없어요.
        // 아마 0에 엄청 가까운 단위벡터같은게 대신 값으로 들어가서, 그게 부호가 바뀌는 원흉이 되어서 카메라가 180도 뒤바뀌어버리는 문제였던것 같습니다.
        // 초기 버전은 이 else문이 없었는데, 정사영을 실시간으로 계속 초기화시켜주는 코드를 추가했습니다.
        // 20260207 수정본 
        else
        {
            // 이전 카메라 forward를 중력 평면에 투영해서 기준으로 사용
            Vector3 prevTangentFwd = Vector3.ProjectOnPlane(transform.forward, gravityUp);
            if (prevTangentFwd.sqrMagnitude > 1e-6f)
                baseForwardRef = prevTangentFwd.normalized;
        }

        // 실시간으로 baseFowardRef를 갱신하고 있는데, 여기에 누적값을 쓰면 카메라가 빙글빙글 계속 자동으로 돕니다.
        // 변화량을 쓰면 안전합니다.
        float deltaX = currentX - lastX;
        lastX = currentX;

        // gravityUp을 기준으로 currentX 만큼 회전하는 쿼터니언 생성
        Quaternion yawRot = Quaternion.AngleAxis(deltaX, gravityUp);

        // 기준 forward 벡터를 gravityUp에 정사영하여 평면상 벡터로 변환
        Vector3 baseForward = Vector3.ProjectOnPlane(baseForwardRef, gravityUp).normalized;

        // 정사영 결과가 너무 작은 경우 예비값 사용
        if (baseForward.sqrMagnitude < 0.01f)
            baseForward = Vector3.ProjectOnPlane(transform.forward, gravityUp).normalized;
        if (baseForward.sqrMagnitude < 0.01f)
            baseForward = Vector3.ProjectOnPlane(Vector3.forward, gravityUp).normalized;

        // right, forward 벡터를 외적을 연산해 수직인 벡터를 구함
        Vector3 right = Vector3.Cross(gravityUp, baseForward).normalized;
        Vector3 forward = Vector3.Cross(right, gravityUp).normalized;

        Vector3 offset = yawRot * (-forward * distance);

        //  Y축 연산

        // pitch 회전축 = yaw 적용 후의 right 축
        Vector3 pitchAxis = yawRot * right;
        pitchAxis.Normalize();

        // pitch 회전 쿼터니언
        Quaternion pitchRot = Quaternion.AngleAxis(currentY, pitchAxis);

        // offset 에 pitch 적용
        offset = pitchRot * offset;

        //  카메라 위치 및 회전 적용
        transform.position = cameraOffset.position + offset;

        Vector3 newForward = (cameraOffset.position - transform.position).normalized;
        Vector3 newRight = Vector3.Cross(gravityUp, newForward).normalized;
        Vector3 newUp = Vector3.Cross(newRight, newForward).normalized;

        transform.rotation = Quaternion.LookRotation(newForward, -newUp);

    }
}