using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Celeste.Mod.AutoPlayer {

    /// Controls the sequence of inputs for the autoplayer
    public class InputController {
        private List<InputFrame> inputs = new List<InputFrame>();
        private int currentFrameIndex = 0;
        private int framesInCurrentInput = 0;
        private bool isRunning = false;
        private bool useDynamicAI = false;
        private readonly HttpClient httpClient = new();
        private const string AI_DECISION_SERVICE_URL = "http://localhost:5001/api/decision/decide";

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

            // Dynamic AI mode: query the AI decision service based on current game state
            if (useDynamicAI) {
                // Only query AI if game state was updated this frame
                bool wasUpdated = GameStateMonitor.WasUpdatedThisFrame();
                DebugLog.Write($"[InputController] DynamicAI: WasUpdatedThisFrame={wasUpdated}");

                if (wasUpdated) {
                    var gameState = GameStateMonitor.GetLatestGameState();
                    if (gameState != null) {
                        // Make a decision based on current game state
                        if (AISourceConfig.UseRemoteAIService) {
                            // Use remote HTTP service
                            _ = QueryAIDecisionServiceAsync(gameState);
                        } else {
                            // Use local AI decision maker
                            QueryLocalAIDecisionMaker(gameState);
                        }

                        // Clear the update flag after querying the AI source
                        GameStateMonitor.ClearUpdateFlag();
                    } else {
                        DebugLog.Write("[InputController] Game state is null");
                    }
                } else {
                    // waiting for game state to update
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

        /// <summary>
        /// Query the AI Decision Service via HTTP to get the next action sequence
        /// </summary>
        private async Task QueryAIDecisionServiceAsync(AIDecisionMaker.GameStateSnapshot gameState) {
            try {
                // Convert game state to DTO
                var gameStateDto = GameStateDto.FromSnapshot(gameState);

                // Serialize to JSON
                var json = JsonSerializer.Serialize(gameStateDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send POST request to AI Decision Service
                var response = await httpClient.PostAsync(AI_DECISION_SERVICE_URL, content);

                if (response.IsSuccessStatusCode) {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    DebugLog.Write($"[InputController] AI Service response: {responseContent}");

                    // Parse the response
                    using (var doc = JsonDocument.Parse(responseContent)) {
                        var root = doc.RootElement;

                        if (root.TryGetProperty("sequence", out var sequenceElement)) {
                            var sequence = JsonSerializer.Deserialize<InputFrameDto[]>(sequenceElement.GetRawText());

                            if (sequence != null && sequence.Length > 0) {
                                // Only replace sequence if we've completed the current one
                                if (inputs.Count == 0 || currentFrameIndex >= inputs.Count) {
                                    DebugLog.Write($"[InputController] AI Decision Changed: {sequence.Length} frames queued");
                                    inputs.Clear();

                                    // Convert DTOs to InputFrames
                                    foreach (var dto in sequence) {
                                        inputs.Add(dto.ToInputFrame());
                                    }

                                    currentFrameIndex = 0;
                                    framesInCurrentInput = 0;
                                } else {
                                    DebugLog.Write("[InputController] Ignoring new sequence - current sequence still playing");
                                }
                            }
                        }
                    }
                } else {
                    DebugLog.Write($"[InputController] AI Service error: HTTP {response.StatusCode}");
                }
            } catch (HttpRequestException ex) {
                DebugLog.Write($"[InputController] HTTP error: {ex.Message}");
            } catch (TaskCanceledException) {
                DebugLog.Write("[InputController] AI Service request timeout");
            } catch (JsonException ex) {
                DebugLog.Write($"[InputController] JSON parse error: {ex.Message}");
            } catch (Exception ex) {
                DebugLog.Write($"[InputController] Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Query the local AIDecisionMaker class to get the next action sequence
        /// </summary>
        private void QueryLocalAIDecisionMaker(AIDecisionMaker.GameStateSnapshot gameState) {
            try {
                // Call the local AI decision maker
                var sequence = AIDecisionMaker.DecideNextAction(gameState);

                if (sequence != null && sequence.Length > 0) {
                    // Only replace sequence if we've completed the current one
                    if (inputs.Count == 0 || currentFrameIndex >= inputs.Count) {
                        DebugLog.Write($"[InputController] Local AI Decision Changed: {sequence.Length} frames queued");
                        inputs.Clear();
                        inputs.AddRange(sequence);
                        currentFrameIndex = 0;
                        framesInCurrentInput = 0;
                    } else {
                        DebugLog.Write("[InputController] Ignoring new sequence - current sequence still playing");
                    }
                }
            } catch (Exception ex) {
                DebugLog.Write($"[InputController] Local AI error: {ex.Message}");
            }
        }
    }
}
