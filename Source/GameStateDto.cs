using System;

namespace Celeste.Mod.AutoPlayer {

    /// <summary>
    /// Rectangular bounds
    /// </summary>
    public class BoundsDataDto {
        public float X { get; set; }
        public float Y { get; set; }
        public float W { get; set; }
        public float H { get; set; }
    }

    /// <summary>
    /// Spike entity with bounds and direction
    /// </summary>
    public class SpikeDataDto {
        public BoundsDataDto Bounds { get; set; }
        public int Direction { get; set; }
    }

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
        public string ChapterTime { get; set; } = "";
        public string RoomName { get; set; } = "";
        public string SolidsData { get; set; } = "";
        public BoundsDataDto[] StaticSolids { get; set; } = new BoundsDataDto[0];
        public BoundsDataDto[] Spinners { get; set; } = new BoundsDataDto[0];
        public BoundsDataDto[] Lightning { get; set; } = new BoundsDataDto[0];
        public SpikeDataDto[] Spikes { get; set; } = new SpikeDataDto[0];
        public BoundsDataDto[] WindTriggers { get; set; } = new BoundsDataDto[0];
        public BoundsDataDto[] JumpThrus { get; set; } = new BoundsDataDto[0];

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
                PlayerStateName = snapshot.PlayerStateName ?? "",
                ChapterTime = snapshot.ChapterTime ?? "",
                RoomName = snapshot.RoomName ?? "",
                SolidsData = snapshot.SolidsData ?? "",
                StaticSolids = ConvertBoundsArray(snapshot.StaticSolids),
                Spinners = ConvertBoundsArray(snapshot.Spinners),
                Lightning = ConvertBoundsArray(snapshot.Lightning),
                Spikes = ConvertSpikesArray(snapshot.Spikes),
                WindTriggers = ConvertBoundsArray(snapshot.WindTriggers),
                JumpThrus = ConvertBoundsArray(snapshot.JumpThrus)
            };
        }

        /// <summary>
        /// Convert BoundsData array to DTO array
        /// </summary>
        private static BoundsDataDto[] ConvertBoundsArray(AIDecisionMaker.BoundsData[] source) {
            if (source == null) return new BoundsDataDto[0];
            var result = new BoundsDataDto[source.Length];
            for (int i = 0; i < source.Length; i++) {
                result[i] = new BoundsDataDto {
                    X = source[i].X,
                    Y = source[i].Y,
                    W = source[i].W,
                    H = source[i].H
                };
            }
            return result;
        }

        /// <summary>
        /// Convert SpikeData array to DTO array
        /// </summary>
        private static SpikeDataDto[] ConvertSpikesArray(AIDecisionMaker.SpikeData[] source) {
            if (source == null) return new SpikeDataDto[0];
            var result = new SpikeDataDto[source.Length];
            for (int i = 0; i < source.Length; i++) {
                result[i] = new SpikeDataDto {
                    Bounds = new BoundsDataDto {
                        X = source[i].Bounds.X,
                        Y = source[i].Bounds.Y,
                        W = source[i].Bounds.W,
                        H = source[i].Bounds.H
                    },
                    Direction = source[i].Direction
                };
            }
            return result;
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
