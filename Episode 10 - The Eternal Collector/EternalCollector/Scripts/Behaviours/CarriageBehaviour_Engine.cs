using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[System.Serializable]
public class EngineSoundConfig
{
    public AK.Wwise.Event StartEvent;
    public AK.Wwise.Event StopEvent;
    public AK.Wwise.RTPC IntensityRTPC;

    public void StartAudio(List<GameObject> emitters)
    {
        if (StartEvent.IsValid())
        {
            foreach (var emitter in emitters)
                StartEvent.Post(emitter);
        }
    }

    public void StopAudio(List<GameObject> emitters)
    {
        if (StopEvent.IsValid())
        {
            foreach (var emitter in emitters)
                StopEvent.Post(emitter);
        }
    }

    public void SetIntensity(List<GameObject> emitters, float intensity)
    {
        if (IntensityRTPC.IsValid())
        {
            foreach (var emitter in emitters)
                IntensityRTPC.SetValue(emitter, intensity);
        }
    }
}

[CreateAssetMenu(menuName = "TEC/Carriage Behaviours/Engine", fileName = "CarriageBehaviour_Engine")]
public class CarriageBehaviour_Engine : BaseCarriageBehaviour
{
    [SerializeField] float BaseEnginePower = 4000;
    [SerializeField] [Range(0f, 1f)] float BaseEngineEfficiency = 0.5f;

    [SerializeField] EngineSoundConfig LargeEngineAudio;
    [SerializeField] EngineSoundConfig SmallEngineAudio;
    [SerializeField] CarriageUpgrade UpgradeRequiredForLargeAudio;

    EngineSoundConfig ActiveSoundConfig => UsingLargeEngines ? LargeEngineAudio : SmallEngineAudio;

    bool UsingLargeEngines = false;

    List<VisualEffect> CachedEffects;
    List<GameObject> Emitters;

    float WorkingEngineEfficiency;
    float LastSpeedFactor = -1f;

    protected override void PostBind()
    {
        CachedEffects = LinkedCarriage.GetVFX(TemplateBehaviour);
        Emitters = new List<GameObject>(CachedEffects.Count);

        StartEngineAudio();
    }

    public override void OnDestroyCarriage()
    {
        base.OnDestroyCarriage();

        StopEngineAudio();
    }

    void StartEngineAudio()
    {
        foreach (var effect in CachedEffects)
        {
            if (effect.gameObject.activeSelf)
                Emitters.Add(effect.gameObject);
        }

        ActiveSoundConfig.SetIntensity(Emitters, EternalCollector.Instance.EffectiveSpeedFactor * 100f);
        ActiveSoundConfig.StartAudio(Emitters);
    }

    void StopEngineAudio()
    {
        ActiveSoundConfig.StopAudio(Emitters);
        CachedEffects.Clear();
    }

    public override void RefreshUpgrades()
    {
        _EnginePower = BaseEnginePower;
        WorkingEngineEfficiency = BaseEngineEfficiency;

        foreach (var upgrade in EternalCollector.Instance.UnlockedUpgrades)
        {
            if (!upgrade.ApplicableBehaviours.Contains(TemplateBehaviour))
                continue;

            if (upgrade == UpgradeRequiredForLargeAudio && !UsingLargeEngines)
            {
                if (!UsingLargeEngines)
                    StopEngineAudio();

                UsingLargeEngines = true;
                StartEngineAudio();
            }

            foreach (var effect in upgrade.Effects)
            {
                if (effect.Target == EUpgradeTarget.Engine_Power)
                    effect.Modify(ref _EnginePower);
                else if (effect.Target == EUpgradeTarget.Engine_Efficiency)
                    effect.Modify(ref WorkingEngineEfficiency);
            }
        }

        base.RefreshUpgrades();
    }

    public override void Tick_Power()
    {
        base.Tick_Power();

        // determine the amount of power needed
        float powerRequested = _EnginePower * EternalCollector.Instance.RequestedSpeedFactor / WorkingEngineEfficiency;
        powerRequested *= ToDManager.Instance.DeltaTime;
        powerRequested /= 3600f; // convert to kWH

        // request the amount of power needed
        EternalCollector.Instance.RequestPower(powerRequested);
    }

    public override void Tick_General()
    {
        ActiveSoundConfig.SetIntensity(Emitters, EternalCollector.Instance.RequestedSpeedFactor * 100f);

        // synchronise the visual effects if the speed has changed
        if (EternalCollector.Instance.EffectiveSpeedFactor != LastSpeedFactor)
        {
            LastSpeedFactor = EternalCollector.Instance.EffectiveSpeedFactor;

            for (int index = 0; index < CachedEffects.Count; index++)
                CachedEffects[index].SetFloat("Speed", LastSpeedFactor);
        }
    }
}

