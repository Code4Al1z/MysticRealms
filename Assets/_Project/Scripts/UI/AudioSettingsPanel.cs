using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsPanel : MonoBehaviour
{
    [SerializeField] private AudioSettings audioSettings;

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Wwise Game Parameters")]
    [SerializeField] private AK.Wwise.RTPC masterVolumeRTPC;
    [SerializeField] private AK.Wwise.RTPC musicVolumeRTPC;
    [SerializeField] private AK.Wwise.RTPC sfxVolumeRTPC;

    private void OnEnable()
    {
        if (audioSettings == null) return;

        if (masterSlider != null)
        {
            masterSlider.value = audioSettings.masterVolume;
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
        }

        if (musicSlider != null)
        {
            musicSlider.value = audioSettings.musicVolume;
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = audioSettings.sfxVolume;
            sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        }

        ApplyAll();
    }

    private void OnDisable()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
    }

    private void OnMasterChanged(float value)
    {
        if (audioSettings != null) 
            audioSettings.masterVolume = value;
        if (masterVolumeRTPC  != null)
        masterVolumeRTPC.SetGlobalValue(value * 100f);
    }

    private void OnMusicChanged(float value)
    {
        if (audioSettings != null) 
            audioSettings.musicVolume = value;
        if (musicVolumeRTPC != null)
            musicVolumeRTPC.SetGlobalValue(value * 100f);
    }

    private void OnSFXChanged(float value)
    {
        if (audioSettings != null) 
            audioSettings.sfxVolume = value;
        if (sfxVolumeRTPC != null)
            sfxVolumeRTPC.SetGlobalValue(value * 100f);
    }

    private void ApplyAll()
    {
        if (audioSettings == null) return;
        if (masterVolumeRTPC != null)
            masterVolumeRTPC.SetGlobalValue(audioSettings.masterVolume * 100f);
        if (musicVolumeRTPC != null)
            musicVolumeRTPC.SetGlobalValue(audioSettings.musicVolume   * 100f);
        if (sfxVolumeRTPC != null)
            sfxVolumeRTPC.SetGlobalValue(audioSettings.sfxVolume       * 100f);
    }
}
