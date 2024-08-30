using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class LightningConfig
{
    public float StartCellSize = 0.5f;
    public float EndCellSize = 0.2f;

    public int MinBranchInterval = 5;
    public int MaxBranchInterval = 10;

    public int MinBranchLength = 30;
    public int MaxBranchLength = 50;
    public float BranchVerticalChance = 0.1f;
    public float BranchDeviationChance = 0.5f;
    public int BranchCullLength = 10;
    public AnimationCurve BranchScaleWithProgress;

    public float LightningFlashTime = 0.5f;
    public float LightningPersistenceTime = 0.1f;

    public GameObject LightningBoltPrefab;
}

public class LightningGenerator : MonoBehaviour
{
    [SerializeField] LightningConfig Config;
    [SerializeField] float LightningHeight = 75f;
    [SerializeField] CarriageUpgrade LightningRodUpgrade;
    [SerializeField] float CarriageStrikeChance = 0.1f;

    [SerializeField] float MinLightningStrikeInterval = 0.1f;
    [SerializeField] float MaxLightningStrikeInterval = 5f;
    [SerializeField, Range(0f, 1f)] float StrikeTimeVariation = 0.25f;

    [SerializeField] GameObject DestructionVFXPrefab;
    [SerializeField] GameObject LightningSoundEffectPrefab;

    TrackTile CurrentPlayerTile = null;

    float LightningIntensity = 0f;
    float TimeUntilNextStrike = 0f;

    public static LightningGenerator Instance { get; private set; } = null;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"Found duplicate LightningGenerator on {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetIntensity(float newValue)
    {
        if (LightningIntensity <= 0f && newValue > 0f)
            PickNextStrikeTime();

        LightningIntensity = newValue;
    }

    // Start is called before the first frame update
    void Start()
    {
        PickNextStrikeTime();
    }

    void PickNextStrikeTime()
    {
        TimeUntilNextStrike = Mathf.Lerp(MaxLightningStrikeInterval, MinLightningStrikeInterval, LightningIntensity);
        TimeUntilNextStrike *= 1f + Random.Range(-StrikeTimeVariation, StrikeTimeVariation);
    }

    // Update is called once per frame
    void Update()
    {
        // lightning is off?
        if (LightningIntensity <= 0f)
            return;

        if (EternalCollector.Instance != null && EternalCollector.Instance.CurrentPlayerTile != null)
        {
            // current player tile has changed
            if (CurrentPlayerTile == null || CurrentPlayerTile != EternalCollector.Instance.CurrentPlayerTile)
            {
                CurrentPlayerTile = EternalCollector.Instance.CurrentPlayerTile;
            }
        }

        // counting down till next strike?
        if (TimeUntilNextStrike > 0f)
        {
            TimeUntilNextStrike -= Time.deltaTime;

            if (TimeUntilNextStrike <= 0f)
            {
                PickNextStrikeTime();
                SpawnLightning();
            }
        }
    }

    GameObject LightningTarget;
    void OnHitScrap()
    {
        var destructionGO = Instantiate(DestructionVFXPrefab, LightningTarget.transform.position, Quaternion.identity);
        AkSoundEngine.PostEvent(AK.EVENTS.PLAY_LIGHTNING_DESTRUCTION, destructionGO);

        Destroy(LightningTarget);
        LightningTarget = null;
    }

    void OnHitLightningRod()
    {
        EternalCollector.Instance.OnLightningStrike();
        LightningTarget = null;
    }

    void SpawnLightning()
    {
        UnityAction onHitAction = null;
        LightningTarget = null;
        if (EternalCollector.IsUpgradeUnlocked(LightningRodUpgrade) && Random.Range(0f, 1f) < CarriageStrikeChance)
        {
            // pick a random carriage
            int carriageIndex = Random.Range(0, EternalCollector.Instance.NumCarriages);
            var carriage = EternalCollector.Instance.GetCarriage(carriageIndex);

            List<GameObject> candidateMarkers = new List<GameObject>();
            foreach(var marker in carriage.ActiveMarkers)
            {
                if (marker.Type == Marker.EMarkerType.Lightning_Rod)
                    candidateMarkers.Add(marker.gameObject);
            }

            // pick a random candidate
            if (candidateMarkers.Count > 0)
            {
                onHitAction = OnHitLightningRod;
                LightningTarget = candidateMarkers[Random.Range(0, candidateMarkers.Count)];
            }
        }
        else
        {
            // pick a random piece of scrap
            LightningTarget = CurrentPlayerTile.PickRandomScrap();
            onHitAction = OnHitScrap;
        }

        if (LightningTarget == null)
            return;

        var lightningSoundEffectGO = Instantiate(LightningSoundEffectPrefab, LightningTarget.transform.position + Vector3.up * LightningHeight, Quaternion.identity);
        AkSoundEngine.PostEvent(AK.EVENTS.PLAY_LIGHTNING_STRIKE, lightningSoundEffectGO);
        BuildLightning(LightningTarget.transform.position + Vector3.up * LightningHeight, LightningTarget.transform.position, onHitAction);
    }

    void BuildLightning(Vector3 start, Vector3 end, UnityAction onHitAction)
    {
        var newLightningGO = Instantiate(Config.LightningBoltPrefab, end, Quaternion.identity, transform);
        LightningBolt newBolt = newLightningGO.GetComponent<LightningBolt>();
        newBolt.Build(start, end, Config, onHitAction);
    }
}
