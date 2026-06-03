using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WindowPanelController : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown windowModeDropdown;

    private void Start()
    {
        // 1. Define.Resolution을 바탕으로 드롭다운 옵션을 자동 생성
        InitDropdownOptions();

        // 2. 게임 시작 시, 저장된 설정(SettingsData)을 불러와서 적용
        InitSettings();
    }

    // 해상도 드롭다운 목록 자동 세팅
    private void InitDropdownOptions()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        // Define.cs에 있는 해상도 리스트를 텍스트로 변환해서 드롭다운에 추가
        foreach (Vector2Int res in Define.Resolution)
        {
            options.Add($"{res.x} x {res.y}");
        }

        resolutionDropdown.AddOptions(options);
    }

    // 초기화 함수
    private void InitSettings()
    {
        // 1. DataManager의 SettingsData에서 값 가져오기
        int savedResIndex = DataManager.Instance.data.resolutionIndex;
        int savedModeIndex = DataManager.Instance.data.windowModeIndex;

        // 2. 인덱스 유효성 체크 (Define.Resolution 사용)
        if (savedResIndex < 0 || savedResIndex >= Define.Resolution.Count)
            savedResIndex = 0;

        // 3. 드롭다운 UI 값을 저장된 값으로 변경
        if (resolutionDropdown != null) resolutionDropdown.value = savedResIndex;
        if (windowModeDropdown != null) windowModeDropdown.value = savedModeIndex;
        
        // 4. UI 갱신 (드롭다운 텍스트 업데이트)
        if (resolutionDropdown != null) resolutionDropdown.RefreshShownValue();
        if (windowModeDropdown != null) windowModeDropdown.RefreshShownValue();

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
        if (index < 0 || index >= Define.Resolution.Count)
            return;

        DisplayResolutionHelper.ApplyResolution(index, Screen.fullScreenMode);
    }

    private void ApplyWindowMode(int index)
    {
        int resIndex = DataManager.Instance != null
            ? DataManager.Instance.data.resolutionIndex
            : 0;
        DisplayResolutionHelper.ApplyWindowMode(index, resIndex);
    }
}