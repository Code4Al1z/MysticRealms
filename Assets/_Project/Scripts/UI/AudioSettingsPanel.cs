using UnityEngine;
using TMPro;

public class AudioSettingsPanel : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private WaveformSlider masterSlider;
    [SerializeField] private WaveformSlider musicSlider;
    [SerializeField] private WaveformSlider sfxSlider;

    [Header("Labels")]
    [SerializeField] private TMP_Text masterLabel;
    [SerializeField] private TMP_Text musicLabel;
    [SerializeField] private TMP_Text sfxLabel;

    [Header("Data")]
    [SerializeField] private AudioSettings audioSettings;

    [Header("Wwise")]
    [SerializeField] private AK.Wwise.RTPC masterVolumeRTPC;
    [SerializeField] private AK.Wwise.RTPC musicVolumeRTPC;
    [SerializeField] private AK.Wwise.RTPC sfxVolumeRTPC;

    private void OnEnable()
    {
        if (audioSettings == null) return;

        SetupSlider(masterSlider, audioSettings.masterVolume, OnMasterChanged, "Master Volume", masterLabel);
        SetupSlider(musicSlider, audioSettings.musicVolume, OnMusicChanged, "Music Volume", musicLabel);
        SetupSlider(sfxSlider, audioSettings.sfxVolume, OnSFXChanged, "SFX Volume", sfxLabel);

        ApplyAll();
    }

    private void OnDisable()
    {
        if (masterSlider != null) masterSlider.OnValueChanged -= OnMasterChanged;
        if (musicSlider != null) musicSlider.OnValueChanged -= OnMusicChanged;
        if (sfxSlider != null) sfxSlider.OnValueChanged -= OnSFXChanged;
    }

    private void SetupSlider(WaveformSlider slider, float initialValue,
                              System.Action<float> callback, string labelText, TMP_Text label)
    {
        if (slider == null) return;
        slider.SetValue(initialValue);
        slider.OnValueChanged += callback;
        if (label != null) label.text = labelText;
    }

    private void OnMasterChanged(float v)
    {
        if (audioSettings != null) audioSettings.masterVolume = v;
        if (masterVolumeRTPC != null) masterVolumeRTPC.SetGlobalValue(v * 100f);
    }

    private void OnMusicChanged(float v)
    {
        if (audioSettings != null) audioSettings.musicVolume = v;
        if (musicVolumeRTPC != null) musicVolumeRTPC.SetGlobalValue(v * 100f);
    }

    private void OnSFXChanged(float v)
    {
        if (audioSettings != null) audioSettings.sfxVolume = v;
        if (sfxVolumeRTPC != null) sfxVolumeRTPC.SetGlobalValue(v * 100f);
    }

    private void ApplyAll()
    {
        if (audioSettings == null) return;
        if (masterVolumeRTPC != null) masterVolumeRTPC.SetGlobalValue(audioSettings.masterVolume * 100f);
        if (musicVolumeRTPC != null) musicVolumeRTPC.SetGlobalValue(audioSettings.musicVolume * 100f);
        if (sfxVolumeRTPC != null) sfxVolumeRTPC.SetGlobalValue(audioSettings.sfxVolume * 100f);
    }
}