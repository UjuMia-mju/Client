using UnityEngine;

public class Aquamarine : MonoBehaviour
{
    private const string SOCKET = "Socket";


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
            if (other.CompareTag("Planet"))
            {
                isTriggered = false;
                return;
            }
        }

        // 호스트 단독 판정 (피어 측 trigger는 무시)
        if (ConnectManager.Instance == null || !ConnectManager.Instance.isHost) return;



    }

    public void SetActiveAquaTriggerByAnimEvent()
    {
        isTriggered = true;
    }
}
