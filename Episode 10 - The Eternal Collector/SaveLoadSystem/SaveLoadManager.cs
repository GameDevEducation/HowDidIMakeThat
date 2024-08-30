using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class SavedGameState
{
    public class CarriageEntry
    {
        public string Type;
    }

    public class UpgradeEntry
    {
        public string Name;
    }

    public class EventEntry
    {
        public string Name;
        public float Duration;
        public float CurrentTime;
    }

    public class PlayerState
    {
        public int CarriageIndex;
    }

    public class TrainState
    {
        public float EnergyStored;
        public float ScrapStored;
        public float ThrottleSetting;
        public string GameMode;

        public List<CarriageEntry> Carriages = new List<CarriageEntry>();
        public List<UpgradeEntry> Upgrades = new List<UpgradeEntry>();
    }

    public class WeatherState
    {
        public string ActiveWeather;
    }

    public class SeasonState
    {
        public float CurrentDay;
        public float CurrentTime;

        public float SeasonStartDay;
        public float SeasonEndDay;

        public int CurrentCycle;
        public int CurrentSeasonIndex;

        public List<EventEntry> Events = new List<EventEntry>();
    }

    public PlayerState Player = new PlayerState();
    public TrainState Train = new TrainState();
    public WeatherState Weather = new WeatherState();
    public SeasonState Season = new SeasonState();
}

public interface ISaveLoadParticipant
{
    void PrepareForSave(SavedGameState savedGame);
}

public class SaveLoadManager : MonoBehaviour
{
    public enum ESaveSlot
    {
        None,

        Slot1,
        Slot2,
        Slot3,
        Slot4
    }

    public enum ESaveType
    {
        Manual,
        Automatic
    }

    [SerializeField] float AutoSaveInterval = 300f;

    public static SaveLoadManager Instance { get; private set; } = null;

    public SavedGameState LoadedState { get; private set; } = null;
    protected ESaveSlot CurrentSlot;

    public string DefaultGameMode { get; private set; } = string.Empty;

    float TimeUntilNextAutosave;
    bool GameInProgress = false;

    public bool HasAnySaveGames
    {
        get
        {
            if (IsSavePresent(ESaveSlot.Slot1, ESaveType.Manual))
                return true;
            if (IsSavePresent(ESaveSlot.Slot1, ESaveType.Automatic))
                return true;
            if (IsSavePresent(ESaveSlot.Slot2, ESaveType.Manual))
                return true;
            if (IsSavePresent(ESaveSlot.Slot2, ESaveType.Automatic))
                return true;
            if (IsSavePresent(ESaveSlot.Slot3, ESaveType.Manual))
                return true;
            if (IsSavePresent(ESaveSlot.Slot3, ESaveType.Automatic))
                return true;
            if (IsSavePresent(ESaveSlot.Slot4, ESaveType.Manual))
                return true;
            if (IsSavePresent(ESaveSlot.Slot4, ESaveType.Automatic))
                return true;

            return false;
        }
    }

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"Found duplicate SaveLoadManager on {gameObject.name}");

            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        Instance = this;
    }

    private void Update()
    {
        if (GameInProgress && !PauseManager.IsPaused && LoadedState != null)
        {
            TimeUntilNextAutosave += Time.deltaTime;

            if (TimeUntilNextAutosave >= AutoSaveInterval)
            {
                TimeUntilNextAutosave = 0;

                RequestSave(CurrentSlot, ESaveType.Automatic);
            }
        }
    }

    public string GetSaveDate(ESaveSlot slot, ESaveType saveType)
    {
        string path = GetSavePath(slot, saveType);
        var lastModifiedTime = File.GetLastWriteTime(path);

        return lastModifiedTime.ToLongDateString() + " @ " + lastModifiedTime.ToLongTimeString();
    }

    string GetSavePath(ESaveSlot slot, ESaveType saveType)
    {
        return Path.Combine(Application.persistentDataPath, $"SaveFile_{slot}_{saveType.ToString()}.json");
    }

    public bool IsSavePresent(ESaveSlot slot, ESaveType saveType)
    {
        return File.Exists(GetSavePath(slot, saveType));
    }

    public void RequestSave(ESaveSlot slot, ESaveType saveType)
    {
        LoadedState = new SavedGameState();
        string savedGamePath = GetSavePath(slot, saveType);

        WeatherManager.Instance.PrepareForSave(LoadedState);
        EternalCollector.Instance.PrepareForSave(LoadedState);
        SeasonManager.Instance.PrepareForSave(LoadedState);

        File.WriteAllText(savedGamePath, JsonConvert.SerializeObject(LoadedState, Formatting.Indented));

        CurrentSlot = slot;
    }

    public void RequestLoad(ESaveSlot slot, ESaveType saveType)
    {
        string savedGamePath = GetSavePath(slot, saveType);

        LoadedState = JsonConvert.DeserializeObject<SavedGameState>(File.ReadAllText(savedGamePath));
        CurrentSlot = slot;
    }

    public void ClearLoadedState()
    {
        LoadedState = null;
        CurrentSlot = ESaveSlot.None;
    }

    public void SetDefaultGameMode(string newDefault)
    {
        DefaultGameMode = newDefault;
    }

    public void OnGameBegin()
    {
        TimeUntilNextAutosave = 0f;
        GameInProgress = true;
    }

    public void OnGameEnd()
    {
        GameInProgress = false;
    }
}
