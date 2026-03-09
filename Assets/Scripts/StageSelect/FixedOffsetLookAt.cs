using UnityEngine;

public class FixedOffsetLookAt : MonoBehaviour
{
    public Transform target; // 행성(부모) 객체
    // 설정하신 좌표 기준 오프셋: Vector3(13.33, -1.97, -1.09) - Vector3(4, 0, 0)
    public Vector3 offset = new Vector3(9.33f, -1.97f, -1.09f);

    void LateUpdate()
    {
        if (target != null)
        {
            // 1. 위치 결정: 타겟의 위치에 오프셋만 더함 (부모의 회전 영향 안 받음)
            transform.position = target.position + offset;

            // 2. 회전 결정: 항상 카메라 정면을 바라보게 함
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
}