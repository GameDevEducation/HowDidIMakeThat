using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Duality/Player Config")]
public class PlayerConfig : ScriptableObject
{
    [Header("Horizontal Movement")]
    [field: SerializeField] public float HorizontalAcceleration { get; protected set; } = 0.1f;
    [field: SerializeField] public float HorizontalDrag { get; protected set; } = 0.5f;
    [field: SerializeField] public float MaxHorizontalVelocity { get; protected set; } = 5.0f;
    [field: SerializeField] public float HorizontalCounterBoost { get; protected set; } = 2.0f;

    [Header("Vertical Movement")]
    [field: SerializeField] public float VerticalAcceleration { get; protected set; } = 0.1f;
    [field: SerializeField] public float VerticalDrag { get; protected set; } = 0.5f;
    [field: SerializeField] public float MaxVerticalVelocity { get; protected set; } = 5.0f;
    [field: SerializeField] public float VerticalCounterBoost { get; protected set; } = 2.0f;

    [Header("Appearance")]
    [field: SerializeField] public float VelocityToRollScale { get; protected set; } = 8.0f;
}
