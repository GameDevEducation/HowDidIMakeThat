using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TEC/Weather Set", fileName = "Weather Set")]
public class WeatherSet : ScriptableObject
{
    public enum ESky
    {
        Clear,
        Cloudy,
        Overcast,
        Stormy
    }

    public string WeatherName;

    public float FadeInTime = 10f;
    public ESky Sky = ESky.Clear;

    public bool EnableRain = false;
    [ConditionalField("EnableRain"), Range(0f, 1f)] public float RainIntensity = 0.5f;
    [ConditionalField("EnableRain"), Range(0f, 1f)] public float RainFluctuation = 0.1f;
    [ConditionalField("EnableRain")] public float RainFluctuationPeriod = 30f;

    public bool EnableHail = false;
    [ConditionalField("EnableHail"), Range(0f, 1f)] public float HailIntensity = 0.5f;
    [ConditionalField("EnableHail"), Range(0f, 1f)] public float HailFluctuation = 0.1f;
    [ConditionalField("EnableHail")] public float HailFluctuationPeriod = 30f;

    public bool EnableLightning = false;
    [ConditionalField("EnableLightning"), Range(0f, 1f)] public float LightningIntensity = 0.5f;
    [ConditionalField("EnableLightning"), Range(0f, 1f)] public float LightningFluctuation = 0.1f;
    [ConditionalField("EnableLightning")] public float LightningFluctuationPeriod = 30f;

    public bool EnableSnow = false;
    [ConditionalField("EnableSnow"), Range(0f, 1f)] public float SnowIntensity = 0.5f;
    [ConditionalField("EnableSnow"), Range(0f, 1f)] public float SnowFluctuation = 0.1f;
    [ConditionalField("EnableSnow")] public float SnowFluctuationPeriod = 30f;

    public bool EnableFog = false;
    [ConditionalField("EnableFog"), Range(0f, 1f)] public float FogIntensity = 0.5f;
    [ConditionalField("EnableFog"), Range(0f, 1f)] public float FogFluctuation = 0.1f;
    [ConditionalField("EnableFog")] public float FogFluctuationPeriod = 30f;

    public int NumActive => (EnableFog ? 1 : 0) + (EnableRain ? 1 : 0) + (EnableSnow ? 1 : 0) + (EnableHail ? 1 : 0);
}
