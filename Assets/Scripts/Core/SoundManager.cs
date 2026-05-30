using UnityEngine;

public class SoundManager : MonoBehaviorSingleton<SoundManager>
{
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
        bgmPlayer.volume = bgmVolume;
        sfxPlayer.volume = sfxVolume;
    }

    public void StopAllSound()
    {
        bgmPlayer.Stop();
        sfxPlayer.Stop(); // PlayOneShot으로 재생 중인 소리도 멈춥니다.
    }

    // 재생 로직 (이전과 동일)
    public void PlayBGM(string bgmName)
    {
        AudioClip clip = soundData.GetBGM(bgmName); 
        if (clip != null)
        {
            if (bgmPlayer.clip == clip && bgmPlayer.isPlaying) return;
            bgmPlayer.clip = clip;
            bgmPlayer.loop = true;
            bgmPlayer.volume = bgmVolume;
            bgmPlayer.Play();
        }
    }

    public void PlaySFX(string sfxName)
    {
        AudioClip clip = soundData.GetSFX(sfxName);
        if (clip != null)
        {
            sfxPlayer.PlayOneShot(clip, 1f); 
        }
    }

    /// <summary>
    /// 월드 위치 기준 3D SFX 재생.
    /// SoundData에 등록된 sfxName을 찾아 임시 AudioSource로 재생한다.
    /// </summary>
    public void PlaySFXAt(
        string sfxName,
        Vector3 position,
        float volumeScale = 1f,
        float minPitch = 1f,
        float maxPitch = 1f,
        float minDistance = 2f,
        float maxDistance = 18f)
    {
        if (soundData == null) return;
        AudioClip clip = soundData.GetSFX(sfxName);
        if (clip == null) return;

        var sfxObj = new GameObject($"SFX_{sfxName}");
        sfxObj.transform.position = position;

        AudioSource source = sfxObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = Mathf.Clamp01(sfxVolume * volumeScale);
        source.pitch = Random.Range(minPitch, maxPitch);
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.Play();

        float lifeTime = clip.length / Mathf.Max(0.01f, source.pitch) + 0.05f;
        Destroy(sfxObj, lifeTime);
    }

    // 볼륨 조절 및 Getter
    public void SetBGMVolume(float volume) 
    { 
        bgmVolume = volume; 
        bgmPlayer.volume = volume; 
    }
    public void SetSFXVolume(float volume) 
    { 
        sfxVolume = volume; 
        sfxPlayer.volume = volume; 
    }
    public float GetBGMVolume() => bgmVolume;
    public float GetSFXVolume() => sfxVolume;
}