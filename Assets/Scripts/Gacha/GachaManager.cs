using System.Collections.Generic;
using UnityEngine;

public class GachaManager : MonoBehaviour
{
    // 초기 전체 아이템 목록 (인스펙터에서 할당)
    public List<GachaItem> allItems;

    // 현재 뽑을 수 있는 남은 아이템 목록
    private List<GachaItem> currentPool;

    // 획득한 아이템 목록
    private List<GachaItem> obtainedItems;

    void Start()
    {
        // 게임 시작 시 초기화
        ResetGacha();
    }

    // 가챠 시스템 초기화 (리셋)
    public void ResetGacha()
    {
        // 원본 리스트를 복사해서 현재 풀을 만듦 (원본 보호)
        currentPool = new List<GachaItem>(allItems);
        obtainedItems = new List<GachaItem>();
        Debug.Log("가챠 시스템이 초기화되었습니다.");
    }

    // 아이템 뽑기 함수
    public void PullItem()
    {
        if (currentPool.Count == 0)
        {
            Debug.LogWarning("더 이상 뽑을 아이템이 없습니다!");
            return;
        }

        // 1. 남은 아이템들의 가중치 총합 계산
        int totalWeight = 0;
        foreach (var item in currentPool)
        {
            totalWeight += item.weight;
        }

        // 2. 랜덤 값 생성 (0 ~ 총 가중치)
        int randomValue = Random.Range(0, totalWeight);

        // 3. 가중치 기반 아이템 선택
        GachaItem selectedItem = null;
        int currentWeightSum = 0;

        for (int i = 0; i < currentPool.Count; i++)
        {
            currentWeightSum += currentPool[i].weight;
            
            // 랜덤 값이 현재 구간에 해당하면 당첨
            if (randomValue < currentWeightSum)
            {
                selectedItem = currentPool[i];
                
                // 중복 방지를 위해 풀에서 제거
                currentPool.RemoveAt(i);
                break;
            }
        }

        // 4. 결과 처리
        if (selectedItem != null)
        {
            obtainedItems.Add(selectedItem);
            Debug.Log($"획득: {selectedItem.itemName} (등급: {selectedItem.rarity})");
            
        }
    }
}