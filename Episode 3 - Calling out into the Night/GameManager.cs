using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DailyEvent
{
    public int Day;

    public DailyEvent(int _day)
    {
        Day = _day;
    }
}

public class DailyEvent_DidNotWaterPlants : DailyEvent
{
    public DailyEvent_DidNotWaterPlants(int _day) : base(_day) { }
}

public class DailyEvent_CouldNotWaterPlants : DailyEvent
{
    public DailyEvent_CouldNotWaterPlants(int _day) : base(_day) { }
}

public class DailyEvent_CouldNotFilterWater : DailyEvent
{
    public DailyEvent_CouldNotFilterWater(int _day) : base(_day) { }
}

public class DailyEvent_DidNotFilterWater : DailyEvent
{
    public DailyEvent_DidNotFilterWater(int _day) : base(_day) { }
}

public class DailyEvent_DidNotDrinkWater : DailyEvent
{
    public DailyEvent_DidNotDrinkWater(int _day) : base(_day) { }
}

public class DailyEvent_CouldNotDrinkWater : DailyEvent
{
    public DailyEvent_CouldNotDrinkWater(int _day) : base(_day) { }
}

public class DailyEvent_DidNotEat : DailyEvent
{
    public DailyEvent_DidNotEat(int _day) : base(_day) { }
}

public class DailyEvent_CouldNotEat : DailyEvent
{
    public DailyEvent_CouldNotEat(int _day) : base(_day) { }
}

public class DailyEvent_Exhaustion : DailyEvent
{
    public DailyEvent_Exhaustion(int _day) : base(_day) { }
}

public enum UpdateStage
{
    Plants,
    Purifier,
    UI
}

[System.Serializable]
public class OnNextDayHandler : UnityEvent<UpdateStage> {}

[System.Serializable]
public class TimeOfDayConfiguration
{
    public float Duration = 240f;

    public AnimationCurve LightIntensity;
    public Gradient LightColour;

    public bool CanUseComputer = false;

    public bool AutoSwitchToNextConfig = false;

    public bool ForceSleep = false;
}

public class GameManager : MonoBehaviour
{
    protected bool _IsPaused = false;

    [Header("Day Handling")]
    public OnNextDayHandler OnNextDay = new OnNextDayHandler();
    public List<Light> AllLights;
    public int MinDaysToAdvance = 1;
    public int MaxDaysToAdvance = 3;

    public int CurrentDay = 1;
    public int FinalDay = 23;
    public bool IsDayTime = true;
    public float CurrentTime = 0f;

    [Header("Computer Config")]
    public float UsingComputerBufferTime = 30f;

    [Header("Day Configuration")]
    public TimeOfDayConfiguration DayConfiguration;
    public UnityEvent OnWakeUp;
    public UnityEvent OnEndGame;
    public UnityEvent OnSunrise;

    [Header("Night Configuration")]
    public TimeOfDayConfiguration NightConfiguration;
    public UnityEvent OnGoToSleep;
    public UnityEvent OnSunset;

    [Header("Player Hydration")]
    public WaterTank WaterSource;
    public float PlayerHydrationLevel = 1f;
    public float PlayerMaxWaterPerDay = 2f;
    public float PlayerWaterUsagePerDay = 2f;
    protected bool PlayerDrankToday = false;
    protected float PlayerWaterAmountDrunk = 0f;

    [Header("Player Food")]
    public FoodStack FoodSource;
    public int PlayerFoodLevel = 0;
    public int PlayerMaxFoodPerDay = 2;
    public int PlayerFoodConsumptionPerDay = 2;
    protected bool PlayerAteToday = false;
    protected int PlayerFoodAmountEaten = 0;

    [Header("Emergency Radio")]
    public int FrequencyBlocksScanned = 3;
    public int FrequencyBlocksScannedPerDay_Min = 2;
    public int FrequencyBlocksScannedPerDay_Max = 4;
    protected bool PlayerRanRadioScanToday = false;
    public bool RadioScanRequested { get; set;}
    public UnityEvent OnPerformRadioScan;

    private List<DailyEvent> AllEvents = new List<DailyEvent>();

    private static GameManager _Instance = null;
    private bool Asleep = false;
    private bool UsingComputer = false;

    public int PreviousDay { get; private set;}

    public List<DailyEvent> DaysEvents
    {
        get
        {
            return AllEvents;
        }
    }

    public static GameManager Instance
    {
        get
        {
            return _Instance;
        }
    }

    void Awake()
    {
        if (_Instance)
        {
            Destroy(gameObject);
            return;
        }

        _Instance = this;
    }

    public bool IsPerformingRadioScan
    {
        get
        {
            return PlayerRanRadioScanToday;
        }
    }

    public void PerformRadioScan()
    {
        PlayerRanRadioScanToday = true;
        RadioScanRequested = false;
        OnPerformRadioScan?.Invoke();
    }

    public void SetUsingComputer(bool newValue)
    {
        UsingComputer = newValue;
    }

    public int NumDaysPassed
    {
        get
        {
            return CurrentDay - PreviousDay;
        }
    }

    TimeOfDayConfiguration CurrentConfig
    {
        get
        {
            return IsDayTime ? DayConfiguration : NightConfiguration;
        }
    }

    public bool IsPaused
    {
        get
        {
            return _IsPaused;
        }
    }

    public void SetPaused(bool newValue)
    {
        _IsPaused = newValue;
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdateForTimeOfDay();

        AkSoundEngine.PostEvent("Play_Wind", gameObject);
        AkSoundEngine.PostEvent("Play_Ambient_Hum", gameObject);
    }

    void OnDestroy()
    {
        AkSoundEngine.PostEvent("Stop_Wind", gameObject);
        AkSoundEngine.PostEvent("Stop_Ambient_Hum", gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (_IsPaused)
            return;

        // do not advance time while asleep
        if (Asleep)
            return;

            // if (Input.GetKeyDown(KeyCode.N))
            // {
            //     GoToSleep();
            //     UpdateForTimeOfDay();
            //     return;
            // }

        // update the time
        CurrentTime += Time.deltaTime;

        // while using the computer time slows and will stop at the buffer time
        if (UsingComputer && (CurrentTime > (CurrentConfig.Duration - UsingComputerBufferTime)))
        {
            CurrentTime = CurrentConfig.Duration - UsingComputerBufferTime;
        }

        // reached the end of the time period?
        if (CurrentTime >= CurrentConfig.Duration)
        {
            // check if the config can switch
            if (CurrentConfig.AutoSwitchToNextConfig)
            {
                IsDayTime = !IsDayTime;
                CurrentTime = 0f;

                if (!IsDayTime)
                    OnSunset?.Invoke();
            }
            else if (CurrentConfig.ForceSleep)
            {
                RegisterDailyEvent(new DailyEvent_Exhaustion(CurrentDay));

                GoToSleep();
            }
        }

        // update for the new time of day
        UpdateForTimeOfDay();
    }

    protected void UpdateForTimeOfDay()
    {
        // calculate the new intensity
        float newIntensity = CurrentConfig.LightIntensity.Evaluate(CurrentTime / CurrentConfig.Duration);
        Color newColor = CurrentConfig.LightColour.Evaluate(CurrentTime / CurrentConfig.Duration);

        // update each of the lights
        foreach(Light light in AllLights)
        {
            light.intensity = newIntensity;
            light.color = newColor;
        }
    }

    public void RegisterDailyEvent(DailyEvent dailyEvent)
    {
        AllEvents.Add(dailyEvent);
    }

    public bool CanDrinkWater
    {
        get
        {
            return PlayerHydrationLevel < PlayerMaxWaterPerDay;
        }
    }

    public void DrinkWater(float amount)
    {
        PlayerHydrationLevel += amount;
        PlayerWaterAmountDrunk += amount;

        if (amount > 0)
        {
            PlayerDrankToday = true;        
            AkSoundEngine.PostEvent("Play_Drink_Water", Camera.main.gameObject);
        }
    }

    public bool CanEatFood
    {
        get
        {
            return PlayerFoodLevel < PlayerMaxFoodPerDay;
        }
    }

    public void EatFood(int amount)
    {
        PlayerFoodLevel += amount;

        if (amount > 0)
        {
            PlayerAteToday = true;

            ++PlayerFoodAmountEaten;

            AkSoundEngine.PostEvent("Play_Eat_Food", Camera.main.gameObject);
        }
    }

    public void GoToSleep()
    {
        Asleep = true;

        OnGoToSleep?.Invoke();
    }

    public void AdvanceDay()
    {
        Asleep = false;

        PreviousDay = CurrentDay;
        
        // determine the number of days to advance
        int numDaysToAdvance = Random.Range(MinDaysToAdvance, MaxDaysToAdvance + 1);
        CurrentDay += numDaysToAdvance;

        // determine the additional forced consumption of water
        float forcedWaterConsumption = numDaysToAdvance * PlayerWaterUsagePerDay - PlayerWaterAmountDrunk;
        WaterSource.ConsumeFilteredWater(forcedWaterConsumption);

        // determine the additional forced consumption of food
        int forcedFoodConsumption = numDaysToAdvance * PlayerFoodConsumptionPerDay - PlayerFoodAmountEaten;
        FoodSource.ConsumeFood(forcedFoodConsumption);

        // apply forced radio scanning
        int numDaysScanned = PlayerRanRadioScanToday ? numDaysToAdvance : (numDaysToAdvance - 1);
        for (int day = 0; day < numDaysScanned; ++day)
        {
            FrequencyBlocksScanned += Random.Range(FrequencyBlocksScannedPerDay_Min, FrequencyBlocksScannedPerDay_Max + 1);
        }

        // update player hydration
        PlayerHydrationLevel = Mathf.Max(0f, PlayerHydrationLevel - PlayerWaterUsagePerDay);

        // update player food level
        PlayerFoodLevel = Mathf.Max(0, PlayerFoodLevel - PlayerFoodConsumptionPerDay);

        // register events if the player did not eat or drink
        if (!PlayerDrankToday)
        {
            if (WaterSource.IsWaterAvailable)
                RegisterDailyEvent(new DailyEvent_DidNotDrinkWater(PreviousDay));
            else
                RegisterDailyEvent(new DailyEvent_CouldNotDrinkWater(PreviousDay));
        }
        if (!PlayerAteToday)
        {
            if (FoodSource.IsFoodAvailable)
                RegisterDailyEvent(new DailyEvent_DidNotEat(PreviousDay));
            else
                RegisterDailyEvent(new DailyEvent_CouldNotEat(PreviousDay));
        }

        // advise everything the day advanced
        OnNextDay?.Invoke(UpdateStage.Plants);
        OnNextDay?.Invoke(UpdateStage.Purifier);
        OnNextDay?.Invoke(UpdateStage.UI);

        // process the events

        // Update the config
        IsDayTime = true;
        CurrentTime = 0;

        // reset the counters of the amount eaten and runk
        PlayerFoodAmountEaten = 0;
        PlayerWaterAmountDrunk = 0;

        // reset the activity flags
        RadioScanRequested = false;
        PlayerRanRadioScanToday = false;
        PlayerDrankToday = false;
        PlayerAteToday = false;

        // handle the morning
        if (CurrentDay >= FinalDay)
            OnEndGame?.Invoke();
        else
        {
            UpdateForTimeOfDay();

            OnWakeUp?.Invoke();
            OnSunrise?.Invoke();
        }

        // clear the list of events that happened
        AllEvents.Clear();
    }
}
