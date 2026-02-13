using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 버튼을 누를때마다 패널 이동
/// </summary>
public class SettingsPanelController : MonoBehaviour
{
    [SerializeField] private List<GameObject> panels;

    [Header("Buttons")]
    [SerializeField] private GameObject leftButton;
    [SerializeField] private GameObject rightButton;
    
    // 현재 보고 있는 패널의 번호 (0부터 시작)
    private int currentIndex = 0;
    
    private void Start()
    {
        // 게임 시작 시 초기화 (첫 번째 패널만 보이기)
        UpdatePanels();
    }

    public void OnLeftButtonClicked()
    {
        if (panels.Count == 0) return;

        currentIndex--;

        // 0보다 작아지면(첫번째 왼쪽), 마지막 패널로 이동 (루프 기능)
        if (currentIndex < 0)
        {
            currentIndex = panels.Count - 1;
        }
        
        UpdatePanels();
    }
    
    public void OnRightButtonClicked()
    {
        if (panels.Count == 0) return;

        currentIndex++;

        // 리스트 개수를 넘어가면(마지막 오른쪽), 첫 번째 패널로 이동 (루프 기능)
        if (currentIndex >= panels.Count)
        {
            currentIndex = 0;
        }
        
        UpdatePanels();
    }

    // 실제 패널을 끄고 켜는 로직을 분리하여 관리
    private void UpdatePanels()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            // 현재 인덱스와 같으면 켜고(true), 다르면 끕니다(false)
            bool isActive = (i == currentIndex);
            panels[i].SetActive(isActive);
        }
    }
}