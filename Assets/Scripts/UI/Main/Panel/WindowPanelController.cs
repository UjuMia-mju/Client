using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WindowPanelController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown windowModeDropdown;

    [Header("Settings")]
    [SerializeField] private List<Vector2Int> resolutions = new List<Vector2Int>()
    {
        new Vector2Int(1920, 1080), // Index 0
        new Vector2Int(2560, 1440), // Index 1
        new Vector2Int(3840, 2160)  // Index 2
    };

    private void Start()
    {
        // 게임 시작 시, 저장된 설정(SettingsData)을 불러와서 적용
        InitSettings();
    }

    // 초기화 함수
    private void InitSettings()
    {
        // 1. DataManager의 SettingsData에서 값 가져오기
        int savedResIndex = DataManager.Instance.data.resolutionIndex;
        int savedModeIndex = DataManager.Instance.data.windowModeIndex;

        // 2. 인덱스 유효성 체크 (혹시 해상도 목록이 바뀌었을 때 에러 방지)
        if (savedResIndex < 0 || savedResIndex >= resolutions.Count)
            savedResIndex = 0;

        // 3. 드롭다운 UI 값을 저장된 값으로 변경
        resolutionDropdown.value = savedResIndex;
        windowModeDropdown.value = savedModeIndex;
        
        // 4. UI 갱신 (드롭다운 텍스트 업데이트)
        resolutionDropdown.RefreshShownValue();
        windowModeDropdown.RefreshShownValue();

        // 5. 실제 화면 해상도 및 모드 적용
        ApplyResolution(savedResIndex);
        ApplyWindowMode(savedModeIndex);
    }

    // =========================================================
    // UI 이벤트 연결용 (Dropdown OnValueChanged)
    // =========================================================

    public void OnResolutionChanged()
    {
        int index = resolutionDropdown.value;

        // 1. 실제 화면 적용
        ApplyResolution(index);

        // 2. SettingsData에 값 저장
        DataManager.Instance.data.resolutionIndex = index;
        DataManager.Instance.Save();
    }

    public void OnWindowModeChanged()
    {
        int index = windowModeDropdown.value;

        // 1. 실제 화면 적용
        ApplyWindowMode(index);

        // 2. SettingsData에 값 저장
        DataManager.Instance.data.windowModeIndex = index;
        DataManager.Instance.Save();
    }

    // =========================================================
    // 실제 적용 로직
    // =========================================================

    private void ApplyResolution(int index)
    {
        if (index < 0 || index >= resolutions.Count) return;

        int width = resolutions[index].x;
        int height = resolutions[index].y;

        Screen.SetResolution(width, height, Screen.fullScreenMode);
    }

    private void ApplyWindowMode(int index)
    {
        switch (index)
        {
            case 0: // 전체 화면
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1: // 테두리 없는 창 모드
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            case 2: // 창 모드 (HD 고정)
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                break;
        }
    }
}