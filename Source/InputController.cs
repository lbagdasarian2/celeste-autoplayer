using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.AutoPlayer {

    /// Controls the sequence of inputs for the autoplayer
    public class InputController {
        private List<InputFrame> inputs = new List<InputFrame>();
        private int currentFrameIndex = 0;
        private int framesInCurrentInput = 0;
        private bool isRunning = false;

        public bool IsRunning => isRunning;
        public int TotalFrames => inputs.Sum(i => i.Frames);

        /// Initialize the input sequence
        public void Initialize(params InputFrame[] sequence) {
            inputs = new List<InputFrame>(sequence);
            currentFrameIndex = 0;
            framesInCurrentInput = 0;
            isRunning = true;
            Logger.Log(nameof(AutoPlayerModule), $"Input sequence initialized with {inputs.Count} actions, total frames: {TotalFrames}");
        }

        /// Get the current input to apply
        public Actions GetCurrentInput() {
            if (!isRunning || inputs.Count == 0) {
                DebugLog.Write($"GetCurrentInput: isRunning={isRunning}, inputCount={inputs.Count}");
                return Actions.None;
            }

            if (currentFrameIndex >= inputs.Count) {
                isRunning = false;
                DebugLog.Write("Input sequence completed");
                return Actions.None;
            }

            var currentInput = inputs[currentFrameIndex];
            DebugLog.Write($"[Frame {framesInCurrentInput}] Current action: {currentInput.Action} (needs {currentInput.Frames} frames)");

            // Advance to next input frame after we've held this one for the specified duration
            framesInCurrentInput++;
            if (framesInCurrentInput >= currentInput.Frames) {
                DebugLog.Write($"Completed {currentInput.Action} for {currentInput.Frames} frames");
                currentFrameIndex++;
                framesInCurrentInput = 0;

                if (currentFrameIndex < inputs.Count) {
                    DebugLog.Write($"Transitioning to next action: {inputs[currentFrameIndex].Action}");
                }
            }

            return currentInput.Action;
        }

        /// Reset the input controller
        public void Stop() {
            isRunning = false;
            currentFrameIndex = 0;
            framesInCurrentInput = 0;
        }
    }
}
