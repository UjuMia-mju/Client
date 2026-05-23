using UnityEngine;

public class ObjectsGravityController : MonoBehaviour
{
    // 중력의 주체
    public PlanetGravity planet;

    private Items objects;

    // 중력 방향의 반대 벡터
    public Vector3 gravityUp { get; private set; }

    private void Start()
    {
        if (planet == null)
        {
            planet = FindFirstObjectByType<PlanetGravity>();
        }
        objects = GetComponent<Items>();
        objects.rb.useGravity = false;  // 리지드바디의 기본 중력은 필요 없으므로 비활성화
        planet.Attract(objects);
    }

    // 중력을 적용
    private void FixedUpdate()
    {
        gravityUp = planet.GetGravityUp(objects.gameObject.transform);
        objects.rb.useGravity = false;

        bool grounded = IsGrounded();
        float linVel = objects.rb.linearVelocity.sqrMagnitude;
        float angVel = objects.rb.angularVelocity.sqrMagnitude;

        const float STOP_LIN_SQR = 0.5f * 0.5f;   // ≤ 0.5 m/s면정지
        const float STOP_ANG_SQR = 1.0f * 1.0f;
        if (grounded && linVel < STOP_LIN_SQR && angVel < STOP_ANG_SQR)
        {
            objects.rb.linearVelocity = Vector3.zero;
            objects.rb.angularVelocity = Vector3.zero;
            // return 제거 → Attract()는 계속 호출해서 지면에 붙어있게 함
        }

        planet.Attract(objects);
    }

    private bool IsGrounded()
    {
        Vector3 gravityDir = (planet.transform.position - objects.transform.position).normalized;
        LayerMask groundMask = LayerMask.GetMask(Define.Layer.GROUND, Define.Layer.WALKABLE_COLLIDER);
        
        bool hit = Physics.Raycast(objects.transform.position, gravityDir, 2.0f, groundMask);
        Debug.DrawLine(objects.transform.position, objects.transform.position + gravityDir * 2.0f, hit ? Color.green : Color.red);
        return hit;
    }
}
