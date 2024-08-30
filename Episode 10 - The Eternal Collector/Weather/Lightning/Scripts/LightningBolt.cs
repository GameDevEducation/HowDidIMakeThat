using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum EBranchDirection
{
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest,

    NumValues
}

public enum ELightningCellType
{
    Trunk,
    Branch
}

public class LightningCell
{
    public ELightningCellType Type;
    public Vector3 Position;
    public float CellSize;
    public int LifeRemaining;
    public EBranchDirection BranchDirection;
}

public class LightningSlice
{
    public List<LightningCell> Cells = new List<LightningCell>();

    public void AddCell(LightningCell cell)
    {
        Cells.Add(cell);
    }

    Vector3 DirectionToVector(EBranchDirection direction)
    {
        switch (direction)
        {
            case EBranchDirection.North: return Vector3.forward;
            case EBranchDirection.NorthEast: return Vector3.forward + Vector3.right;
            case EBranchDirection.East: return Vector3.right;
            case EBranchDirection.SouthEast: return -Vector3.forward + Vector3.right;
            case EBranchDirection.South: return -Vector3.forward;
            case EBranchDirection.SouthWest: return -Vector3.forward - Vector3.right;
            case EBranchDirection.West: return -Vector3.right;
            case EBranchDirection.NorthWest: return Vector3.forward - Vector3.right;
        }

        return Vector3.zero;
    }

    EBranchDirection GetRandomDirection()
    {
        return (EBranchDirection)Random.Range(0, (int)EBranchDirection.NumValues);
    }

    EBranchDirection GetRandomAdjacentDirection(EBranchDirection startDirection)
    {
        int newDirection = (int)startDirection + Random.Range(-1, 2);
        newDirection = (newDirection + (int)EBranchDirection.NumValues) % (int)EBranchDirection.NumValues;

        return (EBranchDirection)newDirection;
    }

    public bool GrowSlice(float trunkProgress, bool allowBranch, out LightningSlice nextSlice,
                          Vector3 boltStart, Vector3 boltEnd, LightningConfig config)
    {
        nextSlice = null;

        foreach (var cell in Cells)
        {
            // determine the size
            float size = Mathf.Lerp(config.StartCellSize, config.EndCellSize, trunkProgress);
            float offset = (cell.CellSize + size) * 0.4f;

            // is this a trunk cell?
            if (cell.Type == ELightningCellType.Trunk)
            {
                // calculate the next cell position (must be down)
                Vector3 nextCellPosition = cell.Position;
                nextCellPosition += offset * Random.Range(-1, 2) * Vector3.forward;
                nextCellPosition += offset * Random.Range(-1, 2) * Vector3.right;
                nextCellPosition += offset * Vector3.down;

                // at the end of this trunk?
                if (nextCellPosition.y < boltEnd.y)
                    continue;

                // setup a new slice
                if (nextSlice == null)
                    nextSlice = new LightningSlice();

                // add the cell
                nextSlice.AddCell(new LightningCell()
                {
                    Type = ELightningCellType.Trunk,
                    Position = nextCellPosition,
                    CellSize = size
                });

                // are we splitting?
                if (allowBranch)
                {
                    // pick a random direction
                    var direction = GetRandomDirection();
                    var offsetVector = DirectionToVector(direction);

                    // must be horizontal
                    nextCellPosition = cell.Position + offset * offsetVector;

                    // calculate the branch length
                    int branchLength = Random.Range(config.MinBranchLength, config.MaxBranchLength);

                    branchLength = Mathf.RoundToInt(branchLength * config.BranchScaleWithProgress.Evaluate(trunkProgress));
                    if (branchLength < config.BranchCullLength)
                        continue;

                    nextSlice.AddCell(new LightningCell()
                    {
                        Type = ELightningCellType.Branch,
                        Position = nextCellPosition,
                        CellSize = size,
                        LifeRemaining = branchLength,
                        BranchDirection = direction
                    });
                }
            }
            else
            {
                // is the branch out of life?
                if (cell.LifeRemaining == 1)
                    continue;

                // setup a new slice
                if (nextSlice == null)
                    nextSlice = new LightningSlice();

                // pick a random direction
                bool changeDirection = Random.Range(0f, 1f) < config.BranchDeviationChance;
                var direction = changeDirection ? GetRandomAdjacentDirection(cell.BranchDirection) : cell.BranchDirection;
                var offsetVector = DirectionToVector(direction);

                // can be horizontal or vertical
                Vector3 nextCellPosition = cell.Position + offset * offsetVector;

                if (Random.Range(0f, 1f) < config.BranchVerticalChance)
                    nextCellPosition += Vector3.up * offset * (Random.Range(0, 2) == 0 ? -1f : 1f);

                // add the cell
                nextSlice.AddCell(new LightningCell()
                {
                    Type = ELightningCellType.Branch,
                    Position = nextCellPosition,
                    CellSize = size,
                    LifeRemaining = cell.LifeRemaining - 1,
                    BranchDirection = direction
                });
            }
        }

        return nextSlice != null;
    }
}

public class LightningBolt : MonoBehaviour
{
    [SerializeField] GameObject LightningCellPrefab;
    List<LightningElement> LightningElements;

    UnityAction OnHitAction;
    float FlashProgress = 0f;
    bool PerformingFlash = false;

    public void Build(Vector3 start, Vector3 end, LightningConfig config, UnityAction onHitAction)
    {
        List<LightningSlice> slices = new List<LightningSlice>();
        OnHitAction = onHitAction;

        // Add the initial slice
        var startingSlice = new LightningSlice();
        startingSlice.AddCell(new LightningCell()
        {
            Type = ELightningCellType.Trunk,
            Position = start,
            CellSize = config.StartCellSize
        });
        slices.Add(startingSlice);

        int maxTrunkSlices = Mathf.RoundToInt(Mathf.Abs(start.y - end.y) / config.EndCellSize);
        int slicesUntilNextBranch = Random.Range(config.MinBranchInterval, config.MaxBranchInterval);

        // generate the lightning bolt
        bool boltUpdated = true;
        bool allowBranch = false;
        int numElements = 1;
        while (boltUpdated)
        {
            boltUpdated = false;

            // should a branch happen?
            allowBranch = slicesUntilNextBranch == 0;
            if (slicesUntilNextBranch == 0)
                slicesUntilNextBranch = Random.Range(config.MinBranchInterval, config.MaxBranchInterval);

            // attempt to grow the current slice
            LightningSlice newSlice = null;
            float trunkProgress = (float)slices.Count / maxTrunkSlices;
            boltUpdated = slices[^1].GrowSlice(trunkProgress, allowBranch, out newSlice, start, end, config);

            // new slice created?
            if (newSlice != null)
            {
                slices.Add(newSlice);
                numElements += newSlice.Cells.Count;
            }

            --slicesUntilNextBranch;
        }

        // find the last trunk point
        Vector3 actualEnd = Vector3.zero;
        for (int index = slices.Count - 1; index >= 0; index--)
        {
            var slice = slices[index];
            bool endFound = false;
            foreach (var cell in slice.Cells)
            {
                if (cell.Type == ELightningCellType.Trunk)
                {
                    actualEnd = cell.Position;
                    endFound = true;
                    break;
                }
            }

            if (endFound)
                break;
        }

        // spawn the elements
        LightningElements = new List<LightningElement>(numElements);
        for (var sliceIndex = 0; sliceIndex < slices.Count; sliceIndex++)
        {
            var slice = slices[sliceIndex];
            float startTime = Mathf.Lerp(0f, config.LightningFlashTime, (float)sliceIndex / (slices.Count - 1));

            foreach (var cell in slice.Cells)
            {
                var cubeGO = GameObject.Instantiate(LightningCellPrefab, transform);
                cubeGO.transform.localPosition = cell.Position - actualEnd;
                cubeGO.transform.localScale = Vector3.one * cell.CellSize;

                var element = cubeGO.GetComponent<LightningElement>();
                element.SetTimes(startTime, config.LightningPersistenceTime);
                LightningElements.Add(element);
            }
        }

        PerformingFlash = true;
    }

    private void Update()
    {
        if (PerformingFlash)
        {
            // synchronise the elements
            bool anyVisible = false;
            foreach(var element in LightningElements)
                anyVisible |= element.SyncToTime(FlashProgress);

            FlashProgress += Time.deltaTime;

            // destroy once the flash is done
            if (!anyVisible)
            {
                if (OnHitAction != null)
                    OnHitAction.Invoke();

                PerformingFlash = false;
                Destroy(gameObject);
            }
        }
    }
}
