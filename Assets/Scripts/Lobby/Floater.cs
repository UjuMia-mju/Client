using UnityEngine;

public class Floater : MonoBehaviour
{
    private Rigidbody rb;
    
    [SerializeField]
    private bool bTorque = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // 1. 랜덤한 방향으로 툭 쳐주기 (초기 이동 속도)
        Vector3 randomDirection = Random.onUnitSphere;
        randomDirection.z = 0; // 화면 앞뒤로 깊게 들어가는 걸 방지
        
        float speed = Random.Range(2f, 5f);
        rb.AddForce(randomDirection.normalized * speed, ForceMode.VelocityChange);

        // 2. 빙글빙글 도는 회전력 주기
        if (bTorque)
        {
            Vector3 randomTorque = Random.onUnitSphere;
            rb.AddTorque(randomTorque * Random.Range(0.5f, 2f), ForceMode.VelocityChange);
        }
        
    }
}