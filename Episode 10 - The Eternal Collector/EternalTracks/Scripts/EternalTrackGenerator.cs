using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

[System.Serializable]
public class BiomeSpawnSettings
{
    public BiomeConfig Biome;
    public int MinSegments = 2;
    public int MaxSegments = 5;
}

public class EternalTrackGenerator : MonoBehaviour
{
    public enum TrackResolution
    {
        Resolution_64x64 = 64,
        Resolution_96x96 = 96,
        Resolution_128x128 = 128,
        Resolution_256x256 = 256,
        Resolution_512x512 = 512,
        Resolution_1024x1024 = 1024,
    }

    [SerializeField] float TileSize = 400f;
    [SerializeField] TrackResolution Resolution = TrackResolution.Resolution_64x64;
    [SerializeField] float MaximumHeight = 64f;
    [SerializeField] int NumTilesToRetain = 5;
    [SerializeField] float TrackPointSpacing = 0.5f;
    [SerializeField] int NumTrackTileMeshesInPool = 25;

    [SerializeField] float TrackFadeDistance = 64f;
    [SerializeField] AnimationCurve TrackFadeCurve;

    [SerializeField] float TileBlendDistance = 64f;
    [SerializeField] AnimationCurve TileBlendCurve;

    [SerializeField] GameObject TilePrefab;

    [SerializeField] List<BiomeSpawnSettings> Biomes;
    [SerializeField] Transform TrackRoot;

    [SerializeField] int NumStartingTiles = 10;
    [SerializeField] UnityEvent<TrackTile> OnLevelSpawned = new UnityEvent<TrackTile>();

    [SerializeField] int DefaultSpawnAmount = 250;

    int CurrentTileIndex = 0;
    public static TrackTile CurrentTile => Instance.SpawnedTiles[Instance.CurrentTileIndex];

    public static int CurrentObjectSpawnLevel => EternalCollector.EffectAmountToSpawn(Instance.DefaultSpawnAmount);

    [SerializeField] bool DEBUG_SpawnNextTile = true;

    SegmentConfig[] SegmentConfigs = new SegmentConfig[NumTrackSegments]
    {
        new SegmentConfig() { Start = new Vector2Int(-6, -6), End = new Vector2Int(-6,  6),
                              Entry = Direction.East,  Exit = Direction.North },
        new SegmentConfig() { Start = new Vector2Int(-6,  6), End = new Vector2Int( 6,  6),
                              Entry = Direction.South, Exit = Direction.East },
        new SegmentConfig() { Start = new Vector2Int( 6,  6), End = new Vector2Int( 6, -6),
                              Entry = Direction.West,  Exit = Direction.South },
        new SegmentConfig() { Start = new Vector2Int( 6, -6), End = new Vector2Int(-6, -6),
                              Entry = Direction.North, Exit = Direction.West }
    };

    List<TrackTile> SpawnedTiles = new List<TrackTile>();
    Dictionary<Vector2Int, TrackTile> TileLookup = new Dictionary<Vector2Int, TrackTile>();

    BiomeSpawnSettings CurrentBiome;
    int NumBiomeSegments = 0;

    const int NumTrackSegments = 4;
    TrackSegment[] TrackSegments = new TrackSegment[NumTrackSegments];
    int CurrentSegmentIndex = 0;
    int IndexInSegment = 0;
    TrackSegment CurrentSegment => TrackSegments[CurrentSegmentIndex];
    TrackTileConfig CurrentTileConfig => CurrentSegment.TileConfigs[IndexInSegment];

    ObjectPool<Mesh> TrackTileMeshPool;

    Vector3[] ProcGenVertices = null;
    Color[] ProcGenVertexColours = null;
    Vector2[] ProcGenUVCoords;
    int[] ProcGenIndices;

    public static EternalTrackGenerator Instance { get; private set; } = null;
    public static int ScrapSpaceToReserve = 200;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"Found duplicate EternalTrackGenerator on {gameObject.name}");
            Destroy(Instance.gameObject);
            return;
        }

        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        AkSoundEngine.SetSwitch(AK.SWITCHES.WEATHER.GROUP, AK.SWITCHES.WEATHER.SWITCH.DAY_GOOD, SettingsManager.Instance.gameObject);

        // need to start the music playing?
        if (SettingsManager.Instance.MusicPlayID == AkSoundEngine.AK_INVALID_PLAYING_ID)
            SettingsManager.Instance.MusicPlayID = AkSoundEngine.PostEvent(AK.EVENTS.PLAY_MUSIC, SettingsManager.Instance.gameObject);

        // setup the mesh pool
        TrackTileMeshPool = new ObjectPool<Mesh>(CreateTrackTileMesh,
                                                 RetrieveTrackTileMesh,
                                                 ReturnTrackTileMeshToPool,
                                                 DestroyTrackTileMesh,
                                                 true,
                                                 NumTrackTileMeshesInPool,
                                                 NumTrackTileMeshesInPool * 2);

        SelectNewBiome();

        // spawn the first track segment
        TrackSegments[0] = new TrackSegment(SegmentConfigs[0]);
        GenerateNextTrackSegment();

        // spawn the initial set of tiles
        for (int tileIndex = 0; tileIndex < NumStartingTiles; ++tileIndex)
            SpawnNextTile();
        CurrentTileIndex = NumStartingTiles / 2;

        OnLevelSpawned.Invoke(CurrentTile);
    }

    public void GetTilesNear(TrackTile tile, int range, List<TrackTile> foundTiles)
    {
        // reset the list of tiles
        foundTiles.Clear();
        foundTiles.Add(tile);

        int tileIndex = SpawnedTiles.IndexOf(tile);
        if (tileIndex >= 0)
        {
            if ((tileIndex + 1) < SpawnedTiles.Count)
                foundTiles.Add(SpawnedTiles[tileIndex + 1]);
            if ((tileIndex - 1) >= 0)
                foundTiles.Add(SpawnedTiles[tileIndex - 1]);
        }
    }

    void GenerateNextTrackSegment()
    {
        // clear the previous segment
        int previousIndex = (CurrentSegmentIndex + NumTrackSegments - 1) % NumTrackSegments;
        TrackSegments[previousIndex] = null;

        // find the first null segment
        for (int index = 1; index <= NumTrackSegments; ++index)
        {
            // allocate the segment if free
            int actualIndex = (CurrentSegmentIndex + index) % 4;
            if (TrackSegments[actualIndex] == null)
            {
                TrackSegments[actualIndex] = new TrackSegment(SegmentConfigs[actualIndex]);
                break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (DEBUG_SpawnNextTile)
        {
            DEBUG_SpawnNextTile = false;
            SpawnNextTile();
        }
    }

    public static void AdvanceTile()
    {
        ++Instance.CurrentTileIndex;
        Instance.SpawnNextTile();
    }

    void SelectNewBiome()
    {
        // pick a biome other than the current one
        List<BiomeSpawnSettings> availableSpawns = new List<BiomeSpawnSettings>(Biomes);
        availableSpawns.Remove(CurrentBiome);

        // select the biome
        CurrentBiome = availableSpawns[Random.Range(0, availableSpawns.Count)];
        NumBiomeSegments = Random.Range(CurrentBiome.MinSegments, CurrentBiome.MaxSegments);
    }

    void SpawnNextTile()
    {
        // spawn the tile
        Vector3 spawnLocation = TileSize * (Vector3.right * CurrentTileConfig.Location.x +
                                            Vector3.forward * CurrentTileConfig.Location.y);
        GenerateTile(spawnLocation, CurrentTileConfig.Location,
                     CurrentTileConfig.Entry, CurrentTileConfig.Exit,
                     CurrentTileConfig.Type, CurrentBiome.Biome);

        // advance the index
        ++IndexInSegment;
        if (IndexInSegment >= CurrentSegment.TileConfigs.Count)
        {
            IndexInSegment = 0;
            CurrentSegmentIndex = (CurrentSegmentIndex + 1) % NumTrackSegments;
            GenerateNextTrackSegment();
        }

        // need to pick a new type of biome to spawn
        --NumBiomeSegments;
        if (NumBiomeSegments == 0)
            SelectNewBiome();
    }

    void GenerateTile(Vector3 position, Vector2Int gridLocation,
                      Direction entrySide, Direction exitSide, TrackType type,
                      BiomeConfig biome)
    {
        // calculate the size of each cell
        int vertsPerSide = (int)Resolution;

        // generate the raw vertex data
        biome.PerformHeightGeneration(gridLocation, vertsPerSide, TileSize, MaximumHeight,
                                      ref ProcGenVertices, ref ProcGenVertexColours);

        // generate the track area
        float[] distancesFromTrack;
        MakeTrackArea(ProcGenVertices, vertsPerSide, TileSize / 2f, entrySide, exitSide, out distancesFromTrack);

        // blend with adjacent tiles
        BlendHeightWithNeighbours(gridLocation, ProcGenVertices, vertsPerSide);

        // instantiate the tile and generate the mesh
        var newTileGO = Instantiate(TilePrefab, position, Quaternion.identity, TrackRoot);
        var newTileMF = newTileGO.GetComponent<MeshFilter>();
        newTileGO.name = $"Tile_{gridLocation.x}x{gridLocation.y}";

        var trackTileScript = newTileGO.GetComponent<TrackTile>();
        trackTileScript.Bind(gridLocation, entrySide, exitSide, type, TileSize / 2f, TrackPointSpacing);
        SpawnedTiles.Add(trackTileScript);
        TileLookup[gridLocation] = trackTileScript;

        trackTileScript.LinkedMesh = PaintAndGenerateMesh(biome, gridLocation, ProcGenVertices, ProcGenVertexColours, 
                                                          TileSize / 2f, vertsPerSide, distancesFromTrack, newTileGO.transform,
                                                          trackTileScript);

        newTileGO.SetActive(true);
    }

    #region Height Blending
    void BlendHeightWithNeighbours(Vector2Int gridLocation, Vector3[] vertices, int vertsPerSide)
    {
        // check for any neighbours we need to blend with
        if (TileLookup.ContainsKey(gridLocation + Offsets.North))
            BlendHeightWithOtherTile(vertices, vertsPerSide, Direction.North, TileLookup[gridLocation + Offsets.North]);
        if (TileLookup.ContainsKey(gridLocation + Offsets.East))
            BlendHeightWithOtherTile(vertices, vertsPerSide, Direction.East, TileLookup[gridLocation + Offsets.East]);
        if (TileLookup.ContainsKey(gridLocation + Offsets.South))
            BlendHeightWithOtherTile(vertices, vertsPerSide, Direction.South, TileLookup[gridLocation + Offsets.South]);
        if (TileLookup.ContainsKey(gridLocation + Offsets.West))
            BlendHeightWithOtherTile(vertices, vertsPerSide, Direction.West, TileLookup[gridLocation + Offsets.West]);
    }

    void BlendHeightWithOtherTile(Vector3[] vertices, int vertsPerSide, Direction entrySide, TrackTile previousTile)
    {
        // first transfer the edge verts
        BlendHeightWithOtherTile_TransferEdgeVerts(vertices, vertsPerSide, entrySide, previousTile);

        // now blend the edge verts through the current terrain
        BlendHeightWithOtherTile_Smooth(vertices, vertsPerSide, entrySide);
    }

    void BlendHeightWithOtherTile_TransferEdgeVerts(Vector3[] vertices, int vertsPerSide, Direction entrySide, TrackTile previousTile)
    {
        Vector2Int sourceStart;
        Vector2Int sourceEnd;
        Vector2Int destinationStart;
        Vector2Int step;

        // determine the movement variables
        if (entrySide == Direction.North)
        {
            sourceStart = new Vector2Int(0, 0);
            sourceEnd = new Vector2Int(vertsPerSide, 0);
            destinationStart = new Vector2Int(0, vertsPerSide - 1);
            step = Vector2Int.right;
        }
        else if (entrySide == Direction.East)
        {
            sourceStart = new Vector2Int(0, 0);
            sourceEnd = new Vector2Int(0, vertsPerSide);
            destinationStart = new Vector2Int(vertsPerSide - 1, 0);
            step = Vector2Int.up;
        }
        else if (entrySide == Direction.South)
        {
            sourceStart = new Vector2Int(0, vertsPerSide - 1);
            sourceEnd = new Vector2Int(vertsPerSide, vertsPerSide - 1);
            destinationStart = new Vector2Int(0, 0);
            step = Vector2Int.right;
        }
        else
        {
            sourceStart = new Vector2Int(vertsPerSide - 1, 0);
            sourceEnd = new Vector2Int(vertsPerSide - 1, vertsPerSide);
            destinationStart = new Vector2Int(0, 0);
            step = Vector2Int.up;
        }

        // transfer the edge points so they match exactly
        Vector2Int source = sourceStart;
        Vector2Int destination = destinationStart;
        var previousTileVerts = previousTile.LinkedMesh.vertices;
        while (source != sourceEnd)
        {
            int sourceIndex = source.x + source.y * vertsPerSide;
            int destinationIndex = destination.x + destination.y * vertsPerSide;

            vertices[destinationIndex].y = previousTileVerts[sourceIndex].y;

            source += step;
            destination += step;
        }
    }

    void BlendHeightWithOtherTile_Smooth(Vector3[] vertices, int vertsPerSide, Direction entrySide)
    {
        Vector2Int blendAxisStep;
        Vector2Int blendTangentStep;
        Vector2Int blendAxisStart;

        // determine the movement variables
        if (entrySide == Direction.North)
        {
            blendAxisStart = new Vector2Int(1, vertsPerSide - 1);

            blendAxisStep = Vector2Int.down;
            blendTangentStep = Vector2Int.right;
        }
        else if (entrySide == Direction.East)
        {
            blendAxisStart = new Vector2Int(vertsPerSide - 1, 1);

            blendAxisStep = Vector2Int.left;
            blendTangentStep = Vector2Int.up;
        }
        else if (entrySide == Direction.South)
        {
            blendAxisStart = new Vector2Int(1, 0);

            blendAxisStep = Vector2Int.up;
            blendTangentStep = Vector2Int.right;
        }
        else
        {
            blendAxisStart = new Vector2Int(0, 1);

            blendAxisStep = Vector2Int.right;
            blendTangentStep = Vector2Int.up;
        }

        // apply the blending
        int numFadeSegments = Mathf.CeilToInt((int)Resolution * TileBlendDistance / TileSize);
        Vector2Int sourceAxisStart = blendAxisStart;
        blendAxisStart += blendAxisStep;
        for (int segmentIndex = 1; segmentIndex <= numFadeSegments; segmentIndex++)
        {
            float blendFactor = TileBlendCurve.Evaluate((float)segmentIndex / numFadeSegments);

            // apply the blending
            Vector2Int destLocation = blendAxisStart;
            Vector2Int sourceLocation = sourceAxisStart;
            for (int index = 0; index < (vertsPerSide - 2); ++index)
            {
                int destIndex = destLocation.x + destLocation.y * vertsPerSide;
                int sourceIndex = sourceLocation.x + sourceLocation.y * vertsPerSide;

                vertices[destIndex].y = Mathf.Lerp(vertices[sourceIndex].y,
                                                   vertices[destIndex].y,
                                                   blendFactor);

                destLocation += blendTangentStep;
                sourceLocation += blendTangentStep;
            }

            blendAxisStart += blendAxisStep;
        }
    }
    #endregion

    #region Colour Blending
    void BlendColourWithNeighbours(Vector2Int gridLocation, Color[] vertexColours, int vertsPerSide)
    {
        // check for any neighbours we need to blend with
        if (TileLookup.ContainsKey(gridLocation + Offsets.North))
            BlendColourWithOtherTile(vertexColours, vertsPerSide, Direction.North, TileLookup[gridLocation + Offsets.North]);
        if (TileLookup.ContainsKey(gridLocation + Offsets.East))
            BlendColourWithOtherTile(vertexColours, vertsPerSide, Direction.East, TileLookup[gridLocation + Offsets.East]);
        if (TileLookup.ContainsKey(gridLocation + Offsets.South))
            BlendColourWithOtherTile(vertexColours, vertsPerSide, Direction.South, TileLookup[gridLocation + Offsets.South]);
        if (TileLookup.ContainsKey(gridLocation + Offsets.West))
            BlendColourWithOtherTile(vertexColours, vertsPerSide, Direction.West, TileLookup[gridLocation + Offsets.West]);
    }

    void BlendColourWithOtherTile(Color[] vertexColours, int vertsPerSide, Direction entrySide, TrackTile previousTile)
    {
        // first transfer the edge verts
        BlendColourWithOtherTile_TransferEdgeVerts(vertexColours, vertsPerSide, entrySide, previousTile);

        // now blend the edge verts through the current terrain
        BlendColourHeightWithOtherTile_Smooth(vertexColours, vertsPerSide, entrySide);
    }

    void BlendColourWithOtherTile_TransferEdgeVerts(Color[] vertexColours, int vertsPerSide, Direction entrySide, TrackTile previousTile)
    {
        Vector2Int sourceStart;
        Vector2Int sourceEnd;
        Vector2Int destinationStart;
        Vector2Int step;

        // determine the movement variables
        if (entrySide == Direction.North)
        {
            sourceStart = new Vector2Int(0, 0);
            sourceEnd = new Vector2Int(vertsPerSide, 0);
            destinationStart = new Vector2Int(0, vertsPerSide - 1);
            step = Vector2Int.right;
        }
        else if (entrySide == Direction.East)
        {
            sourceStart = new Vector2Int(0, 0);
            sourceEnd = new Vector2Int(0, vertsPerSide);
            destinationStart = new Vector2Int(vertsPerSide - 1, 0);
            step = Vector2Int.up;
        }
        else if (entrySide == Direction.South)
        {
            sourceStart = new Vector2Int(0, vertsPerSide - 1);
            sourceEnd = new Vector2Int(vertsPerSide, vertsPerSide - 1);
            destinationStart = new Vector2Int(0, 0);
            step = Vector2Int.right;
        }
        else
        {
            sourceStart = new Vector2Int(vertsPerSide - 1, 0);
            sourceEnd = new Vector2Int(vertsPerSide - 1, vertsPerSide);
            destinationStart = new Vector2Int(0, 0);
            step = Vector2Int.up;
        }

        // transfer the edge points so they match exactly
        Vector2Int source = sourceStart;
        Vector2Int destination = destinationStart;
        var previousTileColours = previousTile.LinkedMesh.colors;
        while (source != sourceEnd)
        {
            int sourceIndex = source.x + source.y * vertsPerSide;
            int destinationIndex = destination.x + destination.y * vertsPerSide;

            vertexColours[destinationIndex] = previousTileColours[sourceIndex];

            source += step;
            destination += step;
        }
    }

    void BlendColourHeightWithOtherTile_Smooth(Color[] vertexColours, int vertsPerSide, Direction entrySide)
    {
        Vector2Int blendAxisStep;
        Vector2Int blendTangentStep;
        Vector2Int blendAxisStart;

        // determine the movement variables
        if (entrySide == Direction.North)
        {
            blendAxisStart = new Vector2Int(1, vertsPerSide - 1);

            blendAxisStep = Vector2Int.down;
            blendTangentStep = Vector2Int.right;
        }
        else if (entrySide == Direction.East)
        {
            blendAxisStart = new Vector2Int(vertsPerSide - 1, 1);

            blendAxisStep = Vector2Int.left;
            blendTangentStep = Vector2Int.up;
        }
        else if (entrySide == Direction.South)
        {
            blendAxisStart = new Vector2Int(1, 0);

            blendAxisStep = Vector2Int.up;
            blendTangentStep = Vector2Int.right;
        }
        else
        {
            blendAxisStart = new Vector2Int(0, 1);

            blendAxisStep = Vector2Int.right;
            blendTangentStep = Vector2Int.up;
        }

        // apply the blending
        int numFadeSegments = Mathf.CeilToInt((int)Resolution * TileBlendDistance / TileSize);
        Vector2Int sourceAxisStart = blendAxisStart;
        blendAxisStart += blendAxisStep;
        for (int segmentIndex = 1; segmentIndex <= numFadeSegments; segmentIndex++)
        {
            float blendFactor = TileBlendCurve.Evaluate((float)segmentIndex / numFadeSegments);

            // apply the blending
            Vector2Int destLocation = blendAxisStart;
            Vector2Int sourceLocation = sourceAxisStart;
            for (int index = 0; index < (vertsPerSide - 2); ++index)
            {
                int destIndex = destLocation.x + destLocation.y * vertsPerSide;
                int sourceIndex = sourceLocation.x + sourceLocation.y * vertsPerSide;

                vertexColours[destIndex] = Color.Lerp(vertexColours[sourceIndex],
                                                      vertexColours[destIndex],
                                                      blendFactor);

                destLocation += blendTangentStep;
                sourceLocation += blendTangentStep;
            }

            blendAxisStart += blendAxisStep;
        }
    }
    #endregion

    #region Track Area Flattening
    void MakeTrackArea(Vector3[] vertices, int vertsPerSide, float halfSize,
                       Direction entrySide, Direction exitSide,
                       out float[] distancesFromTrack)
    {
        Vector3 trackPerpendicular = Vector3.right;
        Vector3 referencePoint = new Vector3(0f, 0f, -halfSize);
        distancesFromTrack = new float[vertsPerSide * vertsPerSide];

        // check if curved
        bool isCurved = true;
        if ((entrySide == Direction.North && exitSide == Direction.South) ||
            (entrySide == Direction.South && exitSide == Direction.North) ||
            (entrySide == Direction.East && exitSide == Direction.West) ||
            (entrySide == Direction.West && exitSide == Direction.East))
        {
            isCurved = false;

            if (entrySide == Direction.North)
            {
                trackPerpendicular = Vector3.right;
                referencePoint = new Vector3(0f, 0f, halfSize);
            }
            else if (entrySide == Direction.East)
            {
                trackPerpendicular = Vector3.forward;
                referencePoint = new Vector3(halfSize, 0f, 0f);
            }
            else if (entrySide == Direction.South)
            {
                trackPerpendicular = Vector3.right;
                referencePoint = new Vector3(0f, 0f, -halfSize);
            }
            else if (entrySide == Direction.West)
            {
                trackPerpendicular = Vector3.forward;
                referencePoint = new Vector3(-halfSize, 0f, 0f);
            }
        }
        else
        {
            if (entrySide == Direction.North)
                referencePoint = exitSide == Direction.East ? new Vector3(halfSize, 0f, halfSize) :
                                                               new Vector3(-halfSize, 0f, halfSize);
            else if (entrySide == Direction.East)
                referencePoint = exitSide == Direction.North ? new Vector3(halfSize, 0f, halfSize) :
                                                               new Vector3(halfSize, 0f, -halfSize);
            else if (entrySide == Direction.South)
                referencePoint = exitSide == Direction.East ? new Vector3(halfSize, 0f, -halfSize) :
                                                               new Vector3(-halfSize, 0f, -halfSize);
            else if (entrySide == Direction.West)
                referencePoint = exitSide == Direction.North ? new Vector3(-halfSize, 0f, halfSize) :
                                                               new Vector3(-halfSize, 0f, -halfSize);
        }

        // apply flattening
        for (int row = 0; row < vertsPerSide; row++)
        {
            for (int col = 0; col < vertsPerSide; col++)
            {
                int index = row * vertsPerSide + col;

                // calculate the distance from the track
                float distance = 0f;
                if (isCurved)
                {
                    Vector3 vecToPoint = vertices[index] - referencePoint;
                    vecToPoint.y = 0;

                    // skip the reference point
                    if (vecToPoint.sqrMagnitude < float.Epsilon)
                        continue;

                    vecToPoint.Normalize();
                    Vector3 trackPoint = vecToPoint * halfSize + referencePoint;

                    distance = Mathf.Abs(Vector3.Dot(vertices[index] - trackPoint, vecToPoint));
                }
                else
                {
                    distance = Mathf.Abs(Vector3.Dot(vertices[index] - referencePoint, trackPerpendicular));
                }

                distancesFromTrack[index] = distance;

                if (distance >= TrackFadeDistance)
                    continue;

                float fade = TrackFadeCurve.Evaluate(distance / TrackFadeDistance);
                vertices[index].y = Mathf.Lerp(0f, vertices[index].y, fade);
            }
        }
    }
    #endregion

    #region Mesh Pooling
    Mesh CreateTrackTileMesh()
    {
        return new Mesh();
    }

    void RetrieveTrackTileMesh(Mesh mesh)
    {

    }

    void ReturnTrackTileMeshToPool(Mesh mesh)
    {

    }

    void DestroyTrackTileMesh(Mesh mesh)
    {
        Destroy(mesh);
    }
    #endregion

    #region Mesh Generation
    Mesh PaintAndGenerateMesh(BiomeConfig biome, Vector2Int gridLocation,
                              Vector3[] vertices, Color[] vertexColours,
                              float halfSize, int vertsPerSide,
                              float[] distancesFromTrack, Transform objectRoot,
                              TrackTile trackTileScript)
    {
        int numVertices = vertices.Length;

        if (ProcGenUVCoords == null)
            ProcGenUVCoords = new Vector2[numVertices];

        for (int index = 0; index < numVertices; index++)
        {
            ProcGenUVCoords[index] = new Vector2((vertices[index].x + halfSize) / TileSize,
                                                 (vertices[index].z + halfSize) / TileSize);
        }

        // generate the mesh indices
        if (ProcGenIndices == null)
            ProcGenIndices = new int[numVertices * 3 * 2];

        for (int row = 0; row < (vertsPerSide - 1); row++)
        {
            for (int col = 0; col < (vertsPerSide - 1); col++)
            {
                int baseVertexIndex = row * vertsPerSide + col;

                ProcGenIndices[(baseVertexIndex) * 6 + 0] = baseVertexIndex;
                ProcGenIndices[(baseVertexIndex) * 6 + 1] = baseVertexIndex + vertsPerSide;
                ProcGenIndices[(baseVertexIndex) * 6 + 2] = baseVertexIndex + vertsPerSide + 1;
                ProcGenIndices[(baseVertexIndex) * 6 + 3] = baseVertexIndex;
                ProcGenIndices[(baseVertexIndex) * 6 + 4] = baseVertexIndex + vertsPerSide + 1;
                ProcGenIndices[(baseVertexIndex) * 6 + 5] = baseVertexIndex + 1;
            }
        }

        Mesh mesh = TrackTileMeshPool.Get();
        mesh.indexFormat = ProcGenIndices.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 :
                                                           UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, ProcGenUVCoords);
        mesh.SetTriangles(ProcGenIndices, 0);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        Vector3[] normals = mesh.normals;

        // paint the terrain
        biome.PerformTerrainPainting(gridLocation, vertsPerSide, MaximumHeight,
                                     vertices, normals, vertexColours);

        // blend with adjacent tiles
        BlendColourWithNeighbours(gridLocation, vertexColours, vertsPerSide);

        // place the objects
        biome.PerformObjectPlacement(gridLocation, vertsPerSide, MaximumHeight,
                                     TileSize, vertices, normals, vertexColours,
                                     distancesFromTrack, objectRoot, trackTileScript);

        mesh.SetColors(vertexColours);

        return mesh;
    }
    #endregion

    #region Track Cleanup
    public static void SetLastOccupiedTile(TrackTile tile)
    {
        // cleanup any old tiles
        int tileIndex = Instance.SpawnedTiles.IndexOf(tile) - Instance.NumTilesToRetain;
        if (tileIndex >= 0)
        {
            var currentTile = CurrentTile;

            // destroy the tiles
            for (int index = 0; index < tileIndex; ++index)
            {
                var tileToDestroy = Instance.SpawnedTiles[index];
                Instance.TileLookup.Remove(tileToDestroy.GridLocation);
                Instance.TrackTileMeshPool.Release(tileToDestroy.LinkedMesh);

                Destroy(tileToDestroy.gameObject);
            }
            Instance.SpawnedTiles.RemoveRange(0, tileIndex);

            // update the current tile
            Instance.CurrentTileIndex = Instance.SpawnedTiles.IndexOf(currentTile);
        }
    }
    #endregion

    #region Positioning
    public static TrackTile GetPreviousTileTo(TrackTile tile)
    {
        int previousTileIndex = Instance.SpawnedTiles.IndexOf(tile) - 1;
        return previousTileIndex >= 0 ? Instance.SpawnedTiles[previousTileIndex] : null;
    }

    static int GetIndexForPosition(TrackTile tile, Vector3 position)
    {
        // attempt to find the index of the segment that is closest to this position
        for (int index = tile.TrackPoints.Length - 2; index >= 0; index--)
        {
            var vectorToPoint = position - tile.TrackPoints[index].Position;
            var segmentVector = tile.TrackPoints[index + 1].Position - tile.TrackPoints[index].Position;

            float projection = Vector3.Dot(vectorToPoint, segmentVector.normalized);
            if (projection >= 0f && projection <= segmentVector.magnitude * 1.5f)
                return index;
        }

        return -1;
    }

    public static bool GetEnginePositionData(TrackTile tile, float distance, Vector3 carriageVector,
                                             out Vector3 headPosition, out Vector3 tailPosition)
    {
        var previousTile = GetPreviousTileTo(tile);

        // attempt to find the head position
        int headIndex = -1;
        for (int index = tile.TrackPoints.Length - 2; index >= 0; index--)
        {
            if (distance >= tile.TrackPoints[index].PathDistance && distance < tile.TrackPoints[index + 1].PathDistance)
            {
                headIndex = index;
                break;
            }
        }

        if (headIndex < 0 || headIndex >= (tile.TrackPoints.Length - 1))
        {
            headPosition = tailPosition = Vector3.zero;
            return false;
        }

        // calculate the head position
        float progressFactor = Mathf.InverseLerp(tile.TrackPoints[headIndex].PathDistance,
                                                 tile.TrackPoints[headIndex + 1].PathDistance,
                                                 distance);
        headPosition = Vector3.Lerp(tile.TrackPoints[headIndex].Position, tile.TrackPoints[headIndex + 1].Position, progressFactor);

        float carriageLength = carriageVector.magnitude;

        // attempt to find the tail position in this segment
        if (FindPointOnTrack(tile, headPosition, carriageLength, headIndex, out tailPosition))
            return true;

        // try to find the tail segment in the previous section
        if (previousTile != null && FindPointOnTrack(previousTile, headPosition, carriageLength, previousTile.NumTrackPoints - 1, out tailPosition))
            return true;

        headPosition = tailPosition = Vector3.negativeInfinity;

        return false;
    }

    static bool FindPointOnTrack(TrackTile searchTile, Vector3 anchorPoint, float distanceFromAnchor, 
                                 int searchIndexStart, out Vector3 intersectionPoint)
    {
        for (int index = searchIndexStart - 1; index >= 0; index--)
        {
            Vector3 p1 = searchTile.TrackPoints[index].Position;
            Vector3 p2 = searchTile.TrackPoints[index + 1].Position;

            if (FindIntersection(anchorPoint, distanceFromAnchor, p1, p2, out intersectionPoint))
            {
                return true;
            }
        }

        intersectionPoint = Vector3.negativeInfinity;
        return false;
    }

    static bool FindIntersection(Vector3 circleCentre, float circleRadius, Vector3 lineStart, Vector3 lineEnd, out Vector3 intersection)
    {
        intersection = Vector3.zero;

        Vector3 segmentDelta = lineEnd - lineStart;
        float a = Vector3.Dot(segmentDelta, segmentDelta);
        float b = 2f * Vector3.Dot(segmentDelta, lineStart - circleCentre);
        float c = Vector3.Dot(circleCentre, circleCentre);
        c += Vector3.Dot(lineStart, lineStart);
        c -= 2f * Vector3.Dot(circleCentre, lineStart);
        c -= circleRadius * circleRadius;

        float bb4ac = b * b - 4f * a * c;

        // no intersection?
        if (Mathf.Abs(a) < float.Epsilon || bb4ac < 0)
            return false;

        float mu1 = (-b - Mathf.Sqrt(bb4ac)) / (2f * a);

        // valid intersection
        if (mu1 >= 0 && mu1 <= 1.5f)
        {
            intersection = new Vector3(lineStart.x + mu1 * (lineEnd.x - lineStart.x),
                                       0f,
                                       lineStart.z + mu1 * (lineEnd.z - lineStart.z));
            return true;
        }

        return false;
    }

    #endregion
}
