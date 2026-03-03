using UnityEngine;

// 행성의 만유인력을 처리하는 클래스
public class PlanetGravity : MonoBehaviour
{
    private float gravityMag = -10f;

    // 플레이어의 만유인력 로직
    public void Attract(MovingObject body)
    {
        // 1. 만유인력
        Vector3 gravityUp = (body.transform.position - transform.position).normalized;

        body.rb.AddForce(gravityUp * gravityMag);

        if (body.CompareTag(Define.Tag.PLAYER))
        {
            // 2. 지면에 항상 꼿꼿히 서게 함
            Quaternion surfaceRotation = Quaternion.FromToRotation(body.transform.up, body.groundDir) * body.transform.rotation;

            body.rb.MoveRotation(surfaceRotation);
        }

        else if (body.CompareTag(Define.Tag.ITEM))
        {

        }
    }

    public Vector3 GetGravityUp(Transform t)
    {
        return (t.position - transform.position).normalized;
    }
}