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
            public int JumpTimer { get; set; }
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

        /// Reset AI state when autoplayer starts
        internal static void Reset() {
            hasJumpedThisFrame = false;
            DebugLog.Write("[AIDecisionMaker] Reset - ready for new session");
        }

        /// Decides what action to take based on current game state
        /// Returns the action(s) to perform and duration in frames
        public static (Actions action, int frames) DecideNextAction(GameStateSnapshot state) {
            if (state == null) {
                return (Actions.None, 1);
            }

            // If player is on ground and we haven't jumped yet this frame, jump
            if (state.OnGround && !hasJumpedThisFrame) {
                hasJumpedThisFrame = true;
                DebugLog.Write("[AIDecisionMaker] Decision: JUMP (on ground, not yet jumped)");
                return (Actions.Jump, 1);
            }

            // If player leaves ground, reset the jump flag
            if (!state.OnGround) {
                hasJumpedThisFrame = false;
            }

            // Otherwise do nothing
            DebugLog.Write("[AIDecisionMaker] Decision: NONE");
            return (Actions.None, 1);
        }
    }
}