using UnityEngine;
using System.Collections.Generic;


public class SpaceshipAssembly : MonoBehaviour
{
    [SerializeField]
    private List<Items> targetPrefabLists; // 조립에 필요한 타겟 프리팹 리스트

    private int currentTargetIndex = 0; // 현재 조립 중인 타겟 인덱스, 게임 도중 다른 숫자로 바뀌거나 하지 않고 순차적으로만 늘어남.

    // data는 현재 우주선에 넣으려는 아이템의 게임 오브젝트
    // 호스트 전용: 직접 판정 (피어 요청은 PlayManager.OnPeerSpaceshipInsert에서 호출)
    public void AddTargetItems(GameObject data)
    {
        if (!data.CompareTag(Define.Tag.ITEM))
        {
            Debug.Log("해당 객체가 아이템이 아니라서 우주선에 넣을 수 없습니다.");
            return;
        }

        Items item = data.GetComponent<Items>();

        if (item == null)
        {
            Debug.Log("Items 컴포넌트가 없습니다.");
            return;
        }

        if (targetPrefabLists[currentTargetIndex].itemStringKey == item.itemStringKey)
        {
            Debug.Log((currentTargetIndex + 1) + "번째 미션 클리어");
            Destroy(data);
            currentTargetIndex++;

            // 피어들에게 현재 인덱스 동기화 (씬 단독 실행 시 ConnectManager 없을 수 있으므로 null 체크)
            if (ConnectManager.Instance != null)
                PacketSender.Instance.BroadcastSpaceshipUpdate(currentTargetIndex);

            if (currentTargetIndex >= targetPrefabLists.Count)
            {
                Debug.Log("모든 부품을 모았습니다! 우주선 완성!");
                CompleteAssembly();
            }
        }
        else
        {
            Debug.Log("대상 아이템이 아닙니다.");
        }
    }

    // TODO : 우주선 완성 시의 연출이나 다음 단계로 넘어가는 로직을 추가할 수 있습니다. UI 담당과의 협업이 필요합니다.
    public void CompleteAssembly()
    {
        Debug.Log("우주선 조립이 완료되었습니다!");

        // 호스트만 피어들에게 완료 브로드캐스트 (씬 단독 실행 시 ConnectManager 없을 수 있으므로 null 체크)
        if (ConnectManager.Instance != null && ConnectManager.Instance.isHost)
            PacketSender.Instance.BroadcastSpaceshipComplete(true);

        GameRuleManager.Instance.ReturnToStageSelectScene(true);
    }

    // 피어 전용: 호스트로부터 받은 인덱스 동기화
    public void SyncIndex(int index)
    {
        currentTargetIndex = index;
        Debug.Log($"{currentTargetIndex}번째 미션 클리어");
    }
}