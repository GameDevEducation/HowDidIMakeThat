using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CollapseModeEnum
{
    None,

    Fall,
    Explode,
    Disappear
}

[CreateAssetMenu(fileName = "BuildingPiece", menuName = "Injaia/Building Piece", order = 1)]
public class BuildingPieceSO : ScriptableObject
{
    public int HitPoints = -1;
    
    public float Mass = 1f;
    public CollapseModeEnum CollapseMode = CollapseModeEnum.None;
    public float ExplosionForce = 20f;

    public bool IsPathBlocker = false;

    public bool KillsAttackersOnDestruction = false;
}
