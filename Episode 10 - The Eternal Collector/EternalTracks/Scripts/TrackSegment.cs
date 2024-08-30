using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vector2IntExtensions;

public enum TrackType
{
    Straight,
    RightTurn,
    LeftTurn
}

public enum Direction
{
    North,
    East,
    South,
    West
}

public static class Offsets
{
    public static Vector2Int North = Vector2Int.up;
    public static Vector2Int East = Vector2Int.right;
    public static Vector2Int South = Vector2Int.down;
    public static Vector2Int West = Vector2Int.left;
}

public class SegmentConfig
{
    public Vector2Int Start;
    public Vector2Int End;
    public Direction Entry;
    public Direction Exit;
}

public class TrackTileConfig
{
    public TrackType Type;
    public Vector2Int Location;
    public Direction Entry;
    public Direction Exit;
}

class TrackSegment
{
    public List<TrackTileConfig> TileConfigs = new List<TrackTileConfig>();

    static Dictionary<Direction, Direction> LeftTurns  = null;
    static Dictionary<Direction, Direction> RightTurns = null;
    static Dictionary<Direction, Direction> Opposites  = null;
    static Dictionary<Direction, Vector2Int> Steps     = null;

    Direction EntryDirection;
    Direction ExitDirection;
    TrackType CurrentTrackType;
    Vector2Int CurrentPosition;

    void SetupDictionaries()
    {
        if (LeftTurns == null)
        {
            LeftTurns                   = new Dictionary<Direction, Direction>();
            RightTurns                  = new Dictionary<Direction, Direction>();
            Opposites                   = new Dictionary<Direction, Direction>();
            Steps                       = new Dictionary<Direction, Vector2Int>();

            LeftTurns[Direction.North]  = Direction.West;
            LeftTurns[Direction.East]   = Direction.North;
            LeftTurns[Direction.South]  = Direction.East;
            LeftTurns[Direction.West]   = Direction.South;

            RightTurns[Direction.North] = Direction.East;
            RightTurns[Direction.East]  = Direction.South;
            RightTurns[Direction.South] = Direction.West;
            RightTurns[Direction.West]  = Direction.North;

            Opposites[Direction.North]  = Direction.South;
            Opposites[Direction.East]   = Direction.West;
            Opposites[Direction.South]  = Direction.North;
            Opposites[Direction.West]   = Direction.East;

            Steps[Direction.North]      = Offsets.North;
            Steps[Direction.East]       = Offsets.East;
            Steps[Direction.South]      = Offsets.South;
            Steps[Direction.West]       = Offsets.West;
        }
    }

    public TrackSegment(SegmentConfig config)
    {
        SetupDictionaries();

        TileConfigs.Add(new TrackTileConfig()
        {
            Type = TrackType.RightTurn,
            Location = config.Start,
            Entry = config.Entry,
            Exit = config.Exit
        });

        EntryDirection          = Opposites[config.Exit];
        ExitDirection           = config.Exit;
        CurrentPosition         = config.Start + Steps[ExitDirection];
        Vector2Int segmentAxis  = (config.End - config.Start).Normalise();

        while (CurrentPosition != config.End)
        {
            // determine the distance from the start and the end
            int distToStart = Mathf.Abs((CurrentPosition - config.Start).Dot(segmentAxis));
            int distToEnd   = Mathf.Abs((CurrentPosition - config.End).Dot(segmentAxis)) - 1;

            // need to be at least 2 tiles from the end and 2 from the start to perform a deviation
            if (distToStart >= 2 && distToEnd >= 2) 
            {
                // perform a deviation?
                if (Random.Range(0f, 1f) >= 0.5f)
                {
                    // determine the deviation length
                    int deviationLength = Mathf.Min(distToStart, distToEnd);

                    // determine the deviation type
                    bool isLeft = Random.Range(0, 1f) >= 0.5f;

                    // add the turn off
                    AddSegment(isLeft ? TrackType.LeftTurn : TrackType.RightTurn);

                    // add the deviation entry
                    for (int index = 0; index < deviationLength; ++index)
                        AddSegment(TrackType.Straight);

                    // add the u-turn
                    AddSegment(isLeft ? TrackType.RightTurn : TrackType.LeftTurn);
                    AddSegment(isLeft ? TrackType.RightTurn : TrackType.LeftTurn);

                    // add the deviation exit
                    for (int index = 0; index < deviationLength; ++index)
                        AddSegment(TrackType.Straight);

                    // add the turn back
                    AddSegment(isLeft ? TrackType.LeftTurn : TrackType.RightTurn);

                    continue;
                }
            }

            AddSegment(TrackType.Straight);
        }
    }

    void AddSegment(TrackType trackType)
    {
        // determine the exit direction
        if (trackType == TrackType.Straight)
            ExitDirection = Opposites[EntryDirection];
        else if (trackType == TrackType.LeftTurn)
            ExitDirection = LeftTurns[EntryDirection];
        else if (trackType == TrackType.RightTurn)
            ExitDirection = RightTurns[EntryDirection];

        TileConfigs.Add(new TrackTileConfig()
        {
            Type = trackType,
            Location = CurrentPosition,
            Entry = EntryDirection,
            Exit = ExitDirection
        });

        // update the position
        CurrentPosition += Steps[ExitDirection];
        EntryDirection = Opposites[ExitDirection];
    }
}