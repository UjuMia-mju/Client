using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Drawing;

public class GachaSpinnerUI : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform contentReel;   // 움직일 긴 띠 (Content)
    public GameObject itemSlotPrefab;   // 아이템 슬롯 프리팹
    public GachaManager gachaManager;   // 매니저 참조 (전체 아이템 목록 접근용)

    [Header("Animation Settings")]
    public int totalSlots = 50;         // 릴에 보여줄 총 슬롯 개수
    public int winnerIndex = 45;        // 당첨 아이템이 위치할 인덱스
    public float spinDuration = 5f;     // 돌아가는 시간
    public AnimationCurve slowingCurve; // 감속 그래프 (에디터에서 설정)

    [Header("Result Popup Settings")]
    public GameObject resultPanel;      // 결과창 패널
    public Image resultIconImage;       // 결과 아이콘
    public TextMeshProUGUI resultNameText;   // 결과 이름
    public TextMeshProUGUI resultRarityText; // 결과 등급
    public Button closeButton;          // 닫기 버튼

    [Header("Audio Settings")]
    public AudioSource audioSource; // 오디오 소스 컴포넌트 연결
    public AudioClip tickSound;     // 스핀 틱 사운드
    public AudioClip winSound;      // 당첨 사운드

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

        // 닫기 버튼을 누르면 CloseResultPopup 함수 실행
        if(closeButton != null)
            closeButton.onClick.AddListener(CloseResultPopup);
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

        // 루프 밖에서 Near Miss 여부 결정 (50% 확률)
        bool triggerNearMiss = Random.value < 0.5f; 

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject newSlot = Instantiate(itemSlotPrefab, contentReel);
            spawnedSlots.Add(newSlot);

            // 아이템 데이터 가져오기
            GachaItem itemData;
            if (i == winnerIndex)
            {
                itemData = winner; // 진짜 당첨아이템
            }
            else if (triggerNearMiss && i == winnerIndex + 1)
            {
                // Near Miss (50%)
                // 당첨 아이템 바로 옆에 높은 등급의 아이템 배치
                itemData = gachaManager.GetRandomLegendaryItem();

                Debug.Log("Near Miss 실행");
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
        // 소리 재생을 위한 변수
        int lastIndex = 0;

        float elapsedTime = 0f;
        while (elapsedTime < spinDuration)
        {
            elapsedTime += Time.deltaTime;
            // 0~1 사이 진행률
            float percentage = elapsedTime / spinDuration;
            // 커브를 적용하여 빠르다가 느려지는 진행률로 변환
            float curvePercent = slowingCurve.Evaluate(percentage);

            contentReel.anchoredPosition = Vector2.Lerp(startPosition, endPosition, curvePercent);

            // 틱 사운드 재생
            // 현재 Content가 얼마나 이동했는지 계산 (왼쪽으로 가니까 음수를 양수로 변환)
            float currentAbsX = Mathf.Abs(contentReel.anchoredPosition.x);
            // 경계선(왼쪽 모서리) 인식 보정
            // 원래 위치에 '슬롯 너비의 절반'을 더함.
            // 시작 부분(왼쪽 선)이 닿을 때 인덱스가 바뀜.
            float adjustedX = currentAbsX + (slotWidth / 2f);
            // 인덱스 계산
            int currentIndex = (int)(adjustedX / slotWidth);
            // 아이템 번호가 바뀌었다면(=하나가 지나가면)
            if (currentIndex != lastIndex)
            {
                // 소리가 너무 인위적이지 않게 피치에 약간 랜덤성 추가
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                // 소리 재생
                audioSource.PlayOneShot(tickSound);
                // 마지막 인덱스 업데이트
                lastIndex = currentIndex;
            }

            yield return null;
        }

        // 5. 최종 위치 보정 및 결과 발표
        contentReel.anchoredPosition = endPosition;
        Debug.Log("애니메이션 종료! 최종 획득: " + winner.itemName);


        // 스핀이 멈추고 0.5초 후에 결과창 띄우며 당첨 사운드 재생
        yield return new WaitForSeconds(0.5f);

        // 당첨 사운드 재생
        if (winSound != null)
        {
            audioSource.pitch = 1f; // 당첨 사운드는 피치 고정
            audioSource.PlayOneShot(winSound);
        }
        ShowResultPopup(winner);
    }

    // 결과창 내용을 채우고 보여주는 함수
    void ShowResultPopup(GachaItem item)
    {
        if (resultPanel == null) return;

        // 아이콘, 이름, 등급, 확률 등 정보 채우기
        resultIconImage.sprite = item.icon;
        resultNameText.text = item.itemName;
        resultRarityText.text = item.rarity.ToString();

        // 결과창 활성화
        resultPanel.SetActive(true);

        // 여기에 결과창 활성화 시 소리 효과음 추가
    }

    // 닫기 버튼을 누르면 실행될 함수
    public void CloseResultPopup()
    {
        resultPanel.SetActive(false);
    }
}