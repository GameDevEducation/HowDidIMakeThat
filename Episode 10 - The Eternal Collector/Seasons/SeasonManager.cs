using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeasonManager : MonoBehaviour, ISaveLoadParticipant
{
    [SerializeField] List<Season> Seasons;
    [SerializeField] TimeBridge_Light LightBridge;
    [SerializeField] float MinIntervalBetweenEventRoll;
    [SerializeField] float MaxIntervalBetweenEventRoll;
    [SerializeField, Range(0f, 1f)] float ChanceOfSecondEvent = 0f;
    [SerializeField] UI_EventDisplay EventDisplay;

    float SeasonStartDay;
    float SeasonEndDay;
    int CurrentSeasonIndex = -1;
    float TimeUntilNextEventRoll;

    public static SeasonManager Instance { get; private set; } = null;

    public int CurrentCycle { get; private set; } = 0;
    public Season PreviousSeason => Seasons[(CurrentSeasonIndex - 1 + Seasons.Count) % Seasons.Count];
    public Season CurrentSeason => Seasons[CurrentSeasonIndex];
    public Season NextSeason => Seasons[(CurrentSeasonIndex + 1) % Seasons.Count];

    List<Event_Base> ActiveEvents = new List<Event_Base> ();

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"Found a duplicate SeasonManager on {gameObject.name}");
            Destroy(gameObject);

            return;
        }

        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (SaveLoadManager.Instance.LoadedState != null)
        {
            CurrentCycle = SaveLoadManager.Instance.LoadedState.Season.CurrentCycle;
            CurrentSeasonIndex = SaveLoadManager.Instance.LoadedState.Season.CurrentSeasonIndex;
            SeasonStartDay = SaveLoadManager.Instance.LoadedState.Season.SeasonStartDay;
            SeasonEndDay = SaveLoadManager.Instance.LoadedState.Season.SeasonEndDay;

            foreach (var activeEvent in SaveLoadManager.Instance.LoadedState.Season.Events)
            {
                Event_Base foundEvent = null;

                // first find the event
                foreach (var knownEvent in CurrentSeason.Events)
                {
                    if (knownEvent.Name == activeEvent.Name)
                    {
                        foundEvent = knownEvent;
                        break;
                    }
                }

                // found the event?
                if (foundEvent != null)
                    StartEvent(foundEvent, activeEvent.Duration, activeEvent.CurrentTime);
            }

            TimeUntilNextEventRoll = MaxIntervalBetweenEventRoll;

            AdvanceSeason(true);
        }
        else
        {
            AdvanceSeason(false);

            TimeUntilNextEventRoll = Random.Range(MinIntervalBetweenEventRoll, MaxIntervalBetweenEventRoll);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // tick any active events
        for (int eventIndex = 0; eventIndex < ActiveEvents.Count; eventIndex++)
        {
            var activeEvent = ActiveEvents[eventIndex];

            if (activeEvent.Tick())
            {
                activeEvent.OnFinish();

                EventDisplay.StopEvent(activeEvent);

                ActiveEvents.RemoveAt(eventIndex);
                --eventIndex;
            }
        }
    }

    public void Tick(float currentTime)
    {
        if (ToDManager.Instance.CurrentDay >= SeasonEndDay)
            AdvanceSeason();

        if (ActiveEvents.Count == 0)
        {
            if (TimeUntilNextEventRoll > 0)
                TimeUntilNextEventRoll -= ToDManager.Instance.DeltaTime / 3600f;

            if (TimeUntilNextEventRoll <= 0f)
                SelectEvents(CurrentSeason.Events);
        }

        float seasonProgress = Mathf.InverseLerp(SeasonStartDay, SeasonEndDay, ToDManager.Instance.CurrentDay);
        float seasonIntensity = CurrentSeason.SeasonIntensity.Evaluate(seasonProgress);

        // get the ending values of the previous season
        float previousIrradiance = PreviousSeason.SolarIrradianceModifier;
        float previousTimeSkew = PreviousSeason.DayLengthModifier;

        // get the current target value
        float targetIrradiance = Mathf.Lerp(1f, CurrentSeason.SolarIrradianceModifier, seasonIntensity);
        float targetTimeSkew = Mathf.Lerp(0f, CurrentSeason.DayLengthModifier, seasonIntensity);

        // update the values
        LightBridge.UpdateSeasonSolarIrradianceModifier(Mathf.Lerp(previousIrradiance, targetIrradiance, seasonProgress));
        LightBridge.UpdateSeasonTimeSkew(Mathf.Lerp(previousTimeSkew, targetTimeSkew, seasonProgress));
    }

    void AdvanceSeason(bool skipEvents = false)
    {
        if (CurrentSeasonIndex == Seasons.Count - 1)
            ++CurrentCycle;

        // find the next season
        CurrentSeasonIndex = (CurrentSeasonIndex + 1) % Seasons.Count;

        SeasonStartDay = Mathf.FloorToInt(ToDManager.Instance.CurrentDay);
        SeasonEndDay = Mathf.FloorToInt(SeasonStartDay + Random.Range(CurrentSeason.MinimumLength, CurrentSeason.MaximumLength + 1));

        if (!skipEvents)
            SelectEvents(CurrentSeason.Events);
    }

    void SelectEvents(List<Event_Base> availableEvents)
    {
        // no available events - do nothing
        if (CurrentSeason.Events == null && CurrentSeason.Events.Count == 0)
            return;

        TimeUntilNextEventRoll = Random.Range(MinIntervalBetweenEventRoll, MaxIntervalBetweenEventRoll);

        // would any of the active events prevent a new one starting?
        foreach (var activeEvent in ActiveEvents)
        {
            if (activeEvent.InhibitsOtherEvents)
                return;
        }

        List<Event_Base> candidateEvents = new List<Event_Base>();

        // check if any event passes the roll
        foreach(var availableEvent in availableEvents)
        {
            if (availableEvent.MinimumCycleToTrigger > CurrentCycle)
                continue;

            bool viableCandidate = true;
            foreach(var activeEvent in ActiveEvents)
            {
                if (!availableEvent.CanCoExistWith(activeEvent) || activeEvent.CanCoExistWith(availableEvent))
                {
                    viableCandidate = false;
                    break;
                }
            }

            if (!viableCandidate)
                continue;

            if (Random.Range(0f, 1f) < availableEvent.Probability)
                candidateEvents.Add(availableEvent);
        }

        // no events passed the check
        if (candidateEvents.Count == 0)
            return;

        // pick an event
        int eventIndex = Random.Range(0, candidateEvents.Count);
        var selectedEvent = candidateEvents[eventIndex];
        StartEvent(selectedEvent);
        candidateEvents.RemoveAt(eventIndex);

        // can other events co exist?
        if (!selectedEvent.InhibitsOtherEvents && Random.Range(0f, 1f) < ChanceOfSecondEvent)
        {
            // refilter the available events
            for (int index = 0; index < candidateEvents.Count; index++)
            {
                var candidateEvent = candidateEvents[index];

                if (!selectedEvent.CanCoExistWith(candidateEvent) || !candidateEvent.CanCoExistWith(selectedEvent))
                {
                    candidateEvents.RemoveAt(index);
                    --index;
                }
            }

            if (candidateEvents.Count > 0)
            {
                eventIndex = Random.Range(0, candidateEvents.Count);
                selectedEvent = candidateEvents[eventIndex];
                StartEvent(selectedEvent);
            }
        }
    }

    void StartEvent(Event_Base newEvent, float duration = -1f, float currentTime = -1f)
    {
        var eventInstance = ScriptableObject.Instantiate(newEvent);
        ActiveEvents.Add(eventInstance);

        eventInstance.OnStart(duration, currentTime);

        EventDisplay.StartEvent(eventInstance);
    }

    public void PrepareForSave(SavedGameState savedGame)
    {
        savedGame.Season.CurrentDay = ToDManager.Instance.CurrentDay;
        savedGame.Season.CurrentTime = ToDManager.Instance.CurrentTime;
        savedGame.Season.SeasonStartDay = SeasonStartDay;
        savedGame.Season.SeasonEndDay = SeasonEndDay;
        savedGame.Season.CurrentCycle = CurrentCycle;
        savedGame.Season.CurrentSeasonIndex = CurrentSeasonIndex;

        foreach(var activeEvent in ActiveEvents)
        {
            savedGame.Season.Events.Add(new SavedGameState.EventEntry()
            {
                Name = activeEvent.Name,
                Duration = activeEvent.Duration,
                CurrentTime = activeEvent.CurrentTime
            });
        }
    }
}
