using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleAudio : MonoBehaviour
{
    public string VehicleType;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BeginAudio()
    {
        if (!gameObject.activeInHierarchy)
            return;

        AkSoundEngine.SetSwitch("VehicleType", VehicleType, gameObject);
        AkSoundEngine.PostEvent("Play_Engine", gameObject);
    }

    public void StopAudio()
    {
        if (!gameObject.activeInHierarchy)
            return;
            
        AkSoundEngine.PostEvent("Stop_Engine", gameObject);
    }
}
