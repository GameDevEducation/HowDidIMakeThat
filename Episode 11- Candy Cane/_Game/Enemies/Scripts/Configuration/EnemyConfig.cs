using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Config", menuName = "Injaia/Enemy Config", order = 1)]
public class EnemyConfig : ScriptableObject
{
    [Header("General Info")]
    public string Name;
    public List<GameObject> Prefabs;
    public int HitPoints;
    public bool IsBoss;

    [Header("Attack Config")]
    public float AttackInterval = 2f;
    public int DamagePerAttack = 15;
    public float CriticalHitChance = 0f;
    public float CriticalHitMultiplier = 1f;

    [Header("Augmentations")]
    public List<AugmentationConfig> Augmentations;

    public GameObject Prefab
    {
        get
        {
            return Prefabs[Random.Range(0, Prefabs.Count)];
        }
    }
}
