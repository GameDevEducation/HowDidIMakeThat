using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleObjectPlacer : BaseObjectPlacer
{
    [SerializeField] Vector2 NoiseScale = new Vector2(1f / 128f, 1f / 128f);
    [SerializeField] float NoiseThreshold = 0.5f;

    public override void Execute(int vertsPerSide, Vector2Int gridLocation, float maxHeight,
                                 float cellSize, Vector3[] vertices, Vector3[] normals, Color[] vertexColours,
                                 bool[] verticesUsed, float[] distancesFromTrack, Transform objectRoot,
                                 List<PlaceableObjectConfig> placeableObjects, TrackTile trackTileScript)
    {
        List<int> candidateIndices = new List<int>(vertsPerSide * 100);

        // build up candidate positions for each type of object
        foreach(var placeableObject in placeableObjects)
        {
            // clear the existing candidates
            candidateIndices.Clear();

            // attempt to find viable placement locations
            for (int y = 0; y < vertsPerSide; y++)
            {
                for (int x = 0; x < vertsPerSide; x++)
                {
                    int index = x + y * vertsPerSide;

                    // vertex already used so skip
                    if (verticesUsed[index])
                        continue;

                    // within the forbidden range?
                    if (distancesFromTrack[index] < placeableObject.MinRange || 
                        distancesFromTrack[index] > placeableObject.MaxRange)
                        continue;

                    // outside of the height limits?
                    float height = vertices[index].y;
                    if (placeableObject.HasHeightLimits &&
                        (height < placeableObject.MinHeightToSpawn || height > placeableObject.MaxHeightToSpawn))
                        continue;

                    // check if it passes the noise threshold - if so add as a candidate
                    float noiseValue = Mathf.PerlinNoise(x * NoiseScale.x, y * NoiseScale.y);
                    if (noiseValue >= NoiseThreshold)
                        candidateIndices.Add(index);
                }
            }

            // determine the number to spawn
            int numToSpawn = Mathf.RoundToInt(EternalTrackGenerator.CurrentObjectSpawnLevel * placeableObject.NormalisedWeighting);
            if (numToSpawn > candidateIndices.Count)
                numToSpawn = candidateIndices.Count;

            // spawn up to the threshold
            for (int spawnIndex = 0; spawnIndex < numToSpawn; spawnIndex++)
            {
                // pick a random spawn point
                int selectedIndex = Random.Range(0, candidateIndices.Count);
                int candidateIndex = candidateIndices[selectedIndex];
                candidateIndices.RemoveAt(selectedIndex);

                // pick an object to spawn
                var objectPrefab = placeableObject.Objects[Random.Range(0, placeableObject.Objects.Count)];

                // spawn the object
                var spawnedGO = GameObject.Instantiate(objectPrefab, objectRoot);
                spawnedGO.transform.localPosition = vertices[candidateIndex];
                spawnedGO.transform.up = normals[candidateIndex];

                trackTileScript.AddSpawnedObject(spawnedGO);

                // mark as used
                verticesUsed[candidateIndex] = true;
            }
        }
    }
}
