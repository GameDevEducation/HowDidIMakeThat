using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    public Toggle InvertYAxis;
    public Slider MainVolume;
    public Slider SFXVolume;
    public Slider VOVolume;
    public Slider MusicVolume;

    protected bool IsLoading = false;

    public void LoadSettings()
    {
        IsLoading = true;

        float mainVolume = PlayerPrefs.GetFloat("Volume.Main", 50.0f);
        AkSoundEngine.SetRTPCValue("Volume_Master", mainVolume);
        MainVolume.SetValueWithoutNotify(mainVolume);

        float sfxVolume = PlayerPrefs.GetFloat("Volume.SFX", 50.0f);
        AkSoundEngine.SetRTPCValue("Volume_SFX", sfxVolume);
        SFXVolume.SetValueWithoutNotify(sfxVolume);

        float voVolume = PlayerPrefs.GetFloat("Volume.VO", 50.0f);
        AkSoundEngine.SetRTPCValue("Volume_VO", voVolume);
        VOVolume.SetValueWithoutNotify(voVolume);

        float musicVolume = PlayerPrefs.GetFloat("Volume.Music", 50.0f);
        AkSoundEngine.SetRTPCValue("Volume_Music", musicVolume);
        MusicVolume.SetValueWithoutNotify(musicVolume);

        InvertYAxis.isOn = PlayerPrefs.GetInt("Controls.InvertY", 0) == 1;

        IsLoading = false;
    }

    public void InvertYChanged(bool newValue)
    {
        if (IsLoading)
            return;

        PlayerPrefs.SetInt("Controls.InvertY", newValue ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void VolumeChanged_Master(float newValue)
    {
        if (IsLoading)
            return;

        PlayerPrefs.SetFloat("Volume.Main", newValue);
        PlayerPrefs.Save();

        AkSoundEngine.SetRTPCValue("Volume_Master", newValue);
    }

    public void VolumeChanged_SFX(float newValue)
    {
        if (IsLoading)
            return;

        PlayerPrefs.SetFloat("Volume.SFX", newValue);
        PlayerPrefs.Save();

        AkSoundEngine.SetRTPCValue("Volume_SFX", newValue);
    }

    public void VolumeChanged_VO(float newValue)
    {
        if (IsLoading)
            return;

        PlayerPrefs.SetFloat("Volume.VO", newValue);
        PlayerPrefs.Save();

        AkSoundEngine.SetRTPCValue("Volume_VO", newValue);
    }

    public void VolumeChanged_Music(float newValue)
    {
        if (IsLoading)
            return;

        PlayerPrefs.SetFloat("Volume.Music", newValue);
        PlayerPrefs.Save();

        AkSoundEngine.SetRTPCValue("Volume_Music", newValue);
    }}
