using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Event Config", menuName = "Injaia/Gameplay Event/Config", order = 1)]
public class GameplayEventConfig : ScriptableObject
{
    public enum ActivationMode
    {
        Random,
        All
    }

    [Header("General")]
    public string Name;
    public string TargetTag;
    [Range(0f, 300f)] public float MinActivationDelay = 0f;
    [Range(0f, 300f)] public float MaxActivationDelay = 0f;
    public ActivationMode Mode = ActivationMode.Random;

    [Header("Random Seed")]
    public bool IncludeDayOfWeek = true;
    public bool IncludeHour = true;
    public bool IncludeOther = false;
    [ConditionalField("IncludeOther")] public int RandomSeed;

    [Header("Day of Week")]
    public bool DayOfWeek = false;
    [ConditionalField("DayOfWeek")] public bool Sunday = false;
    [ConditionalField("DayOfWeek")] public bool Monday = false;
    [ConditionalField("DayOfWeek")] public bool Tuesday = false;
    [ConditionalField("DayOfWeek")] public bool Wednesday = false;
    [ConditionalField("DayOfWeek")] public bool Thursday = false;
    [ConditionalField("DayOfWeek")] public bool Friday = false;
    [ConditionalField("DayOfWeek")] public bool Saturday = false;

    [Header("UTC Hour")]
    public bool UTCHour = false;
    public bool[] PermittedHours = new bool[24];

    [Header("UTC Minutes")]
    public bool UTCMinutes = false;
    [ConditionalField("UTCMinutes")] [Range(0, 59)] public int StartMinute = 0;
    [ConditionalField("UTCMinutes")] [Range(0, 59)] public int EndMinute = 59;

    public int GetRandomSeed()
    {
        System.DateTime currentTime = System.DateTime.UtcNow;
        int seed = 42;

        if (IncludeOther)
            seed += RandomSeed;
        if (IncludeDayOfWeek)
            seed *= (int)currentTime.DayOfWeek + 1;
        if (IncludeHour)
            seed *= currentTime.Hour;

        return seed;
    }

    public float GetActivationDelay()
    {
        // seed the RNG
        Random.InitState(GetRandomSeed());

        return Random.Range(MinActivationDelay, MaxActivationDelay);
    }

    public bool CanActivate()
    {
        System.DateTime currentTime = System.DateTime.UtcNow;

        // are there day of week restrictions?
        if (DayOfWeek)
        {
            // exit if not permitted to run on the current day
            if (currentTime.DayOfWeek == System.DayOfWeek.Sunday && !Sunday)
                return false;
            if (currentTime.DayOfWeek == System.DayOfWeek.Monday && !Monday)
                return false;
            if (currentTime.DayOfWeek == System.DayOfWeek.Tuesday && !Tuesday)
                return false;
            if (currentTime.DayOfWeek == System.DayOfWeek.Wednesday && !Wednesday)
                return false;
            if (currentTime.DayOfWeek == System.DayOfWeek.Thursday && !Thursday)
                return false;
            if (currentTime.DayOfWeek == System.DayOfWeek.Friday && !Friday)
                return false;
            if (currentTime.DayOfWeek == System.DayOfWeek.Saturday && !Saturday)
                return false;
        }

        // are there any hour restrictions
        if (UTCHour)
        {
            // not permitted to run this hour
            if (!PermittedHours[currentTime.Hour])
                return false;
        }

        // are there any minute restrictions
        if (UTCMinutes)
        {
            // not in permitted minute range
            if (currentTime.Minute < StartMinute || currentTime.Minute > EndMinute)
                return false;
        }

        return true;
    }

    public void Activate()
    {
        // seed the RNG
        Random.InitState(GetRandomSeed());

        GameObject[] targets = GameObject.FindGameObjectsWithTag(TargetTag);
        
        if (Mode == ActivationMode.Random)
        {
            // pick a random target
            GameObject selectedTarget = targets[Random.Range(0, targets.Length)];

            // pick a random target and activate it
            selectedTarget.GetComponent<IGameplayEvent>()?.ActivateEvent();
        }
        else
        {
            // activate all of the targets
            foreach(var selectedTarget in targets)
            {
                selectedTarget.GetComponent<IGameplayEvent>()?.ActivateEvent();
            }
        }
    }
}
