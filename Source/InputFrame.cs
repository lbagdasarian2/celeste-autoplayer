using System;

namespace Celeste.Mod.AutoPlayer;

/// Represents a single input frame with actions
public record InputFrame {
    /// The action to perform (Right = move right, Jump = jump, etc.)
    public Actions Action { get; init; }

    /// How many frames this input should be held for
    public int Frames { get; init; }

    public InputFrame(Actions action, int frames) {
        Action = action;
        Frames = frames;
    }
}

/// Simple action flags for input
[Flags]
public enum Actions {
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Jump = 1 << 2,
    Dash = 1 << 3,
}
