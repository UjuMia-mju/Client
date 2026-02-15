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
            // 또한 현재 접지중인 지면의 벡터로 쿼터니언을 생성해 극점에 다다르지 못하게 함
            // 여기서 groundDir이 필요합니다.
            // 원래 디코에 압축파일로 올렸던 코드에는 여기에 gravityUp을 넣었는데, 털뭉치 이론에 의해 극점에서 불안정해지는 현상이 발생했습니다.
            // 따라서 플레이어가 실제로 접지하고 있는 지면의 법선벡터로 쿼터니언을 생성했습니다.
            Quaternion surfaceRotation = Quaternion.FromToRotation(body.transform.up, body.groundDir) * body.transform.rotation;

            body.rb.MoveRotation(surfaceRotation);
        }

        else if(body.CompareTag(Define.Tag.ITEM))
        {

        }
    }

    public Vector3 GetGravityUp(Transform t)
    {
        return (t.position - transform.position).normalized;
    }
}