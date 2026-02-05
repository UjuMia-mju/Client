using UnityEngine;

public class PlayerRaycastCollisionSystem : MonoBehaviour
{
    private const float RAY_LENGTH = 2.1f;

    private bool isGrounded = true;

    public bool CollisionDetectWithRaycast(Vector3 dirData, LayerMask wallMask)
    {
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

    public bool GetIsGrounded()
    {
        return isGrounded;
    }   
}