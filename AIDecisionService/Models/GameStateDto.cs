namespace AIDecisionService.Models;

/// <summary>
/// Rectangular bounds
/// </summary>
public class BoundsDataDto
{
    public float X { get; set; }
    public float Y { get; set; }
    public float W { get; set; }
    public float H { get; set; }
}

/// <summary>
/// Spike entity with bounds and direction
/// </summary>
public class SpikeDataDto
{
    public BoundsDataDto Bounds { get; set; } = new();
    public int Direction { get; set; }
}

/// <summary>
/// Data Transfer Object for game state from Celeste
/// </summary>
public class GameStateDto
{
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }
    public float PlayerRemX { get; set; }
    public float PlayerRemY { get; set; }
    public float PlayerSpeedX { get; set; }
    public float PlayerSpeedY { get; set; }
    public bool OnGround { get; set; }
    public bool IsHolding { get; set; }
    public float JumpTimer { get; set; }
    public bool AutoJump { get; set; }
    public float LevelBoundsX { get; set; }
    public float LevelBoundsY { get; set; }
    public float LevelBoundsW { get; set; }
    public float LevelBoundsH { get; set; }
    public float WindX { get; set; }
    public float WindY { get; set; }
    public string PlayerStateName { get; set; } = "";
    public string ChapterTime { get; set; } = "";
    public string RoomName { get; set; } = "";
    public string SolidsData { get; set; } = "";
    public BoundsDataDto[] StaticSolids { get; set; } = Array.Empty<BoundsDataDto>();
    public BoundsDataDto[] Spinners { get; set; } = Array.Empty<BoundsDataDto>();
    public BoundsDataDto[] Lightning { get; set; } = Array.Empty<BoundsDataDto>();
    public SpikeDataDto[] Spikes { get; set; } = Array.Empty<SpikeDataDto>();
    public BoundsDataDto[] WindTriggers { get; set; } = Array.Empty<BoundsDataDto>();
    public BoundsDataDto[] JumpThrus { get; set; } = Array.Empty<BoundsDataDto>();
}
