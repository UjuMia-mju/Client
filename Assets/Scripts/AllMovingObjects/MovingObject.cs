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

    public Vector3 groundDir { get; protected set; }

    private const float RAY_LENGTH = 0.6f;
    private const float VELOCITY_HUNDRED = 100f;

    protected bool isGrounded = true;

    // ============ Gizmos 디버그 캐시 ============
    [Header("Debug Gizmos")]
    [SerializeField] private bool drawDebugGizmos = true;

    // 충돌 레이 (수평 방향)
    private Vector3 _gz_collisionOrigin;
    private Vector3 _gz_collisionDir;
    private bool _gz_collisionHit;
    private bool _gz_hasCollisionRay;

    // 지면 레이 (아래 방향)
    private Vector3 _gz_groundOrigin;
    private Vector3 _gz_groundDir;
    private bool _gz_groundHit;
    private Vector3 _gz_groundHitPoint;
    private Vector3 _gz_groundHitNormal;

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
        if (movDir == Vector3.zero) return;

        movDir.Normalize();
        rb.MovePosition(rb.position + movDir * walkSpeed * Time.fixedDeltaTime);
    }

    // 점프 처리
    protected virtual void Jump()
    {
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    protected virtual void RotateToDirection(Vector3 movDir)
    {
        if (movDir == Vector3.zero) return;

        movDir.Normalize();
        Quaternion targetRot = Quaternion.LookRotation(movDir, this.transform.position);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
    }

    // 여러 LayerMask를 받을 수 있게 수정
    protected bool CollisionDetectWithRaycast(Vector3 dirData, params LayerMask[] masks)
    {
        if (dirData.sqrMagnitude < 0.01f)
        {
            _gz_hasCollisionRay = false;
            return false;
        }

        Vector3 dir = dirData.normalized;
        _gz_hasCollisionRay = true;
        _gz_collisionDir = dir;
        _gz_collisionHit = false;

        // 0.5m: 기존 origin (walkable 옆면 false positive 회피용 안전 높이)
        // 0.9m: 가슴 높이 추가 (높은 벽/턱 보강). walkable은 이 높이에서도 거의 안 겹침.
        // 더 낮은 높이는 walkable 옆면(밟고 있는 발판)에 걸려서 inputFreeze가 영구로 켜지므로 사용 금지.
        float[] heights = { 0.5f, 0.9f };

        for (int h = 0; h < heights.Length; h++)
        {
            Vector3 origin = transform.position + transform.up * heights[h];
            if (h == 0) _gz_collisionOrigin = origin;

            foreach (var mask in masks)
            {
                if (Physics.Raycast(origin, dir, RAY_LENGTH, mask))
                {
                    _gz_collisionHit = true;
                    Debug.DrawLine(origin, origin + dir * RAY_LENGTH, Color.yellow);
                    return true;
                }
            }
            Debug.DrawLine(origin, origin + dir * RAY_LENGTH, Color.gray);
        }

        return false;
    }

    // 땅에 닿았는지 레이캐스트로 감지
    protected void GroundDetectingWithRaycast(LayerMask maskData)
    {
        Vector3 origin = transform.position + transform.up * 0.5f;
        Vector3 dir = -transform.up;
        Ray ray = new Ray(origin, dir);

        // Gizmos용 캐시
        _gz_groundOrigin = origin;
        _gz_groundDir = dir;

        if (Physics.Raycast(ray, out RaycastHit hit, RAY_LENGTH, maskData))
        {
            isGrounded = true;
            _gz_groundHit = true;
            _gz_groundHitPoint = hit.point;
            _gz_groundHitNormal = hit.normal;

            Debug.DrawLine(origin, origin + dir * RAY_LENGTH, Color.green);
        }
        else
        {
            isGrounded = false;
            _gz_groundHit = false;

            Debug.DrawLine(origin, origin + dir * RAY_LENGTH, Color.red);
        }
    }

    // 땅의 법선 벡터를 갱신
    protected void GetGroundNormal(LayerMask maskData)
    {
        Vector3 origin = transform.position + transform.up * 0.5f;
        Ray ray = new Ray(origin, -transform.up);

        if (Physics.Raycast(ray, out RaycastHit hit, 10f, maskData))
        {
            groundDir = hit.normal;
        }
    }

    protected float GetMovingAmount()
    {
        return rb.linearVelocity.magnitude * VELOCITY_HUNDRED;
    }

    // ============ Gizmos: Scene 뷰에 항상 표시 ============
    protected virtual void OnDrawGizmos()
    {
        if (!drawDebugGizmos) return;

        // 충돌 레이 (수평): hit=노랑, miss=회색
        if (_gz_hasCollisionRay)
        {
            Gizmos.color = _gz_collisionHit ? Color.yellow : new Color(0.5f, 0.5f, 0.5f, 0.6f);
            Gizmos.DrawLine(_gz_collisionOrigin, _gz_collisionOrigin + _gz_collisionDir * RAY_LENGTH);
            Gizmos.DrawSphere(_gz_collisionOrigin + _gz_collisionDir * RAY_LENGTH, 0.05f);
        }

        // 지면 레이 (아래): hit=초록, miss=빨강
        if (Application.isPlaying)
        {
            Gizmos.color = _gz_groundHit ? Color.green : Color.red;
            Gizmos.DrawLine(_gz_groundOrigin, _gz_groundOrigin + _gz_groundDir * RAY_LENGTH);

            if (_gz_groundHit)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(_gz_groundHitPoint, 0.07f);
                // 법선
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(_gz_groundHitPoint, _gz_groundHitPoint + _gz_groundHitNormal * 0.5f);
            }
        }
    }
}