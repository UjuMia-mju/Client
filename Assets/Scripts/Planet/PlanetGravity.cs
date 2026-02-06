using UnityEditor.Overlays;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlanetGravity : MonoBehaviour
{
    private float gravityMag = -10f;

    // 플레이어의 만유인력 로직
    public Vector3 AttractPlayer(Player body)
    {
        // 1. 만유인력
        Vector3 gravityUp = (body.transform.position - transform.position).normalized;

        body.rb.AddForce(gravityUp * gravityMag);

        // 2. 지면에 항상 꼿꼿히 서게 함
        // 또한 현재 접지중인 지면의 벡터로 쿼터니언을 생성해 극점에 다다르지 못하게 함
        Quaternion surfaceRotation = Quaternion.FromToRotation(body.transform.up, body.playerRaycastCollisionControl.groundDir) * body.transform.rotation;

        body.rb.MoveRotation(surfaceRotation);

        return gravityUp;
    }
}