[System.Serializable]
public class SettingsData
{
    // [사운드]
    public float bgmVolume = 1.0f;
    public float sfxVolume = 1.0f;
    
    // [조작]
    public float mouseSensitivity = 1.0f;

    // [그래픽] (해상도 인덱스, 화면 모드 인덱스)
    public int resolutionIndex = 0; 
    public int windowModeIndex = 0; 
}