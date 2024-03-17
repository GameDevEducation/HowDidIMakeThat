using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

public class TerrainHelper : MonoBehaviour {
	// public float NoiseAmplitude = 0.01f;
	// public float XNoiseScale = 128f;
	// public float ZNoiseScale = 128f;

	// public float MainShapeSacle = 0.95f;
	// public float IslandThreshold = 0.15f;
	// public float IslandShapeScale = 0.1f;

	// public void GenerateTerrain() {
	// 	// retrieve the terrain
	// 	Terrain terrain = gameObject.GetComponent<Terrain>();

	// 	// grab the height information
	// 	float[,] terrainHeights = new float[terrain.terrainData.heightmapWidth,terrain.terrainData.heightmapHeight];

	// 	int width = terrain.terrainData.heightmapWidth;
	// 	int height = terrain.terrainData.heightmapHeight;
	// 	int maxDistFromCentre = Mathf.Min(width / 2, height / 2);

	// 	// set some initial height data
	// 	for (int x = 0; x < width; ++x)
	// 	{
	// 		for(int z = 0; z < height; ++z)
	// 		{
	// 			// distance from centre?
	// 			float distFromCentre = Mathf.Sqrt((x - (width / 2))*(x - (width / 2)) + (z - (height / 2))*(z - (height / 2)));

	// 			// percentage distance from centre
	// 			float percentageDist = Mathf.Clamp01(distFromCentre / maxDistFromCentre);

	// 			// apply the main height calculation
	// 			terrainHeights[x, z] = MainShapeSacle * Mathf.Sin(percentageDist * percentageDist * percentageDist * percentageDist * percentageDist * Mathf.PI);

	// 			terrainHeights[x, z] *= 1f + 0.05f * Mathf.Sin(percentageDist * Mathf.PI * 16);

	// 			// apply the island height?
	// 			if (percentageDist < IslandThreshold)
	// 			{
	// 				terrainHeights[x, z] += IslandShapeScale * Mathf.Cos(percentageDist * Mathf.PI);
	// 			}

	// 			// apply noise
	// 			terrainHeights[x, z] += NoiseAmplitude * Mathf.PerlinNoise(XNoiseScale * x / width, ZNoiseScale * z / height) - (NoiseAmplitude * 0.5f);
	// 		}
	// 	}

	// 	// update the heights
	// 	terrain.terrainData.SetHeights(0, 0, terrainHeights);
	// }

	public MeshFilter OutputMeshFilter;

	public Color32 SandColour = new Color32(236, 211, 129, 255);
	public Color32 SnowColour = new Color32(255, 255, 255, 255);
	public Color32 GrassColour = new Color32(35, 165, 35, 255);
	public Color32 RockColour = new Color32(101, 93, 71, 255);
	public float SandHeight = 6f;
	public float GrassHeight = 8f;
	public float SnowHeight = 90f;
	public float SlopeThresholdForRock = 0.5f;

	public void GenerateMesh() {
		// retrieve the terrain
		Terrain terrain = gameObject.GetComponent<Terrain>();

		// grab the height information
		float[,] terrainHeights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight);

		int width = 128;
		int height = 128;

		// sample the heights
		float[,] workingHeights = new float[width, height];
		int xSamples = terrain.terrainData.heightmapWidth / width;
		int zSamples = terrain.terrainData.heightmapHeight / height;
		float heightScale = 1f / (xSamples * zSamples);
		for (int x = 0; x < width; ++x)
		{
			for (int z = 0; z < height; ++z)
			{
				workingHeights[x,z] = 0;

				for (int xTerrain = 0; xTerrain < xSamples; ++xTerrain)
				{
					for (int zTerrain = 0; zTerrain < zSamples; ++zTerrain)
					{
						workingHeights[x, z] += terrainHeights[x * xSamples + xTerrain, z * zSamples + zTerrain];
					}
				}

				workingHeights[x, z] *= heightScale;
			}
		}

		Vector3 sampleScale = terrain.terrainData.heightmapScale;
		sampleScale.x *= xSamples;
		sampleScale.z *= zSamples;

		// generate the vertices and indices
		Vector3[] vertices = new Vector3[(width + 0) * (height + 0)];
		Color32[] vertexColours = new Color32[(width + 0) * (height + 0)];
		Vector2[] uvCoords = new Vector2[(width + 0) * (height + 0)];
		int[] triangles = new int[width * height * 2 * 3];
		int vertIndex = 0;
		for (int x = 0; x < width; ++x)
		{
			for (int z = 0; z < height; ++z)
			{
				vertices[vertIndex] = new Vector3(x * sampleScale.x,  workingHeights[z, x] * sampleScale.y, z * sampleScale.z);
				uvCoords[vertIndex] = new Vector2((float)x / (width - 1), (float)z / (height - 1));

				if (vertices[vertIndex].y < SandHeight)
					vertexColours[vertIndex] = SandColour;
				else if (vertices[vertIndex].y < GrassHeight)
					vertexColours[vertIndex] = Color32.Lerp(SandColour, GrassColour, (vertices[vertIndex].y - SandHeight) / (GrassHeight - SandHeight));
				else if (vertices[vertIndex].y < SnowHeight)
					vertexColours[vertIndex] = GrassColour;
				else
					vertexColours[vertIndex] = Color32.Lerp(GrassColour, SnowColour, Mathf.Clamp01((vertices[vertIndex].y - SnowHeight) / 2f));

				if ((x < (width - 1)) && (z < (height - 1)))
				{
					triangles[(vertIndex * 6) + 0] = vertIndex + 1;
					triangles[(vertIndex * 6) + 1] = vertIndex + width + 1;
					triangles[(vertIndex * 6) + 2] = vertIndex + width;
					triangles[(vertIndex * 6) + 3] = vertIndex + width;
					triangles[(vertIndex * 6) + 4] = vertIndex;
					triangles[(vertIndex * 6) + 5] = vertIndex + 1;
				}

				++vertIndex;
			}
		}

		// update the mesh
		OutputMeshFilter.mesh.vertices = vertices;
		OutputMeshFilter.mesh.triangles = triangles;
		OutputMeshFilter.mesh.uv = uvCoords;
		OutputMeshFilter.mesh.RecalculateBounds();
		OutputMeshFilter.mesh.RecalculateNormals();

		// based on the normal mix in some of the rock colour
		for (int vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex)
		{
			if ((vertices[vertexIndex].y >= GrassHeight) && (vertices[vertexIndex].y <= SnowHeight))
			{
				float slope = 1f - OutputMeshFilter.mesh.normals[vertexIndex].y;

				if (slope < SlopeThresholdForRock)
					vertexColours[vertexIndex] = Color32.Lerp(vertexColours[vertexIndex], RockColour, slope / SlopeThresholdForRock);
				else
					vertexColours[vertexIndex] = RockColour;
			}
		}

		// save out all of the vertices to the terrain map image
		vertIndex = 0;
		Texture2D terrainMap = new Texture2D(width, height, TextureFormat.ARGB32, false);
		for (int x = 0; x < width; ++x)
		{
			for (int z = 0; z < height; ++z)
			{
				Color pixelColour = vertexColours[vertIndex];
				terrainMap.SetPixel(x, z, pixelColour);

				++vertIndex;
			}
		}

		// update the texture
		terrainMap.Apply();
		// byte[] bytes = terrainMap.EncodeToPNG();
     	// File.WriteAllBytes("TerrainMap.png", bytes);

		OutputMeshFilter.mesh.colors32 = vertexColours;

		AssetDatabase.CreateAsset(OutputMeshFilter.mesh, "Assets/TerrainMesh.asset");
		AssetDatabase.SaveAssets();

		terrain.enabled = false;
	}

	// Use this for initialization
	void Start () {
		GenerateMesh();
	}
	
	// Update is called once per frame
	void Update () {
    //  for (int i=0; i<OutputMeshFilter.mesh.vertices.Length; i++){
    //      Vector3 norm = OutputMeshFilter.gameObject.transform.TransformDirection(OutputMeshFilter.mesh.normals[i]);
    //      Vector3 vert = OutputMeshFilter.gameObject.transform.TransformPoint(OutputMeshFilter.mesh.vertices[i]);
    //      Debug.DrawRay(vert, norm * 10f, Color.red);
    //  }
	}
}
