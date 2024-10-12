using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AugmentationType
{
    None
}

[CreateAssetMenu(fileName = "Augmentation Config", menuName = "Injaia/Augmentation Config", order = 1)]
public class AugmentationConfig : ScriptableObject
{
    [Header("General Info")]
    public string Name;
    public AugmentationType Type;
    public float AugmentationStrength;

    [Header("Who is effected?")]
    public bool EffectsSelf;
    public bool EffectsSquad;
    public bool EffectsInRadius;
    [RangeAttribute(0f, 100f)]
    public float EffectRadius;
}
