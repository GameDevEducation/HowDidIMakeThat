using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseHeightModifier : MonoBehaviour
{
    [SerializeField] [Range(0f, 1f)] protected float Intensity = 1f;

    public abstract void Execute(int vertsPerSide, Vector2Int gridLocation, float maxHeight, Vector3[] vertices);
}
