using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // 씬에 있는 메인 카메라를 찾아옵니다
        mainCamera = Camera.main;
    }

    // Update가 아니라 LateUpdate를 쓰는 이유는, 캐릭터의 위치나 애니메이션이 Update에서 먼저 처리된 후에 카메라 방향을 맞춰야 하기 때문입니다
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.forward = mainCamera.transform.forward;
        }
    }
}