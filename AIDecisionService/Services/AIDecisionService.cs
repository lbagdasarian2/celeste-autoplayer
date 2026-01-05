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

        // Check if we can safely perform LongJumpDashCombo (64 pixels horizontal)
        float landingX = state.PlayerX + ActionSequences.LONG_JUMPDASH_COMBO_HORIZONTAL_DISTANCE;

        if (WouldLandOnSpike(landingX, state.PlayerY, state))
        {
            Console.WriteLine($"[AIDecisionService] Decision: SHORT RIGHT MOVE - Would land on spike at X={landingX:F1}");
            return ActionSequences.RightShortMove();
        }
        else if (!HasSolidGround(landingX, state.PlayerY, state))
        {
            Console.WriteLine($"[AIDecisionService] Decision: SHORT RIGHT MOVE - No solid ground at X={landingX:F1}");
            return ActionSequences.RightShortMove();
        }
        else
        {
            Console.WriteLine($"[AIDecisionService] Decision: LONG JUMP DASH COMBO - Safe landing at X={landingX:F1}");
            return ActionSequences.LongJumpDashCombo();
        }
    }

    /// <summary>
    /// Check if landing at the given position would hit a spike
    /// </summary>
    private bool WouldLandOnSpike(float landingX, float currentY, GameStateDto state)
    {
        // Check each spike to see if our landing position would overlap
        foreach (var spike in state.Spikes)
        {
            // For horizontal spikes (Direction 0 = up, 1 = down), check if we'd land on top
            // We need to check if the landing X position is within the spike's horizontal bounds
            // and if we're at approximately the right Y level (within a few pixels)

            // Check horizontal overlap: landing position is within spike's X range
            bool horizontalOverlap = landingX >= spike.Bounds.X &&
                                     landingX <= (spike.Bounds.X + spike.Bounds.W);

            // For upward-facing spikes (Direction 0), we'd hit them if we're landing on or near them
            // Allow some tolerance for landing near the spike Y position (within 8 pixels above)
            if (horizontalOverlap)
            {
                float spikeTop = spike.Bounds.Y;
                float spikeBottom = spike.Bounds.Y + spike.Bounds.H;

                // Check if player's current Y or potential landing Y is near the spike
                // Spikes pointing up (Direction 0): dangerous if we're above and landing on them
                // We consider it dangerous if we'd land within 16 pixels of the spike top
                bool verticalDanger = currentY >= (spikeTop - 16) && currentY <= (spikeBottom + 8);

                if (verticalDanger)
                {
                    Console.WriteLine($"[AIDecisionService] Spike danger detected at X={spike.Bounds.X:F1}, Y={spike.Bounds.Y:F1}, W={spike.Bounds.W:F1}, H={spike.Bounds.H:F1}, Dir={spike.Direction}");
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Check if there's solid ground to land on at the given position
    /// </summary>
    private bool HasSolidGround(float landingX, float currentY, GameStateDto state)
    {
        // SolidsData is a grid where each character represents an 8x8 pixel tile
        // '0' = no solid, any other digit = solid ground
        // The string is formatted with newlines separating rows

        if (string.IsNullOrEmpty(state.SolidsData))
        {
            Console.WriteLine("[AIDecisionService] No SolidsData available");
            return false; // No data, assume unsafe
        }

        // Split into rows
        var rows = state.SolidsData.Split('\n');
        if (rows.Length == 0)
        {
            return false;
        }

        // Calculate tile position (each tile is 8x8 pixels)
        int tileX = (int)(landingX / 8f);
        int currentTileY = (int)(currentY / 8f);

        // Search both upward and downward from current Y position to find ground
        // Check within a reasonable range (64 pixels up and down)
        int searchRangeUp = Math.Max(0, currentTileY - 8);   // 64 pixels up (8 tiles)
        int searchRangeDown = Math.Min(rows.Length, currentTileY + 8); // 64 pixels down (8 tiles)

        for (int searchY = searchRangeUp; searchY < searchRangeDown; searchY++)
        {
            if (searchY < 0 || searchY >= rows.Length)
                continue;

            string row = rows[searchY];
            if (tileX < 0 || tileX >= row.Length)
                continue;

            char tile = row[tileX];

            // Any non-'0' character means there's a solid
            if (tile != '0')
            {
                float solidY = searchY * 8f;
                string direction = searchY < currentTileY ? "above" : (searchY > currentTileY ? "below" : "at");
                Console.WriteLine($"[AIDecisionService] Found solid ground at tile ({tileX}, {searchY}) = '{tile}' ({direction} current position) for landing X={landingX:F1}");
                return true;
            }
        }

        Console.WriteLine($"[AIDecisionService] No solid ground found at X={landingX:F1} within 64 pixels up/down");
        return false;
    }
}
