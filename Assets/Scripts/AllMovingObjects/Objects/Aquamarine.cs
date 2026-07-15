using UnityEngine;

public class Aquamarine : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private int damage = 1;


    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // 최종 트리거 판단
        // 트리거가 false인 경우 그냥 리턴
        if (!isTriggered)
        {
            return;
        }

        // 던져서 부딫힌게 행성인 경우 트리거 비활성화후 리턴
        else if (isTriggered)
        {
            if (other.CompareTag(Define.Tag.PLANET))
            {
                isTriggered = false;
                return;
            }
        }

        // 호스트 단독 판정 (피어 측 trigger는 무시)
        if (ConnectManager.Instance == null || !ConnectManager.Instance.isHost) return;

        // 몬스터 판정
        if (other.CompareTag(Define.Tag.MONSTER))
        {
            //
            Debug.Log("Aquamarine : 몬스터랑 충돌");

            Monster victim = other.GetComponentInParent<Monster>();
            if (victim == null) return;

            //
            Debug.Log("Aquamarine : 검사 문제없고 데미지 줌");

            // TODO : 현재 데미지는 BROADCAST되지 않음. 이를 해결해야 함. 일단 로컬에서는 작동함.
            victim.TakeDamage(damage);
        }

    }

    public void SetActiveAquaTrigger()
    {
        isTriggered = true;
    }
}
