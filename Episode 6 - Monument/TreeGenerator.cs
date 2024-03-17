using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TreeConfig
{
	public GameObject TreePrefab;

	[RangeAttribute(0f, 1f)]
	public float Weighting;

	public float MinHeight;
	public float MaxHeight;

	public float GetWeightingForHeight(float height)
	{
		// no influence outside of heights
		if (height <= MinHeight || height >= MaxHeight)
			return 0;
		
		// linear weighting the farther we are from the centre point between min and max heights
		float weightingScale = (((height - MinHeight) / (MaxHeight - MinHeight)) - 0.5f) * 2f;
		
		return Mathf.Abs(Weighting * weightingScale);
	}
}

public class TreeGenerator : MonoBehaviour {
	public List<TreeConfig> Trees;

	public GameObject TreeNode;
	public Texture2D TreeMap;

	public float XNoiseScale = 32f;
	public float ZNoiseScale = 32f;

	public float NoiseThreshold = 0.5f;
	public float XPositionNoise = 0.5f;
	public float ZPositionNoise = 0.5f;

	public float ChanceToBePlaceable = 0.2f;
	public float PlaceableTreeScale = 0.1f;
	public float MaximumPlaceableSlope = 0.3f;
	public float MaximumPlaceableHeight = 35f;

	public Material PlaceableTreeMaterial;

	protected int TreeIndex = 0;

	// Use this for initialization
	void Start () {
		// retrieve the mesh so that we have access to the normals for slope information
		Mesh terrainMesh = gameObject.GetComponent<MeshFilter>().mesh;

		// Remove any existing trees
		foreach(Transform child in TreeNode.transform)
		{
			Destroy(child.gameObject);
		}
		TreeNode.transform.DetachChildren();

		// check that the texture map is valid
		if (terrainMesh.vertexCount != (TreeMap.width * TreeMap.height))
		{
			Debug.LogError("Vertex count (" + terrainMesh.vertexCount + ") of mesh does not match tree map pixel count");
			return;
		}

		// Generate the trees
		int treeCount = 0;
		for (int x = 0; x < TreeMap.width; ++x)
		{
			for (int z = 0; z < TreeMap.height; ++z)
			{
				// Not allowed to place a tree here?
				if (TreeMap.GetPixel(z, x).r < 0.5f)
					continue;

				// check noise at this point
				float vertexNoise = Mathf.PerlinNoise(XNoiseScale * x / TreeMap.width, ZNoiseScale * z / TreeMap.height);

				// can place a tree here?
				if (vertexNoise >= NoiseThreshold)
				{
					float slope = 1f - terrainMesh.normals[x + (z * TreeMap.width)].y;
					PlaceTreeAt(terrainMesh.vertices[x + (z * TreeMap.width)], slope);
					++treeCount;
				}
			}		
		}

		Debug.Log(treeCount);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	protected void PlaceTreeAt(Vector3 location, float slope)
	{
		List<TreeConfig> candidateTrees = new List<TreeConfig>();
		List<float> candidateWeights = new List<float>();

		// determine which trees area a candidate based on the height
		float totalWeight = 0f;
		foreach(TreeConfig treeConfig in Trees)
		{
			// retrieve the weighting for this height
			float weightingForHeight = treeConfig.GetWeightingForHeight(location.y);

			// skip if the tree can't be placed here
			if (weightingForHeight == 0)
				continue;

			// add in the tree to the candidates
			totalWeight += weightingForHeight;
			candidateTrees.Add(treeConfig);
			candidateWeights.Add(totalWeight);
		}

		// no valid trees?
		if (totalWeight == 0)
			return;

		// roll the random number and pick the tree
		float randomRoll = Random.Range(0f, totalWeight);
		for (int index = 0; index < candidateWeights.Count; ++index)
		{
			// found the matching tree?
			if (randomRoll <= candidateWeights[index])
			{
				PlaceTreeAt(candidateTrees[index], location, slope);
				return;
			}
		}
	}

	protected void PlaceTreeAt(TreeConfig tree, Vector3 location, float slope)
	{
		// Generate a random offset
		Vector3 positionOffset = new Vector3(Random.Range(-XPositionNoise, XPositionNoise), 0f, Random.Range(-ZPositionNoise, ZPositionNoise));
		Vector3 position = location + positionOffset + TreeNode.transform.position;

		// snap the position to the ground
		RaycastHit hitInfo;
		if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out hitInfo, 20f))
		{
			GameObject newTree = GameObject.Instantiate(tree.TreePrefab, 
														hitInfo.point, 
														Quaternion.Euler(0, Random.Range(0, 360f), 0), 
														TreeNode.transform);
			newTree.AddComponent<TreeNode>();
			Vector3 finalScale = Vector3.one * Random.Range(0.9f, 1.1f);

			// if this tree is placeable then mark it as such, otherwise set the final scale
			if ((Random.Range(0f, 1f) < ChanceToBePlaceable) && (slope < MaximumPlaceableSlope) && (location.y <= MaximumPlaceableHeight))
			{
				// setup the tree node
				TreeNode treeNode = newTree.GetComponent<TreeNode>();
				treeNode.UniqueId = TreeIndex;
				treeNode.IsPlaceable = true;
				treeNode.FinalScale = finalScale;
				treeNode.transform.localScale = Vector3.one * PlaceableTreeScale;

				// placeable trees also get a collider
				CapsuleCollider collider = newTree.AddComponent<CapsuleCollider>();
				collider.isTrigger = true;

				// swap the materials
				MeshRenderer mr = newTree.GetComponent<MeshRenderer>();
				treeNode.FinalMaterial = mr.material;
				mr.material = PlaceableTreeMaterial;

				++TreeIndex;
			}
			else
			{
				newTree.transform.localScale = finalScale;
				newTree.isStatic = true;
			}
		}
	}
}
