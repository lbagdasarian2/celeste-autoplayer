namespace AIDecisionService.Models;

/// <summary>
/// Predefined action sequences for common Celeste movements
/// </summary>
public static class ActionSequences
{
    // Actions enum values (must match the Celeste mod)
    private const int ACTIONS_NONE = 0;
    private const int ACTIONS_LEFT = 1 << 0;    // 1
    private const int ACTIONS_RIGHT = 1 << 1;   // 2
    private const int ACTIONS_JUMP = 1 << 2;    // 4
    private const int ACTIONS_DASH = 1 << 3;    // 8
    private const int ACTIONS_UP = 1 << 4;      // 16
    private const int ACTIONS_DOWN = 1 << 5;    // 32

    // Movement calibration constants (measured empirically)
    // RightShortMove: 5 frames of right movement = 7 pixels
    private const float PIXELS_PER_FRAME_RIGHT = 7f / 5f;  // 1.4 pixels per frame

    // JumpDashCombo: 30 total frames (10 jump + 20 dash combo) = 44 pixels horizontal
    // Vertical: 0 pixels on flat ground (lands back at same level)
    // Note: If there's a ledge, vertical will be min(0, -ledge_height)
    private const float JUMPDASH_COMBO_HORIZONTAL_DISTANCE = 44f;  // pixels

    // LongJumpDashCombo: 45 total frames (5 jump + 40 dash combo) = 64 pixels horizontal
    // Vertical: 0 pixels on flat ground (lands back at same level)
    // Note: If there's a ledge, vertical will be min(0, -ledge_height)
    private const float LONG_JUMPDASH_COMBO_HORIZONTAL_DISTANCE = 64f;  // pixels

    /// <summary>
    /// Jump for 10 frames, then immediately dash+up+right+jump for 20 frames
    /// Calibrated: 44 pixels horizontal movement
    /// Vertical: 0 on flat ground, or min(0, -ledge_height) if landing on a ledge
    /// This is the basic ground movement combo
    /// </summary>
    public static InputFrameDto[] JumpDashCombo()
    {
        return new[]
        {
            new InputFrameDto { Action = ACTIONS_JUMP, Frames = 10 },
            new InputFrameDto { Action = ACTIONS_DASH | ACTIONS_UP | ACTIONS_RIGHT | ACTIONS_JUMP, Frames = 20 }
        };
    }

    /// <summary>
    /// No action
    /// </summary>
    public static InputFrameDto[] NoAction()
    {
        return new[] { new InputFrameDto { Action = ACTIONS_NONE, Frames = 1 } };
    }

    /// <summary>
    /// Move right for 5 frames
    /// Calibrated: 5 frames of right movement = 7 pixels (~1.4 pixels/frame)
    /// Useful for short directional movements
    /// </summary>
    public static InputFrameDto[] RightShortMove()
    {
        return new[] { new InputFrameDto { Action = ACTIONS_RIGHT, Frames = 5 } };
    }

    /// <summary>
    /// Jump for 5 frames, then dash+up+right+jump for 40 frames (extended combo)
    /// Calibrated: 64 pixels horizontal movement
    /// Vertical: 0 on flat ground, or min(0, -ledge_height) if landing on a ledge
    /// Covers more distance than JumpDashCombo (64 vs 44 pixels)
    /// </summary>
    public static InputFrameDto[] LongJumpDashCombo()
    {
        return new[]
        {
            new InputFrameDto { Action = ACTIONS_JUMP, Frames = 5 },
            new InputFrameDto { Action = ACTIONS_DASH | ACTIONS_UP | ACTIONS_RIGHT | ACTIONS_JUMP, Frames = 40 }
        };
    }
}
