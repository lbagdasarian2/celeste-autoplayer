using System;

namespace Celeste.Mod.AutoPlayer {

    /// <summary>
    /// Data Transfer Object for game state - used to send game state to AIDecisionService
    /// </summary>
    public class GameStateDto {
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

        /// <summary>
        /// Create a DTO from a GameStateSnapshot
        /// </summary>
        internal static GameStateDto FromSnapshot(AIDecisionMaker.GameStateSnapshot snapshot) {
            return new GameStateDto {
                PlayerX = snapshot.PlayerX,
                PlayerY = snapshot.PlayerY,
                PlayerSpeedX = snapshot.PlayerSpeedX,
                PlayerSpeedY = snapshot.PlayerSpeedY,
                OnGround = snapshot.OnGround,
                IsHolding = snapshot.IsHolding,
                JumpTimer = snapshot.JumpTimer,
                AutoJump = snapshot.AutoJump,
                LevelBoundsX = snapshot.LevelBoundsX,
                LevelBoundsY = snapshot.LevelBoundsY,
                LevelBoundsW = snapshot.LevelBoundsW,
                LevelBoundsH = snapshot.LevelBoundsH,
                WindX = snapshot.WindX,
                WindY = snapshot.WindY,
                PlayerStateName = snapshot.PlayerStateName
            };
        }
    }

    /// <summary>
    /// Data Transfer Object for input frames returned from AIDecisionService
    /// </summary>
    public class InputFrameDto {
        public int Action { get; set; }
        public int Frames { get; set; }

        /// <summary>
        /// Convert to the local InputFrame representation
        /// </summary>
        public InputFrame ToInputFrame() {
            return new InputFrame((Actions)Action, Frames);
        }
    }

    /// <summary>
    /// Response from AIDecisionService containing the decision sequence
    /// </summary>
    public class DecisionResponse {
        public InputFrameDto[] Sequence { get; set; } = new InputFrameDto[0];

        /// <summary>
        /// Convert all DTOs to InputFrames
        /// </summary>
        public InputFrame[] ToInputFrames() {
            var result = new InputFrame[Sequence.Length];
            for (int i = 0; i < Sequence.Length; i++) {
                result[i] = Sequence[i].ToInputFrame();
            }
            return result;
        }
    }
}
