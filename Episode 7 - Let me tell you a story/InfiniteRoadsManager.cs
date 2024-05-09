using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public enum EPieceType
{
    Straight,
    Corner
}

public enum EPathElement
{
    Straight,
    Left,
    Right
}

public enum EDirection
{
    North,
    East,
    South,
    West
}

[System.Serializable]
public class RoadPiece
{
    public GameObject Prefab;
    public EPieceType Type;
}

public class InfiniteRoadsManager : MonoBehaviour
{
    [SerializeField] List<RoadPiece> RoadPieces;
    [SerializeField] List<GameObject> SpawnableItems;
    [SerializeField] Transform PieceRoot;
    [SerializeField] int NumPiecesAhead = 5;
    [SerializeField] int NumPiecesBehind = 5;
    [SerializeField] float PieceSize = 50.8f;
    [SerializeField] CarHandler LinkedCar;

    [SerializeField] float BaseEasterEggChance = 0.01f;
    [SerializeField] float EasterEggChanceIncrease = 0.01f;
    [SerializeField] float EasterEggDayChance = 0.5f;

    float EasterEggChance = 0f;
    bool HasSpawnedEasterEgg = false;

    RoadPieceMetadata[] ActivePieces;
    int CarPiece = -1;

    int NumLeftTurnsPermitted = 1;
    int NumRightTurnsPermitted = 1;
    int NumForwardPiecesPermitted = 3;
    int MaxForwardPiecesPermitted = 3;
    EDirection CurrentDirection = EDirection.North;
    Vector3 NextSpawnLocation;
    bool ForceForward = false;

    public static InfiniteRoadsManager Instance { get; private set; } = null;

    Dictionary<EBuildingType, List<GameObject>> EasterEggBuildingPrefabs = new Dictionary<EBuildingType, List<GameObject>>();
    Dictionary<EBuildingType, List<GameObject>> NonEasterEggBuildingPrefabs = new Dictionary<EBuildingType, List<GameObject>>();

    void Awake()
    {
        Instance = this;

        // build up the prefabs
        foreach(var buildingPrefab in SpawnableItems)
        {
            var buildingScript = buildingPrefab.GetComponent<SpawnableBuilding>();

            // add in the key if missing
            if (!EasterEggBuildingPrefabs.ContainsKey(buildingScript.BuildingType))
                EasterEggBuildingPrefabs[buildingScript.BuildingType] = new List<GameObject>();
            if (!NonEasterEggBuildingPrefabs.ContainsKey(buildingScript.BuildingType))
                NonEasterEggBuildingPrefabs[buildingScript.BuildingType] = new List<GameObject>();


            if (!buildingScript.IsEasterEgg)
                NonEasterEggBuildingPrefabs[buildingScript.BuildingType].Add(buildingPrefab);
            else
                EasterEggBuildingPrefabs[buildingScript.BuildingType].Add(buildingPrefab);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        SpawnInitialPieces();

        CarPiece = ActivePieces.Length / 2;
        PlaceCar(ActivePieces[CarPiece], Random.Range(0.25f, 0.75f));
    }

    // Update is called once per frame
    void Update()
    {
    }

    public CinemachineDollyCart ReachedEndOfSegment()
    {
        SpawnNextBlock();
        return ActivePieces[CarPiece].GetTrack();
    }

    public void ForceOnlyForward()
    {
        ForceForward = true;
    }

    EPathElement PickElement()
    {
        // are we forcing only forward movement
        if (ForceForward)
            return EPathElement.Straight;

        // build the set of candidate elements
        List<EPathElement> candidateElements = new List<EPathElement>();
        for (int count = 0; count < NumLeftTurnsPermitted; ++count)
            candidateElements.Add(EPathElement.Left);
        for (int count = 0; count < NumRightTurnsPermitted; ++count)
            candidateElements.Add(EPathElement.Right);
        for (int count = 0; count < NumForwardPiecesPermitted; ++count)
            candidateElements.Add(EPathElement.Straight);

        // pick a random element
        var element = candidateElements[Random.Range(0, candidateElements.Count)];

        // turning left or right impacts on the probability of being able to turn that way again
        if (element == EPathElement.Left)
        {
            --NumLeftTurnsPermitted;
            ++NumRightTurnsPermitted;
            NumForwardPiecesPermitted = Mathf.Min(NumForwardPiecesPermitted + 1, MaxForwardPiecesPermitted);
        }
        else if (element == EPathElement.Right)
        {
            --NumRightTurnsPermitted;
            ++NumLeftTurnsPermitted;
            NumForwardPiecesPermitted = Mathf.Min(NumForwardPiecesPermitted + 1, MaxForwardPiecesPermitted);
        }
        else if (element == EPathElement.Straight)
        {
            --NumForwardPiecesPermitted;
        }

        return element;
    }

    void SpawnInitialPieces()
    {
        int numToSpawn = NumPiecesAhead + NumPiecesBehind + 1;
        ActivePieces = new RoadPieceMetadata[numToSpawn];

        // spawn all of the pieces
        NextSpawnLocation = PieceRoot.transform.position;
        for (int index = 0; index < numToSpawn; ++index)
        {
            var elementType = index == 0 ? EPathElement.Straight : PickElement();
            ActivePieces[index] = SpawnPiece(elementType);
        }
    }

    void SpawnNextBlock()
    {
        // destroy the oldest piece and shuffle forward the pieces
        Destroy(ActivePieces[0].gameObject);
        for (int index = 1; index < ActivePieces.Length; ++index)
        {
            ActivePieces[index - 1] = ActivePieces[index];
        }

        ActivePieces[ActivePieces.Length - 1] = SpawnPiece(PickElement());
    }

    RoadPieceMetadata SpawnPiece(EPathElement elementType)
    {
        // first locate all the potential pieces
        List<GameObject> candidatePieces = new List<GameObject>();
        foreach(var roadPiece in RoadPieces)
        {
            if (elementType == EPathElement.Straight && roadPiece.Type == EPieceType.Straight)
                candidatePieces.Add(roadPiece.Prefab);
            else if ((elementType == EPathElement.Left || elementType == EPathElement.Right) && 
                     roadPiece.Type == EPieceType.Corner)
            {
                candidatePieces.Add(roadPiece.Prefab);
            }
        }

        // pick a random piece
        var pieceToSpawn = candidatePieces[Random.Range(0, candidatePieces.Count)];

        // determine the piece rotation
        var spawnRotation = Quaternion.identity;
        if (elementType == EPathElement.Left)
        {
            if (CurrentDirection == EDirection.North)
                spawnRotation = Quaternion.Euler(0, 270f, 0);
            else if (CurrentDirection == EDirection.East)
                spawnRotation = Quaternion.Euler(0, 0f, 0);
            else if (CurrentDirection == EDirection.South)
                spawnRotation = Quaternion.Euler(0, 90f, 0);
        }
        else if (elementType == EPathElement.Right)
        {
            if (CurrentDirection == EDirection.East)
                spawnRotation = Quaternion.Euler(0, 270f, 0);
            else if (CurrentDirection == EDirection.North)
                spawnRotation = Quaternion.Euler(0, 180f, 0);
            else if (CurrentDirection == EDirection.West)
                spawnRotation = Quaternion.Euler(0, 90f, 0);
        }
        else if (elementType == EPathElement.Straight)
        {
            if (CurrentDirection == EDirection.East)
                spawnRotation = Quaternion.Euler(0, 90f, 0);
            else if (CurrentDirection == EDirection.South)
                spawnRotation = Quaternion.Euler(0, 180, 0);
            else if (CurrentDirection == EDirection.West)
                spawnRotation = Quaternion.Euler(0, 270f, 0);
        }

        // spawn the road piece
        var roadPieceGO = Instantiate(pieceToSpawn, NextSpawnLocation, spawnRotation, PieceRoot);
        var roadPieceScript = roadPieceGO.GetComponent<RoadPieceMetadata>();

        // update the direction
        var previousDirection = CurrentDirection;
        if (elementType == EPathElement.Left)
        {
            if (CurrentDirection == EDirection.North)
                CurrentDirection = EDirection.West;
            else if (CurrentDirection == EDirection.West)
                CurrentDirection = EDirection.South;
            else if (CurrentDirection == EDirection.South)
                CurrentDirection = EDirection.East;
            else if (CurrentDirection == EDirection.East)
                CurrentDirection = EDirection.North;
        }
        else if (elementType == EPathElement.Right)
        {
            if (CurrentDirection == EDirection.North)
                CurrentDirection = EDirection.East;
            else if (CurrentDirection == EDirection.East)
                CurrentDirection = EDirection.South;
            else if (CurrentDirection == EDirection.South)
                CurrentDirection = EDirection.West;
            else if (CurrentDirection == EDirection.West)
                CurrentDirection = EDirection.North;  
        }

        if (CurrentDirection == EDirection.North)
            NextSpawnLocation += new Vector3(0f, 0f, PieceSize);
        else if (CurrentDirection == EDirection.East)
            NextSpawnLocation += new Vector3(PieceSize, 0f, 0f);
        else if (CurrentDirection == EDirection.South)
            NextSpawnLocation += new Vector3(0f, 0f, -PieceSize);
        else if (CurrentDirection == EDirection.West)
            NextSpawnLocation += new Vector3(-PieceSize, 0f, 0f);

        roadPieceScript.SetEntryDirection(previousDirection, CurrentDirection);
        roadPieceScript.OnSpawned();

        return roadPieceScript;
    }

    void PlaceCar(RoadPieceMetadata roadPiece, float percentageThroughPiece)
    {
        LinkedCar.PlaceCar(roadPiece.GetTrack(), percentageThroughPiece);
    }

    bool IsEasterEggDate
    {
        get
        {
            return System.DateTime.Now.Day == 23 && System.DateTime.Now.Month == 11;
        }
    }

    public GameObject GetBuildingPrefab(EBuildingType requiredType)
    {
        Dictionary<EBuildingType, List<GameObject>> prefabSet = NonEasterEggBuildingPrefabs;

        // can we spawn an easter egg for this building type?
        if (!HasSpawnedEasterEgg && EasterEggBuildingPrefabs[requiredType].Count > 0)
        {
            float workingChance = IsEasterEggDate ? EasterEggDayChance : EasterEggChance;

            // spawn check passed?
            if (Random.Range(0f, 1f) < workingChance)
            {
                EasterEggChance = BaseEasterEggChance;
                prefabSet = EasterEggBuildingPrefabs;

                // if it's not the easter egg date allow multiple easter eggs in the one tile
                if (!IsEasterEggDate)
                    HasSpawnedEasterEgg = true;
            }
            else
                EasterEggChance += EasterEggChanceIncrease;
        }

        var availablePrefabs = prefabSet[requiredType];
        return availablePrefabs[Random.Range(0, availablePrefabs.Count)];
    }

    public void PrepareToSpawnBuildings()
    {
        HasSpawnedEasterEgg = false;
    }
}
