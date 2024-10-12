using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

[System.Serializable]
public class SetWeaponHandler : UnityEvent<AmmoSO> {}

public class AIManager : MonoBehaviour
{
    public List<Spawner> Spawners;
    public List<WaveConfig> Waves;
    public GameObject SquadPrefab;

    protected int CurrentWave = 0;
    protected int CurrentWaveElement = 0;
    protected bool _IsPaused = false;
    protected int LoopCount = 0;

    public bool CanAutoSpawn = false;
    protected float SpawnTimer = -1f;
    protected float IntermissionTimer = -1f;

    protected List<EnemySquad> ActiveSquads = new List<EnemySquad>();

    public List<BuildingPiece> AvailableTargets;

    private static AIManager _Instance;

    public SetWeaponHandler OnSetWeapon;
    protected bool FirstLoopThroughWaves = true;

    public static AIManager Instance
    {
        get
        {
            return _Instance;
        }
    }

    public bool IsPaused
    {
        get
        {
            return _IsPaused;
        }
    }

    public float CurrentHealthScale
    {
        get
        {
            return CurrentWaveConfig.HealthScale * (1 + LoopCount);
        }
    }

    protected WaveConfig CurrentWaveConfig
    {
        get
        {
            return Waves[CurrentWave];
        }
    }

    protected WaveElement CurrentWaveElementConfig
    {
        get
        {
            return CurrentWaveConfig.Elements[CurrentWaveElement];
        }
    }

    public void SetPaused(bool newValue)
    {
        _IsPaused = newValue;
    }

    void Awake()
    {
        if (_Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        SpawnTimer = Waves[0].PreDelay + Waves[0].Elements[0].SpawnDelay;
    }

    // Update is called once per frame
    void Update()
    {
        if (_IsPaused)
            return;

        // is auto spawning allowed?
        if (CanAutoSpawn)
        {
            // on an intermission?
            if (IntermissionTimer > 0)
            {
                IntermissionTimer -= Time.deltaTime;
            } // spawning?
            else if (SpawnTimer > 0)
            {
                // update the spawn timer
                SpawnTimer -= Time.deltaTime;

                // time to spawn?
                if (SpawnTimer <= 0)
                {                    
                    SpawnNextElement();
                }
            }
        }
    }

    protected void SpawnNextElement()
    {
        // spawn the wave
        PerformSpawning(CurrentWaveElementConfig, CurrentWaveConfig.RandomSpawn);

        // incremeent the wave element
        ++CurrentWaveElement;

        // all wave elements done?
        if (CurrentWaveElement >= CurrentWaveConfig.Elements.Count)
        {
            // reset the wave element
            CurrentWaveElement = 0;

            // set the intermission timer
            IntermissionTimer = CurrentWaveConfig.PostDelay;

            // update the wave
            ++CurrentWave;
            if (CurrentWave >= Waves.Count)
            {
                FirstLoopThroughWaves = false;
                CurrentWave = 0;
                ++LoopCount;
            }

            // add the pre delay to the intermission timer
            IntermissionTimer += CurrentWaveConfig.PreDelay;

            // update the spawn timer
            SpawnTimer = CurrentWaveConfig.Elements[0].SpawnDelay;

            // switch weapon if needed
            if (FirstLoopThroughWaves && CurrentWaveConfig.WeaponToAdd != null)
                OnSetWeapon?.Invoke(CurrentWaveConfig.WeaponToAdd);

            return;
        }

        // set the delay
        SpawnTimer = CurrentWaveElementConfig.SpawnDelay;
    }

    protected void PerformSpawning(WaveElement config, SpawnerLocation location)
    {
        List<EnemyAI> squadMembers = new List<EnemyAI>();

        // get the spawner
        Spawner selectedSpawner = Spawners.Where(spawner => spawner.Location == location).First();
        List<GameObject> workingSpawnPoints = selectedSpawner.SpawnPoints.ToList();

        // spawn enemies up to the required amount
        while (squadMembers.Count < config.NumberToSpawn)
        {
            // get a random config
            EnemyConfig enemyConfig = config.RandomEnemyConfig;

            // pick a spawn point
            GameObject spawnPoint = workingSpawnPoints[Random.Range(0, workingSpawnPoints.Count)];
            workingSpawnPoints.Remove(spawnPoint);

            // instantiate the enemy
            GameObject newEnemy = Instantiate(enemyConfig.Prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
            EnemyAI aiLogic = newEnemy.GetComponent<EnemyAI>();
            aiLogic.MovementSpeed = config.MovementSpeed;

            squadMembers.Add(aiLogic);
        }

        // instantiate the squad
        GameObject squadObject = GameObject.Instantiate(SquadPrefab, selectedSpawner.transform.position, selectedSpawner.transform.rotation);
        EnemySquad squadLogic = squadObject.GetComponent<EnemySquad>();

        AkSoundEngine.PostEvent(config.ContainsBosses ? "Play_WaveSpawn" : "Play_BossWaveSpawn", squadObject);

        // perform the squad setup
        squadLogic.Initialise(squadMembers, location);
        squadLogic.OnSquadDestroyed.AddListener(OnSquadDestroyed);
        ActiveSquads.Add(squadLogic);
    }

    public void RegisterTarget(BuildingPiece target)
    {
        AvailableTargets.Add(target);

        target.OnPieceDestroyed.AddListener(OnTargetDestroyed);
    }

    protected void OnTargetDestroyed(BuildingPiece target)
    {
        AvailableTargets.Remove(target);
    }

    protected void OnSquadDestroyed(EnemySquad squad)
    {
        ActiveSquads.Remove(squad);
    }
}
