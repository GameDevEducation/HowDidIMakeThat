using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;

public struct TrackPoint
{
    public Vector3 Position;
    public float PathDistance;
}

public class TrackTile : MonoBehaviour
{
    [SerializeField] MeshFilter LinkedMF;
    [SerializeField] MeshCollider LinkedMC;
    [SerializeField] VisualEffect _RainVFX;
    [SerializeField] VisualEffect _HailVFX;
    [SerializeField] VisualEffect _SnowVFX;
    [SerializeField] Volume _FogVolume;

    [SerializeField] Volume _ClearSkiesVolume;
    [SerializeField] Volume _CloudyVolume;
    [SerializeField] Volume _OvercastVolume;
    [SerializeField] Volume _StormyVolume;

    public Direction Entry { get; private set; }
    public Direction Exit { get; private set; }
    public TrackType Type { get; private set; }
    public float HalfTileSize { get; private set; }
    public TrackPoint[] TrackPoints { get; private set; }
    public int NumTrackPoints => TrackPoints.Length;
    public float TotalDistance { get; private set; }
    public Vector2Int GridLocation { get; private set; }

    public VisualEffect RainVFX => _RainVFX;
    public VisualEffect HailVFX => _HailVFX;
    public VisualEffect SnowVFX => _SnowVFX;
    public Volume FogVolume => _FogVolume;
    public Volume ClearSkiesVolume => _ClearSkiesVolume;
    public Volume CloudyVolume => _CloudyVolume;
    public Volume OvercastVolume => _OvercastVolume;
    public Volume StormyVolume => _StormyVolume;

    public List<GameObject> AllScrap { get; private set; } = new List<GameObject>();

    Dictionary<EObjectSize, List<PlaceableObject>> PlacedScrap = new Dictionary<EObjectSize, List<PlaceableObject>>();
    public Mesh LinkedMesh
    {
        get
        {
            return LinkedMF.sharedMesh;
        }
        set
        {
            LinkedMC.sharedMesh = value;
            LinkedMF.sharedMesh = value;
        }
    }

    public List<PlaceableObject> GetAvailableScrap(EObjectSize size)
    {
        return PlacedScrap.ContainsKey(size) ? PlacedScrap[size] : null;
    }

    public void Bind(Vector2Int _gridLocation, Direction _entry, Direction _exit, TrackType _type, float _halfTileSize, float trackPointSpacing)
    {
        Entry           = _entry;
        Exit            = _exit;
        Type            = _type;
        HalfTileSize    = _halfTileSize;
        GridLocation    = _gridLocation;

        GenerateTrackPoints(trackPointSpacing);
    }

    void GenerateTrackPoints(float maxSpacing = 1f)
    {
        // check if curved
        if ((Entry == Direction.North && Exit == Direction.South) ||
            (Entry == Direction.South && Exit == Direction.North) ||
            (Entry == Direction.East  && Exit == Direction.West) ||
            (Entry == Direction.West  && Exit == Direction.East))
        {
            Vector3 entryPoint = transform.position;
            Vector3 exitPoint  = transform.position;

            if (Entry == Direction.North)
            {
                entryPoint += new Vector3(0f, 0f,  HalfTileSize);
                exitPoint  += new Vector3(0f, 0f, -HalfTileSize);
            }
            else if (Entry == Direction.East)
            {
                entryPoint += new Vector3( HalfTileSize, 0f, 0f);
                exitPoint  += new Vector3(-HalfTileSize, 0f, 0f);
            }
            else if (Entry == Direction.South)
            {
                entryPoint += new Vector3(0f, 0f, -HalfTileSize);
                exitPoint  += new Vector3(0f, 0f,  HalfTileSize);
            }
            else
            {
                entryPoint += new Vector3(-HalfTileSize, 0f, 0f);
                exitPoint  += new Vector3(HalfTileSize,  0f, 0f);
            }

            // determine the number of path points
            Vector3 pathDelta = exitPoint - entryPoint;
            TotalDistance = pathDelta.magnitude;
            int numPoints = Mathf.CeilToInt(TotalDistance / maxSpacing);
            pathDelta /= numPoints;

            // generate the points
            TrackPoints = new TrackPoint[numPoints+1];
            float currentPathDistance = 0f;
            for (int pointIndex = 0; pointIndex <= numPoints; pointIndex++)
            {
                TrackPoints[pointIndex] = new TrackPoint() { Position = entryPoint + (pathDelta * pointIndex), 
                                                             PathDistance = currentPathDistance };
                currentPathDistance += pathDelta.magnitude;
            }
        }
        else
        {
            // determine the number of points and angle delta
            TotalDistance = Mathf.PI * HalfTileSize * 0.5f;
            int numPoints = Mathf.CeilToInt(TotalDistance / maxSpacing);
            float segmentLength = TotalDistance / numPoints;
            float angleDelta = Mathf.PI * 0.5f / numPoints;

            // find the centre point of the circle
            Vector3 referencePoint = transform.position;
            float startAngle = 0f;
            if (Entry == Direction.North)
            {
                angleDelta *= Exit == Direction.East ? -1f : 1f;
                startAngle = Exit == Direction.East ? (Mathf.PI * 1.5f) : (Mathf.PI * 0.5f);
                referencePoint += Exit == Direction.East ? new Vector3(HalfTileSize, 0f, HalfTileSize) :
                                                           new Vector3(-HalfTileSize, 0f, HalfTileSize);
            }
            else if (Entry == Direction.East)
            {
                angleDelta *= Exit == Direction.North ? 1f : -1f;
                startAngle = Exit == Direction.North ? (Mathf.PI * 1f) : 0f;
                referencePoint += Exit == Direction.North ? new Vector3(HalfTileSize, 0f, HalfTileSize) :
                                                            new Vector3(HalfTileSize, 0f, -HalfTileSize);
            }
            else if (Entry == Direction.South)
            {
                angleDelta *= Exit == Direction.East ? 1f : -1f;
                startAngle = Exit == Direction.East ? (Mathf.PI * 1.5f) : (Mathf.PI * 0.5f);
                referencePoint += Exit == Direction.East ? new Vector3(HalfTileSize, 0f, -HalfTileSize) :
                                                           new Vector3(-HalfTileSize, 0f, -HalfTileSize);
            }
            else if (Entry == Direction.West)
            {
                angleDelta *= Exit == Direction.North ? -1f : 1f;
                startAngle = Exit == Direction.North ? (Mathf.PI * 1f) : 0f;
                referencePoint += Exit == Direction.North ? new Vector3(-HalfTileSize, 0f, HalfTileSize) :
                                                            new Vector3(-HalfTileSize, 0f, -HalfTileSize);
            }

            // generate the points
            TrackPoints = new TrackPoint[numPoints + 1];
            float currentPathDistance = 0f;
            float currentAngle = startAngle;
            for (int pointIndex = 0; pointIndex <= numPoints; pointIndex++)
            {
                TrackPoints[pointIndex] = new TrackPoint()
                {
                    Position = referencePoint + HalfTileSize * new Vector3(Mathf.Sin(currentAngle), 0f, Mathf.Cos(currentAngle)),
                    PathDistance = currentPathDistance
                };

                currentAngle += angleDelta;
                currentPathDistance += segmentLength;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    void ClearDestroyedScrap()
    {
        for (int index = AllScrap.Count - 1; index >= 0; index--)
        {
            if (AllScrap[index] == null)
                AllScrap.RemoveAt(index);
        }
    }

    public GameObject PickRandomScrap()
    {
        if (AllScrap == null)
            return null;

        ClearDestroyedScrap();

        if (AllScrap.Count == 0)
            return null;

        return AllScrap[Random.Range(0, AllScrap.Count)];
    }

    private void OnDrawGizmosSelected()
    {
        for (int index = 0; index < TrackPoints.Length - 1; index++)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(TrackPoints[index].Position + Vector3.up, TrackPoints[index + 1].Position + Vector3.up);
        }
    }

    public void AddSpawnedObject(GameObject newObject)
    {
        var placedObject = newObject.GetComponent<PlaceableObject>();
        if (placedObject == null)
            return;

        // track the scrap
        if (placedObject.IsScrap)
        {
            AllScrap.Add(newObject);

            if (!PlacedScrap.ContainsKey(placedObject.Size))
                PlacedScrap[placedObject.Size] = new List<PlaceableObject>(EternalTrackGenerator.ScrapSpaceToReserve);

            PlacedScrap[placedObject.Size].Add(placedObject);
        }
    }
}
