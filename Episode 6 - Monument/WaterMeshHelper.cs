using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterMeshHelper : MonoBehaviour {
	public int width = 128;
	public int height = 128;

	public float xScale = 64f;
	public float zScale = 64f;
	public float heightScale = 0.5f;

	public float timeScale = 0.25f;

	protected MeshFilter meshFilter;
	protected Vector3 minPoint;
	protected Vector3 sampleScale;

	// Use this for initialization
	void Start () {
		// retrieve the mesh
		meshFilter = gameObject.GetComponent<MeshFilter>();

		Bounds renderBounds = gameObject.GetComponent<MeshRenderer>().bounds;

		// Retrieve the min point
		minPoint = renderBounds.min;
		minPoint.y = 0;

		// Determine the sample scale
		sampleScale = renderBounds.extents * 2;
		sampleScale.x /= width;
		sampleScale.z /= height;

		GenerateMesh();

		transform.localScale = Vector3.one;
	}
	
	// Update is called once per frame
	void Update () {
		GenerateMesh();
	}

	void GenerateMesh() {
		// generate the vertices and indices
		Vector3[] vertices = new Vector3[(width + 0) * (height + 0)];
		int[] triangles = new int[width * height * 2 * 3];
		int vertIndex = 0;
		for (int x = 0; x < width; ++x)
		{
			for (int z = 0; z < height; ++z)
			{
				float pointHeight = heightScale * Mathf.PerlinNoise(timeScale * Time.time + (xScale * x / width), 
																	timeScale * Time.time + (zScale * z / height)) - (heightScale * 0.5f);
				vertices[vertIndex] = minPoint + new Vector3(x * sampleScale.x,  pointHeight, z * sampleScale.z);

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
		meshFilter.mesh.vertices = vertices;
		meshFilter.mesh.triangles = triangles;
		meshFilter.mesh.RecalculateBounds();
		meshFilter.mesh.RecalculateNormals();
	}
}
