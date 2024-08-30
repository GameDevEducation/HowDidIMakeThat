using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Event_Base : ScriptableObject
{
    public string Name;
    public string Description;
    public bool InhibitsOtherEvents = false;
    [Range(0f, 1f)] public float Probability = 0f;
    public AnimationCurve IntensityCurve;
    public float MinDuration;
    public float MaxDuration;
    public int MinimumCycleToTrigger;

    [SerializeField, Range(0f, 1f)] protected float ImpactVariation;
    [SerializeField] List<Event_Base> CannotCoExistWith;

    public float Duration { get; private set; }
    public float CurrentTime { get; private set; }

    protected float Intensity => IntensityCurve.Evaluate(CurrentTime / Duration);

    protected float Variation => 1f + Random.Range(-ImpactVariation, ImpactVariation);

    public bool CanCoExistWith(Event_Base other)
    {
        return !CannotCoExistWith.Contains(other);
    }

    public void OnStart(float overrideDuration = -1f, float overrideCurrentTime = -1f)
    {
        Duration = overrideDuration < 0f ? Random.Range(MinDuration, MaxDuration) : overrideDuration;
        CurrentTime = overrideCurrentTime < 0f ? 0f : overrideCurrentTime;

        OnStart_Internal();
    }

    public bool Tick()
    {
        CurrentTime += ToDManager.Instance.DeltaTime / 3600f;

        Tick_Internal();

        return CurrentTime >= Duration;
    }

    public void OnFinish()
    {
        OnFinish_Internal();
    }

    protected abstract void OnStart_Internal();
    protected abstract void Tick_Internal();
    protected abstract void OnFinish_Internal();
}
