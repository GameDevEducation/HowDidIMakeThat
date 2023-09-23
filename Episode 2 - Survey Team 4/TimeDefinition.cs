using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Injaia
{
    [System.Serializable]
    public class LightBehaviour
    {
        public string TargetLightName;
        protected Light TargetLight;
        
        [Header("Intensity")]
        public bool EnableIntensityChanges = false;
        public float MinIntensity = 0f;
        public float MaxIntensity = 1f;
        public AnimationCurve IntensityVsNormalisedTime;

        [Header("Colour")]
        public bool EnableColourChanges = false;
        public Color MinColour = Color.black;
        public Color MaxColour = Color.white;
        public AnimationCurve ColourVsNormalisedTime;

        [Header("Position")]
        public bool EnablePositionChanges = false;
        public Vector3 MinPosition;
        public Vector3 MaxPosition;
        public AnimationCurve PositionVsNormalisedTime;

        [Header("Rotation")]
        public bool EnableRotationChanges = false;
        public Vector3 MinRotation;
        public Vector3 MaxRotation;
        public AnimationCurve RotationVsNormalisedTime;

        public void ApplySettings(float normalisedTime)
        {
            // no target light?
            if (TargetLight == null)
            {
                Debug.LogError("Light " + TargetLightName + " not linked!");
                return;
            }

            // intensity changes?
            if (EnableIntensityChanges)
            {
                TargetLight.intensity = Mathf.Lerp(MinIntensity, MaxIntensity, IntensityVsNormalisedTime.Evaluate(normalisedTime));
            }

            // colour changes?
            if (EnableColourChanges)
            {
                TargetLight.color = Color.Lerp(MinColour, MaxColour, ColourVsNormalisedTime.Evaluate(normalisedTime));
            }

            // position changes?
            if (EnablePositionChanges)
            {
                TargetLight.transform.position = Vector3.Lerp(MinPosition, MaxPosition, PositionVsNormalisedTime.Evaluate(normalisedTime));
            }

            // rotation changes?
            if (EnableRotationChanges)
            {
                float progress = RotationVsNormalisedTime.Evaluate(normalisedTime);
                
                TargetLight.transform.rotation = Quaternion.Euler(Mathf.Lerp(MinRotation.x, MaxRotation.x, progress),
                                                                  Mathf.Lerp(MinRotation.y, MaxRotation.y, progress),
                                                                  Mathf.Lerp(MinRotation.z, MaxRotation.z, progress));
            }
        }

        public void Reset()
        {
            if (MinRotation.x < 0)
                MinRotation.x += 360f;
            if (MinRotation.y < 0)
                MinRotation.y += 360f;
            if (MinRotation.z < 0)
                MinRotation.z += 360f;

            if (MaxRotation.x < MinRotation.x)
                MaxRotation.x += 360f;
            if (MaxRotation.y < MinRotation.y)
                MaxRotation.y += 360f;
            if (MaxRotation.z < MinRotation.z)
                MaxRotation.z += 360f;

            TargetLight = GameObject.Find(TargetLightName).GetComponent<Light>();
        }
    }

    [System.Serializable]
    public class AmbientLightBehaviour
    {
        [Header("Sky Colour")]
        public bool EnableSkyColourChanges;
        public Color MinSkyColour = Color.black;
        public Color MaxSkyColour = Color.white;
        public AnimationCurve SkyColourVsNormalisedTime;

        [Header("Equator Colour")]
        public bool EnableEquatorColourChanges;
        public Color MinEquatorColour = Color.black;
        public Color MaxEquatorColour = Color.white;
        public AnimationCurve EquatorColourVsNormalisedTime;

        [Header("Ground Colour")]
        public bool EnableGroundColourChanges;
        public Color MinGroundColour = Color.black;
        public Color MaxGroundColour = Color.white;
        public AnimationCurve GroundColourVsNormalisedTime;

        [Header("Ambient Colour")]
        public bool EnableAmbientColourChanges;
        public Color MinAmbientColour = Color.black;
        public Color MaxAmbientColour = Color.white;
        public AnimationCurve AmbientColourVsNormalisedTime;

        [Header("Fog Colour")]
        public bool EnableFogColourChanges;
        public Color MinFogColour = Color.black;
        public Color MaxFogColour = Color.white;
        public AnimationCurve FogColourVsNormalisedTime;

        public void ApplySettings(float normalisedTime)
        {
            // sky colour changes?
            if (EnableSkyColourChanges)
            {
                RenderSettings.ambientSkyColor = Color.Lerp(MinSkyColour, MaxSkyColour, SkyColourVsNormalisedTime.Evaluate(normalisedTime));
            }
            
            // equator colour changes?
            if (EnableEquatorColourChanges)
            {
                RenderSettings.ambientEquatorColor = Color.Lerp(MinEquatorColour, MaxEquatorColour, GroundColourVsNormalisedTime.Evaluate(normalisedTime));
            }
            
            // ground colour changes?
            if (EnableGroundColourChanges)
            {
                RenderSettings.ambientGroundColor = Color.Lerp(MinGroundColour, MaxGroundColour, GroundColourVsNormalisedTime.Evaluate(normalisedTime));
            }
            
            // ambient colour changes?
            if (EnableAmbientColourChanges)
            {
                RenderSettings.ambientLight = Color.Lerp(MinAmbientColour, MaxAmbientColour, AmbientColourVsNormalisedTime.Evaluate(normalisedTime));
            }
            
            // ambient colour changes?
            if (EnableFogColourChanges)
            {
                RenderSettings.fogColor = Color.Lerp(MinFogColour, MaxFogColour, FogColourVsNormalisedTime.Evaluate(normalisedTime));
            }            
        }

        public void Reset()
        {

        }
    }

    [CreateAssetMenu(fileName = "TimeDefinition", menuName = "Injaia/Time Definition", order = 1)]
    public class TimeDefinition : ScriptableObject
    {
        [Header("Basics")]
        public string Name;
        public TimeDefinition NextTimePeriod;

        [Header("Timing")]
        public float Duration = 300f;
        public float TimeScale_OnPath = 1f;
        public float TimeScale_OffPath = 1f;
        protected float RelativeTime;

        [Header("Lights")]
        public List<LightBehaviour> IndividualLightSetup;

        [Header("Ambients")]
        public AmbientLightBehaviour AmbientLightSetup;

        [Header("Azure Sky Interface")]
        public bool EnableAzureSky = true;
        public float AzureStartTime;
        public float AzureEndTime;
        protected float AzureTimeRange;
        protected UnityEngine.AzureSky.AzureSkyManager AzureManager;

        [Header("Deathclock Interface")]
        public bool HasDeathclock = false;

        public float NormalisedTime
        {
            get
            {
                return Mathf.Clamp01(RelativeTime / Duration);
            }
        }

        public void Reset()
        {
            RelativeTime = 0;

            AmbientLightSetup?.Reset();

            foreach(LightBehaviour light in IndividualLightSetup)
            {
                light.Reset();
            }

            if (EnableAzureSky)
            {
                // determine the time range
                AzureTimeRange = AzureEndTime - AzureStartTime;
                if (AzureTimeRange < 0)
                    AzureTimeRange += 24;

                AzureManager = FindObjectOfType<UnityEngine.AzureSky.AzureSkyManager>();
            }
        }

        public void AdvanceTime(float amount, bool isOnPath)
        {
            // update the time
            RelativeTime += amount * (isOnPath ? TimeScale_OnPath : TimeScale_OffPath);

            // get the normalised time and update all linked items
            float normalisedTime = NormalisedTime;

            // apply the new settings
            AmbientLightSetup?.ApplySettings(normalisedTime);

            foreach(LightBehaviour light in IndividualLightSetup)
            {
                light.ApplySettings(normalisedTime);
            }

            if (EnableAzureSky)
            {
                // determine the azure time time
                float newAzureTime = AzureStartTime + (normalisedTime * AzureTimeRange);
                if (newAzureTime > 24)
                    newAzureTime -= 24;

                AzureManager.timeController.timeline = newAzureTime;
            }
        }
    }
}