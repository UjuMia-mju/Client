using UnityEngine;

public class Aquamarine : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private int damage = 1;


    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Aquamarine OnTriggerEnter - isTriggered:{isTriggered}, parent:{transform.root.parent}");

        // 최종 트리거 판단
        // 트리거가 false인 경우 그냥 리턴
        if (!isTriggered || transform.root.parent != null)
        {
            Debug.Log($"[Aquamarine] BLOCKED - isTriggered:{isTriggered}, root.parent:{transform.root.parent}");
            return;
        }

        // 던져서 부딫힌게 행성인 경우 트리거 비활성화후 리턴
        else if (isTriggered)
        {
            // 행성에 떨어지면 트리거 비활성화인데 조금 조악함.
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

            KingSlime kingSlime = victim.GetComponent<KingSlime>();
            if (kingSlime != null)
            {
                // KingSlime 전용 로직 실행
                kingSlime.SlimeHit(damage);
            }

            // 동작 완료 후 파괴

            Items itemComp = other.GetComponentInParent<Items>();
            if (itemComp != null)
            {
                PacketSender.Instance.SendObjectDestroy(itemComp.itemId);
            }
            Destroy(transform.parent.gameObject);
        }

    }

    public void SetActiveAquaTrigger()
    {
        isTriggered = true;
    }
}