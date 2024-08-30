using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseCarriageBehaviour : ScriptableObject
{
    [SerializeField] EUIScreen _CarriageUIType = EUIScreen.None;
    [SerializeField] float BaseWeight = 1000f;
    [SerializeField] float BasePowerUsage = 0f;
    [SerializeField] AK.Wwise.Event StartAmbientAudio;
    [SerializeField] AK.Wwise.Event StopAmbientAudio;
    [SerializeField] AK.Wwise.RTPC AmbientAudioRTPC;

    public string Category;
    public string DisplayName;
    public int Cost;
    [TextArea(3, 5)] public string Description;

    protected Carriage LinkedCarriage;
    protected BaseCarriageBehaviour TemplateBehaviour;
    public TrackTile CurrentTile { get; private set; }

    protected float _ScrapStorageAvailable = 0f;
    protected float _ScrapStorageUsed = 0f;
    protected float _EnginePower = 0f;
    protected float _Weight = 0f;
    protected float _EnergyStorageAvailable = 0f;
    protected float _EnergyStorageUsed = 0f;
    protected float _PowerUsage = 0f;

    public bool IsHead => LinkedCarriage.IsHead;
    public bool IsBody => LinkedCarriage.IsBody;
    public bool IsTail => LinkedCarriage.IsTail;

    public float ScrapStorageAvailable => _ScrapStorageAvailable;
    public float ScrapStorageUsed => _ScrapStorageUsed;
    public float EnginePower => _EnginePower;
    public float Weight => _Weight;
    public float EnergyStorageCapacity => _EnergyStorageAvailable;
    public float EnergyPowerCapacity => _EnergyStorageUsed;
    public float PowerUsage => _PowerUsage;
    public EUIScreen CarriageUIType => _CarriageUIType;
    public BaseCarriageBehaviour Template => TemplateBehaviour;

    public void Bind(Carriage _LinkedCarriage, BaseCarriageBehaviour _TemplateBehaviour)
    {
        LinkedCarriage = _LinkedCarriage;
        TemplateBehaviour = _TemplateBehaviour;

        PostBind();
    }

    protected virtual void PostBind()
    {

    }

    public void SetCurrentTile(TrackTile tile)
    {
        CurrentTile = tile;
    }

    public virtual void SetScrapStorageUsed(float newStorageUsed)
    {
        _ScrapStorageUsed = newStorageUsed;
    }

    public virtual void SetEnergyStorageUsed(float newStorageUsed)
    {
        _EnergyStorageUsed = newStorageUsed;
    }

    public virtual void RefreshUpgrades()
    {
        _Weight = BaseWeight;
        _PowerUsage = BasePowerUsage;

        foreach (var upgrade in EternalCollector.Instance.UnlockedUpgrades)
        {
            if (!upgrade.ApplicableBehaviours.Contains(TemplateBehaviour))
                continue;

            foreach (var effect in upgrade.Effects)
            {
                if (effect.Target == EUpgradeTarget.Common_Weight)
                    effect.Modify(ref _Weight);
                else if (effect.Target == EUpgradeTarget.Common_PowerUsage)
                    effect.Modify(ref _PowerUsage);
            }
        }
    }

    public virtual void Tick_Power()
    {
        // determine the amount of power needed
        float powerRequested = _PowerUsage;
        powerRequested *= ToDManager.Instance.DeltaTime;
        powerRequested /= 3600f; // convert to kWH

        // request the amount of power needed
        EternalCollector.Instance.RequestPower(powerRequested);
    }

    public virtual void OnDestroyCarriage()
    {
        if (StopAmbientAudio.IsValid())
            StopAmbientAudio.Post(LinkedCarriage.gameObject);
    }

    protected virtual float GetAmbientAudioIntensity()
    {
        return 50;
    }

    public virtual void Tick_General()
    {
        if (EternalCollector.Instance.CurrentPlayerCarriage == LinkedCarriage)
        {
            if (AmbientAudioRTPC.IsValid())
                AmbientAudioRTPC.SetValue(LinkedCarriage.gameObject, GetAmbientAudioIntensity());
        }
    }

    public void OnPlayerEntered()
    {
        if (StartAmbientAudio.IsValid())
            StartAmbientAudio.Post(LinkedCarriage.gameObject);
    }

    public void OnPlayerLeft()
    {
        if (StopAmbientAudio.IsValid())
            StopAmbientAudio.Post(LinkedCarriage.gameObject);
    }
}
