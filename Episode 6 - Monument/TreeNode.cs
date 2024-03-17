using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeNode : MonoBehaviour {
	public bool IsPlaceable = false;
	public Vector3 FinalScale = Vector3.one;
	public Material FinalMaterial;
	public int UniqueId;

	protected float GrowthSpeed = 0.1f;
	protected float GrowthProgress = 0f;
	protected float GrowthTime;
	protected Vector3 StartingScale;
	protected bool Grow = false;

	// Use this for initialization
	void Start () {
		// if this is a placeable tree check if the player already planted it
		if (IsPlaceable)
		{
			// try to access the server interface
			ServerInterface ServerInstance = FindObjectOfType<ServerInterface>();
			if (ServerInstance != null)
			{
				// tree is already planted
				if (ServerInstance.PlayerTreesPlanted.Contains(UniqueId))
				{
					// insta-grow the gree
					Grow = false;
					GrowthProgress = 1f;
					gameObject.GetComponent<MeshRenderer>().material = FinalMaterial;
					transform.localScale = FinalScale;
					
					// remove the collider
					CapsuleCollider collider = gameObject.GetComponent<CapsuleCollider>();
					Destroy(collider);
				}
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		// still updating the growth progress?
		if (Grow && GrowthProgress < 1f)
		{
			// update the progress
			GrowthProgress = Mathf.Clamp01(GrowthProgress + (Time.deltaTime / GrowthTime));

			// update the scale
			transform.localScale = Vector3.Slerp(StartingScale, FinalScale, GrowthProgress);

			if (GrowthProgress >= 1f)
				Grow = false;
		}
	}

	public void StartGrowing() {
		CapsuleCollider collider = gameObject.GetComponent<CapsuleCollider>();

		// play the plant tree sound
		AkSoundEngine.PostEvent("Play_PlantTree", gameObject);

		// determine the relative height difference
		float heightDelta = (collider.bounds.extents.y * 2) * (FinalScale.y - transform.localScale.y);

		// calculate the growth time (add a slight randomness)
		GrowthTime = (heightDelta / GrowthSpeed) * Random.Range(0.9f, 1.1f);

		// store the starting scale
		StartingScale = transform.localScale;

		// swap the material
		gameObject.GetComponent<MeshRenderer>().material = FinalMaterial;

		// enable growing
		Grow = true;
		
		// remove the collider
		Destroy(collider);
	}
}
