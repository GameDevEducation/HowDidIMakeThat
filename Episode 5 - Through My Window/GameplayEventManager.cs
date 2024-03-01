using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScheduledEvent
{
    public GameplayEventConfig Config;
    public float ScheduledTime;

    public ScheduledEvent(GameplayEventConfig _config, float _scheduledTime)
    {
        Config = _config;
        ScheduledTime = _scheduledTime;
    }
}

public class GameplayEventManager : MonoBehaviour
{
    public List<GameplayEventConfig> Events;
    protected List<int> LastRan;

    protected List<ScheduledEvent> ScheduledEvents;

    // Start is called before the first frame update
    void Start()
    {
        // populate when each event last ran
        LastRan = new List<int>(Events.Count);
        while(LastRan.Count < Events.Count)
        {
            LastRan.Add(-1);
        }

        ScheduledEvents = new List<ScheduledEvent>();
    }

    // Update is called once per frame
    void Update()
    {
        System.DateTime currentTime = System.DateTime.UtcNow;
        int currentHourInWeek = ((int)currentTime.DayOfWeek * 24) + currentTime.Hour;

        // schedule any activatable events
        for(int index = 0; index < LastRan.Count; ++index)
        {
            // already ran this hour?
            if (LastRan[index] == currentHourInWeek)
                continue;

            // check if can run
            if (Events[index].CanActivate())
            {
                // flag as run
                LastRan[index] = currentHourInWeek;

                // schedule the event
                ScheduledEvents.Add(new ScheduledEvent(Events[index], Events[index].GetActivationDelay()));
            }
        }

        // update any scheduled events
        for (int index = 0; index < ScheduledEvents.Count; ++index)
        {
            var scheduledEvent = ScheduledEvents[index];
            scheduledEvent.ScheduledTime -= Time.deltaTime;

            // time to activate the event?
            if (scheduledEvent.ScheduledTime <= 0)
            {
                scheduledEvent.Config.Activate();

                // remove the event
                ScheduledEvents.RemoveAt(index);
                --index;
            }
        }
    }
}
