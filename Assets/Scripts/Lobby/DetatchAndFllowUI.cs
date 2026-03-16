using UnityEngine;

public class DetachAndFollowUI : MonoBehaviour
{
    [Header("따라다닐 대상")]
    public Transform target; // 따라다닐 대상의 트랜스폼

    [Header("위치 조정")]
    public Vector3 offset = new Vector3(0, 0, 0); // Target의 중심으로부터의 거리

    void Start()
    {
        // 스폰되는 순간 부모(LobbyAstronut)로부터 분리.
        transform.SetParent(null);
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // 회전은 무시하고 위치만 따라다님
            transform.position = target.position + offset;
        }
        else
        {
            // LobbyAstronut이 사라지면(ex 게임시작) 자동으로 사라짐.
            Destroy(gameObject);
        }
    }
}