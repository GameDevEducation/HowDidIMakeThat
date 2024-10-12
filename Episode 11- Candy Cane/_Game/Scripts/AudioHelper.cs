using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioHelper : MonoBehaviour
{
    protected static bool MusicPlaying = false;

    public void SetRTPC(string name, float value)
    {
        AkSoundEngine.SetRTPCValue(name, value);
    }

    public void PostEvent(string name, GameObject target)
    {
        AkSoundEngine.PostEvent(name, target);
    }

    public void SetMusicPlaying()
    {
        AudioHelper.MusicPlaying = true;
    }

    public bool GetMusicPlaying()
    {
        return AudioHelper.MusicPlaying;
    }
}
