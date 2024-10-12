using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class SpawnAmount
{
    public EnemyConfig AIConfig;

    [RangeAttribute(0f, 1f)]
    public float Weighting = 0.5f;
}

[System.Serializable]
public class WaveElement
{
    [Header("Timing")]
    public float SpawnDelay = 30f;

    [Header("Composition")]
    public int NumberToSpawn = 12;
    public float MovementSpeed = 1f;
    public List<SpawnAmount> MandatoryAIs;

    [Header("Augmentations")]
    public int NumberToAugment = 0;
    public List<AugmentationConfig> Augmentations;

    public bool ContainsBosses
    {
        get
        {
            foreach(SpawnAmount amount in MandatoryAIs)
            {
                if (amount.AIConfig.IsBoss)
                    return true;
            }

            return false;
        }
    }

    [System.NonSerialized]
    private List<float> _normalisedThresholds;
    public List<float> NormalisedThresholds
    {
        get
        {
            if (_normalisedThresholds == null)
            {
                // generate the normalised list
                float sum = MandatoryAIs.Sum(config => config.Weighting);
                _normalisedThresholds = MandatoryAIs.Select(config => config.Weighting / sum).ToList();

                // convert into thresholds
                for (int index = 1; index < _normalisedThresholds.Count; ++index)
                {
                    _normalisedThresholds[index] += _normalisedThresholds[index - 1];
                }
            }

            return _normalisedThresholds;
        }
    }

    public EnemyConfig RandomEnemyConfig
    {
        get
        {
            float roll = Random.Range(0f, 1f);

            // find the first match
            int matchingIndex = -1;
            for (int index = 0; index < NormalisedThresholds.Count; ++index)
            {
                if (roll <= NormalisedThresholds[index])
                    matchingIndex = index;
                else
                    break;
            }

            return matchingIndex >= 0 ? MandatoryAIs[matchingIndex].AIConfig : MandatoryAIs.Last().AIConfig;
        }
    }
}

[CreateAssetMenu(fileName = "Wave Config", menuName = "Injaia/Wave Config", order = 1)]
public class WaveConfig : ScriptableObject
{
    [Header("Locations")]
    public bool North;
    public bool East;
    public bool West;
    public bool South;

    [Header("Elements")]
    public float PreDelay = 5f;
    public float PostDelay = 5f;
    public float HealthScale = 1.5f;
    public List<WaveElement> Elements;

    [Header("Weapon")]
    public AmmoSO WeaponToAdd;

    [System.NonSerialized]
    private List<SpawnerLocation> _availableLocations = null;

    public SpawnerLocation RandomSpawn
    {
        get
        {
            if (_availableLocations == null)
            {
                _availableLocations = new List<SpawnerLocation>();

                if (North)
                    _availableLocations.Add(SpawnerLocation.North);
                if (East)
                    _availableLocations.Add(SpawnerLocation.East);
                if (West)
                    _availableLocations.Add(SpawnerLocation.West);
                if (South)
                    _availableLocations.Add(SpawnerLocation.South);
            }

            return _availableLocations[Random.Range(0, _availableLocations.Count)];
        }
    }
}
