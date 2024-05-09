using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[System.Serializable]
public class SpawnChanceOverride
{
    public EBuildingType Type;
    [Range(0f, 1f)] public float Probability = 1f;
}

public class RoadPieceMetadata : MonoBehaviour
{
    [SerializeField] EPieceType _PieceType;
    [SerializeField] CinemachineDollyCart ForwardPath;
    [SerializeField] CinemachineDollyCart ReversePath;
    [SerializeField] List<BuildingSpawnPoint> SpawnPoints;
    [SerializeField] List<SpawnChanceOverride> SpawnChanceOverrides;
    EDirection _EntryDirection;
    EDirection _ExitDirection;

    public EPieceType PieceType => _PieceType;
    public EDirection EntryDirection => _EntryDirection;
    public EDirection ExitDirection => _ExitDirection;

    public void SetEntryDirection(EDirection entryDirection, EDirection exitDirection)
    {
        _EntryDirection = entryDirection;
        _ExitDirection = exitDirection;
    }

    public CinemachineDollyCart GetTrack()
    {
        CinemachineDollyCart chosenPath = null;

        if (_PieceType == EPieceType.Straight)
        {
            chosenPath = ForwardPath;
        }
        else if (_PieceType == EPieceType.Corner)
        {
            if (_EntryDirection == EDirection.West && _ExitDirection == EDirection.North)
                chosenPath = ReversePath;
            else if (_EntryDirection == EDirection.North && _ExitDirection == EDirection.East)
                chosenPath = ReversePath;
            else if (_EntryDirection == EDirection.East && _ExitDirection == EDirection.South)
                chosenPath = ReversePath;
            else if (_EntryDirection == EDirection.South && _ExitDirection == EDirection.West)
                chosenPath = ReversePath;
            else if (_EntryDirection == EDirection.West && _ExitDirection == EDirection.South)
                chosenPath = ForwardPath;
            else if (_EntryDirection == EDirection.South && _ExitDirection == EDirection.East)
                chosenPath = ForwardPath;
            else if (_EntryDirection == EDirection.East && _ExitDirection == EDirection.North)
                chosenPath = ForwardPath;
            else if (_EntryDirection == EDirection.North && _ExitDirection == EDirection.West)
                chosenPath = ForwardPath;
        }

        //Debug.Log(_EntryDirection + " => " + _ExitDirection + " using " + chosenPath);

        return chosenPath;
    }

    public void OnSpawned()
    {
        InfiniteRoadsManager.Instance.PrepareToSpawnBuildings();

        // spawn the buildings
        foreach(var spawnPoint in SpawnPoints)
        {
            var prefab = InfiniteRoadsManager.Instance.GetBuildingPrefab(spawnPoint.BuildingType);

            // check for a probability override
            float probability = float.MaxValue;
            foreach(var spawnChanceOverride in SpawnChanceOverrides)
            {
                if (spawnChanceOverride.Type == spawnPoint.BuildingType)
                {
                    probability = spawnChanceOverride.Probability;
                    break;
                }
            }

            // spawn roll faileed
            if (Random.Range(0f, 1f) > probability)
                continue;

            var newBuilding = Instantiate(prefab, Vector3.zero, Quaternion.identity, spawnPoint.transform);
            newBuilding.transform.localPosition = Vector3.zero;
            newBuilding.transform.localRotation = Quaternion.identity;
        }
    }
}
