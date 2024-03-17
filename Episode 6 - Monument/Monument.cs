using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monument : MonoBehaviour {
	[System.Serializable]
	public class BrickWrapper
	{
		public Brick brickScript;
		public MeshRenderer brickRenderer;
		public float azimuth = 0;
		public float elevation = -90f;
		public MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
		public Message brickMessage = null;

		public BrickWrapper(Brick _brickScript, MeshRenderer _brickRenderer)
		{
			brickScript = _brickScript;
			brickRenderer = _brickRenderer;
		}
	}

	public GameObject BrickPrefab;
	public Vector3 brickDimensions;
	public GameObject BrickAnchor;
	public Material[] BrickMaterials;

	[Header("Building Mode")]
	public int NumBricksX = 10;
	public int NumBricksZ = 5;
	protected int bricksPerLayer = 0;

	[Header("Sphere Mode")]
	public float RotationalSpeed = 10f;
	public int MinimumNumberOfRings = 7;
	public int BricksPerRing = 10;
	public int NumberOfRings = 7;
	public float MinimumRadius = 5f;
	public float RadiusScale = 0.1f;
	public float SphereRingVerticalBuffer = 0.5f;
	public int MinimumBricksForSphere = 30;
	protected List<BrickWrapper> ActiveBricks = new List<BrickWrapper>();
	protected List<BrickWrapper> PendingBricks = new List<BrickWrapper>();
	protected List<Message> ActiveMessages = new List<Message>();
	protected float AddBrickCooldown = 0f;

	protected Vector3 BrickOffset;

	protected int totalNumBricks = 0;

	protected int lastNumServerBricks = 0;
	protected int lastNumPlayerBricks = 0;
	protected ServerInterface serverInstance;

	public int NumBricksToPreheat = 10;

	public float PulseAmount = 0.1f;
	public float PulseTime = 12f;

	// Use this for initialization
	void Start () {
		bricksPerLayer = (NumBricksX + NumBricksZ) * 2;
		BrickOffset = new Vector3(brickDimensions.x * (NumBricksX - 1) - brickDimensions.z, 0, brickDimensions.x * NumBricksZ) * -0.5f;
		BrickOffset.y += brickDimensions.y;
		BrickOffset += BrickAnchor.transform.position;

		serverInstance = FindObjectOfType<ServerInterface>();
	}
	
	// Update is called once per frame
	void Update () {
		// debug key in editor to queue up more bricks
		if (Application.isEditor && Input.GetKeyDown(KeyCode.B))
			PlaceBrick_Sphere();

		// if there are a different number of messages then 
		if (serverInstance.ServerMessages.Count != lastNumServerBricks || serverInstance.PlayerMessages.Count != lastNumPlayerBricks)
		{
			UpdateBricks();
		}

		// enqueue additional random bricks if needed if we don't have enough for the sphere
		while((ActiveBricks.Count + PendingBricks.Count) < MinimumBricksForSphere)
		{
			PlaceBrick_Sphere();
		}

		// determine the total number of bricks and the working radius
		int totalNumBricks = ActiveBricks.Count + PendingBricks.Count;
		NumberOfRings = MinimumNumberOfRings + (totalNumBricks - MinimumBricksForSphere) / BricksPerRing;
		float workingRadius = MinimumRadius + (totalNumBricks - MinimumBricksForSphere) * RadiusScale;

		// if there are no bricks then do nothing
		if (totalNumBricks == 0)
			return;

		// work out the maximum azimuth
		float maxAzimuth = NumberOfRings * 360f;

		// if we have no active bricks then preheat the monument
		if (ActiveBricks.Count == 0)
		{
			// preheat bricks up to the required threshold
			while (PendingBricks.Count > 0 && ActiveBricks.Count < NumBricksToPreheat)
			{
				// add to the start of the active bricks
				ActiveBricks.Insert(0, PendingBricks[0]);
				PendingBricks[0].brickRenderer.enabled = true;

				// update the transparency on the brick
				PendingBricks[0].propertyBlock.SetFloat("_Transparency", 0f);
				PendingBricks[0].brickRenderer.SetPropertyBlock(PendingBricks[0].propertyBlock);

				// remove from the pending bricks
				PendingBricks.RemoveAt(0);
			}

			// update the azimuth on any of the preheated bricks
			for(int index = 0; index < ActiveBricks.Count; ++index)
			{
				BrickWrapper brick = ActiveBricks[index];

				brick.azimuth = (index + 1) * 360f * NumberOfRings / totalNumBricks;

				// set the rotation of the brick
				brick.brickScript.gameObject.transform.Rotate(0, -brick.azimuth, 0);
			}
		}

		// update the azimuth and elevation of all of the active bricks
		bool removedBrick = false;
		for (int index = 0; index < ActiveBricks.Count; ++index)
		{
			BrickWrapper brick = ActiveBricks[index];

			// update the azimuth
			brick.azimuth += RotationalSpeed * Time.deltaTime;

			// update the elevation
			brick.elevation = Mathf.Lerp(-90f, 90f, brick.azimuth / maxAzimuth);

			// update the transparency on the brick
			float newTransparency = 1f;
			if (brick.azimuth <= 360f)
			{
				newTransparency = 0;
				brick.brickScript.BrickTextMesh.enabled = false;
			}
			else if (brick.azimuth <= 720f)
			{
				newTransparency = (brick.azimuth - 360f) / 360f;
				brick.brickScript.BrickTextMesh.enabled = true;
			}
			else if (brick.azimuth >= (maxAzimuth - 720f))
				newTransparency = (maxAzimuth - brick.azimuth - 360f) / 360f;
			else if (brick.azimuth >= (maxAzimuth - 360f))
			{
				newTransparency = 0;
				brick.brickScript.BrickTextMesh.enabled = false;
			}

			// apply the transparency
			brick.propertyBlock.SetFloat("_Transparency", newTransparency);
			brick.brickRenderer.SetPropertyBlock(brick.propertyBlock);

			// is this brick done?
			if (brick.azimuth >= maxAzimuth)
			{
				// reset the brick
				brick.azimuth = 0f;
				brick.elevation = -90f;
				brick.brickRenderer.enabled = false;

				// add it to the pending bricks
				PendingBricks.Add(brick);

				// remove it from this list
				ActiveBricks.RemoveAt(index);
				--index;

				removedBrick = true;
			}
		}

		// update the brick cooldown remaining
		if (AddBrickCooldown > 0)
			AddBrickCooldown -= Time.deltaTime;

		// dequeue a pending brick if present
		if ((PendingBricks.Count > 0) && (removedBrick || AddBrickCooldown <= 0))
		{
			// add to the start of the active bricks
			ActiveBricks.Insert(0, PendingBricks[0]);
			PendingBricks[0].brickRenderer.enabled = true;

			// update the transparency on the brick
			PendingBricks[0].propertyBlock.SetFloat("_Transparency", 0f);
			PendingBricks[0].brickRenderer.SetPropertyBlock(PendingBricks[0].propertyBlock);

			// remove from the pending bricks
			PendingBricks.RemoveAt(0);

			// update the cooldown time
			AddBrickCooldown = (360f * NumberOfRings / RotationalSpeed) / totalNumBricks;
		}

		// calculate the scale factor to apply to the radius of the monument
		float radiusScale = 1f + (Mathf.Sin(Time.time * 2f * Mathf.PI / PulseTime) * PulseAmount);

		// move everything to its new location based on the azimuth and elevation
		for (int index = 0; index < ActiveBricks.Count; ++index)
		{
			BrickWrapper brick = ActiveBricks[index];

			// calculate the location of the brick
			float radiusAtElevation = MinimumRadius * Mathf.Cos(brick.elevation * Mathf.Deg2Rad) * radiusScale;
			Vector3 offset = new Vector3(radiusAtElevation * Mathf.Cos(brick.azimuth * Mathf.Deg2Rad),
										 workingRadius * Mathf.Sin(brick.elevation * Mathf.Deg2Rad),
										 radiusAtElevation * Mathf.Sin(brick.azimuth * Mathf.Deg2Rad));

			// set the position of the brick
			brick.brickScript.gameObject.transform.localPosition = offset + (Vector3.up * workingRadius);

			// set the rotation of the brick
			brick.brickScript.gameObject.transform.Rotate(0, -RotationalSpeed * Time.deltaTime, 0);
		}
	}

	void UpdateBricks()
	{
		// player messages go first
		foreach(Message message in serverInstance.PlayerMessages)
		{
			// is the message not already in the list?
			if(!ActiveMessages.Contains(message))
			{
				// add to the active messages
				ActiveMessages.Insert(0, message);
				
				// queue up the brick
				QueueNewMessage(message);
			}
		}

		// server messages go last and only if different
		foreach(Message message in serverInstance.ServerMessages)
		{
			// is the message not already in the list?
			if(!ActiveMessages.Contains(message))
			{
				// add to the active messages
				ActiveMessages.Insert(0, message);
				
				// queue up the brick
				QueueNewMessage(message);
			}
		}
	}

	void QueueNewMessage(Message message)
	{
		BrickWrapper foundBrickWrapper = null;

		// first check if any of our bricks don't have an associated message
		foreach(BrickWrapper brickWrapper in PendingBricks)
		{
			// found an empty brick
			if (brickWrapper.brickMessage == null)
			{
				foundBrickWrapper = brickWrapper;
			}
		}

		// next check if any of our active bricks don't have a message (if no pending were valid)
		if (foundBrickWrapper == null)
		{
			foreach(BrickWrapper brickWrapper in ActiveBricks)
			{
				// found an empty brick
				if (brickWrapper.brickMessage == null)
				{
					foundBrickWrapper = brickWrapper;
				}
			}
		}

		// finally our fallback is to insert a new brick if need be
		if (foundBrickWrapper == null)
		{
			GameObject newBrick = Instantiate(BrickPrefab, -Vector3.up * 100f, Quaternion.Euler(0, -90f, 0), BrickAnchor.transform);
			MeshRenderer brickRenderer = newBrick.GetComponent<MeshRenderer>();
			brickRenderer.material = BrickMaterials[Random.Range(0, BrickMaterials.Length)];
			brickRenderer.enabled = false;

			PendingBricks.Add(new BrickWrapper(newBrick.GetComponent<Brick>(), brickRenderer));

			foundBrickWrapper = PendingBricks[PendingBricks.Count - 1];
		}

		// update the brick to match
		foundBrickWrapper.brickMessage = message;
		foundBrickWrapper.brickScript.SetMessage(message, this);
	}

	void PlaceBrick_Sphere()
	{
		GameObject newBrick = Instantiate(BrickPrefab, -Vector3.up * 100f, Quaternion.Euler(0, -90f, 0), BrickAnchor.transform);
		MeshRenderer brickRenderer = newBrick.GetComponent<MeshRenderer>();
		brickRenderer.material = BrickMaterials[Random.Range(0, BrickMaterials.Length)];
		brickRenderer.enabled = false;

		PendingBricks.Add(new BrickWrapper(newBrick.GetComponent<Brick>(), brickRenderer));
	}

	void PlaceBrick_Building()
	{
		// determine the layer and position in the layer
		int brickLayer = totalNumBricks / bricksPerLayer;
		int positionInLayer = totalNumBricks % bricksPerLayer;

		// is this an odd or even layer?
		bool isOddLayer = brickLayer % 2 == 1;

		// set the y position of the brick
		Vector3 brickPosition = brickLayer * brickDimensions.y * Vector3.up;

		// determine which side the brick is on
		//    2
		// 3     1
		//    0
		int brickSide = 0;
		int positionInSide = positionInLayer;
		if (positionInLayer >= NumBricksX)
		{
			if (positionInLayer >= (NumBricksX + NumBricksZ))
			{
				if (positionInLayer >= (NumBricksX + NumBricksX + NumBricksZ))
				{
					positionInSide -= NumBricksX + NumBricksX + NumBricksZ;
					brickSide = 3;
				}
				else
				{
					positionInSide -= NumBricksX + NumBricksZ;
					brickSide = 2;
				}
			}
			else
			{
				positionInSide -= NumBricksX;
				brickSide = 1;
			}
		}

		// Set the rotation of the brick
		Quaternion brickRotation = Quaternion.Euler(0, brickSide * -90f, 0);

		// Calculate the x and z positions of the brick
		if (brickSide == 0)
		{
			brickPosition.x = positionInSide * brickDimensions.x;
			brickPosition.z = 0;

			if (isOddLayer)
			{
				brickPosition.x -= brickDimensions.z;
			}
			else
			{
			}
		}
		else if (brickSide == 1)
		{
			brickPosition.x = (NumBricksX - 1) * brickDimensions.x;
			brickPosition.z = positionInSide * brickDimensions.x;

			if (isOddLayer)
			{
				brickPosition.x += brickDimensions.z;
				brickPosition.z += brickDimensions.z;
			}
			else
			{
				brickPosition.x += brickDimensions.z;
				brickPosition.z += brickDimensions.z * 2;
			}
		}
		else if (brickSide == 2)
		{
			brickPosition.x = (NumBricksX - positionInSide - 1) * brickDimensions.x;
			brickPosition.z = (NumBricksZ - 1) * brickDimensions.x;

			if (isOddLayer)
			{
				brickPosition.z += brickDimensions.x;
			}
			else
			{
				brickPosition.x -= brickDimensions.z;
				brickPosition.z += brickDimensions.x;
			}
		}
		else if (brickSide == 3)
		{
			brickPosition.x = 0;
			brickPosition.z = (NumBricksZ - positionInSide - 1) * brickDimensions.x;

			if (isOddLayer)
			{
				brickPosition.x -= brickDimensions.z * 2;
				brickPosition.z += brickDimensions.z * 2;
			}
			else
			{
				brickPosition.x -= brickDimensions.z * 2;
				brickPosition.z += brickDimensions.z;
			}
		}

		// instantiate the brick
		GameObject newBrick = Instantiate(BrickPrefab, brickPosition + BrickOffset, brickRotation, BrickAnchor.transform);
		newBrick.GetComponent<MeshRenderer>().material = BrickMaterials[Random.Range(0, BrickMaterials.Length)];

		// increase the number of bricks
		++totalNumBricks;
	}
}
