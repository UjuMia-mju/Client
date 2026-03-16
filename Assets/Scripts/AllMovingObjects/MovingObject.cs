using UnityEngine;

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
    protected LayerMask walkable;

    public Vector3 groundDir {get; protected set; }

    private const float RAY_LENGTH = 0.6f;
    private const float VELOCITY_HUNDRED = 100f;

    protected bool isGrounded = true;


    // 초기화
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        groundMask = LayerMask.GetMask(Define.Layer.GROUND);
        wallMask = LayerMask.GetMask(Define.Layer.WALL);
        walkable = LayerMask.GetMask(Define.Layer.WALKABLE_COLLIDER);
    }

    // 이동 처리
    protected virtual void Moving(Vector3 movDir)
    {
        if (movDir == Vector3.zero)
        {
            return;
        }

        movDir.Normalize();
        rb.MovePosition(rb.position + movDir * walkSpeed * Time.fixedDeltaTime);
    }

    // 점프 처리
    protected virtual void Jump()
    {
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    // 캐릭터가 바라보는 방향을 트랜스폼 t 기준으로 입력이 있을 때만 한번 회전시킴
    protected virtual void RotateToDirection(Vector3 movDir)
    {
        if (movDir == Vector3.zero)
        {
            return;
        }
        else
        {
            movDir.Normalize();
            Quaternion targetRot = Quaternion.LookRotation(movDir, this.transform.position);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed*Time.fixedDeltaTime));
        }
    }

    // 벽 충돌을 레이캐스트로 감지
    protected bool CollisionDetectWithRaycast(Vector3 dirData, LayerMask maskData)
    {
        if (dirData.sqrMagnitude < 0.01f)
            return false;


        Ray ray = new Ray(this.transform.position, dirData.normalized);
        RaycastHit hit;

        Debug.DrawLine(ray.origin, ray.origin + dirData.normalized * (2.1f), Color.red);

        if (Physics.Raycast(ray, out hit, RAY_LENGTH, maskData))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // 땅에 닿았는지 레이캐스트로 감지
    protected void GroundDetectingWithRaycast(LayerMask maskData)
    {
        // 행성 방향으로 레이캐스트 발사 - 레이캐스트
        Vector3 origin = transform.position + transform.up * 0.5f;
        Ray ray = new Ray(origin, -transform.up);

        RaycastHit hit;

        Debug.DrawLine(ray.origin, ray.origin + ray.direction * (RAY_LENGTH), Color.red);

        // 발이 땅에 닿았을 때를 감지
        if (Physics.Raycast(ray, out hit, RAY_LENGTH, maskData))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    // 땅에 닿았는지와는 무관하게 땅의 법선 벡터를 구함
    // 이 함수가 필요한 이유는 이 함수 참조중인 PlanetGravity 클래스에서 땅의 법선 벡터를 필요로 하기 때문입니다.
    // 해당 참조로 이동해 확인바랍니다.
    protected void GetGroundNormal(LayerMask maskData)
    {
        Vector3 origin = transform.position + transform.up * 0.5f;
        Ray ray = new Ray(origin, -transform.up);

        RaycastHit hit;

        // 계속 땅을 감지해 법선 벡터를 수집
        if (Physics.Raycast(ray, out hit, 10f, maskData))
        {
            groundDir = hit.normal;
        }
    }

    protected float GetMovingAmount()
    {
        return rb.linearVelocity.magnitude * VELOCITY_HUNDRED;
    }

}