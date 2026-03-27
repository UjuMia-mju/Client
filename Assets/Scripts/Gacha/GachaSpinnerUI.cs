using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaSpinnerUI : MonoBehaviour
{
    public event System.Action<GachaItem> OnSpinFinished;

    [Header("UI References")]
    public RectTransform contentReel;   // 움직일 긴 띠 (Content)
    public GameObject itemSlotPrefab;   // 아이템 슬롯 프리팹
    public GachaManager gachaManager;   // 매니저 참조 (전체 아이템 목록 접근용)

    [Header("Animation Settings")]
    public int totalSlots = 50;         // 릴에 보여줄 총 슬롯 개수
    public int winnerIndex = 45;        // 당첨 아이템이 위치할 인덱스
    public float spinDuration = 5f;     // 돌아가는 시간
    public AnimationCurve slowingCurve; // 감속 그래프 (에디터에서 설정)

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private float slotWidth;

    void Start()
    {
        // 슬롯 하나의 너비를 미리 계산 (패딩 포함)
        // HorizontalLayoutGroup의 세팅에 따라 계산 방식이 다를 수 있음.
        // 가장 확실한 건 프리팹의 너비 + Spacing 값.
        HorizontalLayoutGroup layout = contentReel.GetComponent<HorizontalLayoutGroup>();
        slotWidth = itemSlotPrefab.GetComponent<RectTransform>().rect.width + layout.spacing;
        
        // 기본적으로 감속 커브가 없으면 선형으로 설정
        if (slowingCurve.length == 0)
        {
            slowingCurve = AnimationCurve.Linear(0, 0, 1, 1);
        }
    }

    // 외부(GachaManager)에서 이 함수를 호출하여 연출 시작
    public void StartSpinAnimation(GachaItem winningItem)
    {
        StartCoroutine(SpinRoutine(winningItem));
    }

    IEnumerator SpinRoutine(GachaItem winner)
    {
        // 1. 기존 슬롯 초기화
        foreach (var slot in spawnedSlots) Destroy(slot);
        spawnedSlots.Clear();
        // Content 위치 초기화
        contentReel.anchoredPosition = Vector2.zero;

        // 2. 가짜 릴 채우기
        // winnerIndex 위치에만 진짜 당첨 아이템을 넣고, 나머지는 랜덤한 '필러'로 채움
        for (int i = 0; i < totalSlots; i++)
        {
            GameObject newSlot = Instantiate(itemSlotPrefab, contentReel);
            spawnedSlots.Add(newSlot);

            // 아이템 데이터 가져오기
            GachaItem itemData;
            if (i == winnerIndex)
            {
                itemData = winner; // 여기가 진짜 당첨!
            }
            else
            {
                // 나머지는 전체 목록에서 아무거나 가져와서 분위기만 냄
                itemData = gachaManager.allItems[Random.Range(0, gachaManager.allItems.Count)];
            }

            // 슬롯 UI 업데이트 (프리팹 구조에 맞춰 수정 필요)
            // 예: newSlot의 자식 Image에 icon 할당
            newSlot.transform.GetChild(0).GetComponent<Image>().sprite = itemData.icon;
        }
        
        // 레이아웃이 즉시 업데이트되도록 강제
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentReel);

        // 3. 목표 위치 계산
        // 당첨 아이템(winnerIndex)이 뷰포트 중앙에 오도록 계산
        // Content는 왼쪽으로 이동하므로 x값은 음수
        float targetX = -1 * (winnerIndex * slotWidth);

        // 랜덤 오차 범위 계산
        // 슬롯 너비의 절반(0.5)까지 가면 옆 아이템으로 넘어가버리니까,
        // 안전한 범위 내에서 랜덤 값을 뽑습니다.
        float randomOffset = Random.Range(-slotWidth * 0.45f, slotWidth * 0.45f);

        // 최종 목표 위치에 오차 적용
        float finalTargetX = targetX + randomOffset;
        
        // 뷰포트 중앙 보정 (뷰포트 너비의 절반만큼 더해줌)
        float viewportHalfWidth = contentReel.parent.GetComponent<RectTransform>().rect.width / 2f;
        // 슬롯 자체의 중앙 보정 (슬롯 너비의 절반만큼 빼줌)
        float slotHalfWidth = slotWidth / 2f;

        Vector2 endPosition = new Vector2(finalTargetX + viewportHalfWidth - slotHalfWidth, contentReel.anchoredPosition.y);
        Vector2 startPosition = contentReel.anchoredPosition;

        // 4. 회전 애니메이션 (Lerp + Animation Curve)
        float elapsedTime = 0f;
        while (elapsedTime < spinDuration)
        {
            elapsedTime += Time.deltaTime;
            // 0~1 사이 진행률
            float percentage = elapsedTime / spinDuration;
            // 커브를 적용하여 '빠르다가 느려지는' 진행률로 변환
            float curvePercent = slowingCurve.Evaluate(percentage);

            contentReel.anchoredPosition = Vector2.Lerp(startPosition, endPosition, curvePercent);

            yield return null;
        }

        // 5. 최종 위치 보정 및 결과 발표
        contentReel.anchoredPosition = endPosition;
        Debug.Log("애니메이션 종료! 최종 획득: " + winner.itemName);
        OnSpinFinished?.Invoke(winner);
    }
}