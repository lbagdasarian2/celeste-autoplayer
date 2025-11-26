using AIDecisionService.Models;

namespace AIDecisionService.Services;

/// <summary>
/// Analyzes game state and determines the next action for the autoplayer
/// </summary>
public class AIDecisionService
{
    // Actions enum values (must match the Celeste mod)
    private const int ACTIONS_NONE = 0;
    private const int ACTIONS_LEFT = 1 << 0;    // 1
    private const int ACTIONS_RIGHT = 1 << 1;   // 2
    private const int ACTIONS_JUMP = 1 << 2;    // 4
    private const int ACTIONS_DASH = 1 << 3;    // 8
    private const int ACTIONS_UP = 1 << 4;      // 16
    private const int ACTIONS_DOWN = 1 << 5;    // 32

    /// <summary>
    /// Decides what action to take based on current game state
    /// Returns a sequence of input frames
    /// </summary>
    public InputFrameDto[] DecideNextAction(GameStateDto state)
    {
        if (state == null)
        {
            return new[] { new InputFrameDto { Action = ACTIONS_NONE, Frames = 1 } };
        }

        // Log state at this decision point
        Console.WriteLine($"[AIDecisionService] Querying: OnGround={state.OnGround}, PlayerY={state.PlayerY:F1}, SpeedY={state.PlayerSpeedY:F2}");

        // If player is on ground, perform the jump sequence
        if (state.OnGround)
        {
            Console.WriteLine("[AIDecisionService] Decision: JUMP sequence (10 frames jump, then immediately dash+up+right+jump for 20 frames)");

            // Build the input sequence:
            // 1. Hold jump for 10 frames
            // 2. Hold dash + up + right + jump for 20 frames (no gap, immediate combo)
            return new[]
            {
                new InputFrameDto { Action = ACTIONS_JUMP, Frames = 10 },
                new InputFrameDto { Action = ACTIONS_DASH | ACTIONS_UP | ACTIONS_RIGHT | ACTIONS_JUMP, Frames = 20 }
            };
        }

        // Default: just return None (no action when not on ground)
        Console.WriteLine("[AIDecisionService] Decision: None (not on ground)");
        return new[] { new InputFrameDto { Action = ACTIONS_NONE, Frames = 1 } };
    }
}
