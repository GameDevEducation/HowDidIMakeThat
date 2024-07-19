using CommonCore;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class Coordinator : MonoBehaviourSingleton<Coordinator>
{
    enum EState
    {
        Uninitialised,
        PreparingToBuildWorld,
        BuildingWorld,
        Running
    }

    enum EPurpose
    {
        StartingX,
        CeilingHeight,
        FloorHeight,
        Intensity,
        ProgressionRate
    }

    internal class RNGWrapper
    {
        System.Random RNG;

        internal RNGWrapper(int InSeed)
        {
            RNG = new System.Random(InSeed);
        }

        internal float Range(float InMinimum, float InMaximum)
        {
            return (float)(RNG.NextDouble() * (InMaximum - InMinimum)) + InMinimum;
        }

        internal int Range(int InMinimum, int InMaximum)
        {
            return RNG.Next(InMinimum, InMaximum);
        }
    }

    #region Data Structures
    internal class Span
    {
        internal int StartX { get; private set; }
        internal int EndX { get; private set; }
        internal List<SpanColumn> Columns { get; private set; } = null;

        internal float IntensityFactor { get; private set; }
        internal float ProgressionFactor_Start { get; private set; }
        internal float ProgressionFactor_End { get; private set; }

        internal GameObject SpanGO { get; private set; }

        internal Span(GameObject InSpanGO, int InStartX, int InEndEx, int InNumSpans)
        {
            SpanGO = InSpanGO;
            StartX = InStartX;
            EndX = InEndEx;

            Columns = new List<SpanColumn>(InNumSpans);
        }

        internal void AddColumn(SpanColumn InColumn)
        {
            Columns.Add(InColumn);
        }

        internal void CloseSpan()
        {
            IntensityFactor = ProgressionFactor_Start = ProgressionFactor_End = 0;

            ProgressionFactor_Start = Columns[0].ProgressionFactor;

            foreach (var Column in Columns)
            {
                IntensityFactor += Column.IntensityFactor;
            }

            IntensityFactor /= Columns.Count;

            ProgressionFactor_End = Columns[^1].ProgressionFactor;
        }

        internal void Flip()
        {
            Columns.Reverse();
        }

        internal void Despawn()
        {
            if (Columns == null)
                return;

            foreach (var Column in Columns)
                Column.Despawn();

            Destroy(SpanGO);
            SpanGO = null;

            Columns = null;
        }
    }

    internal class SpanColumn
    {
        internal List<GeneratedCell> Cells { get; private set; } = null;
        internal float IntensityFactor { get; private set; }
        internal float ProgressionFactor { get; private set; }
        internal GameObject ColumnGO { get; private set; }

        internal SpanColumn(GameObject InColumnGO, int InNumCells, float InIntensityFactor, float InProgressionFactor)
        {
            ColumnGO = InColumnGO;
            Cells = new List<GeneratedCell>(InNumCells);
            IntensityFactor = InIntensityFactor;
            ProgressionFactor = InProgressionFactor;
        }

        internal void AddCell(GeneratedCell InCell)
        {
            Cells.Add(InCell);
        }

        internal void Despawn()
        {
            if (Cells == null)
                return;

            foreach (var Cell in Cells)
                Cell.Despawn();

            Destroy(ColumnGO);
            ColumnGO = null;

            Cells = null;
        }
    }

    internal class GeneratedCell
    {
        internal Vector3Int Position { get; private set; }
        internal EFaceFlags FaceFlags { get; set; }
        internal bool IsSpawned => CellGO != null;
        GameObject CellGO = null;

        internal GeneratedCell(int InX, int InY, int InDepth)
        {
            Position = new Vector3Int(InX, InY, InDepth);
        }

        internal void BindToGameObject(GameObject InCellGO)
        {
            CellGO = InCellGO;
            CellGO.name = $"Cell_{Position.x}_{Position.y}";
        }

        internal void Despawn()
        {
            Destroy(CellGO);
        }
    }
    #endregion

    [SerializeField] CoordinatorConfig Config;
    [SerializeField] Transform LevelRoot;

    [SerializeField] bool EnableOptimisedVisualAssets = false;

    [SerializeField] bool DEBUG_StartSpawning = false;

    [SerializeField] bool DEBUG_OverrideMovement = false;
    [SerializeField, Range(-1f, 1f)] float DEBUG_MovementSpeed = 0.0f;

    EState CurrentState = EState.Uninitialised;
    string RNGSeedString = null;
    Dictionary<EPurpose, float> NoiseOffsets = new();

    List<Span> ActiveSpans = new();
    int CurrentSpanIndex = 0;

    float CurrentX = 0;

    float RequestedSpeed = 0.0f;
    float WorkingSpeed => DEBUG_OverrideMovement ? DEBUG_MovementSpeed : RequestedSpeed;

    bool bHasCurrentSpan => (ActiveSpans.Count > 0) && (CurrentSpanIndex >= 0) && (CurrentSpanIndex < ActiveSpans.Count);
    Span CurrentSpan => bHasCurrentSpan ? ActiveSpans[CurrentSpanIndex] : null;

    int NumPendingSpawn = 0;

    protected override void OnAwake()
    {
        base.OnAwake();
    }

    private void Update()
    {
        float DeltaTime = Time.deltaTime;

        if (DEBUG_StartSpawning && (CurrentState == EState.Uninitialised))
        {
            DEBUG_StartSpawning = false;
            OnBeginWorldBuilding();
        }

        if (CurrentState == EState.PreparingToBuildWorld)
            Tick_PreparingToBuildWorld(DeltaTime);
        else if (CurrentState == EState.BuildingWorld)
            Tick_BuildingWorld(DeltaTime);
        else if (CurrentState == EState.Running)
            Tick_Running(DeltaTime);

        if (NumPendingSpawn > 0)
            Tick_PendingSpawns();
    }

    #region World Generation
    public void OnBeginWorldBuilding(string InRNGSeedString = null)
    {
        RNGSeedString = InRNGSeedString;
        CurrentState = EState.PreparingToBuildWorld;
    }

    void Tick_PreparingToBuildWorld(float InDeltaTime)
    {
        int RNGSeed = 0;
        bool bSeedSet = false;

        if (RNGSeedString != null)
        {
            bSeedSet = int.TryParse(RNGSeedString, System.Globalization.NumberStyles.HexNumber, CultureInfo.CurrentCulture, out RNGSeed);
        }

        if (!bSeedSet)
            RNGSeed = (int)System.DateTime.Now.Ticks;

        Random.InitState(RNGSeed);
        RNGSeedString = RNGSeed.ToString("X8");

        // pick our starting points in the noise map
        NoiseOffsets[EPurpose.StartingX] = Random.Range(0f, 1f);
        NoiseOffsets[EPurpose.CeilingHeight] = Random.Range(0.00f, 0.25f);
        NoiseOffsets[EPurpose.FloorHeight] = Random.Range(0.25f, 0.50f);
        NoiseOffsets[EPurpose.Intensity] = Random.Range(0.50f, 0.75f);
        NoiseOffsets[EPurpose.ProgressionRate] = Random.Range(0.75f, 1.00f);

        CurrentState = EState.BuildingWorld;
    }

    void Tick_BuildingWorld(float DeltaTime)
    {
        float WorkingProgressionFactor = 0f;

        // Generate the initial set of spans
        for (int SpanIndex = -Config.SpansBehind; SpanIndex <= Config.SpansAhead; SpanIndex++)
        {
            Span NewSpan = GenerateSpanForwards(SpanIndex * Config.ColumnsPerSpan, WorkingProgressionFactor);

            WorkingProgressionFactor = NewSpan.ProgressionFactor_End;
            ActiveSpans.Add(NewSpan);
        }

        CurrentSpanIndex = Config.SpansBehind;
        CurrentX = Config.ColumnsPerSpan / 2;

        CurrentState = EState.Running;
    }

    Span GenerateSpanBackwards(int InStartingX, float InStartingProgressionFactor)
    {
        int WorkingStartingX = InStartingX - Config.ColumnsPerSpan + 1;
        int WorkingEndingX = InStartingX;

        GameObject SpanGO = new GameObject($"Span_{WorkingStartingX}");
        SpanGO.transform.position = CellToWorld(new Vector3Int(WorkingStartingX + Config.ColumnsPerSpan / 2, 0));
        SpanGO.transform.SetParent(LevelRoot, true);

        Span NewSpan = new Span(SpanGO, WorkingStartingX, WorkingEndingX, Config.ColumnsPerSpan);

        for (int ColumnX = 0; ColumnX < Config.ColumnsPerSpan; ++ColumnX)
        {
            NewSpan.AddColumn(GenerateColumn(InStartingX - ColumnX, InStartingProgressionFactor, false, SpanGO.transform));
        }

        NewSpan.Flip();
        NewSpan.CloseSpan();

        return NewSpan;
    }

    Span GenerateSpanForwards(int InStartingX, float InStartingProgressionFactor)
    {
        GameObject SpanGO = new GameObject($"Span_{InStartingX}");
        SpanGO.transform.position = CellToWorld(new Vector3Int(InStartingX + Config.ColumnsPerSpan / 2, 0));
        SpanGO.transform.SetParent(LevelRoot, true);

        Span NewSpan = new Span(SpanGO, InStartingX, InStartingX + Config.ColumnsPerSpan - 1, Config.ColumnsPerSpan);

        for (int ColumnX = 0; ColumnX < Config.ColumnsPerSpan; ++ColumnX)
        {
            NewSpan.AddColumn(GenerateColumn(InStartingX + ColumnX, InStartingProgressionFactor, true, SpanGO.transform));
        }

        NewSpan.CloseSpan();

        return NewSpan;
    }

    SpanColumn GenerateColumn(int InColumnX, float InPreviousProgressionFactor, bool bInIsMovingForwards, Transform InSpanTransform)
    {
        float ColumnNoiseX = (InColumnX * Config.NoiseStepPerColumn) + NoiseOffsets[EPurpose.StartingX];

        float CeilingHeightFactor = Mathf.PerlinNoise(ColumnNoiseX, NoiseOffsets[EPurpose.CeilingHeight]);
        float FloorHeightFactor = Mathf.PerlinNoise(ColumnNoiseX, NoiseOffsets[EPurpose.FloorHeight]);

        float IntensityFactor = Mathf.PerlinNoise(ColumnNoiseX, NoiseOffsets[EPurpose.Intensity]);
        float ProgressionFactorDelta = Mathf.PerlinNoise(ColumnNoiseX, NoiseOffsets[EPurpose.ProgressionRate]) * Config.ProgressionScale;

        int CeilingHeight = Mathf.FloorToInt(Mathf.Lerp(Config.CeilingMinHeight, Config.CeilingMaxHeight, CeilingHeightFactor));
        int FloorHeight = Mathf.FloorToInt(Mathf.Lerp(Config.FloorMinHeight, Config.FloorMaxHeight, FloorHeightFactor));

        float ProgressionFactor = InPreviousProgressionFactor + (bInIsMovingForwards ? ProgressionFactorDelta : -ProgressionFactorDelta);

        GameObject ColumnGO = new GameObject($"Column_{InColumnX}");
        ColumnGO.transform.position = CellToWorld(new Vector3Int(InColumnX, 0));
        ColumnGO.transform.SetParent(InSpanTransform, true);

        SpanColumn NewColumn = new SpanColumn(ColumnGO, Config.CellsPerColumn, IntensityFactor, ProgressionFactor);

        for (int CellY = 0; CellY < Config.CellsPerColumn; ++CellY)
        {
            int CellDepth = Config.LowestCellDepth;

            // floor region?
            if (CellY < Config.FloorMaxHeight)
            {
                int DistFromBottom = CellY;

                if (DistFromBottom < FloorHeight)
                {
                    float InterpFactor = (float)DistFromBottom / (float)FloorHeight;
                    CellDepth = Mathf.FloorToInt(Mathf.Lerp(0, Config.LowestCellDepth, InterpFactor));
                }
            } // ceiling region?
            else if (CellY > (Config.CellsPerColumn - Config.CeilingMaxHeight - 1))
            {
                int DistFromTop = Config.CellsPerColumn - CellY - 1;
                if (DistFromTop < CeilingHeight)
                {
                    float InterpFactor = (float)DistFromTop / (float)CeilingHeight;
                    CellDepth = Mathf.FloorToInt(Mathf.Lerp(0, Config.LowestCellDepth, InterpFactor));
                }
            }

            GeneratedCell NewCell = new GeneratedCell(InColumnX, CellY, CellDepth);

            ++NumPendingSpawn;

            NewColumn.AddCell(NewCell);
        }

        return NewColumn;
    }
    #endregion

    #region Runtime
    void Tick_Running(float InDeltaTime)
    {
        // update our location and quantised positions
        int OldQuantisedX = Mathf.RoundToInt(CurrentX);

        float MovementDelta = InDeltaTime * WorkingSpeed * Config.BaseMovementSpeed;
        CurrentX += MovementDelta;

        int NewQuantisedX = Mathf.RoundToInt(CurrentX);

        // are we in a different span?
        if ((NewQuantisedX < 0) || (NewQuantisedX >= Config.ColumnsPerSpan))
        {
            float RawDelta = (float)NewQuantisedX / (float)Config.ColumnsPerSpan;
            int SpanDelta = (RawDelta > 0) ? Mathf.FloorToInt(RawDelta) : Mathf.CeilToInt(RawDelta);
            int SpanAbsDelta = Mathf.Abs(SpanDelta);

            if (SpanAbsDelta >= Config.MaximumSpanOffsetBeforeSpawning)
            {
                // moving forwards/right
                if (SpanDelta > 0)
                {
                    for (int Index = 0; Index < SpanAbsDelta; ++Index)
                    {
                        // create a new span on the right
                        Span NewSpan = GenerateSpanForwards(ActiveSpans[^1].EndX + 1, ActiveSpans[^1].ProgressionFactor_End);
                        ActiveSpans.Add(NewSpan);

                        // remove the leftmost span
                        ActiveSpans[0].Despawn();
                        ActiveSpans.RemoveAt(0);
                    }
                } // moving backwards/left
                else
                {
                    for (int Index = 0; Index < SpanAbsDelta; ++Index)
                    {
                        // create a new span on the left
                        Span NewSpan = GenerateSpanBackwards(ActiveSpans[0].StartX - 1, ActiveSpans[0].ProgressionFactor_Start);
                        ActiveSpans.Insert(0, NewSpan);

                        // remove the leftmost span
                        ActiveSpans[^1].Despawn();
                        ActiveSpans.RemoveAt(ActiveSpans.Count - 1);
                    }
                }

                // update the coordinates
                CurrentX -= Config.ColumnsPerSpan * SpanDelta;
            }
        }

        // move the world
        LevelRoot.Translate(-Vector3.right * MovementDelta);
    }
    #endregion

    #region Block Spawning
    void Tick_PendingSpawns()
    {
        ReclassifyBlockFaces();

        for (int SpanIndex = 0; SpanIndex < ActiveSpans.Count; ++SpanIndex)
        {
            var Span = ActiveSpans[SpanIndex];

            for (int ColumnIndex = 0; ColumnIndex < Span.Columns.Count; ++ColumnIndex)
            {
                var Column = Span.Columns[ColumnIndex];

                for (int CellIndex = 0; CellIndex < Column.Cells.Count; ++CellIndex)
                {
                    var Cell = Column.Cells[CellIndex];

                    if (Cell.IsSpawned)
                        continue;

                    Vector3 CellWorldPosition = CellToWorld(Cell.Position);
                    Transform CellParent = Column.ColumnGO.transform;
                    GameObject CellGO = null;

                    if (EnableOptimisedVisualAssets)
                    {
                        BlockVisualAsset VisualAsset = Config.SelectVisualAsset(Column.ProgressionFactor, Cell.Position.y, Cell.Position.z);

                        Material CellMaterial = FindOrRegisterMaterial(VisualAsset);

                        CellGO = VisualAsset.SpawnGameObject(Cell.FaceFlags, CellWorldPosition, Quaternion.identity, CellParent, CellMaterial);
                    }
                    else
                    {
                        GameObject Prefab = Config.SelectPrefab(Column.ProgressionFactor, Cell.Position.y, Cell.Position.z);
                        CellGO = GameObject.Instantiate(Prefab, CellWorldPosition, Quaternion.identity, CellParent);
                    }

                    Cell.BindToGameObject(CellGO);
                }
            }
        }

        NumPendingSpawn = 0;
    }

    Dictionary<BlockVisualAsset, Material> MaterialRegistry = new ();
    Material FindOrRegisterMaterial(BlockVisualAsset InAsset)
    {
        Material FoundMaterial = null;
        if (!MaterialRegistry.TryGetValue(InAsset, out FoundMaterial))
        {
            FoundMaterial = InAsset.GetMaterialInstanceForAtlas(Config.BlockMaterial);
            MaterialRegistry[InAsset] = FoundMaterial;
        }

        return FoundMaterial;
    }

    void ReclassifyBlockFaces()
    {
        for (int SpanIndex = 0; SpanIndex < ActiveSpans.Count; ++SpanIndex)
        {
            var Span = ActiveSpans[SpanIndex];

            for (int ColumnIndex = 0; ColumnIndex < Span.Columns.Count; ++ColumnIndex)
            {
                var Column = Span.Columns[ColumnIndex];

                for (int CellIndex = 0; CellIndex < Column.Cells.Count; ++CellIndex)
                {
                    var Cell = Column.Cells[CellIndex];

                    DetermineCellFaces(Cell, SpanIndex, ColumnIndex, CellIndex);
                }
            }
        }
    }

    GeneratedCell GetCell(int InSpanIndex, int InColumnIndex, int InCellIndex)
    {
        int WorkingSpanIndex = InSpanIndex;
        int WorkingColumnIndex = InColumnIndex;

        if (InColumnIndex < 0)
        {
            WorkingSpanIndex -= 1;
            WorkingColumnIndex = Config.ColumnsPerSpan + InColumnIndex;
        }
        if (InColumnIndex >= Config.ColumnsPerSpan)
        {
            WorkingSpanIndex += 1;
            WorkingColumnIndex = InColumnIndex - Config.ColumnsPerSpan;
        }

        if ((WorkingSpanIndex < 0) || (WorkingSpanIndex >= ActiveSpans.Count)) 
            return null;

        if ((WorkingColumnIndex < 0) || (WorkingColumnIndex >= Config.ColumnsPerSpan))
            return null;

        if ((InCellIndex < 0) || (InCellIndex >= Config.CellsPerColumn))
            return null;

        return ActiveSpans[WorkingSpanIndex].Columns[WorkingColumnIndex].Cells[InCellIndex];
    }

    void DetermineCellFaces(GeneratedCell InCell, int InSpanIndex, int InColumnIndex, int InCellIndex)
    {
        // all cells have a top
        EFaceFlags NewFlags = EFaceFlags.Top;

        GeneratedCell NorthCell = GetCell(InSpanIndex, InColumnIndex,     InCellIndex + 1);
        GeneratedCell EastCell  = GetCell(InSpanIndex, InColumnIndex + 1, InCellIndex);
        GeneratedCell SouthCell = GetCell(InSpanIndex, InColumnIndex,     InCellIndex - 1);
        GeneratedCell WestCell  = GetCell(InSpanIndex, InColumnIndex - 1, InCellIndex);

        if ((SouthCell != null) && (SouthCell.Position.z > InCell.Position.z))
            NewFlags |= EFaceFlags.Back;
        if ((NorthCell != null) && (NorthCell.Position.z > InCell.Position.z))
            NewFlags |= EFaceFlags.Front;
        if ((EastCell != null) && (EastCell.Position.z > InCell.Position.z))
            NewFlags |= EFaceFlags.Right;
        if ((WestCell != null) && (WestCell.Position.z > InCell.Position.z))
            NewFlags |= EFaceFlags.Left;

        if (InCell.IsSpawned && (NewFlags != InCell.FaceFlags))
            InCell.Despawn();

        InCell.FaceFlags = NewFlags;
    }
    #endregion

    #region Helpers
    Vector3 CellToWorld(Vector3Int InCellCoordinate)
    {
        return LevelRoot.position + new Vector3(InCellCoordinate.x, -InCellCoordinate.z, -((Config.CellsPerColumn / 2) - InCellCoordinate.y));
    }
    #endregion
}
