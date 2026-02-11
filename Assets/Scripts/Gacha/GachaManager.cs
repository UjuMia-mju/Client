using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GachaManager : MonoBehaviour
{
    // 초기 전체 아이템 목록 (인스펙터에서 할당)
    public List<GachaItem> allItems;

    // 현재 뽑을 수 있는 남은 아이템 목록
    private List<GachaItem> currentPool;

    // 획득한 아이템 목록
    private List<GachaItem> obtainedItems;

    [Header("UI Connection")]
    public GachaSpinnerUI spinnerUI; // 인스펙터에서 연결

    void Start()
    {
        // 게임 시작 시 초기화(테스트용으로 자동 리셋)
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
            obtainedItems.Add(selectedItem); //<- 애니메이션 끝난 후로 미뤄도 됨

            Debug.Log($"결과 결정됨: {selectedItem.itemName}. 애니메이션 시작...");
            
            // UI 스피너 돌리기 시작
            spinnerUI.StartSpinAnimation(selectedItem);
        }
    }

    // 확률 정보
    // 디버그용으로 콘솔에 출력되게 구현해뒀음.
    public void DisplayProbabilities()
    {
        // 1. 예외처리 - 남은 아이템이 없으면 계산 불가
        if (currentPool.Count == 0 || currentPool == null)
        {
            Debug.Log("남은 아이템이 없습니다.");
            return;
        }

        // 2. 전체 가중치 합 계산 (남은 아이템들만)
        float totalWeight = 0;
        foreach (var item in currentPool)
        {
            totalWeight += item.weight;
        }

        // 3. 등급별 가중치 집계
        Dictionary<ItemRarity, int> rarityWeightMap = new Dictionary<ItemRarity, int>();
        foreach (var item in currentPool)
        {
            if (rarityWeightMap.ContainsKey(item.rarity))
            {
                rarityWeightMap[item.rarity] += item.weight;
            }
            else
            {
                rarityWeightMap.Add(item.rarity, item.weight);
            }
        }

        // 4. 로그 메시지 생성하기
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<color=yellow>=== 현재 뽑기 확률표 (남은 아이템 기준) ===</color>");

        foreach(var pair in rarityWeightMap)
        {
            // 확률 공식: (해당 등급 가중치 합 / 전체 가중치) *100
            float probability = (pair.Value / totalWeight) * 100f;


            sb.AppendLine($"- <b>{pair.Key}</b>: {probability:F2}% (가중치: {pair.Value}/{totalWeight})");
        }
        Debug.Log(sb.ToString());
    }
}