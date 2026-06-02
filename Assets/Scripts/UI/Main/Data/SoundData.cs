using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundData", menuName = "Scriptable Objects/SoundData")]
public class SoundData : ScriptableObject
{
    [System.Serializable]
    public class BGMData
    {
        public string name; // 구분용 이름 (예: "Title")
        [HideInInspector]
        public AudioClip clip;
        [Tooltip("여러 개를 넣으면 재생 시 랜덤 1개를 선택합니다. (레거시 clip은 자동 이관됨)")]
        public List<AudioClip> clips = new List<AudioClip>();
    }

    [System.Serializable]
    public class SFXData
    {
        public string name; // 구분용 이름 (예: "Jump")
        [HideInInspector]
        public AudioClip clip;
        [Tooltip("여러 개를 넣으면 재생 시 랜덤 1개를 선택합니다. (레거시 clip은 자동 이관됨)")]
        public List<AudioClip> clips = new List<AudioClip>();
    }

    public List<BGMData> bgmList = new List<BGMData>();
    public List<SFXData> sfxList = new List<SFXData>();

    private void OnValidate()
    {
        MigrateLegacyClips();
    }

    // 이름을 넣으면 오디오 클립을 찾아주는 함수
    public AudioClip GetBGM(string name)
    {
        MigrateLegacyClips();
        foreach (var data in bgmList)
        {
            if (data.name == name) return PickRandomClip(data.clips, data.clip);
        }
        return null;
    }

    public AudioClip GetSFX(string name)
    {
        MigrateLegacyClips();
        foreach (var data in sfxList)
        {
            if (data.name == name) return PickRandomClip(data.clips, data.clip);
        }
        return null;
    }

    /// <summary>
    /// 리스트가 있으면 랜덤으로 하나를 선택하고, 비어 있으면 legacyClip을 반환합니다.
    /// </summary>
    private AudioClip PickRandomClip(List<AudioClip> clipList, AudioClip legacyClip)
    {
        if (clipList != null && clipList.Count > 0)
        {
            // null 클립이 섞여 있을 수 있으므로 유효한 클립만 랜덤 선택
            int validCount = 0;
            for (int i = 0; i < clipList.Count; i++)
            {
                if (clipList[i] != null)
                    validCount++;
            }

            if (validCount > 0)
            {
                int pick = Random.Range(0, validCount);
                int cursor = 0;
                for (int i = 0; i < clipList.Count; i++)
                {
                    AudioClip c = clipList[i];
                    if (c == null) continue;
                    if (cursor == pick) return c;
                    cursor++;
                }
            }
        }

        return legacyClip;
    }

    /// <summary>
    /// 과거 단일 clip 필드 값을 clips[0]으로 자동 이관한다.
    /// </summary>
    private void MigrateLegacyClips()
    {
        if (bgmList != null)
        {
            for (int i = 0; i < bgmList.Count; i++)
                MigrateLegacyClip(bgmList[i]);
        }

        if (sfxList != null)
        {
            for (int i = 0; i < sfxList.Count; i++)
                MigrateLegacyClip(sfxList[i]);
        }
    }

    private static void MigrateLegacyClip(BGMData data)
    {
        if (data == null || data.clip == null) return;
        if (data.clips == null) data.clips = new List<AudioClip>();
        if (!data.clips.Contains(data.clip))
            data.clips.Insert(0, data.clip);
        data.clip = null;
    }

    private static void MigrateLegacyClip(SFXData data)
    {
        if (data == null || data.clip == null) return;
        if (data.clips == null) data.clips = new List<AudioClip>();
        if (!data.clips.Contains(data.clip))
            data.clips.Insert(0, data.clip);
        data.clip = null;
    }
}