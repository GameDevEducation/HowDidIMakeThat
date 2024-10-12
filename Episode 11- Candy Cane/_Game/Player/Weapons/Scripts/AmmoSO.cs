using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AmmoBehaviour
{
    None,

    ClusterBomb,
    Explode
}

[System.Serializable]
public class StatusEffect
{
    public enum Types
    {
        None,

        Slow,
        Burn,
        Stun
    }

    public Types Type;
    public float Duration = 0f;
    public float Strength = 0f;
}

[System.Serializable]
public class FireModeConfig
{
    [Header("General")]
    [TextArea(1, 4)]
    public string FlavourText;
    public AmmoBehaviour Behaviour;
    public float Cooldown = 0.5f;
    public float RadiusOfEffect = 1f;
    public float ActivationTime = 1f;
    public GameObject ProjectilePrefab;

    [Header("Damage Config")]
    public float BaseDamage = 10f;
    public float CriticalHitChance = 0f;
    public float CriticalHitMultiplier;

    [Header("Cluster Config")]
    public float DetonationHeight = 5f;
    public int NumPerCluster = 12;
    public AmmoSO AmmoToSpawn;
    public float DetonationForce = 10f;
    public LayerMask DetonationLayerMask;

    [Header("Visual Effects")]
    public GameObject ImpactEffect;
    public string ImpactSound;
    public string FireSound;    

    [Header("Status Effects")]
    public List<StatusEffect> StatusEffects;
}

[CreateAssetMenu(fileName = "Ammo Config", menuName = "Injaia/Ammo Config", order = 1)]
public class AmmoSO : ScriptableObject
{
    [Header("General")]
    public string Name;
    [TextArea(1, 4)]
    public string FlavourText;

    [Header("Primary Fire")]
    public bool HasPrimaryFire = true;
    public FireModeConfig PrimaryFire;

    [Header("Secondary Fire")]
    public bool HasSecondaryFire = false;
    public FireModeConfig SecondaryFire;
}
