using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsManager : MonoBehaviour, IPausable
{
    protected class HapticEffectData
    {
        public System.Guid PlayingID;
        public HapticEffect Effect { get; private set;}
        public HapticEffect.EBlendMode Blending { get; private set;}
        public float Duration { get; private set;}
        public float TimeRemaining { get; private set; }
        public bool Looping { get; private set;}

        public HapticEffectData(HapticEffect _effect, float _duration, HapticEffect.EBlendMode _blending, bool _looping)
        {
            PlayingID = System.Guid.NewGuid();
            Effect = _effect;
            TimeRemaining = Duration = _duration;
            Blending = _blending;
            Looping = _looping;
        }

        public bool AdvanceTime(float amount)
        {
            TimeRemaining -= amount;

            if (Looping && TimeRemaining <= 0)
                TimeRemaining += Duration;

            return TimeRemaining <= 0;
        }

        public void SetDuration(float newDuration)
        {
            Duration = newDuration;
            if (TimeRemaining > newDuration)
                TimeRemaining = newDuration;
        }

        public void ApplyTo(ref float lowFrequencyMotor, ref float highFrequencyMotor)
        {
            float baseValue_LowFreq = -1f;
            float baseValue_HighFreq = -1f;

            // single value mode?
            if (Effect.Type == HapticEffect.EType.SingleValue)
            {
                baseValue_LowFreq = Effect.Gamepad_LowFrequencyMotor_Value;
                baseValue_HighFreq = Effect.Gamepad_HighFrequencyMotor_Value;
            } // curve mode
            else
            {
                float progress = TimeRemaining / Duration;

                if (Effect.Gamepad_LowFrequencyMotor_Curve.keys.Length > 0)
                    baseValue_LowFreq = Effect.Gamepad_LowFrequencyMotor_Curve.Evaluate(progress);
                if (Effect.Gamepad_HighFrequencyMotor_Curve.keys.Length > 0)
                    baseValue_HighFreq = Effect.Gamepad_HighFrequencyMotor_Curve.Evaluate(progress);
            }

            // blend the values in
            switch(Blending)
            {
                case HapticEffect.EBlendMode.Overwrite:
                {
                    if (baseValue_LowFreq > -0.9f)
                        lowFrequencyMotor = baseValue_LowFreq;
                    if (baseValue_HighFreq > -0.9f)
                        highFrequencyMotor = baseValue_HighFreq;
                }
                break;

                case HapticEffect.EBlendMode.Add:
                {
                    if (baseValue_LowFreq > -0.9f)
                        lowFrequencyMotor += baseValue_LowFreq;
                    if (baseValue_HighFreq > -0.9f)
                        highFrequencyMotor += baseValue_HighFreq;
                }
                break;

                case HapticEffect.EBlendMode.Subtract:
                {
                    if (baseValue_LowFreq > -0.9f)
                        lowFrequencyMotor -= baseValue_LowFreq;
                    if (baseValue_HighFreq > -0.9f)
                        highFrequencyMotor -= baseValue_HighFreq;
                }
                break;

                case HapticEffect.EBlendMode.Multiply:
                {
                    if (baseValue_LowFreq > -0.9f)
                        lowFrequencyMotor *= baseValue_LowFreq;
                    if (baseValue_HighFreq > -0.9f)
                        highFrequencyMotor *= baseValue_HighFreq;
                }
                break;                                                
            }
        }
    }

    protected List<HapticEffectData> ActiveEffects = new List<HapticEffectData>();

    public static HapticsManager Instance { get; private set; }

    private bool EnableHaptics = true;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        PauseManager.Instance.RegisterPausable(this);

        EnableHaptics = PlayerPrefs.GetInt("Controls.EnableHaptics", 1) == 1;
    }

    // Update is called once per frame
    void Update()
    {
        // do nothing if pausee
        if (PauseManager.IsPaused)
            return;

        // update gamepad hapticw
        if(Gamepad.current != null)
            Update_Internal_Gamepad();
    }

    protected void Update_Internal_Gamepad()
    {
        float lowFrequencyMotor = 0f;
        float highFrequencyMotor = 0f;

        // process each of the effects
        for (int index = 0; index < ActiveEffects.Count; ++index)
        {            
            // does the effect have a duration?
            if (ActiveEffects[index].TimeRemaining > 0)
            {
                // has the efect expired?
                if (ActiveEffects[index].AdvanceTime(Time.deltaTime))
                {
                    // remove the effect
                    ActiveEffects.RemoveAt(index);

                    // update the index and resume
                    --index;
                    continue;
                }
            }

            // apply the effect
            ActiveEffects[index].ApplyTo(ref lowFrequencyMotor, ref highFrequencyMotor);
        }

        if (EnableHaptics)
            Gamepad.current.SetMotorSpeeds(lowFrequencyMotor, highFrequencyMotor);
        else
            Gamepad.current.SetMotorSpeeds(0, 0);
    }

    public void PlayEffect(HapticEffect effect)
    {
        InternalPlayEffect(effect, effect.Duration, effect.Blending, effect.Looping);
    }

    public void PlayEffect(HapticEffect effect, out System.Guid playingID)
    {
        playingID = InternalPlayEffect(effect, effect.Duration, effect.Blending, effect.Looping);
    }

    public void PlayEffect(HapticEffect effect, float overrideDuration)
    {
        InternalPlayEffect(effect, overrideDuration, effect.Blending, effect.Looping);
    }

    public void PlayEffect(HapticEffect effect, float overrideDuration, out System.Guid playingID)
    {
        playingID = InternalPlayEffect(effect, overrideDuration, effect.Blending, effect.Looping);
    }

    public void PlayEffect(HapticEffect effect, HapticEffect.EBlendMode overrideBlendMode)
    {
        InternalPlayEffect(effect, effect.Duration, overrideBlendMode, effect.Looping);
    }

    public void PlayEffect(HapticEffect effect, HapticEffect.EBlendMode overrideBlendMode, out System.Guid playingID)
    {
        playingID = InternalPlayEffect(effect, effect.Duration, overrideBlendMode, effect.Looping);
    }

    public void PlayEffect(HapticEffect effect, float overrideDuration, HapticEffect.EBlendMode overrideBlendMode)
    {
        InternalPlayEffect(effect, overrideDuration, overrideBlendMode, effect.Looping);
    }    

    public void PlayEffect(HapticEffect effect, float overrideDuration, HapticEffect.EBlendMode overrideBlendMode, out System.Guid playingID)
    {
        playingID = InternalPlayEffect(effect, overrideDuration, overrideBlendMode, effect.Looping);
    }

    protected System.Guid InternalPlayEffect(HapticEffect effect, float duration, HapticEffect.EBlendMode blending, bool looping)
    {
        // validate the effect
        if (!effect.Validate(duration))
        {
            Debug.LogError("Haptic effect " + effect.name + " has an infinite time but is using a curve. Effect will not play.");
            return System.Guid.Empty;
        }

        HapticEffectData effectData = new HapticEffectData(effect, duration, blending, looping);
        ActiveEffects.Add(effectData);

        // sort the effects so that any multiply ones are last
        ActiveEffects.Sort((lhs, rhs) => lhs.Blending.CompareTo(rhs.Blending));

        return effectData.PlayingID;
    }

    public void StopEffect(System.Guid playingID)
    {
        // search for and remove the effect
        for (int index = 0; index < ActiveEffects.Count; ++index)
        {
            if (ActiveEffects[index].PlayingID == playingID)
            {
                ActiveEffects.RemoveAt(index);
                return;
            }
        }
    }

    public void SetEffectDuration(System.Guid playingID, float newDuration)
    {
        // search for and remove the effect
        for (int index = 0; index < ActiveEffects.Count; ++index)
        {
            if (ActiveEffects[index].PlayingID == playingID)
            {
                ActiveEffects[index].SetDuration(newDuration);
                return;
            }
        }        
    }

    public void StopAllEffects()
    {
        if (Gamepad.current == null)
            return;

        Gamepad.current.ResetHaptics();
        ActiveEffects.Clear();
    }

    void OnDestroy()
    {
        StopAllEffects();
    }

    #region IPausable
    public bool OnPauseRequested()  { return true; }
    public bool OnResumeRequested() { return true; }

    public void OnPause()  
    { 
        if (Gamepad.current == null)
            return;

        Gamepad.current.PauseHaptics();
    }

    public void OnResume() 
    { 
        EnableHaptics = PlayerPrefs.GetInt("Controls.EnableHaptics", 1) == 1;

        if (Gamepad.current == null)
            return;

        Gamepad.current.ResumeHaptics();
    }
    #endregion       
}
