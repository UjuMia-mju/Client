using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviorSingleton<SoundManager>
{
    public float MASTER_SCALE = 0.5f; 

    [Header("Data Asset")]
    [SerializeField] private SoundData soundData;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmPlayer;
    [SerializeField] private AudioSource sfxPlayer;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    protected override void Awake()
    {
        base.Awake();
        
        ApplyVolumes();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllSound();
    }

    public void StopAllSound()
    {
        bgmPlayer.Stop();
        sfxPlayer.Stop();
    }
    
    // 재생 로직
    public void PlayBGM(string bgmName)
    {
        AudioClip clip = soundData.GetBGM(bgmName); 
        if (clip != null)
        {
            if (bgmPlayer.clip == clip && bgmPlayer.isPlaying) return;
            
            bgmPlayer.clip = clip;
            bgmPlayer.loop = true;
            // 재생 시 MASTER_SCALE 곱하기
            bgmPlayer.volume = bgmVolume * MASTER_SCALE; 
            bgmPlayer.Play();
        }
    }

    public void PlaySFX(string sfxName)
    {
        AudioClip clip = soundData.GetSFX(sfxName);
        if (clip != null)
        {
            // sfxPlayer.volume이 이미 줄어들어 있으므로 1f로 재생
            sfxPlayer.PlayOneShot(clip); 
        }
    }
    
    // 볼륨 조절 (내부 적용)
    private void ApplyVolumes()
    {
        bgmPlayer.volume = bgmVolume * MASTER_SCALE;
        sfxPlayer.volume = sfxVolume * MASTER_SCALE;
    }

    public void SetBGMVolume(float volume) 
    { 
        bgmVolume = volume; 
        ApplyVolumes(); // 즉시 적용
    }

    public void SetSFXVolume(float volume) 
    { 
        sfxVolume = volume; 
        ApplyVolumes(); // 즉시 적용
    }
    
    public float GetBGMVolume() => bgmVolume;
    public float GetSFXVolume() => sfxVolume;
}