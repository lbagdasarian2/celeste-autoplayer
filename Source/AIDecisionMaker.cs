using System;

namespace Celeste.Mod.AutoPlayer {
    /// Analyzes game state and determines the next action for the autoplayer
    internal static class AIDecisionMaker {
        /// Rectangular bounds
        public class BoundsData {
            public float X { get; set; }
            public float Y { get; set; }
            public float W { get; set; }
            public float H { get; set; }
        }

        /// Spike entity with bounds and direction
        public class SpikeData {
            public BoundsData Bounds { get; set; }
            public int Direction { get; set; }
        }

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
            public string ChapterTime { get; set; }
            public string RoomName { get; set; }
            public string SolidsData { get; set; }
            public BoundsData[] StaticSolids { get; set; }
            public BoundsData[] Spinners { get; set; }
            public BoundsData[] Lightning { get; set; }
            public SpikeData[] Spikes { get; set; }
            public BoundsData[] WindTriggers { get; set; }
            public BoundsData[] JumpThrus { get; set; }
        }

        /// Reset AI state when autoplayer starts
        internal static void Reset() {
            DebugLog.Write("[AIDecisionMaker] Reset - ready for new session");
        }

        /// Decides what action to take based on current game state
        /// Returns a sequence of input frames
        public static InputFrame[] DecideNextAction(GameStateSnapshot state) {
            if (state == null) {
                DebugLog.Write("[AIDecisionMaker] Game state is NULL");
                // framesSinceLastJump++;
                return new[] { new InputFrame(Actions.None, 1) };
            }


            // Log state at this decision point
            Logger.Log(nameof(AutoPlayerModule), $"[AIDecisionMaker] Querying: OnGround={state.OnGround}, PlayerY={state.PlayerY:F1}, SpeedY={state.PlayerSpeedY:F2}");


            DebugLog.Write("[AIDecisionMaker] Decision: JUMP sequence (10 frames jump, then immediately dash+up+right+jump for 20 frames)");

            // Build the input sequence:
            // 1. Hold jump for 10 frames
            // 2. Hold dash + up + right + jump for 20 frames (no gap, immediate combo)
            return new[] {
                new InputFrame(Actions.Jump, 10),
                new InputFrame(Actions.Dash | Actions.Up | Actions.Right | Actions.Jump, 20)
            };
        }
    }
}