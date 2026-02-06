using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class PlayerTPCamera : MonoBehaviour
{
    private const float Y_ANGLE_MIN = 0.0f;
    private const float Y_ANGLE_MAX = 50.0f;

    public Transform playerMesh;

    public float distance = 5.0f;

    private float currentX = 0.0f;
    private float currentY = 45.0f;

    // 마우스 민감도, 추후 설정에 민감도 옵션 추가 시 이 상수는 제거하거나 리팩토링해야 합니다. 상의 필요합니다.
    private const float SENSITIVITY = 0.2f;

    private GameObject player;
    private PlayerGravityController pgr;

    private PlayerInputSystem inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputSystem();
        inputActions.Player.Enable();
    }


    private void Start()
    {
        player = transform.parent.gameObject;
        pgr = FindFirstObjectByType<PlayerGravityController>();
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

    }
}


// 인핸스드 인풋을 쓰지 않는 구 버전 코드
// TODO : 인풋 구조 어떤것 사용할지 상의가 필요합니다.
//public class PlayerTPCamera : MonoBehaviour
//{
//    private const float Y_ANGLE_MIN = 0.0f;
//    private const float Y_ANGLE_MAX = 50.0f;

//    public Transform playerMesh;

//    public float distance = 5.0f;

//    private float currentX = 0.0f;
//    private float currentY = 45.0f;


//    private GameObject player;
//    private PlayerGravityController pgr;

//    private void Start()
//    {
//        player = transform.parent.gameObject;
//        pgr = FindFirstObjectByType<PlayerGravityController>();

//    }

//    private void Update()
//    {
//        currentX += Input.GetAxis("Mouse X");
//        currentY += -Input.GetAxis("Mouse Y");

//        currentY = Mathf.Clamp(currentY, Y_ANGLE_MIN, Y_ANGLE_MAX);
//    }

//    private void LateUpdate()
//    {
//        // 행성 중심을 향한 중력 방향 계산
//        Vector3 gravityUp = (playerMesh.position - pgr.planet.transform.position).normalized;

//        //// Vector3.up 벡터를 gravityUp으로 회전시키는데 필요한 쿼터니언을 생성
//        Quaternion gravityRotation = Quaternion.FromToRotation(Vector3.up, gravityUp);

//        //// 마우스 X 입력을 gravityUp 축으로 하는 회전 쿼터니언 생성
//        Quaternion yaw = Quaternion.AngleAxis(currentX, gravityUp);

//        //// 마우스 Y 입력을 gravityUp에 수직인 축(오른쪽 방향)으로 하는 회전 쿼터니언 생성
//        Vector3 right = yaw * gravityRotation * Vector3.right;

//        //// 위에서 생성한 right 벡터를 회전축으로 삼고, currentY(마우스 Y 입력)만큼 회전시키는 쿼터니언을 생성
//        Quaternion pitch = Quaternion.AngleAxis(currentY, right);

//        //// 최종 카메라 회전 쿼터니언 계산
//        Quaternion finalRotation = pitch * yaw * gravityRotation;

//        //// 카메라 위치 설정 및 회전
//        Vector3 dir = finalRotation * Vector3.back * distance;
//        this.transform.position = playerMesh.position + dir;
//        this.transform.rotation = Quaternion.LookRotation(-dir, gravityUp);
//    }
//}
