using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.AutoPlayer {

    /// Controls the sequence of inputs for the autoplayer
    public class InputController {
        private List<InputFrame> inputs = new List<InputFrame>();
        private int currentFrameIndex = 0;
        private int framesInCurrentInput = 0;
        private bool isRunning = false;
        private bool useDynamicAI = false;

        public bool IsRunning => isRunning;
        public int TotalFrames => inputs.Sum(i => i.Frames);

        /// Initialize the input sequence (static mode)
        public void Initialize(params InputFrame[] sequence) {
            inputs = new List<InputFrame>(sequence);
            currentFrameIndex = 0;
            framesInCurrentInput = 0;
            isRunning = true;
            useDynamicAI = false;
            Logger.Log(nameof(AutoPlayerModule), $"Input sequence initialized with {inputs.Count} actions, total frames: {TotalFrames}");
        }

        /// Initialize dynamic AI mode
        public void InitializeDynamicAI() {
            inputs.Clear();
            currentFrameIndex = 0;
            framesInCurrentInput = 0;
            isRunning = true;
            useDynamicAI = true;
            DebugLog.Write("[InputController] Dynamic AI mode initialized");
            Logger.Log(nameof(AutoPlayerModule), "Input controller switched to dynamic AI mode");
        }

        /// Get the current input to apply
        public Actions GetCurrentInput() {
            if (!isRunning) {
                return Actions.None;
            }

            // Dynamic AI mode: query the AI decision maker based on current game state
            if (useDynamicAI) {
                // Only query AI if game state was updated this frame
                bool wasUpdated = GameStateMonitor.WasUpdatedThisFrame();
                DebugLog.Write($"[InputController] DynamicAI: WasUpdatedThisFrame={wasUpdated}");

                if (wasUpdated) {
                    var gameState = GameStateMonitor.GetLatestGameState();
                    if (gameState != null) {
                        // Make a decision based on current game state
                        var (action, duration) = AIDecisionMaker.DecideNextAction(gameState);

                        // Store the action and its duration if it changed
                        if (inputs.Count == 0 || (inputs[0].Action != action || inputs[0].Frames != duration)) {
                            DebugLog.Write($"[InputController] AI Decision Changed: {action} for {duration} frames");
                            inputs.Clear();
                            inputs.Add(new InputFrame(action, duration));
                            currentFrameIndex = 0;
                            framesInCurrentInput = 0;
                        }

                        // Clear the update flag after processing this frame's state
                        GameStateMonitor.ClearUpdateFlag();
                    } else {
                        DebugLog.Write("[InputController] Game state is null");
                    }
                } else {
                    DebugLog.Write("[InputController] Waiting for game state update...");
                }

                // Return the current action from our queue (keep holding until action expires)
                if (inputs.Count > 0 && currentFrameIndex < inputs.Count) {
                    var aiInput = inputs[currentFrameIndex];

                    DebugLog.Write($"[InputController] Returning action: {aiInput.Action} (frame {framesInCurrentInput + 1}/{aiInput.Frames})");
                    var actionToReturn = aiInput.Action;

                    framesInCurrentInput++;
                    if (framesInCurrentInput >= aiInput.Frames) {
                        currentFrameIndex++;
                        framesInCurrentInput = 0;
                    }

                    return actionToReturn;
                }

                DebugLog.Write("[InputController] No action queued yet");
                return Actions.None;
            }

            // Static mode: play pre-defined sequence
            if (inputs.Count == 0) {
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
