using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TEC/Events/Resources", fileName = "Event_Resources")]
public class Event_Resources : Event_Base, IResourceModifier, ISolarIrradianceModifier
{
    [SerializeField] bool EffectsScrap;
    [SerializeField] bool EffectsSolarIrradiance;

    [Header("Scrap Controls")]
    [SerializeField, Range(0.5f, 2f)] protected float ResourceQuantityMultiplier = 1f;
    [SerializeField, Range(0.5f, 2f)] protected float ResourceDensityMultiplier = 1f;

    [Header("Solar Irradiance Controls")]
    [SerializeField, Range(0.5f, 2f)] protected float LightIntensityMultiplier = 1f;

    float WorkingResourceQuantityMultiplier;
    float WorkingResourceDensityMultiplier;
    float WorkingLightIntensityMultiplier;

    protected override void OnStart_Internal()
    {
        WorkingResourceQuantityMultiplier = EffectsScrap ? ResourceQuantityMultiplier * Variation : 1f;
        WorkingResourceDensityMultiplier = EffectsScrap ? ResourceDensityMultiplier * Variation : 1f;
        WorkingLightIntensityMultiplier = EffectsSolarIrradiance ? LightIntensityMultiplier * Variation : 1f;

        EternalCollector.RegisterModifier(this as IResourceModifier);
        EternalCollector.RegisterModifier(this as ISolarIrradianceModifier);
    }

    protected override void Tick_Internal()
    {
    }

    protected override void OnFinish_Internal()
    {
        EternalCollector.DeregisterModifier(this as IResourceModifier);
        EternalCollector.DeregisterModifier(this as ISolarIrradianceModifier);
    }

    public int EffectAmountToSpawn(int currentAmount)
    {
        if (!EffectsScrap)
            return currentAmount;

        return Mathf.RoundToInt(currentAmount * Mathf.Lerp(1f, WorkingResourceQuantityMultiplier, Intensity));
    }

    public float EffectScrapWeight(float currentScrapWeight)
    {
        if (!EffectsScrap)
            return currentScrapWeight;

        return currentScrapWeight * Mathf.Lerp(1f, WorkingResourceDensityMultiplier, Intensity);
    }

    public float EffectIrradiance(float currentIrradiance)
    {
        if (!EffectsSolarIrradiance)
            return currentIrradiance;

        return currentIrradiance * Mathf.Lerp(1f, WorkingLightIntensityMultiplier, Intensity);
    }
}
