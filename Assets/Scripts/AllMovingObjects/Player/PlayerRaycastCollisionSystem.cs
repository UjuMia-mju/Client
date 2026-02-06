using UnityEngine;

// 본 프로젝트는 유니티에서 제공하는 물리엔진으로는 구현하기 힘든 사항들이 몇가지 존재합니다.
// 특히 충돌 부분은 단순 리지드바디를 사용하기엔 너무 불안정하므로, 레이캐스트로 레이어마스크 감지 후 직접 만든 로직으로 후처리합니다.
public class PlayerRaycastCollisionSystem : MonoBehaviour
{
    // 레이 길이
    private const float RAY_LENGTH = 2.1f;

    // 현재 땅에 닿은 상태인지 검사하는 플래그
    private bool isGrounded = true;

    // 현재 밟고 있는 땅의 법선 벡터
    public Vector3 groundDir { get; private set; }

    // 벽 충돌을 레이캐스트로 감지
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

    // 땅에 닿았는지 레이캐스트로 감지
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

    // 땅에 닿았는지와는 무관하게 땅의 법선 벡터를 구함
    // 이 함수가 필요한 이유는 이 함수 참조중인 PlanetGravity 클래스에서 땅의 법선 벡터를 필요로 하기 때문입니다.
    // 해당 참조로 이동해 확인바랍니다.
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

    // 플래그 리턴
    public bool GetIsGrounded()
    {
        return isGrounded;
    }   
}