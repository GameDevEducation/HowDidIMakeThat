using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlaceableObjectConfig
{
    [Header("General")]
    [Range(0f, 1f)] public float DefaultWeighting = 0f;
    public float MinRange = 16f;
    public float MaxRange = 48f;

    [Header("Height Limits")]
    public bool HasHeightLimits = false;
    public float MinHeightToSpawn = 0f;
    public float MaxHeightToSpawn = 0f;

    [Header("Objects")]
    public List<GameObject> Objects;

    public float NormalisedWeighting { get; set; } = 0f;
}

[CreateAssetMenu(menuName = "TEC/Biome", fileName = "BiomeConfig")]
public class BiomeConfig : ScriptableObject
{
    [SerializeField] [Range(0f, 1f)] float MaxHeightProportion = 0.5f;
    [SerializeField] Color BaseColour = Color.white;
    [SerializeField] GameObject HeightRules;
    [SerializeField] GameObject PaintingRules;
    [SerializeField] GameObject PlacementRules;
    [SerializeField] List<PlaceableObjectConfig> Objects;

    BaseHeightModifier[] HeightModifiers => HeightRules.GetComponents<BaseHeightModifier>();
    BaseTerrainPainter[] TerrainPainters => PaintingRules.GetComponents<BaseTerrainPainter>();
    BaseObjectPlacer[] ObjectPlacers => PlacementRules.GetComponents<BaseObjectPlacer>();

    public void PerformHeightGeneration(Vector2Int gridLocation, int vertsPerSide, float tileSize, float maximumHeight,
                                        ref Vector3[] vertices, ref Color[] vertexColours)
    {
        Vector3 cellSize = new Vector3(tileSize / (vertsPerSide - 1), 0f, tileSize / (vertsPerSide - 1));
        int numVertices = vertsPerSide * vertsPerSide;

        float halfTileSize = tileSize / 2f;
        Vector3 vertexOffset = new Vector3(-halfTileSize, 0f, -halfTileSize);

        float workingMaxHeight = MaxHeightProportion * maximumHeight;

        if (vertices == null)
            vertices = new Vector3[numVertices];
        if (vertexColours == null)
            vertexColours = new Color[numVertices];

        // set the position of every vertex
        for (int row = 0; row < vertsPerSide; row++)
        {
            for (int col = 0; col < vertsPerSide; col++)
            {
                int index = row * vertsPerSide + col;

                vertices[index] = vertexOffset + new Vector3(cellSize.x * col, 0f, cellSize.z * row);
                vertexColours[index] = BaseColour;
            }
        }

        // generate the heightmap
        foreach (var heightModifier in HeightModifiers)
            heightModifier.Execute(vertsPerSide, gridLocation, workingMaxHeight, vertices);
    }

    public void PerformTerrainPainting(Vector2Int gridLocation, int vertsPerSide, float maximumHeight,
                                       Vector3[] vertices, Vector3[] normals, Color[] vertexColours)
    {
        float workingMaxHeight = MaxHeightProportion * maximumHeight;

        // paint the terrain
        foreach (var terrainPainter in TerrainPainters)
            terrainPainter.Execute(vertsPerSide, gridLocation, workingMaxHeight, vertices, normals, vertexColours);
    }

    public void PerformObjectPlacement(Vector2Int gridLocation, int vertsPerSide, float maximumHeight,
                                       float tileSize, Vector3[] vertices, Vector3[] normals, Color[] vertexColours,
                                       float[] distancesFromTrack, Transform objectRoot, TrackTile trackTileScript)
    {
        float workingMaxHeight = MaxHeightProportion * maximumHeight;
        float cellSize = tileSize / (vertsPerSide - 1);

        // normalise the weightings
        float weightingSum = 0f;
        foreach (var config in Objects)
            weightingSum += config.DefaultWeighting;
        foreach (var config in Objects)
            config.NormalisedWeighting = config.DefaultWeighting / weightingSum;

        bool[] verticesUsed = new bool[vertsPerSide * vertsPerSide];

        // place the objects
        foreach (var objectPlacer in ObjectPlacers)
            objectPlacer.Execute(vertsPerSide, gridLocation, workingMaxHeight, cellSize, 
                                 vertices, normals, vertexColours, verticesUsed,
                                 distancesFromTrack, objectRoot, Objects, trackTileScript);
    }
}
