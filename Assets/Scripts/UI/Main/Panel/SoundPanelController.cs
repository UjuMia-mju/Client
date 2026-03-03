using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoundPanelController : MonoBehaviour
{
    [Header("Sliders")] 
    [SerializeField] private Slider BGMSlider;
    [SerializeField] private Slider SFXSlider;

    [Header("Volume UI")]
    [SerializeField] private TextMeshProUGUI BGMVolumeText;
    [SerializeField] private TextMeshProUGUI SFXVolumeText;

    private void Start()
    {
        BGMSlider.value = SoundManager.Instance.GetBGMVolume();
        SFXSlider.value = SoundManager.Instance.GetSFXVolume();
    }
    
    public void OnBGMSliderChanged(float value)
    {
        DataManager.Instance.data.bgmVolume = value;
        SoundManager.Instance.SetBGMVolume(value);
        DataManager.Instance.Save();
        
        BGMVolumeText.text = (value*100).ToString("0");
    }

    public void OnSFXSliderChanged(float value)
    {
        DataManager.Instance.data.sfxVolume = value;
        SoundManager.Instance.SetSFXVolume(value);
        DataManager.Instance.Save();
            
        SFXVolumeText.text = (value*100).ToString("0");
    }
}
