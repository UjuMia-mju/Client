using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshPro 사용 시 필수 (기본 UI라면 UnityEngine.UI)

/// <summary>
/// 해상도와 화면 모드 전환
/// </summary>
public class WindowPanelController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown windowModeDropdown;

    // 지원할 해상도 목록 (Inspector에서 세팅 가능)
    [Header("Settings")]
    [SerializeField] private List<Vector2Int> resolutions = new List<Vector2Int>()
    {
        new Vector2Int(1920, 1080), // FHD (Index 0)
        new Vector2Int(2560, 1440), // QHD (Index 1)
        new Vector2Int(3840, 2160)  // UHD (Index 2)
    };

    public void OnResolutionChanged()
    {
        // 1. 드롭다운에서 현재 선택된 인덱스를 가져옴
        int index = resolutionDropdown.value;

        // 2. 리스트에서 해당 해상도 값을 가져옴 (예외 처리 없이 바로 접근)
        int width = resolutions[index].x;
        int height = resolutions[index].y;

        // 3. 해상도 적용 (세 번째 인자는 현재 전체화면 모드 유지)
        Screen.SetResolution(width, height, Screen.fullScreenMode);
        
        Debug.Log($"해상도 변경: {width} x {height}");
    }

    public void OnWindowModeChanged()
    {
        // 0번: 전체 화면, 1번: 창 모드
        int index = windowModeDropdown.value;

        switch (index)
        {
            case 0: // 전체 화면
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1: // 테두리 없는 창 모드
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            case 2: // 일반 창 모드
                // 창모드로 바뀔 때 1280x720 사이즈로 변경
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                break;
        }
        
        Debug.Log($"화면 모드 변경: {Screen.fullScreenMode}");
    }
}