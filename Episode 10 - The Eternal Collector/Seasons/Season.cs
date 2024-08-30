using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TEC/Season", fileName = "Season")]
public class Season : ScriptableObject
{
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField, Range(0.5f, 1.5f)] public float SolarIrradianceModifier { get; private set; } = 1f;
    [field: SerializeField] public int MinimumLength { get; private set; } = 6;
    [field: SerializeField] public int MaximumLength { get; private set; } = 10;
    [field: SerializeField, Range(-2f, 2f)] public float DayLengthModifier { get; private set; } = 0f;
    [field: SerializeField] public AnimationCurve SeasonIntensity { get; private set; }

    [field: SerializeField] public List<Event_Base> Events { get; private set; } = new List<Event_Base>();
}
