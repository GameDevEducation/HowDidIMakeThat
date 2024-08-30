using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;

class WeatherState
{
    public float InitialIntensity;
    public float CurrentIntensity;
    public float TargetIntensity;

    public float BaselineIntensity;
    public float FluctuationPeriod;
    public float FluctuationAmount;

    float FluctuationProgress = 0f;

    public void TickTransition(float progress)
    {
        CurrentIntensity = Mathf.Lerp(InitialIntensity, TargetIntensity, progress);
    }

    public void SetupForFluctuations()
    {
        InitialIntensity = CurrentIntensity;
        TargetIntensity = BaselineIntensity + Random.Range(-FluctuationAmount, FluctuationAmount);
        FluctuationProgress = 0f;
    }

    public void TickFluctuations()
    {
        FluctuationProgress += Time.deltaTime / FluctuationPeriod;

        // fluctuation finished
        if (FluctuationProgress >= 1f)
        {
            // set the new fluctuation time
            FluctuationProgress = 0f;

            // Update the intensities
            InitialIntensity = CurrentIntensity;
            TargetIntensity = BaselineIntensity + Random.Range(-FluctuationAmount, FluctuationAmount);
        }

        TickTransition(FluctuationProgress);
    }
}

public interface IWeatherModifier
{
    public float EffectIntensity(float currentIntensity);
}

public class WeatherManager : MonoBehaviour, ISaveLoadParticipant
{
    [SerializeField] WeatherSet DEBUG_NewWeatherSet;
    [SerializeField] WeatherSet DefaultWeather;
    [SerializeField] AnimationCurve IrradianceModifierVsEffectIntensity;
    [SerializeField] AnimationCurve LightIntensityModifierVsEffectIntensity;

    [SerializeField][Range(0f, 1f)] float RainInducedWind = 0.25f;
    [SerializeField][Range(0f, 1f)] float SnowInducedWind = 1f;
    [SerializeField][Range(0f, 1f)] float SnowInducedRain = 0.15f;

    [SerializeField] AK.Wwise.Event WindStart;
    [SerializeField] AK.Wwise.Event WindStop;
    [SerializeField] AK.Wwise.RTPC WindIntensity;

    [SerializeField] AK.Wwise.Event RainStart;
    [SerializeField] AK.Wwise.Event RainStop;
    [SerializeField] AK.Wwise.RTPC RainIntensity;

    [SerializeField] List<WeatherSet> AllKnownWeatherSets;

    public bool SwitchWeather = false;
    
    WeatherSet ActiveWeatherSet;
    float WeatherTransitionProgress = 0f;

    WeatherState Rain;
    WeatherState Hail;
    WeatherState Lightning;
    WeatherState Snow;
    WeatherState Fog;

    TrackTile CurrentPlayerTile = null;
    List<TrackTile> TrackedTiles = new List<TrackTile>();

    IWeatherModifier ActiveModifier;

    public static WeatherManager Instance { get; private set; } = null;

    public float AverageIntensity
    {
        get
        {
            if (ActiveWeatherSet == null || ActiveWeatherSet.NumActive == 0)
                return 0f;

            float intensitySum = 0f;

            if (ActiveWeatherSet.EnableRain)
                intensitySum += Rain.CurrentIntensity;
            if (ActiveWeatherSet.EnableHail)
                intensitySum += Hail.CurrentIntensity;
            if (ActiveWeatherSet.EnableSnow)
                intensitySum += Snow.CurrentIntensity;
            if (ActiveWeatherSet.EnableFog)
                intensitySum += Fog.CurrentIntensity;

            intensitySum /= ActiveWeatherSet.NumActive;

            return intensitySum;
        }
    }

    public float IrradianceModifier
    {
        get
        {
            if (ActiveWeatherSet == null || ActiveWeatherSet.NumActive == 0)
                return 1f;

            return IrradianceModifierVsEffectIntensity.Evaluate(AverageIntensity);
        }
    }

    public float LightIntensityModifier
    {
        get
        {
            if (ActiveWeatherSet == null || ActiveWeatherSet.NumActive == 0)
                return 1f;

            return LightIntensityModifierVsEffectIntensity.Evaluate(AverageIntensity);
        }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"Found duplicate WeatherManager on {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        WeatherSet weatherToStart = DefaultWeather;

        if (SaveLoadManager.Instance.LoadedState != null)
        {
            foreach(var knownWeather in AllKnownWeatherSets)
            {
                if (knownWeather.WeatherName == SaveLoadManager.Instance.LoadedState.Weather.ActiveWeather)
                {
                    weatherToStart = knownWeather;
                    break;
                }
            }
        }

        RequestWeatherChange(weatherToStart);

        RainStart.Post(gameObject);
        RainIntensity.SetValue(gameObject, 0f);

        WindStart.Post(gameObject);
        WindIntensity.SetValue(gameObject, 0f);
    }

    private void OnDestroy()
    {
        RainIntensity.SetValue(gameObject, 0f);
        RainStop.Post(gameObject);

        WindIntensity.SetValue(gameObject, 0f);
        WindStop.Post(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (EternalCollector.Instance != null && EternalCollector.Instance.CurrentPlayerTile != null)
        {
            // current player tile has changed
            if (CurrentPlayerTile == null || CurrentPlayerTile != EternalCollector.Instance.CurrentPlayerTile)
            {
                CurrentPlayerTile = EternalCollector.Instance.CurrentPlayerTile;
                RefreshRelevantTiles();
            }
        }

        if (SwitchWeather && DEBUG_NewWeatherSet != null)
        {
            SwitchWeather = false;
            RequestWeatherChange(DEBUG_NewWeatherSet);
        }

        Update_Weather();

        float windIntensity = Mathf.Clamp01(Snow.CurrentIntensity * SnowInducedWind + Rain.CurrentIntensity * RainInducedWind);
        WindIntensity.SetValue(gameObject, windIntensity * 100f);

        float rainIntensity = Mathf.Clamp01(Snow.CurrentIntensity * SnowInducedRain + Rain.CurrentIntensity);
        RainIntensity.SetValue(gameObject, rainIntensity * 100f);
    }

    void RefreshRelevantTiles()
    {
        EternalTrackGenerator.Instance.GetTilesNear(CurrentPlayerTile, 1, TrackedTiles);
    }

    void Update_Weather()
    {
        if (ActiveWeatherSet == null)
        {
            // only update the sky if nothing is active
            foreach (var tile in TrackedTiles)
                UpdateSky(tile, 0f);

            return;
        }

        // transitioning to new weather
        if (WeatherTransitionProgress < 1f)
        {
            WeatherTransitionProgress += Time.deltaTime / ActiveWeatherSet.FadeInTime;

            Rain.TickTransition(WeatherTransitionProgress);
            Hail.TickTransition(WeatherTransitionProgress);
            Lightning.TickTransition(WeatherTransitionProgress);
            Snow.TickTransition(WeatherTransitionProgress);
            Fog.TickTransition(WeatherTransitionProgress);

            if (WeatherTransitionProgress >= 1f)
            {
                if (ActiveWeatherSet.EnableRain)
                    Rain.SetupForFluctuations();
                if (ActiveWeatherSet.EnableHail)
                    Hail.SetupForFluctuations();
                if (ActiveWeatherSet.EnableLightning)
                    Lightning.SetupForFluctuations();
                if (ActiveWeatherSet.EnableSnow)
                    Snow.SetupForFluctuations();
                if (ActiveWeatherSet.EnableFog)
                    Fog.SetupForFluctuations();
            }
        }
        else
        {
            if (ActiveWeatherSet.EnableRain)
                Rain.TickFluctuations();
            if (ActiveWeatherSet.EnableHail)
                Hail.TickFluctuations();
            if (ActiveWeatherSet.EnableLightning)
                Lightning.TickFluctuations();
            if (ActiveWeatherSet.EnableSnow)
                Snow.TickFluctuations();
            if (ActiveWeatherSet.EnableFog)
                Fog.TickFluctuations();
        }

        // update all of the effects
        foreach(var tile in TrackedTiles)
        {
            UpdateVFXSet(tile.RainVFX, ActiveModifier != null ? ActiveModifier.EffectIntensity(Rain.CurrentIntensity) : Rain.CurrentIntensity);
            UpdateVFXSet(tile.HailVFX, ActiveModifier != null ? ActiveModifier.EffectIntensity(Hail.CurrentIntensity) : Hail.CurrentIntensity);
            UpdateVFXSet(tile.SnowVFX, ActiveModifier != null ? ActiveModifier.EffectIntensity(Snow.CurrentIntensity) : Snow.CurrentIntensity);
            UpdateVolumes(tile.FogVolume, ActiveModifier != null ? ActiveModifier.EffectIntensity(Fog.CurrentIntensity) : Fog.CurrentIntensity);
            UpdateSky(tile, ActiveModifier != null ? ActiveModifier.EffectIntensity(AverageIntensity) : AverageIntensity);
        }
        LightningGenerator.Instance.SetIntensity(ActiveModifier != null ? ActiveModifier.EffectIntensity(Lightning.CurrentIntensity) : Lightning.CurrentIntensity);
    }

    void UpdateSky(TrackTile tile, float intensity)
    {
        // update the clear skies volume
        tile.ClearSkiesVolume.enabled = intensity < 1f;
        tile.ClearSkiesVolume.weight = 1f - intensity;

        // update the other types of skies
        if (ActiveWeatherSet != null && ActiveWeatherSet.Sky == WeatherSet.ESky.Stormy)
        {
            tile.StormyVolume.enabled = intensity > 0f;
            tile.StormyVolume.weight = intensity;
        }
        else
            tile.StormyVolume.enabled = false;
        if (ActiveWeatherSet != null && ActiveWeatherSet.Sky == WeatherSet.ESky.Cloudy)
        {
            tile.CloudyVolume.enabled = intensity > 0f;
            tile.CloudyVolume.weight = intensity;
        }
        else
            tile.CloudyVolume.enabled = false;
        if (ActiveWeatherSet != null && ActiveWeatherSet.Sky == WeatherSet.ESky.Overcast)
        {
            tile.OvercastVolume.enabled = intensity > 0f;
            tile.OvercastVolume.weight = intensity;
        }
        else
            tile.OvercastVolume.enabled = false;
    }

    void UpdateVFXSet(VisualEffect effect, float intensity)
    {
        if (intensity > 0)
        {
            effect.enabled = true;
            effect.SetFloat("Intensity", intensity);
        }
        else
            effect.enabled = false;
    }

    void UpdateVolumes(Volume volume, float intensity)
    {
        if (intensity > 0)
        {
            volume.enabled = true;
            volume.weight = intensity;
        }
        else
            volume.enabled = false;
    }

    public void ClearModifier()
    {
        ActiveModifier = null;
    }

    public void RequestWeatherChange(WeatherSet newWeatherSet, IWeatherModifier intensityModifier = null)
    {
        ActiveModifier = intensityModifier;

        if (newWeatherSet == ActiveWeatherSet)
            return;

        ActiveWeatherSet = newWeatherSet;
        WeatherTransitionProgress = 0f;

        float currentIntensity_Rain         = Rain != null ? Rain.CurrentIntensity : 0f;
        float currentIntensity_Hail         = Hail != null ? Hail.CurrentIntensity : 0f;
        float currentIntensity_Lightning    = Lightning != null ? Lightning.CurrentIntensity : 0f;
        float currentIntensity_Snow         = Snow != null ? Snow.CurrentIntensity : 0f;
        float currentIntensity_Fog          = Fog != null ? Fog.CurrentIntensity : 0f;

        Rain = new WeatherState() {         FluctuationPeriod = ActiveWeatherSet.RainFluctuationPeriod, 
                                            FluctuationAmount = ActiveWeatherSet.RainFluctuation,
                                            BaselineIntensity = ActiveWeatherSet.RainIntensity};
        Hail = new WeatherState() {         FluctuationPeriod = ActiveWeatherSet.HailFluctuationPeriod, 
                                            FluctuationAmount = ActiveWeatherSet.HailFluctuation,
                                            BaselineIntensity = ActiveWeatherSet.HailIntensity};
        Lightning = new WeatherState() {    FluctuationPeriod = ActiveWeatherSet.LightningFluctuationPeriod, 
                                            FluctuationAmount = ActiveWeatherSet.LightningFluctuation,
                                            BaselineIntensity = ActiveWeatherSet.LightningIntensity};
        Snow = new WeatherState() {         FluctuationPeriod = ActiveWeatherSet.SnowFluctuationPeriod, 
                                            FluctuationAmount = ActiveWeatherSet.SnowFluctuation,
                                            BaselineIntensity = ActiveWeatherSet.SnowIntensity};
        Fog = new WeatherState() {          FluctuationPeriod = ActiveWeatherSet.FogFluctuationPeriod, 
                                            FluctuationAmount = ActiveWeatherSet.FogFluctuation,
                                            BaselineIntensity = ActiveWeatherSet.FogIntensity};

        Rain.InitialIntensity       = currentIntensity_Rain;
        Hail.InitialIntensity       = currentIntensity_Hail;
        Lightning.InitialIntensity  = currentIntensity_Lightning;
        Snow.InitialIntensity       = currentIntensity_Snow;
        Fog.InitialIntensity        = currentIntensity_Fog;

        Rain.TargetIntensity        = ActiveWeatherSet.EnableRain       ? ActiveWeatherSet.RainIntensity        : 0f;
        Hail.TargetIntensity        = ActiveWeatherSet.EnableHail       ? ActiveWeatherSet.HailIntensity        : 0f;
        Lightning.TargetIntensity   = ActiveWeatherSet.EnableLightning  ? ActiveWeatherSet.LightningIntensity   : 0f;
        Snow.TargetIntensity        = ActiveWeatherSet.EnableSnow       ? ActiveWeatherSet.SnowIntensity        : 0f;
        Fog.TargetIntensity         = ActiveWeatherSet.EnableFog        ? ActiveWeatherSet.FogIntensity         : 0f;
    }

    public void PrepareForSave(SavedGameState savedGame)
    {
        savedGame.Weather.ActiveWeather = ActiveWeatherSet != null ? ActiveWeatherSet.WeatherName : string.Empty;
    }
}
