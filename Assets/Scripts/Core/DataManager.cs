using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DataManager : MonoBehaviorSingleton<DataManager>
{
    [Header("Input System")]
    [SerializeField] private InputActionAsset playerInput;

    // InputAction
    public InputActionAsset InputAsset => playerInput;
    
    // 현재 게임 데이터
    public SettingsData data = new SettingsData();

    // 파일 경로
    private string settingsPath;
    private string keybindPath;
    
    public readonly List<Vector2Int> Resolutions = new List<Vector2Int>()
    {
        new Vector2Int(1920, 1080), // Index 0 (FHD)
        new Vector2Int(2560, 1440), // Index 1 (QHD)
        new Vector2Int(3840, 2160)  // Index 2 (UHD)
    };

    protected override void Awake()
    {
        base.Awake();
        
        settingsPath = Path.Combine(Application.persistentDataPath, "settings.json");
        keybindPath = Path.Combine(Application.persistentDataPath, "keybindings.json");

        Load();
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(data, true); 
        File.WriteAllText(settingsPath, json);

        // InputActionAsset에는 SaveBindingOverridesAsJson 확장 메서드가 있습니다.
        string keyJson = playerInput.SaveBindingOverridesAsJson();
        File.WriteAllText(keybindPath, keyJson);

        Debug.Log("모든 데이터 저장 완료");
    }

    public void Load()
    {
        // 1. 일반 설정 로드
        if (File.Exists(settingsPath))
        {
            string json = File.ReadAllText(settingsPath);
            data = JsonUtility.FromJson<SettingsData>(json);
        }
        else
        {
            data = new SettingsData(); 
        }

        // 2. 키 설정 로드
        if (File.Exists(keybindPath))
        {
            string keyJson = File.ReadAllText(keybindPath);
            playerInput.LoadBindingOverridesFromJson(keyJson);
        }

        // 3. 적용
        ApplyDataToSystem();
    }

    // ★ 데이터를 실제 게임 시스템에 적용하는 함수
    private void ApplyDataToSystem()
    {
        // [사운드 적용]
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetBGMVolume(data.bgmVolume);
            SoundManager.Instance.SetSFXVolume(data.sfxVolume);
        }

        // [마우스 감도 적용]
        ControlPanelController.MouseSensitivity = data.mouseSensitivity;

        // [해상도 및 화면 모드 적용]
        ApplyGraphicsSettings();
    }

    // 해상도 적용 로직 분리
    private void ApplyGraphicsSettings()
    {
        // 1. 화면 모드 결정
        FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
        switch (data.windowModeIndex)
        {
            case 0: mode = FullScreenMode.ExclusiveFullScreen; break; // 전체화면
            case 1: mode = FullScreenMode.FullScreenWindow; break;    // 테두리 없음
            case 2: mode = FullScreenMode.Windowed; break;            // 창모드
        }

        // 2. 해상도 결정
        // 저장된 인덱스가 리스트 범위를 벗어나면 0번(기본)으로 안전하게 처리
        int safeIndex = data.resolutionIndex;
        if (safeIndex < 0 || safeIndex >= Resolutions.Count) safeIndex = 0;

        int width = Resolutions[safeIndex].x;
        int height = Resolutions[safeIndex].y;
        
        if (mode == FullScreenMode.Windowed)
        {
            width = 1280; height = 720; // 창모드 시 크기 지정
        }

        // 3. 최종 적용
        Screen.SetResolution(width, height, mode);
    }
}