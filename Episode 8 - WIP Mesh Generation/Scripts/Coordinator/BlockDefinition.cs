using System.Collections.Generic;
using UnityEngine;

[System.Flags]
public enum EPermittedZones
{
    Ceiling = 0x01,
    Middle = 0x02,
    Floor = 0x04,

    All = 0xFF
}

[System.Serializable]
public class BlockTemplate
{
    [field: SerializeField] public GameObject BlockPrefab { get; protected set; }
    [field: SerializeField] public Material BlockMaterial { get; protected set; }
    [field: SerializeField] public BlockVisualAsset BlockVisualAsset { get; protected set; }
}

[System.Serializable]
public class BlockTemplateSet
{
    [field: SerializeField] public List<BlockTemplate> Templates { get; protected set; }
}

[CreateAssetMenu(fileName = "BlockDefinition", menuName = "Duality/Block Definition")]
public class BlockDefinition : ScriptableObject
{
    [field: SerializeField] public EPermittedZones Zones { get; protected set; } = EPermittedZones.All;

    [field: SerializeField] public bool bHasProportionLimits { get; protected set; } = false;
    [field: SerializeField, Range(0f, 1f)] public float MinProportion { get; protected set; } = 0f;
    [field: SerializeField, Range(0f, 1f)] public float MaxProportion { get; protected set; } = 1f;

    [field: SerializeField] public bool bHasDepthLimits { get; protected set; } = false;
    [field: SerializeField, Range(0f, 1f)] public float MinDepth { get; protected set; } = 0f;
    [field: SerializeField, Range(0f, 1f)] public float MaxDepth { get; protected set; } = 1f;

    [field: SerializeField] public List<BlockTemplate> DefaultTemplates { get; protected set; }

    [field: SerializeField] public SerializableDictionary<BiomeConfig, BlockTemplate> BiomeOverrides { get; protected set; } = new();

    public bool IsBlockSuitable(EPermittedZones InZone, float InZoneProportion, float InDepth)
    {
        if ((Zones & InZone) != InZone)
            return false;

        if (bHasProportionLimits && ((InZoneProportion < MinProportion) || (InZoneProportion > MaxProportion)))
            return false;
        if (bHasDepthLimits && ((InDepth < MinDepth) || (InDepth > MaxDepth)))
            return false;

        return true;
    }

    public BlockVisualAsset SelectVisualAsset()
    {
        if (DefaultTemplates.Count == 0)
            return null;

        return DefaultTemplates[Random.Range(0, DefaultTemplates.Count)].BlockVisualAsset;
    }

    public GameObject SelectPrefab()
    {
        if (DefaultTemplates.Count == 0)
            return null;

        return DefaultTemplates[Random.Range(0, DefaultTemplates.Count)].BlockPrefab;
    }
}
