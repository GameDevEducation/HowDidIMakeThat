using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpawnerLocation
{
    None,

    North,
    East,
    South,
    West
}

public class Spawner : MonoBehaviour
{
    public SpawnerLocation Location = SpawnerLocation.None;

    public List<GameObject> SpawnPoints;

    public float ScanRange = 12f;
    public float ScanStepDistance = 2f;
    public float ScanDepth = 10f;
    
    // Start is called before the first frame update
    void Start()
    {
        int numSteps = Mathf.CeilToInt(ScanRange / ScanStepDistance);

        // perform the scan
        for(int x = 0; x < numSteps; ++x)
        {
            for (int z = 0; z < numSteps; ++z)
            {
                // calculate the location
                Vector3 location = transform.position;
                location += transform.forward * ScanStepDistance * (z - (numSteps / 2f));
                location += transform.right * ScanStepDistance * (x - (numSteps / 2f));
                location += Vector3.up * ScanDepth * 0.5f;

                // run a raycast to make sure it hits terrain
                RaycastHit hitResult;
                if (Physics.Raycast(location, Vector3.down, out hitResult, ScanDepth))
                {
                    if (hitResult.collider.gameObject.CompareTag("Ground"))
                    {
                        GameObject spawn = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                        // position the spawn point
                        spawn.transform.position = hitResult.point;
                        spawn.transform.rotation = transform.rotation;
                        spawn.transform.SetParent(transform);
                        spawn.SetActive(false);

                        SpawnPoints.Add(spawn);
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
