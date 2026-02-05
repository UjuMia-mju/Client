using UnityEngine;

public class PlanetGravity : MonoBehaviour
{
    private float gravityMag = -10;

    public void AttractPlayer(Transform body)
    {
        Rigidbody rb = body.GetComponent<Rigidbody>();

        // 만유인력을 받길 원하는 오브젝트(이하 “바디”라 칭함) 의 위치 – 행성 위치 = 행성으로부터 위를 향하는 벡터가 됨
        Vector3 gravityUp = (body.position - transform.position).normalized;
        // 바디의 법선을 위에서 계산한 벡터로 전환하고, 쿼터니언 좌표계에 저장
        Quaternion gravityRotation = Quaternion.FromToRotation(body.up, gravityUp) * body.rotation;

        // 카메라가 바라보는 바로 앞 방향 벡터를 gravituUp에 정사영하고 camForward에 저장한다.
        Vector3 camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, gravityUp).normalized;

        if (camForward.sqrMagnitude < 0.0001f)
        {
            // 정사영 값이 너무 적을 경우 생략하고 중력 정렬만 적용
            rb.MoveRotation(gravityRotation);
            rb.AddForce(gravityUp * gravityMag);
            return;
        }

        // 플레이어가 바라보는 앞 방향을 gravityUp에 정사영하고 playerFoward에 저장한다.
        Vector3 playerForward = Vector3.ProjectOnPlane(body.forward, gravityUp).normalized;

        // playerFoward와 CamFoward 사이의 각도를 gravituUp을 축으로 두 벡터의 각을 내적으로 구한 뒤 그 각도의 방향은 외적과 기준축으로 판단해 angle에 저장한다
        float angle = Vector3.SignedAngle(playerForward, camForward, gravityUp);

        // angle만큼 graivtyUp을 축으로 쿼터니언을 만들어 yaw에 저장한다
        Quaternion yaw = Quaternion.AngleAxis(angle, gravityUp);

        // yaw와 gravityRotation을 합성한 값을 MoveRotation으로 계산시킨다
        rb.MoveRotation(yaw * gravityRotation);

        // gravityUp 방향으로 gravityMag만큼을 스칼라곱하여 중력을 생성(AddForce)
        rb.AddForce(gravityUp * gravityMag);
    }
}