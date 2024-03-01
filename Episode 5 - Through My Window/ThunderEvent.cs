using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThunderEvent : MonoBehaviour, IGameplayEvent
{
    public Light LinkedLight;
    public float BaseLightIntensity = 2f;
    public AnimationCurve LightIntensityCurve;
    [Range(0f, 5f)] public float LightFlashDuration = 0.2f;

    public string WWiseEvent = "Play_Thunder";
    [Range(0f, 5f)] public float MinAudioDelay = 0.1f;
    [Range(0f, 5f)] public float MaxAudioDelay = 0.3f;
    protected float AudioDelay;

    protected bool IsActive = false;
    protected bool PlayedAudio = false;
    protected float Progress = 0f;

    // Start is called before the first frame update
    void Start()
    {
        LinkedLight.intensity = 0f;
        LinkedLight.enabled = false;        
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsActive)
            return;

        // update the time
        Progress += Time.deltaTime;

        // update the light
        LinkedLight.intensity = BaseLightIntensity * LightIntensityCurve.Evaluate(Progress / LightFlashDuration);

        // check if need to play audio
        if (!PlayedAudio && Progress >= AudioDelay)
        {
            PlayedAudio = true;
            AudioShim.PostEvent(WWiseEvent, gameObject);
        }

        // finished event
        if (Progress >= LightFlashDuration && PlayedAudio)
        {
            IsActive = false;
            LinkedLight.intensity = 0f;
            LinkedLight.enabled = false;
        }
    }

    public void ActivateEvent()
    {
        if (IsActive)
            return;

        // Enable the event
        IsActive = true;
        Progress = 0f;
        AudioDelay = Random.Range(MinAudioDelay, MaxAudioDelay);

        // enable and reset the light
        LinkedLight.intensity = 0f;
        LinkedLight.enabled = true;
    }
}
