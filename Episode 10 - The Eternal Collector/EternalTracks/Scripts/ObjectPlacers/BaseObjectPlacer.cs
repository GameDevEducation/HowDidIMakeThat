using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseObjectPlacer : MonoBehaviour
{
    public abstract void Execute(int vertsPerSide, Vector2Int gridLocation, float maxHeight,
                                 float cellSize, Vector3[] vertices, Vector3[] normals, Color[] vertexColours,
                                 bool[] verticesUsed, float[] distancesFromTrack, Transform objectRoot,
                                 List<PlaceableObjectConfig> placeableObjects, TrackTile trackTileScript);
}
