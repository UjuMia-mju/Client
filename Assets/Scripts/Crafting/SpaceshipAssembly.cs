using UnityEngine;
using System.Collections.Generic;


public class SpaceshipAssembly : MonoBehaviour
{
    [SerializeField]
    private List<Items> targetPrefabLists; // 조립에 필요한 타겟 프리팹 리스트

    private int currentTargetIndex = 0; // 현재 조립 중인 타겟 인덱스, 게임 도중 다른 숫자로 바뀌거나 하지 않고 순차적으로만 늘어남.

    // data는 현재 우주선에 넣으려는 아이템의 게임 오브젝트
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

        // 이전 PR에서도 말씀드렸지만 씬 단독 실행은 개발 기간동안은 적어도 가능해야 합니다.
        // 또 게임 클리어 시 클리어 씬을 따로 만들건지, 팝업으로 표시가 되는지 아직 알 수가 없어, 일단 StageSelect로 돌아가게 하고 GameRuleManager의 플래그만 바꾸게 하겠습니다.

        GameRuleManager.Instance.ReturnToStageSelectScene(true);
    }
}