using System;
using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;

/// <summary>
/// UI 데이터를 json 파일로 저장 / 불러오기
/// </summary>
public class DataManager : MonoBehaviorSingleton<DataManager>
{
    // InputAction
    public InputActionAsset playerInput;
    
    // 현재 게임 데이터
    public SettingsData data = new SettingsData();

    // 파일 경로
    private string settingsPath;
    private string keybindPath;

    protected override void Awake()
    {
        base.Awake();
        
        // NOTE: 추후 스크립트 분리
        settingsPath = Path.Combine(Application.persistentDataPath, "settings.json");
        keybindPath = Path.Combine(Application.persistentDataPath, "keybindings.json");
    }

    private void Start()
    {
        Load();
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(data, true); 
        File.WriteAllText(settingsPath, json);
        
        string keyJson = playerInput.SaveBindingOverridesAsJson();
        File.WriteAllText(keybindPath, keyJson);
    }

    public void Load()
    {
        LoadSettingsFromFile();
        LoadKeybindOverridesFromFile();
        ApplyDataToSystem();
    }

    void LoadSettingsFromFile()
    {
        try
        {
            if (!File.Exists(settingsPath))
                return;

            string json = File.ReadAllText(settingsPath);
            if (string.IsNullOrWhiteSpace(json))
                return;

            var loaded = JsonUtility.FromJson<SettingsData>(json);
            if (loaded != null)
                data = loaded;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DataManager: settings.json 로드 실패 — {e.Message}");
        }
    }

    /// <summary>Save()와 동일 출처: keybindings.json 우선, 없으면 예전 PlayerPrefs(rebinds).</summary>
    void LoadKeybindOverridesFromFile()
    {
        try
        {
            string keyJson = null;
            if (File.Exists(keybindPath))
                keyJson = File.ReadAllText(keybindPath);
            if (string.IsNullOrEmpty(keyJson))
                keyJson = PlayerPrefs.GetString("rebinds");
            if (string.IsNullOrEmpty(keyJson))
                return;

            if (playerInput != null)
                playerInput.LoadBindingOverridesFromJson(keyJson);

            if (InputManager.Instance != null)
                InputManager.Instance.Actions.asset.LoadBindingOverridesFromJson(keyJson);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DataManager: 키 바인딩 오버라이드 로드 실패 — {e.Message}");
        }
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

        // 마우스 감도 적용
        ControlPanelController.MouseSensitivity = data.mouseSensitivity;

        // 해상도 및 화면 모드 적용
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
        int safeIndex = data.resolutionIndex;
        
        // Define.Resolution을 직접 호출하여 안전하게 인덱스 검사
        if (safeIndex < 0 || safeIndex >= Define.Resolution.Count) 
        {
            safeIndex = 0;
        }

        // Define.Resolution에서 직접 width, height 값 꺼내오기
        int width = Define.Resolution[safeIndex].x;
        int height = Define.Resolution[safeIndex].y;
        
        if (mode == FullScreenMode.Windowed)
        {
            width = 1280; height = 720; // 창모드 시 크기 지정
        }

        // 3. 최종 적용
        Screen.SetResolution(width, height, mode);
    }
}