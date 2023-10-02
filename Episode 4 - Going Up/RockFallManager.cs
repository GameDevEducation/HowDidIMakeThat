using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockFallManager : MonoBehaviour
{
    #pragma warning disable 0649
    [Header("Prefabs")]
    [SerializeField] private List<GameObject> RockPrefabs;

    [Header("Performance")]
    [SerializeField] private int MaxActiveRocks = 100;
    [SerializeField] private float RockKillHeight = 20f;

    [Header("Spawners")]
    [SerializeField] private List<GameObject> Spawners;

    [Header("Targetting")]
    [SerializeField] private float TargetRadius = 15f;
    [SerializeField] private float MinLaunchForce = 2f;
    [SerializeField] private float MaxLaunchForce = 5f; 

    [Header("Rock Fall Frequency")]
    [SerializeField] private float TimeBetweenFalls = 90f;
    [SerializeField] private float FallTimeVariation = 15f;
    [SerializeField] private float TimeReductionWithSpeed = 1.5f; // at maximum speed falls would happen every 30 seconds

    [Header("Rock Fall Intensity")]
    [SerializeField] private int MinRocksInRockFall = 20;
    [SerializeField] private int MaxRocksInRockFall = 50;
    [SerializeField] private float MinTimeBetweenRocks = 0.05f;
    [SerializeField] private float MaxTimeBetweenRocks = 0.2f;
    #pragma warning restore 0649

    private List<GameObject> ActiveRocks = new List<GameObject>();
    private Platform ActivePlatform;

    private float TimeUntilNextFall = 0f;
    private float TimeUntilNextRock = 0f;
    private int NumRocksToSpawn = 0;
    private bool RockFallInProgress = false;

    // Start is called before the first frame update
    void Start()
    {
        TimeUntilNextFall = Random.Range(-FallTimeVariation, FallTimeVariation) + TimeBetweenFalls;
        ActivePlatform = FindObjectOfType<Platform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (PauseManager.IsPaused)
            return;

        // rock fall in progress?
        if (RockFallInProgress)
        {
            TimeUntilNextRock -= Time.deltaTime;

            // time to spawn a rock
            if (TimeUntilNextRock <= 0)
            {
                SpawnRock();
                --NumRocksToSpawn;

                // more left to spawn
                if (NumRocksToSpawn > 0)
                    TimeUntilNextRock = Random.Range(MinTimeBetweenRocks, MaxTimeBetweenRocks);
                else
                {
                    // queue up the next fall
                    TimeUntilNextFall = Random.Range(-FallTimeVariation, FallTimeVariation) + TimeBetweenFalls;
                    RockFallInProgress = false;
                }
            }
        }
        else
        {
            TimeUntilNextFall -= Time.deltaTime * (1f + ActivePlatform.CurrentSpeed) * TimeReductionWithSpeed;

            if (TimeUntilNextFall <= 0)
                BeginRockFall();
        }

        // cull any rocks that are below the platform
        for (int index = 0; index < ActiveRocks.Count; ++index)
        {
            // cleanup the rock if it is below the platform
            if (ActiveRocks[index].transform.position.y < (ActivePlatform.LowestPoint - RockKillHeight))
            {
                Destroy(ActiveRocks[index]);
                ActiveRocks.RemoveAt(index);
                --index;
            }
        }
    }

    void BeginRockFall()
    {
        // setup the rock fall
        RockFallInProgress = true;
        TimeUntilNextRock = Random.Range(MinTimeBetweenRocks, MaxTimeBetweenRocks);
        NumRocksToSpawn = Random.Range(MinRocksInRockFall, MaxRocksInRockFall);
    }

    void SpawnRock()
    {
        // select a rock
        GameObject selectedPrefab = RockPrefabs[Random.Range(0, RockPrefabs.Count)];
    
        // select a spawner
        GameObject selectedSpawner = Spawners[Random.Range(0, Spawners.Count)];

        // spawn the rock
        GameObject spawnedRock = GameObject.Instantiate(selectedPrefab, selectedSpawner.transform.position, Quaternion.identity);

        // initialise the rock
        Rock rockScript = spawnedRock.GetComponent<Rock>();
        Vector3 targetPoint = ActivePlatform.transform.position + new Vector3(Random.Range(-TargetRadius, TargetRadius), 0f, Random.Range(-TargetRadius, TargetRadius));
        rockScript.Launch(ActivePlatform, targetPoint, Random.Range(MinLaunchForce, MaxLaunchForce));

        // add to the rock list and cleanup any old ones
        ActiveRocks.Add(spawnedRock);
        if (ActiveRocks.Count > MaxActiveRocks)
        {
            Destroy(ActiveRocks[0]);
            ActiveRocks.RemoveAt(0);
        }
    }
}
