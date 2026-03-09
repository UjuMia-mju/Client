using UnityEngine;

public class OrbitCenterController : MonoBehaviour
{
    public Vector3 spinAxis = Vector3.up; 
    public float spinSpeed = 10f;         

    private void Update()
    {
        // 조건문 싹 제거! 무슨 일이 있어도 영원히 돕니다.
        transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
    }
}