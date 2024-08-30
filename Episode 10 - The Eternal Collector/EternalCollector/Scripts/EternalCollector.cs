using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class SpeedBand
{
    public float PowerToWeightRatio;
    public float MaxSpeedKPH;
}

[System.Serializable]
public class GameModePreset
{
    public string ID;
    public string Name;
    public float InitialScrapStored;
    [Range(0f, 1f)] public float InitialEnergyPercentage;
    public int ScrapPerCostUnit;
    public int PurchaseTime;
    public float SolarIrradianceMultiplier;
    public float ScrapWeightMultiplier;
}

public interface ISolarIrradianceModifier
{
    public float EffectIrradiance(float currentIrradiance);
}

public interface IResourceModifier
{
    public int EffectAmountToSpawn(int currentAmount);
    public float EffectScrapWeight(float currentScrapWeight);
}

public interface IShopModifier
{
    public int EffectPrice(int currentPrice);
    public float EffectDeliveryTime(float currentDeliveryTime);
}

public class EternalCollector : MonoBehaviour, ISaveLoadParticipant
{
    [SerializeField] Transform TrainRoot;
    [SerializeField] GameObject CarriagePrefab;
    [SerializeField] GameObject CameraPrefab;
    [SerializeField] GameObject PlayerGO;
    [SerializeField] BaseCarriageBehaviour EngineBehaviour;
    [SerializeField] List<BaseCarriageBehaviour> AvailableCarriageBehaviours;
    [SerializeField] List<BaseCarriageBehaviour> InitialCarriages;
    [SerializeField] float MinimumSpeedKPH = 0f;
    [SerializeField] float AccelerationKPH = 5f;
    [SerializeField] float CarriageSpacing = 5f;
    [SerializeField] float MaxRotationSpeed = 30f;
    [SerializeField] float TranslationLerpMultiplier = 1.1f;

    [SerializeField] string DefaultGameMode = "Default";
    [SerializeField] List<GameModePreset> GameModePresets;

    [SerializeField] List<SpeedBand> SpeedBands;
    [SerializeField] CarriageUpgrade EmergencyRetrieval;
    [SerializeField] UnityEvent OnPlayerDied = new UnityEvent();

    [SerializeField] float RetirementSpeedKPH = 200f;
    [SerializeField] Cinemachine.CinemachineVirtualCamera RetirementCamera;
    [SerializeField] UnityEvent OnRetire = new UnityEvent();

    [SerializeField] float LightningBoostTime = 1f;
    [SerializeField, Range(0f, 1f)] float LightningEnergyGain = 0.25f;
    float LightningBoostTimeRemaining = 0f;

    [SerializeField] float _HighLODDistance = 200f;

    [SerializeField] List<CarriageUpgrade> AllKnownUpgrades;

    public float HighLODDistanceSq => _HighLODDistance * _HighLODDistance;

    public bool IsInRetirementMode { get; private set; } = false;

    [SerializeField] bool DEBUG_AddCarriage = false;
    [SerializeField] bool DEBUG_UnlockUpgrades = false;
    [SerializeField] List<CarriageUpgrade> DEBUG_UpgradesToUnlock;

    public bool PurchaseInProgress { get; private set; } = false;
    float PurchaseTimeRemaining = -1f;

    float MinimumSpeed => MinimumSpeedKPH / 3.6f;

    GameModePreset CurrentGameMode;
    public int ScrapPerCostUnit => CurrentGameMode.ScrapPerCostUnit;

    public List<CarriageUpgrade> UnlockedUpgrades;

    class CarriagePositionData
    {
        public Vector3 HeadPosition;
        public Vector3 TailPosition;
    }
    List<CarriagePositionData> CarriagePositions = new List<CarriagePositionData>();

    public float EffectiveSpeedFactor
    {
        get
        {
            if (IsInRetirementMode)
                return 1f;

            return !OutOfPower ? RequestedSpeedFactor : 0f;
        }
    }
    public float RequestedSpeedFactor { get; private set; } = 0f;
    public float CurrentSpeed { get; private set; } = 0f;
    public float CurrentSpeedKPH => CurrentSpeed * 3.6f;

    public float TargetSpeed => Mathf.Lerp(MinimumSpeed, (CurrentMaxSpeedKPH / 3.6f), EffectiveSpeedFactor);
    public float TargetSpeedKPH => TargetSpeed * 3.6f;
    List<Carriage> Carriages = new List<Carriage> ();

    float DistanceInTile = 0f;

    float MaxTranslationSpeed => CurrentSpeed * TranslationLerpMultiplier;

    public float TotalTrainPower { get; private set; } = 0f;
    public float TotalTrainWeight { get; private set; } = 0f;
    public float CurrentPowerToWeightRatio { get; private set; } = 0f;

    public static EternalCollector Instance { get; private set; } = null;
    public float ScrapStorageUsed { get; private set; } = 0f;
    public float ScrapStorageAvailable { get; private set; } = 0f;
    public float EnergyPowerStored { get; private set; } = 0f;
    public float EnergyStorageCapacity { get; private set; } = 0f;
    public float CurrentMaxSpeedKPH { get; private set; } = 0f;

    public float CurrentPowerRequests { get; private set; } = 0f;
    public float CurrentPowerDelta { get; private set; } = 0f;
    public bool OutOfPower => EnergyPowerStored <= 0f;
    public int NumCarriages => Carriages.Count;

    public Carriage CurrentPlayerCarriage { get; private set; } = null;
    public TrackTile CurrentPlayerTile => CurrentPlayerCarriage != null ? CurrentPlayerCarriage.CurrentTile : null;

    List<ISolarIrradianceModifier> Modifiers_Irradiance = new List<ISolarIrradianceModifier>();
    List<IResourceModifier> Modifiers_Resources = new List<IResourceModifier>();
    List<IShopModifier> Modifiers_Shop = new List<IShopModifier>();

    public static void RegisterModifier(ISolarIrradianceModifier modifier)
    {
        Instance.Modifiers_Irradiance.Add(modifier);
    }

    public static void RegisterModifier(IResourceModifier modifier)
    {
        Instance.Modifiers_Resources.Add(modifier);
    }

    public static void RegisterModifier(IShopModifier modifier)
    {
        Instance.Modifiers_Shop.Add(modifier);
    }

    public static void DeregisterModifier(ISolarIrradianceModifier modifier)
    {
        Instance.Modifiers_Irradiance.Remove(modifier);
    }

    public static void DeregisterModifier(IResourceModifier modifier)
    {
        Instance.Modifiers_Resources.Remove(modifier);
    }

    public static void DeregisterModifier(IShopModifier modifier)
    {
        Instance.Modifiers_Shop.Remove(modifier);
    }

    public static float EffectIrradiance(float currentIrradiance)
    {
        foreach(var modifier in Instance.Modifiers_Irradiance)
            currentIrradiance = modifier.EffectIrradiance(currentIrradiance);

        return currentIrradiance * Instance.CurrentGameMode.SolarIrradianceMultiplier;
    }

    public static int EffectAmountToSpawn(int currentAmount)
    {
        foreach (var modifier in Instance.Modifiers_Resources)
            currentAmount = modifier.EffectAmountToSpawn(currentAmount);

        return currentAmount;
    }

    public static float EffectScrapWeight(float currentScrapWeight)
    {
        foreach (var modifier in Instance.Modifiers_Resources)
            currentScrapWeight = modifier.EffectScrapWeight(currentScrapWeight);

        return currentScrapWeight * Instance.CurrentGameMode.ScrapWeightMultiplier;
    }

    public static int EffectPrice(int currentPrice)
    {
        foreach (var modifier in Instance.Modifiers_Shop)
            currentPrice = modifier.EffectPrice(currentPrice);

        return Mathf.FloorToInt(currentPrice / 25f) * 25;
    }

    public static float EffectDeliveryTime(float currentDeliveryTime)
    {
        foreach (var modifier in Instance.Modifiers_Shop)
            currentDeliveryTime = modifier.EffectDeliveryTime(currentDeliveryTime);

        return currentDeliveryTime;
    }

    public static bool IsUpgradeUnlocked(CarriageUpgrade upgrade)
    {
        if (Instance == null)
            return false;

        return Instance.UnlockedUpgrades.Contains(upgrade);
    }

    public static int GetNumApplicableCarriages(CarriageUpgrade upgrade)
    {
        int numApplicableCarriages = 0;
        foreach (var carriage in Instance.Carriages)
        {
            // no behaviour restriction or behaviour restriction matches
            if (upgrade.ApplicableBehaviours.Count == 0 ||
                upgrade.ApplicableBehaviours.Contains(carriage.Behaviour.Template))
                ++numApplicableCarriages;
        }

        if (!upgrade.IsPerCarriage)
            numApplicableCarriages = 1;

        return numApplicableCarriages;
    }

    public static int CalculateCost(CarriageUpgrade upgrade)
    {
        return EffectPrice(upgrade.Cost * Instance.ScrapPerCostUnit * GetNumApplicableCarriages(upgrade));
    }

    public static int CalculateCost(BaseCarriageBehaviour behaviour)
    {
        // determine the number of relevant upgrades
        int existingUpgradesCost = 0;
        foreach(var upgrade in Instance.UnlockedUpgrades)
        {
            if (!upgrade.IsPerCarriage)
                continue;

            // is the upgrade applicable?
            if (upgrade.ApplicableBehaviours.Count == 0 ||
                upgrade.ApplicableBehaviours.Contains(behaviour))
            {
                existingUpgradesCost += upgrade.Cost;
            }
        }

        return EffectPrice((behaviour.Cost + existingUpgradesCost) * Instance.ScrapPerCostUnit);
    }

    public CarriageUpgrade UpgradeToPurchase { get; private set; } = null;
    public BaseCarriageBehaviour CarriageToPurchase { get; private set; } = null;

    public static void PurchaseUpgrade(CarriageUpgrade upgrade)
    {
        Instance.SpendScrap(CalculateCost(upgrade));

        Instance.PurchaseInProgress = true;
        Instance.PurchaseTimeRemaining = EffectDeliveryTime(Instance.CurrentGameMode.PurchaseTime);
        Instance.CarriageToPurchase = null;
        Instance.UpgradeToPurchase = upgrade;
    }

    public static void PurchaseCarriage(BaseCarriageBehaviour behaviour)
    {
        Instance.SpendScrap(CalculateCost(behaviour));

        Instance.PurchaseInProgress = true;
        Instance.PurchaseTimeRemaining = EffectDeliveryTime(Instance.CurrentGameMode.PurchaseTime);
        Instance.CarriageToPurchase = behaviour;
        Instance.UpgradeToPurchase = null;
    }

    protected void SpendScrap(float amountToSpend)
    {
        if (amountToSpend > ScrapStorageUsed)
        {
            Debug.LogError($"Trying to spend {amountToSpend} of {ScrapStorageUsed} available scrap");
            return;
        }

        ScrapStorageUsed -= amountToSpend;

        RedistributeScrap();
    }

    public void StoreScrap(float amount)
    {
        ScrapStorageUsed += amount;
        RedistributeScrap();
    }

    public void StoreEnergy(float amount)
    {
        EnergyPowerStored += amount;
        RedistributeEnergy();
    }

    public Carriage GetCarriage(int carriageIndex)
    {
        return Carriages[carriageIndex];
    }

    public void OnLightningStrike()
    {
        LightningBoostTimeRemaining = LightningBoostTime;

        AkSoundEngine.PostEvent(AK.EVENTS.PLAY_LIGHTNING_POWERBOOST, Camera.main.gameObject);
    }

    void SetGameMode(string newGameMode)
    {
        foreach(var preset in GameModePresets)
        {
            if (newGameMode == preset.ID)
            {
                CurrentGameMode = preset;
                return;
            }
        }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"Found duplicate EternalCollector on {gameObject.name}");
            Destroy(gameObject);
        }

        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (SaveLoadManager.Instance.LoadedState != null)
            SetGameMode(SaveLoadManager.Instance.LoadedState.Train.GameMode);
        else if (!string.IsNullOrEmpty(SaveLoadManager.Instance.DefaultGameMode))
            SetGameMode(SaveLoadManager.Instance.DefaultGameMode);
        else
            SetGameMode(DefaultGameMode);

        SaveLoadManager.Instance.OnGameBegin();

        PauseManager.Instance.ForceResume();
    }

    void OnDestroy()
    {
        SaveLoadManager.Instance.OnGameEnd();
    }

    void Update()
    {
        if (PauseManager.IsPaused)
            return;

        // handle lightning caused energy boost
        if (LightningBoostTimeRemaining > 0f)
        {
            LightningBoostTimeRemaining -= Time.deltaTime;

            if (LightningBoostTimeRemaining > 0f)
            {
                StoreEnergy(LightningEnergyGain * EnergyStorageCapacity * Time.deltaTime / LightningBoostTime);
            }
        }

        // Purchase currently underway?
        if (PurchaseInProgress)
        {
            PurchaseTimeRemaining -= Time.deltaTime;
            if (PurchaseTimeRemaining <= 0f)
            {
                PurchaseInProgress = false;

                // complete the purchase
                if (UpgradeToPurchase != null)
                    UnlockUpgrade(UpgradeToPurchase);
                else if (CarriageToPurchase != null)
                    SpawnCar(CarriageToPurchase);

                // refresh the visual appearance
                foreach (var carriage in Carriages)
                    carriage.SynchroniseVisuals();

                UpgradeToPurchase = null;
                CarriageToPurchase = null;
            }
        }

        if (DEBUG_AddCarriage)
        {
            DEBUG_AddCarriage = false;

            SpawnCar(AvailableCarriageBehaviours[Random.Range(0, AvailableCarriageBehaviours.Count - 2)]);
        }
        if (DEBUG_UnlockUpgrades)
        {
            DEBUG_UnlockUpgrades = false;

            foreach (var upgrade in DEBUG_UpgradesToUnlock)
                UnlockUpgrade(upgrade);

            // refresh the visual appearance
            foreach (var carriage in Carriages)
                carriage.SynchroniseVisuals();

            DEBUG_UpgradesToUnlock.Clear();
        }

        TickCarriages_Power();

        RefreshStats();

        TickCarriages_General();
    }

    public bool IsHead(Carriage carriage)
    {
        if (Carriages.Count == 0)
            return false;

        return Carriages[0] == carriage;
    }

    public bool IsBody(Carriage carriage)
    {
        var index = Carriages.IndexOf(carriage);
        return index > 0 && index < Carriages.Count - 1;
    }

    public bool IsTail(Carriage carriage)
    {
        if (Carriages.Count <= 1)
            return false;

        return Carriages[Carriages.Count - 1] == carriage;
    }

    public void SetSpeedFactor(float newSpeedFactor)
    {
        RequestedSpeedFactor = newSpeedFactor;
    }

    public void OnChangedCarriage(Carriage previousCarriage, Carriage currentCarriage)
    {
        bool carriageChanged = CurrentPlayerCarriage != currentCarriage;

        if (carriageChanged && CurrentPlayerCarriage)
            CurrentPlayerCarriage.OnPlayerLeft();

        CurrentPlayerCarriage = currentCarriage;

        if (carriageChanged && CurrentPlayerCarriage)
            CurrentPlayerCarriage.OnPlayerEntered();

        CarriageUI.Instance.OnChangedCarriage(previousCarriage, currentCarriage);
    }

    public void OnHitGround(TrackTile impactTile)
    {
        // teleport the player if there is an emergency retrieval system
        if (IsUpgradeUnlocked(EmergencyRetrieval))
        {
            foreach(var marker in Carriages[^1].ActiveMarkers)
            {
                if (marker.Type == Marker.EMarkerType.EmergencyRetrieval_RespawnPoint)
                    CharacterMotor.Instance.transform.position = marker.transform.position;
            }
        }
        else
        {
            OnPlayerDied.Invoke();
        }
    }

    public void Retire()
    {
        IsInRetirementMode = true;

        foreach (var marker in Carriages[0].ActiveMarkers)
        {
            if (marker.Type == Marker.EMarkerType.Retirement_Camera)
            {
                RetirementCamera.transform.SetParent(marker.transform);
                RetirementCamera.transform.localPosition = Vector3.zero;
                RetirementCamera.transform.localRotation = Quaternion.identity;
            }
        }

        OnRetire.Invoke();
    }

    void UnlockUpgrade(CarriageUpgrade upgrade)
    {
        UnlockedUpgrades.Add(upgrade);

        RecalculateCapacities();
    }

    public void RequestPower(float powerRequested)
    {
        CurrentPowerRequests += powerRequested;
    }

    void TickCarriages_Power()
    {
        float lastPowerStored = EnergyPowerStored;

        // update power info for every carriage
        CurrentPowerRequests = 0f;
        EnergyStorageCapacity = 0f;
        EnergyPowerStored = 0f;
        foreach (var carriage in Carriages)
        {
            carriage.Tick_Power();

            EnergyStorageCapacity += carriage.EnergyStorageCapacity;
            EnergyPowerStored += carriage.EnergyPowerCapacity;
        }

        CurrentPowerDelta = (EnergyPowerStored - lastPowerStored) - CurrentPowerRequests;

        // consume the requested amount of energy and rebalance the energy stored
        EnergyPowerStored = Mathf.Max(0f, EnergyPowerStored - CurrentPowerRequests);
        if (EnergyStorageCapacity > 0f)
        {
            RedistributeEnergy();
        }
    }

    void TickCarriages_General()
    {
        foreach (var carriage in Carriages)
            carriage.Tick_General();
    }

    void RecalculateCapacities()
    {
        // update stats
        ScrapStorageAvailable = 0f;
        EnergyStorageCapacity = 0f;
        foreach (var carriage in Carriages)
        {
            ScrapStorageAvailable += carriage.ScrapStorageAvailable;
            EnergyStorageCapacity += carriage.EnergyStorageCapacity;
        }
    }

    void RefreshStats()
    {
        // update stats
        TotalTrainPower         = 0f;
        TotalTrainWeight        = 0f;
        ScrapStorageAvailable   = 0f;
        ScrapStorageUsed        = 0f;
        foreach (var carriage in Carriages)
        {
            ScrapStorageAvailable   += carriage.ScrapStorageAvailable;
            ScrapStorageUsed        += carriage.ScrapStorageUsed;

            TotalTrainPower         += carriage.EnginePower;
            TotalTrainWeight        += carriage.Weight + carriage.ScrapStorageUsed;
        }

        CurrentPowerToWeightRatio = TotalTrainWeight > 0f ? (1000f * TotalTrainPower / TotalTrainWeight) : 0f;

        if (!IsInRetirementMode)
        {
            // determine the current max speed
            CurrentMaxSpeedKPH = SpeedBands[SpeedBands.Count - 1].MaxSpeedKPH;
            foreach (var speedBand in SpeedBands)
            {
                if (CurrentPowerToWeightRatio <= speedBand.PowerToWeightRatio)
                {
                    CurrentMaxSpeedKPH = speedBand.MaxSpeedKPH * CurrentPowerToWeightRatio / speedBand.PowerToWeightRatio;
                    break;
                }
            }
        }
        else
            CurrentMaxSpeedKPH = RetirementSpeedKPH;

        // rebalance the storage
        if (ScrapStorageAvailable > 0f)
        {
            RedistributeScrap();
        }
    }

    private void RedistributeScrap()
    {
        float percentageStorageUsed = ScrapStorageUsed / ScrapStorageAvailable;

        float newTotalAmountStored = 0f;
        foreach (var carriage in Carriages)
        {
            if (carriage.ScrapStorageAvailable <= 0f)
                continue;

            carriage.SetScrapStorageUsed(carriage.ScrapStorageAvailable * percentageStorageUsed);
            newTotalAmountStored += carriage.ScrapStorageUsed;
        }

        // non-zero delta?
        float delta = ScrapStorageUsed - newTotalAmountStored;
        if (!Mathf.Approximately(delta, 0f))
        {
            // find the first carriage capable of storage and apply the delta to it
            foreach (var carriage in Carriages)
            {
                if (carriage.ScrapStorageAvailable <= 0f)
                    continue;

                carriage.SetScrapStorageUsed(carriage.ScrapStorageUsed + delta);
                break;
            }
        }
    }

    private void RedistributeEnergy()
    {
        float percentageEnergyStored = EnergyPowerStored / EnergyStorageCapacity;

        float newTotalAmountStored = 0f;
        foreach (var carriage in Carriages)
        {
            if (carriage.EnergyStorageCapacity <= 0f)
                continue;

            carriage.SetEnergyStorageUsed(carriage.EnergyStorageCapacity * percentageEnergyStored);
            newTotalAmountStored += carriage.EnergyPowerCapacity;
        }

        // non-zero delta?
        float delta = EnergyPowerStored - newTotalAmountStored;
        if (!Mathf.Approximately(delta, 0f))
        {
            // find the first carriage capable of storage and apply the delta to it
            foreach (var carriage in Carriages)
            {
                if (carriage.EnergyStorageCapacity <= 0f)
                    continue;

                carriage.SetEnergyStorageUsed(carriage.EnergyPowerCapacity + delta);
                break;
            }
        }
    }

    private void LateUpdate()
    {
        CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, TargetSpeed, (AccelerationKPH / 3.6f) * Time.deltaTime);

        // update the distance in tile
        DistanceInTile += CurrentSpeed * Time.deltaTime;

        // is this beyond the current tile?
        if (DistanceInTile >= EternalTrackGenerator.CurrentTile.TotalDistance)
        {
            DistanceInTile -= EternalTrackGenerator.CurrentTile.TotalDistance;
            EternalTrackGenerator.AdvanceTile();
        }

        RecalculateCarriagePositions();

        ApplyCarriagePositions(false);
    }

    // Update is called once per frame
    void FixedUpdate()
    {

    }

    public void OnLevelSpawned(TrackTile tile)
    {
        // start in the middle of the tile
        DistanceInTile = tile.TotalDistance / 2f;

        if (SaveLoadManager.Instance.LoadedState != null)
        {
            foreach (var upgrade in SaveLoadManager.Instance.LoadedState.Train.Upgrades)
            {
                // find the upgrade in the registry
                foreach (var availableUpgrade in AllKnownUpgrades)
                {
                    if (availableUpgrade.name == upgrade.Name)
                        UnlockUpgrade(availableUpgrade);
                }
            }

            foreach(var carriage in SaveLoadManager.Instance.LoadedState.Train.Carriages)
            {
                // find the carriage in the registry
                foreach(var availableCarriage in AvailableCarriageBehaviours)
                {
                    if (availableCarriage.name == carriage.Type)
                        SpawnCar(availableCarriage);
                }
            }

            EnergyPowerStored = SaveLoadManager.Instance.LoadedState.Train.EnergyStored;
            ScrapStorageUsed = SaveLoadManager.Instance.LoadedState.Train.ScrapStored;

            SetSpeedFactor(SaveLoadManager.Instance.LoadedState.Train.ThrottleSetting);

            int playerCarriageIndex = SaveLoadManager.Instance.LoadedState.Player.CarriageIndex;
            if (playerCarriageIndex < 0)
                playerCarriageIndex = 0;

            PlayerGO.transform.position = Carriages[playerCarriageIndex].PlayerPosition;
            PlayerGO.transform.SetParent(Carriages[playerCarriageIndex].transform);
        }
        else
        {
            SpawnCar(EngineBehaviour);

            // spawn the initial carriage set
            foreach (var initialCar in InitialCarriages)
            {
                SpawnCar(initialCar);
            }

            RefreshStats();

            ScrapStorageUsed = CurrentGameMode.InitialScrapStored;
            EnergyPowerStored = CurrentGameMode.InitialEnergyPercentage * EnergyStorageCapacity;

            PlayerGO.transform.position = Carriages[0].PlayerPosition;
        }

        RedistributeEnergy();

        RedistributeScrap();

        RefreshStats();
    }

    void SpawnCar(BaseCarriageBehaviour carriageBehaviour)
    {
        // spawn the carriage
        GameObject carriageGO = Instantiate(CarriagePrefab, TrainRoot);
        carriageGO.name = $"Carriage_{(Carriages.Count + 1)}_{carriageBehaviour.name}";
        var carriage = carriageGO.GetComponent<Carriage>();
        carriage.SetBehaviour(carriageBehaviour);
        Carriages.Add(carriage);
        CarriagePositions.Add(new CarriagePositionData());

        RecalculateCarriagePositions();
        ApplyCarriagePositions(true);

        // Refresh the visuals of the previous carriage
        if (Carriages.Count > 2)
            Carriages[Carriages.Count - 2].SynchroniseVisuals();

        RecalculateCapacities();
    }

    void RecalculateCarriagePositions()
    {
        TrackTile workingTile = EternalTrackGenerator.CurrentTile;

        Vector3 headPosition;
        Vector3 tailPosition;
        float distanceInTile = DistanceInTile;
        for (int index = 0; index < Carriages.Count; ++index)
        {
            var carriage = Carriages[index];

            // update the positions if found
            if (EternalTrackGenerator.GetEnginePositionData(workingTile, distanceInTile,
                                                            carriage.CarriageVector,
                                                            out headPosition, out tailPosition))
            {
                carriage.SetCurrentTile(workingTile);

                CarriagePositions[index].HeadPosition = headPosition;
                CarriagePositions[index].TailPosition = tailPosition;
            }
            else
                Debug.LogError("Failed to find position");

            distanceInTile -= carriage.CarriageVector.magnitude + CarriageSpacing;
            if (distanceInTile <= 0)
            {
                workingTile = EternalTrackGenerator.GetPreviousTileTo(workingTile);
                distanceInTile += workingTile.TotalDistance;
            }
        }

        EternalTrackGenerator.SetLastOccupiedTile(workingTile);
    }

    void ApplyCarriagePositions(bool isInitialSpawn)
    {
        for (int index = 0; index < Carriages.Count; ++index)
        {
            var carriagePositionData = CarriagePositions[index];
            var carriage = Carriages[index];

            MoveCarriage(carriage, carriagePositionData.HeadPosition, carriagePositionData.TailPosition, 
                         isInitialSpawn && index == (Carriages.Count - 1));
        }
    }

    void MoveCarriage(Carriage carriage, Vector3 headPosition, Vector3 tailPosition, bool isInitialSpawn)
    {
        Vector3 newPosition = (headPosition + tailPosition) / 2f;
        float yawAngle = Mathf.Atan2(headPosition.x - tailPosition.x, headPosition.z - tailPosition.z);
        Quaternion newRotation = Quaternion.Euler(0f, 90f + yawAngle * Mathf.Rad2Deg, 0f);

        // position the carriage
        if (isInitialSpawn)
        {
            carriage.transform.position = newPosition;
            carriage.transform.rotation = newRotation;
        }
        else
        {
            carriage.transform.position = Vector3.MoveTowards(carriage.transform.position, newPosition, MaxTranslationSpeed * Time.deltaTime);
            carriage.transform.rotation = Quaternion.RotateTowards(carriage.transform.rotation, newRotation, MaxRotationSpeed * Time.deltaTime);
        }
    }

    public void PrepareForSave(SavedGameState savedGame)
    {
        savedGame.Train.EnergyStored = EnergyPowerStored;
        savedGame.Train.ScrapStored = ScrapStorageUsed;
        savedGame.Train.ThrottleSetting = RequestedSpeedFactor;
        savedGame.Train.GameMode = CurrentGameMode.ID;

        foreach(var carriage in Carriages)
        {
            savedGame.Train.Carriages.Add(new SavedGameState.CarriageEntry()
            {
                Type = carriage.Behaviour.Template.name
            });
        }

        foreach (var upgrade in UnlockedUpgrades)
        {
            savedGame.Train.Upgrades.Add(new SavedGameState.UpgradeEntry()
            {
                Name = upgrade.name
            });
        }

        savedGame.Player.CarriageIndex = CurrentPlayerCarriage != null ? Carriages.IndexOf(CurrentPlayerCarriage) : -1;
    }
}
