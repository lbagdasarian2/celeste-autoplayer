namespace AIDecisionService.Models;

/// <summary>
/// Data Transfer Object for game state from Celeste
/// </summary>
public class GameStateDto
{
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }
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
}
