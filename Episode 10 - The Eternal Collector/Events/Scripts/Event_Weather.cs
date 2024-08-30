using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TEC/Events/Weather", fileName = "Event_Weather")]
public class Event_Weather : Event_Base, IWeatherModifier, ISolarIrradianceModifier
{
    [SerializeField] WeatherSet Weather;
    [SerializeField, Range(0.5f, 1.5f)] float IntensityMultiplier = 1f;
    [SerializeField] WeatherSet WeatherOnFinish;

    int StartWeatherCountdown = 30;

    protected override void OnStart_Internal()
    {
        StartWeatherCountdown = 30;
    }

    protected override void Tick_Internal()
    {
        if (StartWeatherCountdown > 0)
        {
            --StartWeatherCountdown;

            if (StartWeatherCountdown == 0)
                WeatherManager.Instance.RequestWeatherChange(Weather, this);
        }
    }

    protected override void OnFinish_Internal()
    {
        WeatherManager.Instance.ClearModifier();

        if (WeatherOnFinish != null)
            WeatherManager.Instance.RequestWeatherChange(WeatherOnFinish);
    }

    public float EffectIntensity(float currentIntensity)
    {
        return currentIntensity * Intensity * IntensityMultiplier;
    }

    public float EffectIrradiance(float currentIrradiance)
    {
        return currentIrradiance * WeatherManager.Instance.IrradianceModifier;
    }
}
