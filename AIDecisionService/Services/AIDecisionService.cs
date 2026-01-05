using AIDecisionService.Models;

namespace AIDecisionService.Services;

/// <summary>
/// Analyzes game state and determines the next action for the autoplayer
/// </summary>
public class AIDecisionService
{
    // Track previous state to calculate movement delta (static so it persists across requests)
    private static GameStateDto? _previousState = null;

    /// <summary>
    /// Decides what action to take based on current game state
    /// Returns a sequence of input frames
    /// </summary>
    public InputFrameDto[] DecideNextAction(GameStateDto state)
    {
        if (state == null)
        {
            return ActionSequences.NoAction();
        }

        // Log state at this decision point
        Console.WriteLine($"[AIDecisionService] Querying: OnGround={state.OnGround}, PlayerX={state.PlayerX:F1}, PlayerY={state.PlayerY:F1}, SpeedY={state.PlayerSpeedY:F2}");
        Console.WriteLine($"[AIDecisionService] RoomName={state.RoomName}, ChapterTime={state.ChapterTime}");
        Console.WriteLine($"[AIDecisionService] Spikes={state.Spikes.Length}, StaticSolids={state.StaticSolids.Length}, Spinners={state.Spinners.Length}");
        Console.WriteLine($"[AIDecisionService] SolidsData length={state.SolidsData.Length} chars");

        // Calculate pixel movement since last decision (both horizontal and vertical)
        if (_previousState != null)
        {
            float deltaX = state.PlayerX - _previousState.PlayerX;
            float deltaY = state.PlayerY - _previousState.PlayerY;
            Console.WriteLine($"[AIDecisionService] Movement Result: ΔX={deltaX:F1} pixels, ΔY={deltaY:F1} pixels (from ({_previousState.PlayerX:F1},{_previousState.PlayerY:F1}) to ({state.PlayerX:F1},{state.PlayerY:F1}))");
        }

        // Store current state for next decision
        _previousState = state;

        // Always return LongJumpDashCombo for calibration
        Console.WriteLine($"[AIDecisionService] Decision: LONG JUMP DASH COMBO - Input at PlayerX={state.PlayerX:F1}, PlayerY={state.PlayerY:F1}");
        return ActionSequences.LongJumpDashCombo();
    }
}
