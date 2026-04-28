using UnityEngine;
using System.Collections.Generic;
using Protocol;

[System.Serializable]
public class SpaceshipMission
{
    public int currentCount; // 현재 조립된 아이템 수
    public int targetCount; // 조립에 필요한 아이템 수
    public Items targetItem;    // 타겟 아이템
}

public class SpaceshipAssembly : MonoBehaviour
{
    [SerializeField]
    private List<SpaceshipMission> targetMission; // 목표 미션
    public IReadOnlyList<SpaceshipMission> TargetMission => targetMission;

    public IReadOnlyList<SpaceshipMission> TargetMission => targetMission;

    // data는 현재 우주선에 넣으려는 아이템의 게임 오브젝트
    // 호스트 전용: 직접 판정 (피어 요청은 PlayManager.OnPeerSpaceshipInsert에서 호출)
    // 반환값: 아이템 삽입 성공 여부
    public bool AddTargetItems(GameObject data)
    {
        if (!data.CompareTag(Define.Tag.ITEM))
        {
            Debug.Log("해당 객체가 아이템이 아니라서 우주선에 넣을 수 없습니다.");
            return false;
        }

        Items item = data.GetComponent<Items>();

        if (item == null)
        {
            Debug.Log("Items 컴포넌트가 없습니다.");
            return false;
        }

        SpaceshipMission mission = targetMission.Find(
            m => m.targetItem.itemStringKey == item.itemStringKey && m.currentCount < m.targetCount
        );

        if (mission == null)
        {
            Debug.Log("대상 아이템이 아닙니다.");
            return false;
        }

        if (ConnectManager.Instance != null)
            PacketSender.Instance.SendObjectDestroy(item.itemId);
        Destroy(data);

        mission.currentCount++;
        Debug.Log($"[{mission.targetItem.itemStringKey}] {mission.currentCount}/{mission.targetCount} 투입 완료");

        if (ConnectManager.Instance != null)
            PacketSender.Instance.BroadcastSpaceshipUpdate(mission.targetItem.itemStringKey, mission.currentCount);

        if (targetMission.TrueForAll(m => m.currentCount >= m.targetCount))
        {
            Debug.Log("모든 부품을 모았습니다! 우주선 완성!");
            CompleteAssembly();
        }

        return true;
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

    // 피어 전용: 호스트로부터 받은 미션 카운트 동기화
    public void SyncMission(string itemStringKey, int currentCount)
    {
        SpaceshipMission mission = targetMission.Find(m => m.targetItem.itemStringKey == itemStringKey);
        if (mission == null)
        {
            Debug.LogWarning($"[SpaceshipAssembly] SyncMission: {itemStringKey}에 해당하는 미션을 찾을 수 없습니다.");
            return;
        }

        mission.currentCount = currentCount;
        Debug.Log($"[{itemStringKey}] 동기화: {currentCount}/{mission.targetCount}");
    }
}