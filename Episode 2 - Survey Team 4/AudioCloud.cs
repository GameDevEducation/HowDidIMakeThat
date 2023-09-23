using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioCloud : MonoBehaviour
{
    [Header("Cloud Config")]
    public Vector3 CloudCentre;
    public float LayerHeightDelta = 1f;
    
    [Header("Layer Config")]
    public int NumEmittersPerLayer = 6;
    public float EmitterDistance = 2f;

    protected List<GameObject> LayerAnchors = new List<GameObject>();
    protected List<List<GameObject>> Emitters = new List<List<GameObject>>();

    protected int NumLayers = 6;

    [Header("Day Config")]
    public float RotationSpeed_Day = 15f;
    public float AvgDistance_Day = 15f;
    public float DistanceAmplitude_Day = 3f;
    public float DistancePeriod_Day = 30f;
    public AnimationCurve Intensity_Day;
    public float MinTriggerTime_Day = 1f;
    public float MaxTriggerTime_Day = 4f;
    public List<string> LayerEvents_Day;

    [Header("Night Config")]
    public float RotationSpeed_Night = 45f;
    public float AvgDistance_Night = 7.5f;
    public float DistanceAmplitude_Night = 5f;
    public float DistancePeriod_Night = 10f;
    public AnimationCurve Intensity_Night;
    public float MinTriggerTime_Night = 0.5f;
    public float MaxTriggerTime_Night = 2f;
    public List<string> LayerEvents_Night;

    protected float CurrentTime;
    public bool IsDayTime = true;

    [Header("Behaviour")]
    public Injaia.TimeKeeper Keeper;
    protected float NextTriggerTime = -1f;

    protected float DeathclockIntensity = 1f;

    protected List<string> LayerEvents
    {
        get { return IsDayTime ? LayerEvents_Day : LayerEvents_Night; }
    }

    protected AnimationCurve IntensityCurve
    {
        get { return IsDayTime ? Intensity_Day : Intensity_Night; }
    }

    protected float TriggerTime
    {
        get { return IsDayTime ? Random.Range(MinTriggerTime_Day, MinTriggerTime_Night) : Random.Range(MinTriggerTime_Night, MaxTriggerTime_Night); }
    }

    protected float RotationSpeed
    {
        get { return IsDayTime ? RotationSpeed_Day : RotationSpeed_Night; }
    }

    protected float AvgDistance
    {
        get { return IsDayTime ? AvgDistance_Day : AvgDistance_Night; }
    }

    protected float DistanceAmplitude
    {
        get { return IsDayTime ? DistanceAmplitude_Day : DistanceAmplitude_Night; }
    }

    protected float DistancePeriod
    {
        get { return IsDayTime ? DistancePeriod_Day : DistancePeriod_Night; }
    }

    public void SetIsNight(bool isNight)
    {
        IsDayTime = !isNight;
    }

    public void OnDeathclockTicked(float progress)
    {
        DeathclockIntensity = progress;
    }

    public void OnDeathclockReset()
    {
        DeathclockIntensity = 1f;
    }

    // Start is called before the first frame update
    void Start()
    {
        float emitterArc = (Mathf.PI * 2f) / NumEmittersPerLayer;

        // spawn the layers
        for (int layer = 0; layer < NumLayers; ++layer)
        {
            int relativeLayer = layer - (NumLayers / 2);

            // spawn the base layer object
            Vector3 layerCentre = CloudCentre + Vector3.up * (LayerHeightDelta * relativeLayer);

            // spawn the layer anchor and position it
            GameObject layerAnchor = new GameObject("Layer " + (layer + 1).ToString());
            layerAnchor.transform.parent = transform;
            layerAnchor.transform.localPosition = layerCentre;
            LayerAnchors.Add(layerAnchor);

            float workingDistance = EmitterDistance;

            // spawn the emitters
            List<GameObject> emittersForLayer = new List<GameObject>();
            for (int emitterIndex = 0; emitterIndex < NumEmittersPerLayer; ++emitterIndex)
            {
                GameObject emitter = new GameObject("Emitter " + (emitterIndex + 1).ToString());

                emitter.transform.parent = layerAnchor.transform;
                emitter.transform.localPosition = layerCentre + workingDistance * Vector3.forward * Mathf.Sin(emitterArc * emitterIndex)
                                                              + workingDistance * Vector3.right * Mathf.Cos(emitterArc * emitterIndex);

                emittersForLayer.Add(emitter);
                // GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                // debugSphere.transform.parent = emitter.transform;
                // debugSphere.transform.localPosition = Vector3.zero;
            }

            Emitters.Add(emittersForLayer);
        }

        CurrentTime = 0;
    }

    // Update is called once per frame
    void Update()
    {
        CurrentTime += Time.deltaTime;

        // update the layers
        bool clockwise = true;
        foreach(GameObject layerAnchor in LayerAnchors)
        {
            // update the rotation
            Vector3 currentEulerAngles = layerAnchor.transform.localEulerAngles;
            currentEulerAngles.y += (clockwise ? 1f : -1f) * RotationSpeed * Time.deltaTime;
            layerAnchor.transform.localEulerAngles = currentEulerAngles;

            // apply the scale changes
            float newDistance = AvgDistance + DistanceAmplitude * Mathf.Sin(CurrentTime * 2f * Mathf.PI / DistancePeriod);
            layerAnchor.transform.localScale = Vector3.one * newDistance;

            clockwise = !clockwise;
        }

        // time to trigger more sounds
        NextTriggerTime -= Time.deltaTime;
        if (NextTriggerTime < 0)
        {
            // if the random roll passes then play the sound
            if (Random.Range(0f, 1f) < (IntensityCurve.Evaluate(Keeper.NormalisedTime) * DeathclockIntensity))
            {
                // pick a layer and emitter
                int layer = Random.Range(0, NumLayers);
                int emitter = Random.Range(0, NumEmittersPerLayer);

                // Play a sound event from a random emitter within a random layer
                //AkSoundEngine.PostEvent(LayerEvents[layer], Emitters[layer][emitter]);
            }

            // update the trigger time
            NextTriggerTime = TriggerTime;
        }
    }
}
