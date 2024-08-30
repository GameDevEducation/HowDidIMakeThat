using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

public enum EComparisonType
{
    None,
    Any,
    All
}

[System.Serializable]
public class VisibilityRule
{
    public EComparisonType PresentComparison;
    public List<CarriageUpgrade> UpgradesPresent;

    public EComparisonType AbsentComparison;
    public List<CarriageUpgrade> UpgradesAbsent;

    public bool Matches()
    {
        bool matchFound = false;

        // check if any ones that must be present are
        if (UpgradesPresent != null && UpgradesPresent.Count > 0)
        {
            int numPresent = 0;
            foreach (var upgrade in UpgradesPresent)
            {
                if (EternalCollector.IsUpgradeUnlocked(upgrade))
                    ++numPresent;
            }

            if (PresentComparison == EComparisonType.All && numPresent == UpgradesPresent.Count)
                matchFound = true;
            else if (PresentComparison == EComparisonType.Any && numPresent > 0)
                matchFound = true;
            else if (PresentComparison == EComparisonType.None && numPresent == 0)
                matchFound = true;

            if (!matchFound)
                return false;
        }
        else
            matchFound = true;

        if (UpgradesAbsent != null && UpgradesAbsent.Count > 0)
        {
            int numAbsent = 0;
            foreach (var upgrade in UpgradesAbsent)
            {
                if (!EternalCollector.IsUpgradeUnlocked(upgrade))
                    ++numAbsent;
            }

            if (AbsentComparison == EComparisonType.All)
                matchFound &= numAbsent == UpgradesAbsent.Count;
            else if (AbsentComparison == EComparisonType.Any)
                matchFound &= numAbsent > 0;
            else if (AbsentComparison == EComparisonType.None)
                matchFound &= numAbsent == 0;
        }

        return matchFound;
    }
}

[System.Serializable]
public class CarriageMapEntry
{
    [System.Flags]
    public enum ECarriagePosition : int
    {
        Head    = 0x01,
        Body    = 0x02,
        Tail    = 0x04,

        All = Head | Body | Tail
    }

    public string Path;
    public GameObject LinkedMesh_LOD0;
    public GameObject LinkedMesh_LOD1;
    public GameObject LinkedCollision;
    public GameObject LinkedUI;
    public GameObject LinkedLight;
    public List<Marker> LinkedMarkers;

    public List<BaseCarriageBehaviour> SupportedBehaviours;
    public List<VisibilityRule> VisibilityRules;
    public ECarriagePosition ValidPositions = ECarriagePosition.All;
    public bool RequiresUpperDeck = false;
    public bool RequiresNoUpperDeck = false;

    public List<Marker> Synchronise(BaseCarriageBehaviour behaviour, Carriage currentCarriage)
    {
        bool isVisible = false;

        // no behaviour filter or behaviour filter matches
        if (SupportedBehaviours == null || SupportedBehaviours.Count == 0 || 
            SupportedBehaviours.Contains(behaviour))
        {
            // no visibility rules so must be visible
            if (VisibilityRules == null || VisibilityRules.Count == 0)
                isVisible = true;
            else
            {
                foreach(var rule in VisibilityRules)
                {
                    if (rule.Matches())
                    {
                        isVisible = true;
                        break;
                    }
                }
            }
        }

        // check if it requires an upper deck be present or absent
        if (!currentCarriage.HasUpperDeck && RequiresUpperDeck)
            isVisible = false;
        if (currentCarriage.HasUpperDeck && RequiresNoUpperDeck)
            isVisible = false;

        // check if valid for this carriage location
        if (currentCarriage.IsTail && !ValidPositions.HasFlag(ECarriagePosition.Tail))
            isVisible = false;
        else if (currentCarriage.IsHead && !ValidPositions.HasFlag(ECarriagePosition.Head))
            isVisible = false;
        else if (currentCarriage.IsBody && !ValidPositions.HasFlag(ECarriagePosition.Body))
            isVisible = false;

        if (LinkedMesh_LOD0 != null && LinkedMesh_LOD1 != null)
        {
            LinkedMesh_LOD0.SetActive(isVisible && currentCarriage.UseHighLOD);
            LinkedMesh_LOD1.SetActive(isVisible && !currentCarriage.UseHighLOD);
        }
        else if (LinkedMesh_LOD0 != null)
            LinkedMesh_LOD0.SetActive(isVisible);
        if (LinkedCollision != null)
            LinkedCollision.SetActive(isVisible);
        if (LinkedUI != null)
            LinkedUI.SetActive(isVisible);
        if (LinkedLight != null)
            LinkedLight.SetActive(isVisible);

        return isVisible ? LinkedMarkers : null;
    }

    public void UpdateLOD(bool useHighLOD)
    {
        if (LinkedMesh_LOD0 != null && LinkedMesh_LOD1 != null)
        {
            bool isVisible = LinkedMesh_LOD0.activeSelf || LinkedMesh_LOD1.activeSelf;

            LinkedMesh_LOD0.SetActive(isVisible && useHighLOD);
            LinkedMesh_LOD1.SetActive(isVisible && !useHighLOD);
        }
    }
}

[System.Serializable]
public class VisualEffectSet
{
    public BaseCarriageBehaviour RequiredBehaviour;
    public List<VisualEffect> Effects;
}

public class Carriage : MonoBehaviour
{
    [SerializeField] Transform Head;
    [SerializeField] Transform Tail;
    [SerializeField] Transform PlayerSpawn;
    [SerializeField] Transform CameraMount;
    [SerializeField] Transform MeshRoot;
    [SerializeField] Transform ColliderRoot;

    [SerializeField] List<CarriageMapEntry> ComponentMap;
    [SerializeField] BaseCarriageBehaviour TemplateBehaviour;
    [SerializeField] List<VisualEffectSet> VFXSets;

    [SerializeField] AK.Wwise.Event StartAmbientAudio;
    [SerializeField] AK.Wwise.Event StopAmbientAudio;
    [SerializeField] AK.Wwise.RTPC AmbientAudioRTPC;

    BaseCarriageBehaviour ActiveBehaviour;

    public Vector3 CarriageVector => Tail.position - Head.position;
    public Vector3 TailPosition => Tail.position;
    public Vector3 HeadPosition => Head.position;
    public Vector3 PlayerPosition => PlayerSpawn.position;
    public Vector3 CameraMountPosition => CameraMount.position;
    public Transform CameraMountTransform => CameraMount;
    public TrackTile CurrentTile { get; private set; }
    public List<Marker> ActiveMarkers { get; private set; } = new List<Marker>();

    public float EnginePower                => ActiveBehaviour.EnginePower;
    public float Weight                     => ActiveBehaviour.Weight;

    public float ScrapStorageAvailable      => ActiveBehaviour.ScrapStorageAvailable;
    public float ScrapStorageUsed           => ActiveBehaviour.ScrapStorageUsed;

    public float EnergyStorageCapacity      => ActiveBehaviour.EnergyStorageCapacity;
    public float EnergyPowerCapacity        => ActiveBehaviour.EnergyPowerCapacity;

    public EUIScreen CarriageUIType         => ActiveBehaviour.CarriageUIType;
    public BaseCarriageBehaviour Behaviour  => ActiveBehaviour;

    public bool IsHead => EternalCollector.Instance != null ? EternalCollector.Instance.IsHead(this) : false;
    public bool IsBody => EternalCollector.Instance != null ? EternalCollector.Instance.IsBody(this) : true;
    public bool IsTail => EternalCollector.Instance != null ? EternalCollector.Instance.IsTail(this) : false;
    public bool HasUpperDeck { get; private set; } = false;

    bool WasUsingHighLOD;
    public bool UseHighLOD
    {
        get
        {
            if (CharacterMotor.Instance != null)
            {
                Vector3 vectorToPlayer = transform.position - CharacterMotor.Instance.transform.position;
                vectorToPlayer.y = 0;

                return vectorToPlayer.sqrMagnitude <= EternalCollector.Instance.HighLODDistanceSq;
            }

            return false;
        }
    }

    public void Start()
    {
        if (TemplateBehaviour != null)
            SynchroniseVisuals();

        WasUsingHighLOD = UseHighLOD;
    }

    public void Update()
    {
        bool shouldUseHighLOD = UseHighLOD;
        if (shouldUseHighLOD != WasUsingHighLOD)
        {
            WasUsingHighLOD = shouldUseHighLOD;

            foreach (var entry in ComponentMap)
            {
                entry.UpdateLOD(shouldUseHighLOD);
            }
        }

        if (AmbientAudioRTPC.IsValid())
            AmbientAudioRTPC.SetValue(gameObject, Mathf.Lerp(25f, 100f, EternalCollector.Instance.EffectiveSpeedFactor));
    }

    private void OnDestroy()
    {
        if (ActiveBehaviour != null)
            ActiveBehaviour.OnDestroyCarriage();
    }

    public void OnPlayerEntered()
    {
        if (ActiveBehaviour != null)
            ActiveBehaviour.OnPlayerEntered();

        if (StartAmbientAudio.IsValid())
            StartAmbientAudio.Post(gameObject);
    }

    public void OnPlayerLeft()
    {
        if (ActiveBehaviour != null)
            ActiveBehaviour.OnPlayerLeft();

        if (StopAmbientAudio.IsValid())
            StopAmbientAudio.Post(gameObject);
    }

    public void Tick_Power()
    {
        if (ActiveBehaviour != null)
            ActiveBehaviour.Tick_Power();
    }

    public void Tick_General()
    {
        if (ActiveBehaviour != null)
            ActiveBehaviour.Tick_General();
    }

    public void SetBehaviour(BaseCarriageBehaviour behaviour)
    {
        TemplateBehaviour = behaviour;
        ActiveBehaviour = ScriptableObject.Instantiate(TemplateBehaviour);
        SynchroniseVisuals();
        ActiveBehaviour.Bind(this, TemplateBehaviour);
    }

    public void SetCurrentTile(TrackTile tile)
    {
        CurrentTile = tile;
        if (ActiveBehaviour != null)
            ActiveBehaviour.SetCurrentTile(tile);
    }

    public List<VisualEffect> GetVFX(BaseCarriageBehaviour behaviour)
    {
        foreach(var vfxSet in VFXSets)
        {
            if (vfxSet.RequiredBehaviour == behaviour)
                return vfxSet.Effects;
        }

        return null;
    }

    public void SynchroniseVisuals()
    {
        ActiveMarkers.Clear();

        HasUpperDeck = false;

        // check if this carriage has an upper deck based on the current upgrades
        if (EternalCollector.Instance != null)
        {
            foreach (var upgrade in EternalCollector.Instance.UnlockedUpgrades)
            {
                if (upgrade.ApplicableBehaviours.Contains(TemplateBehaviour))
                {
                    HasUpperDeck |= upgrade.AddsUpperDeck;
                }
            }
        }

        // Process each component in the map to determine if visible
        foreach (var entry in ComponentMap)
        {
            var markers = entry.Synchronise(TemplateBehaviour, this);

            if (markers != null)
                ActiveMarkers.AddRange(markers);
        }

        if (ActiveBehaviour != null)
            ActiveBehaviour.RefreshUpgrades();
    }

    public void SetScrapStorageUsed(float newStorageUsed)
    {
        if (ActiveBehaviour != null)
            ActiveBehaviour.SetScrapStorageUsed(newStorageUsed);
    }

    public void SetEnergyStorageUsed(float newStorageUsed)
    {
        if (ActiveBehaviour != null)
            ActiveBehaviour.SetEnergyStorageUsed(newStorageUsed);
    }

#if UNITY_EDITOR
    public void RebuildCarriageData()
    {
        // need to have both a mesh and collision root
        if (MeshRoot == null || ColliderRoot == null)
            throw new System.ArgumentNullException("MeshRoot or ColliderRoot are null");

        // build up a map of the child objects
        Dictionary<string, Transform> meshCategories = new Dictionary<string, Transform>();
        for (int index = 0; index < MeshRoot.childCount; index++)
        {
            var child = MeshRoot.GetChild(index);
            meshCategories[child.name] = child;
        }

        Dictionary<string, Transform> colliderCategories = new Dictionary<string, Transform>();
        for (int index = 0; index < ColliderRoot.childCount; index++)
        {
            var child = ColliderRoot.GetChild(index);
            colliderCategories[child.name] = child;
        }

        RebuildCarriageData_Internal(meshCategories, colliderCategories);
    }

    void RebuildCarriageData_Internal(Dictionary<string, Transform> meshCategories,
                                      Dictionary<string, Transform> colliderCategories)
    {
        Undo.RegisterCompleteObjectUndo(this, "Rebuild component map");

        ComponentMap = new List<CarriageMapEntry>();

        // traverse the mesh categories
        foreach (var kvp in meshCategories)
        {
            // first build up the list of colliders for this category if present
            Dictionary<string, GameObject> colliders = new Dictionary<string, GameObject>();
            if (colliderCategories.ContainsKey(kvp.Key))
            {
                var colliderSet = colliderCategories[kvp.Key];
                for (int index = 0; index < colliderSet.childCount; ++index)
                {
                    var child = colliderSet.GetChild(index);
                    colliders[child.name] = child.gameObject;
                }
            }

            // now loop through the meshes for this category
            var meshSet = kvp.Value;
            for (int index = 0; index < meshSet.childCount; ++index)
            {
                var meshGO = meshSet.GetChild(index).gameObject;
                var colliderGO = colliders.ContainsKey(meshGO.name) ? colliders[meshGO.name] : null;
                var fullPath = kvp.Key + "/" + meshGO.name;

                ComponentMap.Add(new CarriageMapEntry() {
                                                            Path = fullPath,
                                                            LinkedMesh_LOD0 = meshGO,
                                                            LinkedCollision = colliderGO
                                                        });
            }
        }

        EditorUtility.SetDirty(this);
    }
#endif // UNITY_EDITOR
}
