using UnityEngine;

public class PlayerRaycastCollisionSystem : MonoBehaviour
{
    private const float RAY_LENGTH = 2.1f;

    private bool isGrounded = true;

    public Vector3 groundDir { get; private set; }

    public bool CollisionDetectWithRaycast(Vector3 dirData, LayerMask wallMask)
    {
        if (dirData.sqrMagnitude < 0.01f)
            return false;

        Vector3 rayTargetDir = transform.TransformDirection(dirData);

        Ray ray = new Ray(this.transform.position, rayTargetDir);
        RaycastHit hit;


        Debug.DrawLine(ray.origin, ray.origin + ray.direction * (RAY_LENGTH), Color.blue);

        if (Physics.Raycast(ray, out hit, RAY_LENGTH, wallMask))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void GroundDetectingWithRaycast(LayerMask groundMask)
    {
        // 행성 방향으로 레이캐스트 발사 - 레이캐스트
        Vector3 origin = transform.position + transform.up * 0.5f;
        Ray ray = new Ray(origin, -transform.up);

        RaycastHit hit;

        Debug.DrawLine(ray.origin, ray.origin + ray.direction * (1.1f), Color.red);

        // 발이 땅에 닿았을 때를 감지
        if (Physics.Raycast(ray, out hit, 1.1f, groundMask))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    public void GetGroundNormal(LayerMask groundMask)
    {
        Vector3 origin = transform.position + transform.up * 0.5f;
        Ray ray = new Ray(origin, -transform.up);

        RaycastHit hit;

        // 계속 땅을 감지해 법선 벡터를 수집
        if (Physics.Raycast(ray, out hit, 10f, groundMask))
        {
            groundDir = hit.normal;
        }
    }

    public bool GetIsGrounded()
    {
        return isGrounded;
    }   
}