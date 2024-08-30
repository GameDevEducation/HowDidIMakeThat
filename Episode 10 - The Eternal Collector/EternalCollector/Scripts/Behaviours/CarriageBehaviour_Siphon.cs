using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TEC/Carriage Behaviours/Siphon", fileName = "CarriageBehaviour_Siphon")]
public class CarriageBehaviour_Siphon : BaseCarriageBehaviour
{
    public enum ETargetState
    {
        Unknown,
        SiphoningInProgress,
        Collected
    }

    [SerializeField] [Range(0f, 1f)] float IdlePowerUsage = 0.25f;

    [SerializeField] float ScanInterval = 1f;
    [SerializeField] float ScanRange = 64f;
    [SerializeField] float CaptureDistance = 1f;

    [SerializeField] float BaseScanAngle = 30f;
    [SerializeField] float BaseCollectionChance = 0.25f;
    [SerializeField] float BaseStorageCapacity = 500f;

    [SerializeField] float RecentTimeWindow = 5f;
    [SerializeField] float SiphonSpeedBoost = 1.25f;

    Dictionary<Marker, PlaceableObject> SiphonTargets = new Dictionary<Marker, PlaceableObject>();
    Dictionary<Marker, ETargetState> SiphonTargetStates = new Dictionary<Marker, ETargetState>();
    float TimeTillNextScan = 0f;

    float WorkingScanAngle = 0f;
    float WorkingCollectionChance = 0f;

    const int NumRecentItems = 10;
    float[] RecentCollection_Times = new float[NumRecentItems];
    float[] RecentCollection_Weights = new float[NumRecentItems];
    string[] RecentCollection_Names = new string[NumRecentItems];
    int RecentCollection_WriteIndex = 0;

    bool SiphonsEnabled_Small = false;
    bool SiphonsEnabled_Medium = false;
    bool SiphonsEnabled_Large = false;


    public override void RefreshUpgrades()
    {
        WorkingScanAngle        = BaseScanAngle;
        WorkingCollectionChance = BaseCollectionChance;
        _ScrapStorageAvailable  = BaseStorageCapacity;

        foreach (var upgrade in EternalCollector.Instance.UnlockedUpgrades)
        {
            if (!upgrade.ApplicableBehaviours.Contains(TemplateBehaviour))
                continue;

            foreach (var effect in upgrade.Effects)
            {
                if (effect.Target == EUpgradeTarget.Siphon_CollectionChance)
                    effect.Modify(ref WorkingCollectionChance);
                else if (effect.Target == EUpgradeTarget.Siphon_ScanAngle)
                    effect.Modify(ref WorkingScanAngle);
                else if (effect.Target == EUpgradeTarget.Common_ScrapStorageCapacity)
                    effect.Modify(ref _ScrapStorageAvailable);
            }
        }

        base.RefreshUpgrades();
    }

    public override void Tick_Power()
    {
        // intentionally not running base.Tick_Power() as we only use power when siphoning

        // baseline amount of power if we are not siphoning things
        float powerRequested = IdlePowerUsage * _PowerUsage;

        // increase the power usage if we are actively grabbing things
        if (LinkedCarriage.ActiveMarkers.Count > 0)
            powerRequested += (1f - IdlePowerUsage) * _PowerUsage * SiphonTargets.Count / LinkedCarriage.ActiveMarkers.Count;

        // determine the amount of power needed based on number of targets being grabbed
        powerRequested *= ToDManager.Instance.DeltaTime;
        powerRequested /= 3600f; // convert to kWH

        // request the amount of power needed
        EternalCollector.Instance.RequestPower(powerRequested);
    }

    protected override float GetAmbientAudioIntensity()
    {
        if (CanSiphonTargets)
        {
            if (SiphonsEnabled_Large)
                return 100f;
            else if (SiphonsEnabled_Medium)
                return 60f;
            else if (SiphonsEnabled_Small)
                return 25f;
        }
        return 0f;
    }

    bool CanSiphonTargets
    {
        get
        {
            // if we're out of power then we can't siphon anything
            if (EternalCollector.Instance.OutOfPower)
                return false;

            // no spare storage capacity?
            if (ScrapStorageUsed >= ScrapStorageAvailable)
                return false;

            return true;
        }
    }

    public override void Tick_General()
    {
        base.Tick_General();

        SpringCleanItems();

        // has our set of siphons changed?
        if (SiphonTargets.Count != LinkedCarriage.ActiveMarkers.Count)
        {
            SiphonsEnabled_Small = false;
            SiphonsEnabled_Medium = false;
            SiphonsEnabled_Large = false;

            // populate the list of siphons
            foreach (var marker in LinkedCarriage.ActiveMarkers)
            {
                if (marker.Type == Marker.EMarkerType.Siphon_Small)
                    SiphonsEnabled_Small = true;
                if (marker.Type == Marker.EMarkerType.Siphon_Medium)
                    SiphonsEnabled_Medium = true;
                if (marker.Type == Marker.EMarkerType.Siphon_Large)
                    SiphonsEnabled_Large = true;

                if (!SiphonTargets.ContainsKey(marker))
                {
                    SiphonTargets[marker] = null;
                    SiphonTargetStates[marker] = ETargetState.Unknown;
                }
            }
        }

        if (CanSiphonTargets)
        {
            // is it time to scan for targets?
            TimeTillNextScan -= Time.deltaTime;
            if (TimeTillNextScan <= 0f)
            {
                TimeTillNextScan += ScanInterval;
                ScanForTargets();
            }

            UpdateSiphoning(false);
        }
        else
            UpdateSiphoning(true);
    }

    void ScanForTargets()
    {
        var smallScrap = CurrentTile.GetAvailableScrap(EObjectSize.Small);
        var mediumScrap = CurrentTile.GetAvailableScrap(EObjectSize.Medium);
        var largeScrap = CurrentTile.GetAvailableScrap(EObjectSize.Large);

        // find targets for any siphons without ones
        foreach (var siphon in LinkedCarriage.ActiveMarkers)
        {
            // currently has no target?
            var currentState = SiphonTargetStates[siphon];
            if (currentState == ETargetState.Unknown ||
                SiphonTargets[siphon] == null)
            {
                // tile may have been unloaded so reset the object
                SiphonTargetStates[siphon] = ETargetState.Unknown;

                var scrapPool = smallScrap;
                if (siphon.Type == Marker.EMarkerType.Siphon_Medium)
                    scrapPool = mediumScrap;
                else if (siphon.Type == Marker.EMarkerType.Siphon_Large)
                    scrapPool = largeScrap;
                AttemptToFindTarget(siphon, scrapPool);
            }
        }
    }

    void AttemptToFindTarget(Marker siphon, List<PlaceableObject> scrapPool)
    {
        float cosScanAngle = Mathf.Cos(Mathf.Deg2Rad * WorkingScanAngle);
        List<PlaceableObject> candidateTargets = new List<PlaceableObject>(scrapPool.Count);

        // filter the targets to find ones that are potentially viable
        foreach(var scrap in scrapPool)
        {
            // scrap pools can validly contain nulls
            if (scrap == null)
                continue;

            // skip if already being siphoned
            if (scrap.IsBeingSiphoned)
                continue;

            var vectorToScrap = scrap.transform.position - siphon.transform.position;

            // out of range
            if (vectorToScrap.sqrMagnitude > ScanRange * ScanRange)
                continue;

            // out of angle
            vectorToScrap.y = 0;
            if (Vector3.Dot(siphon.transform.forward, vectorToScrap.normalized) < cosScanAngle)
                continue;

            // failed the pickup dice roll
            if (Random.Range(0f, 1f) > WorkingCollectionChance)
                continue;

            candidateTargets.Add(scrap);
        }

        // check if this target is collectable
        if (candidateTargets.Count > 0)
        {
            var target = candidateTargets[Random.Range(0, candidateTargets.Count)];
            SiphonTargets[siphon] = target;
            SiphonTargetStates[siphon] = ETargetState.SiphoningInProgress;
            target.BeginSiphoning();
        }
    }

    void UpdateSiphoning(bool forceDrop)
    {
        // move the objects towards the emitters
        foreach (var siphon in LinkedCarriage.ActiveMarkers)
        {
            // currently being siphoned?
            var currentState = SiphonTargetStates[siphon];
            if (currentState == ETargetState.SiphoningInProgress)
            {
                UpdateSiphoning(siphon, SiphonTargets[siphon], forceDrop);
            }
        }
    }

    void UpdateSiphoning(Marker siphon, PlaceableObject target, bool forceDrop)
    {
        // target has been unloaded
        if (target == null)
        {
            SiphonTargetStates[siphon] = ETargetState.Unknown;
            return;
        }

        if (forceDrop)
        {
            target.PerformFall();

            SiphonTargets[siphon] = null;
            SiphonTargetStates[siphon] = ETargetState.Unknown;

            return;
        }

        float ScrapMoveSpeed = Mathf.Max(EternalCollector.Instance.CurrentSpeed * SiphonSpeedBoost, 10);

        target.transform.position = Vector3.MoveTowards(target.transform.position, 
                                                        siphon.transform.position,
                                                        ScrapMoveSpeed * Time.deltaTime);

        // within capture range?
        if (Vector3.Distance(siphon.transform.position, target.transform.position) < CaptureDistance)
        {
            if (target.Size == EObjectSize.Small)
                AkSoundEngine.PostEvent(AK.EVENTS.PLAY_SIPHON_SMALLSCRAP, siphon.gameObject);
            else if (target.Size == EObjectSize.Medium)
                AkSoundEngine.PostEvent(AK.EVENTS.PLAY_SIPHON_MEDIUMSCRAP, siphon.gameObject);
            else if (target.Size == EObjectSize.Large)
                AkSoundEngine.PostEvent(AK.EVENTS.PLAY_SIPHON_LARGESCRAP, siphon.gameObject);

            // Attempt to store the scrap
            if (ScrapStorageUsed < ScrapStorageAvailable)
            {
                _ScrapStorageUsed += Mathf.Min(EternalCollector.EffectScrapWeight(target.ChosenWeight), 
                                               ScrapStorageAvailable - ScrapStorageUsed);

                StoreItem(target);
            }

            Destroy(target.gameObject);
            SiphonTargets[siphon] = null;
            SiphonTargetStates[siphon] = ETargetState.Unknown;
        }
    }

    void RefreshRecentItemStats()
    {
        // process the recent items
        _RecentItems = string.Empty;
        _AcquisitionRate = "---";
        float minTime = float.MaxValue;
        float maxTime = float.MinValue;
        float recentMassSum = 0f;
        for (int offset = 0; offset < NumRecentItems; offset++)
        {
            int index = (RecentCollection_WriteIndex + offset + 1) % NumRecentItems;

            if (RecentCollection_Times[index] == 0)
                continue;

            if (_RecentItems.Length > 0)
                _RecentItems += System.Environment.NewLine;

            _RecentItems += RecentCollection_Names[index];

            minTime = Mathf.Min(minTime, RecentCollection_Times[index]);
            maxTime = Mathf.Max(maxTime, RecentCollection_Times[index]);
            recentMassSum += RecentCollection_Weights[index];
        }

        float deltaTime = maxTime - minTime;
        if (deltaTime > 0)
            _AcquisitionRate = $"{(recentMassSum / deltaTime):0.0 kg/s}";
    }

    string _AcquisitionRate = null;
    public string AcquisitionRate
    {
        get
        {
            if (_AcquisitionRate == null)
                RefreshRecentItemStats();

            return _AcquisitionRate;
        }
    }

    string _RecentItems = null;
    public string RecentItems
    {
        get
        {
            if (_RecentItems == null)
                RefreshRecentItemStats();

            return _RecentItems;
        }
    }

    void SpringCleanItems()
    {
        float timeHorizon = Time.time - RecentTimeWindow;
        for (int index = 0; index < NumRecentItems; index++)
        {
            if (RecentCollection_Times[index] <= timeHorizon)
            {
                RecentCollection_Times[index] = 0f;
                RecentCollection_Weights[index] = 0f;
                RecentCollection_Names[index] = null;

                _RecentItems = null;
            }
        }
    }

    void StoreItem(PlaceableObject item)
    {
        RecentCollection_Times[RecentCollection_WriteIndex] = Time.time;
        RecentCollection_Weights[RecentCollection_WriteIndex] = EternalCollector.EffectScrapWeight(item.ChosenWeight);
        RecentCollection_Names[RecentCollection_WriteIndex] = item.DisplayName;

        _RecentItems = null;

        RecentCollection_WriteIndex = (RecentCollection_WriteIndex + 1) % NumRecentItems;
    }
}

