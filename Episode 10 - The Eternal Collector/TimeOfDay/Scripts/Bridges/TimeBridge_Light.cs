using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class TimeBridge_Light : TimeBridge_Base
{
    [SerializeField] Transform SunAndMoonRoot;
    [SerializeField] Light LinkedSun;
    [SerializeField] Light LinkedMoon;
    [SerializeField] float SunriseTime = 6f;
    [SerializeField] float SunsetTime = 18f;
    [SerializeField] float MaxSolarIrradiance_Day = 1000f;
    [SerializeField] float MaxSolarIrradiance_Night = 10f;
    [SerializeField] AnimationCurve SolarIrradianceVsTime_Day;
    [SerializeField] AnimationCurve SolarIrradianceVsTime_Night;

    public static float SolarIrradiance { get; private set; } = 0f;

    float SolarIrradianceSeasonModifier = 1f;

    float DaylightLength;
    float NightLength;
    float BaseLightIntensity;
    float SeasonTimeSkew;

    float EffectiveSunriseTime => SunriseTime + SeasonTimeSkew;
    float EffectiveSunsetTime => SunsetTime - SeasonTimeSkew;

    public bool IsDayTime => ToDManager.Instance.CurrentTime >= EffectiveSunriseTime && ToDManager.Instance.CurrentTime <= EffectiveSunsetTime;
    float MaxSolarIrradiance => IsDayTime ? MaxSolarIrradiance_Day : MaxSolarIrradiance_Night;
    AnimationCurve SolarIrradianceVsTime => IsDayTime ? SolarIrradianceVsTime_Day : SolarIrradianceVsTime_Night;

    private HDAdditionalLightData LinkedSunData;
    private HDAdditionalLightData LinkedMoonData;

    private void Awake()
    {
        LinkedSunData = LinkedSun.GetComponent<HDAdditionalLightData>();
        LinkedMoonData = LinkedMoon.GetComponent<HDAdditionalLightData>();
        LinkedMoonData.EnableShadows(false);
        BaseLightIntensity = LinkedSunData.intensity;
    }

    void Start()
    {
    }

    public void UpdateSeasonSolarIrradianceModifier(float newValue)
    {
        SolarIrradianceSeasonModifier = newValue;
        LinkedSunData.intensity = BaseLightIntensity * newValue * WeatherManager.Instance.LightIntensityModifier;
    }

    public void UpdateSeasonTimeSkew(float newValue)
    {
        SeasonTimeSkew = newValue;
    }

    public override void OnTick(float CurrentTime)
    {
        if (CurrentTime >= EffectiveSunriseTime && CurrentTime <= EffectiveSunsetTime)
        {
            float dayProgress = Mathf.InverseLerp(EffectiveSunriseTime, EffectiveSunsetTime, CurrentTime);
            SolarIrradiance = MaxSolarIrradiance * SolarIrradianceVsTime.Evaluate(dayProgress);
            SolarIrradiance *= SolarIrradianceSeasonModifier;
            LinkedMoonData.EnableShadows(false);
            LinkedSunData.EnableShadows(true);
        }
        else
        {
            LinkedSunData.EnableShadows(false);
            LinkedMoonData.EnableShadows(true);
            SolarIrradiance = MaxSolarIrradiance * SolarIrradianceSeasonModifier;
        }

        DaylightLength = EffectiveSunsetTime - EffectiveSunriseTime;
        NightLength = EffectiveSunriseTime + (ToDManager.Instance.DayLength - EffectiveSunsetTime);

        float requiredAngle = 0f;
        if (CurrentTime < EffectiveSunriseTime) // pre-dawn
            requiredAngle = Mathf.Lerp(180f, 360f, (CurrentTime + (ToDManager.Instance.DayLength - EffectiveSunsetTime)) / NightLength);
        else if (CurrentTime < EffectiveSunsetTime) // during the day
        {
            requiredAngle = Mathf.Lerp(0f, 180f, (CurrentTime - EffectiveSunriseTime) / DaylightLength);
        }
        else // post sunset
            requiredAngle = Mathf.Lerp(180f, 360f, (CurrentTime - EffectiveSunsetTime) / NightLength);

        SunAndMoonRoot.rotation = Quaternion.Euler(requiredAngle, 0f, 0f);
    }
}
