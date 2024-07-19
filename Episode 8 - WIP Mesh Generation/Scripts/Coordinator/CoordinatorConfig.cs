using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CoordinatorConfig", menuName = "Duality/Coordinator Config")]
public class CoordinatorConfig : ScriptableObject
{
    [Header("World Generation")]
    [field: SerializeField] public int CeilingMinHeight { get; protected set; } = 2;
    [field: SerializeField] public int CeilingMaxHeight { get; protected set; } = 10;
    [field: SerializeField] public int FloorMinHeight { get; protected set; } = 2;
    [field: SerializeField] public int FloorMaxHeight { get; protected set; } = 10;
    [field: SerializeField] public int ColumnsPerSpan { get; protected set; } = 10;
    [field: SerializeField] public int CellsPerColumn { get; protected set; } = 60;
    [field: SerializeField] public int SpansAhead { get; protected set; } = 10;
    [field: SerializeField] public int SpansBehind { get; protected set; } = 10;
    [field: SerializeField] public int LowestCellDepth { get; protected set; } = 5;

    [field: SerializeField] public float NoiseStepPerColumn { get; protected set; } = 1f / 200f;
    [field: SerializeField] public float ProgressionScale { get; protected set; } = 1f;

    [field: SerializeField] public List<BlockDefinition> Blocks { get; protected set; } = new();
    [field: SerializeField] public List<BiomeConfig> Biomes { get; protected set; } = new();
    [field: SerializeField] public Material BlockMaterial { get; protected set; }

    [Header("Movement")]
    [field: SerializeField] public float BaseMovementSpeed { get; protected set; } = 10.0f;
    [field: SerializeField] public int MaximumSpanOffsetBeforeSpawning { get; protected set; } = 1;

    protected int FloorStart => 0;
    protected int FloorEnd => FloorMaxHeight - 1;
    protected int CeilingStart => CellsPerColumn - CeilingMaxHeight;
    protected int CeilingEnd => CellsPerColumn - 1;

    public BlockVisualAsset SelectVisualAsset(float InProgressionFactor, int InCellY, int InCellDepth)
    {
        var SelectedBlock = SelectBlockDefinition(InProgressionFactor, InCellY, InCellDepth);

        return SelectedBlock.SelectVisualAsset();
    }

    public GameObject SelectPrefab(float InProgressionFactor, int InCellY, int InCellDepth)
    {
        var SelectedBlock = SelectBlockDefinition(InProgressionFactor, InCellY, InCellDepth);

        return SelectedBlock.SelectPrefab();
    }

    BlockDefinition SelectBlockDefinition(float InProgressionFactor, int InCellY, int InCellDepth)
    {
        List<BlockDefinition> CandidateBlocks = new List<BlockDefinition>(Blocks.Count);

        float DepthFactor = (float)InCellDepth / (float)LowestCellDepth;
        EPermittedZones Zone;
        float ZoneProportion;

        if (InCellY <= FloorEnd)
        {
            Zone = EPermittedZones.Floor;
            ZoneProportion = 1f - Mathf.Clamp01((float)InCellY / (float)FloorEnd);
        }
        else if (InCellY >= CeilingStart)
        {
            Zone = EPermittedZones.Ceiling;
            ZoneProportion = Mathf.Clamp01((float)(InCellY - CeilingStart) / (float)CeilingMaxHeight);
        }
        else
        {
            Zone = EPermittedZones.Middle;
            ZoneProportion = 1f - Mathf.Clamp01(((float)(InCellY - FloorEnd) / (float)(CeilingStart - FloorEnd)) * 2f - 1f);
        }

        foreach (var Block in Blocks)
        {
            if (Block.IsBlockSuitable(Zone, ZoneProportion, DepthFactor))
            {
                CandidateBlocks.Add(Block);
            }
        }

        if (CandidateBlocks.Count == 0)
            return null;

        return CandidateBlocks[Random.Range(0, CandidateBlocks.Count)];
    }
}
