using System;

namespace Celeste.Mod.AutoPlayer {
    /// Analyzes game state and determines the next action for the autoplayer
    internal static class AIDecisionMaker {
        /// Game state snapshot for decision making
        public class GameStateSnapshot {
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
            public string PlayerStateName { get; set; }
        }

        // Track if we've already jumped on the current ground contact
        private static bool hasJumpedThisFrame = false;
        // Track frames since last jump to prevent rapid re-jumping
        private static int framesSinceLastJump = 0;

        /// Reset AI state when autoplayer starts
        internal static void Reset() {
            hasJumpedThisFrame = false;
            framesSinceLastJump = 0;
            DebugLog.Write("[AIDecisionMaker] Reset - ready for new session");
        }

        /// Decides what action to take based on current game state
        /// Returns a sequence of input frames
        public static InputFrame[] DecideNextAction(GameStateSnapshot state) {
            if (state == null) {
                DebugLog.Write("[AIDecisionMaker] Game state is NULL");
                framesSinceLastJump++;
                return new[] { new InputFrame(Actions.None, 1) };
            }

            // Increment frame counter
            framesSinceLastJump++;

            // Log state at this decision point
            DebugLog.Write($"[AIDecisionMaker] Querying: OnGround={state.OnGround}, hasJumpedThisFrame={hasJumpedThisFrame}, framesSinceLastJump={framesSinceLastJump}, PlayerY={state.PlayerY:F1}, SpeedY={state.PlayerSpeedY:F2}");

            // Reset jump flag if enough fetches have passed since last jump (each fetch = ~100 frames, so 2 fetches = ~200 frames)
            if (hasJumpedThisFrame && framesSinceLastJump > 2) {
                DebugLog.Write($"[AIDecisionMaker] Resetting hasJumpedThisFrame after {framesSinceLastJump} fetches");
                hasJumpedThisFrame = false;
                framesSinceLastJump = 0;
            }

            // If player is on ground and we haven't jumped yet this cycle, perform the jump sequence
            if (state.OnGround && !hasJumpedThisFrame) {
                hasJumpedThisFrame = true;
                framesSinceLastJump = 0;
                DebugLog.Write("[AIDecisionMaker] Decision: JUMP sequence (10 frames jump, then immediately dash+up+right+jump for 20 frames)");

                // Build the input sequence:
                // 1. Hold jump for 10 frames
                // 2. Hold dash + up + right + jump for 20 frames (no gap, immediate combo)
                return new[] {
                    new InputFrame(Actions.Jump, 10),
                    new InputFrame(Actions.Dash | Actions.Up | Actions.Right | Actions.Jump, 20)
                };
            }

            // Not jumping yet - just dash, but use shorter frame count so AI can re-query sooner
            // This allows the AI to check if it's time to jump again before too many frames pass
            DebugLog.Write($"[AIDecisionMaker] Decision: Dash (for {Math.Min(30, framesSinceLastJump == 0 ? 100 : 30)} frames)");
            return new[] { new InputFrame(Actions.Dash, 30) };  // Hold dash for 30 frames, then AI will query again
        }
    }
}