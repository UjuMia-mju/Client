using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundData", menuName = "Scriptable Objects/SoundData")]
public class SoundData : ScriptableObject
{
    [System.Serializable]
    public class BGMData
    {
        public string name; // 구분용 이름 (예: "Title")
        public AudioClip clip;
    }

    [System.Serializable]
    public class SFXData
    {
        public string name; // 구분용 이름 (예: "Jump")
        public AudioClip clip;
    }

    public List<BGMData> bgmList = new List<BGMData>();
    public List<SFXData> sfxList = new List<SFXData>();

    // 이름을 넣으면 오디오 클립을 찾아주는 함수
    public AudioClip GetBGM(string name)
    {
        foreach (var data in bgmList)
        {
            if (data.name == name) return data.clip;
        }
        return null;
    }

    public AudioClip GetSFX(string name)
    {
        foreach (var data in sfxList)
        {
            if (data.name == name) return data.clip;
        }
        return null;
    }
}