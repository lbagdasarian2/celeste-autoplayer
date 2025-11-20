using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.AutoPlayer {
    /// Monitors game state from CelesteTAS endpoint and logs it
    internal static class GameStateMonitor {
        private static readonly HttpClient httpClient = new();
        private const string GAMESTATE_URL = "http://localhost:32270/tas/game_state";
        private const int FETCH_INTERVAL = 100; // Fetch every 10 frames (~166ms)
        private static bool hooksApplied = false;
        private static bool monitoringEnabled = false;
        private static int frameCounter = -1; // Start at -1 so first frame triggers fetch immediately
        private static AIDecisionMaker.GameStateSnapshot latestGameState = null;
        private static bool gameStateUpdatedThisFrame = false;

        internal static void Initialize() {
            if (hooksApplied) {
                return;
            }

            try {
                DebugLog.Write("[GameStateMonitor.Initialize] Setting up hooks...");
                httpClient.Timeout = TimeSpan.FromMilliseconds(100); // Quick timeout

                using (new DetourConfigContext(new DetourConfig("AutoPlayer", priority: int.MaxValue)).Use()) {
                    // Hook into Engine.Update to fetch game state each frame
                    On.Monocle.Engine.Update += On_Engine_Update;
                }

                hooksApplied = true;
                monitoringEnabled = true;
                DebugLog.Write("[GameStateMonitor.Initialize] Hooks applied successfully");
            } catch (Exception ex) {
                DebugLog.WriteException("GameStateMonitor.Initialize", ex);
                Logger.Log(nameof(AutoPlayerModule), $"[GameStateMonitor] ERROR initializing: {ex}");
            }
        }

        internal static void Cleanup() {
            if (!hooksApplied) {
                return;
            }

            try {
                On.Monocle.Engine.Update -= On_Engine_Update;
                monitoringEnabled = false;
                hooksApplied = false;
                DebugLog.Write("[GameStateMonitor.Cleanup] Hooks removed");
            } catch (Exception ex) {
                DebugLog.Write($"[GameStateMonitor.Cleanup] ERROR: {ex.Message}");
            }
        }

        private static void On_Engine_Update(On.Monocle.Engine.orig_Update orig, Engine self, Microsoft.Xna.Framework.GameTime gameTime) {
            orig(self, gameTime);

            if (monitoringEnabled) {
                frameCounter++;
                // Fetch and log game state every FETCH_INTERVAL frames
                if (frameCounter >= FETCH_INTERVAL) {
                    frameCounter = 0;
                    _ = FetchAndLogGameStateAsync();
                }
            }
        }

        private static async Task FetchAndLogGameStateAsync() {
            try {
                var response = await httpClient.GetAsync(GAMESTATE_URL);

                if (response.IsSuccessStatusCode) {
                    var content = await response.Content.ReadAsStringAsync();

                    // Log the full JSON response
                    DebugLog.Write($"[GameState] Full JSON: {content}");

                    // Skip if response is null
                    if (content == "null" || string.IsNullOrEmpty(content)) {
                        DebugLog.Write("[GameStateMonitor] Response is null - CelesteTAS endpoint not ready");
                        return;
                    }

                    // Parse to validate JSON and extract key info
                    using (var doc = JsonDocument.Parse(content)) {
                        var root = doc.RootElement;

                        // Extract DeltaTime
                        float deltaTime = 0f;
                        if (root.TryGetProperty("DeltaTime", out var deltaTimeElement)) {
                            deltaTime = deltaTimeElement.GetSingle();
                        }

                        // Extract Player info
                        float posX = 0f, posY = 0f, posRemX = 0f, posRemY = 0f;
                        float speedX = 0f, speedY = 0f, starFlySpeedLerp = 0f;
                        bool onGround = false, isHolding = false, autoJump = false;
                        int jumpTimer = 0, maxFall = 0;

                        if (root.TryGetProperty("Player", out var playerElement)) {
                            if (playerElement.TryGetProperty("Position", out var posElement)) {
                                posX = posElement.GetProperty("X").GetSingle();
                                posY = posElement.GetProperty("Y").GetSingle();
                            }
                            if (playerElement.TryGetProperty("PositionRemainder", out var posRemElement)) {
                                posRemX = posRemElement.GetProperty("X").GetSingle();
                                posRemY = posRemElement.GetProperty("Y").GetSingle();
                            }
                            if (playerElement.TryGetProperty("Speed", out var speedElement)) {
                                speedX = speedElement.GetProperty("X").GetSingle();
                                speedY = speedElement.GetProperty("Y").GetSingle();
                            }
                            if (playerElement.TryGetProperty("starFlySpeedLerp", out var starFlyElement)) {
                                starFlySpeedLerp = starFlyElement.GetSingle();
                            }
                            if (playerElement.TryGetProperty("OnGround", out var groundElement)) {
                                onGround = groundElement.GetBoolean();
                            }
                            if (playerElement.TryGetProperty("IsHolding", out var holdingElement)) {
                                isHolding = holdingElement.GetBoolean();
                            }
                            if (playerElement.TryGetProperty("JumpTimer", out var jumpTimerElement)) {
                                jumpTimer = jumpTimerElement.GetInt32();
                            }
                            if (playerElement.TryGetProperty("AutoJump", out var autoJumpElement)) {
                                autoJump = autoJumpElement.GetBoolean();
                            }
                            if (playerElement.TryGetProperty("MaxFall", out var maxFallElement)) {
                                maxFall = maxFallElement.GetInt32();
                            }
                        }

                        // Extract Level info (Bounds and Wind)
                        float levelBoundsX = 0f, levelBoundsY = 0f, levelBoundsW = 0f, levelBoundsH = 0f;
                        float windX = 0f, windY = 0f;

                        if (root.TryGetProperty("Level", out var levelElement)) {
                            if (levelElement.TryGetProperty("Bounds", out var boundsElement)) {
                                levelBoundsX = boundsElement.GetProperty("X").GetSingle();
                                levelBoundsY = boundsElement.GetProperty("Y").GetSingle();
                                levelBoundsW = boundsElement.GetProperty("W").GetSingle();
                                levelBoundsH = boundsElement.GetProperty("H").GetSingle();
                            }
                            if (levelElement.TryGetProperty("WindDirection", out var windElement)) {
                                windX = windElement.GetProperty("X").GetSingle();
                                windY = windElement.GetProperty("Y").GetSingle();
                            }
                        }

                        // Extract other top-level properties
                        string chapterTime = "";
                        string roomName = "";
                        string playerStateName = "";

                        if (root.TryGetProperty("ChapterTime", out var timeElement)) {
                            chapterTime = timeElement.GetString() ?? "";
                        }
                        if (root.TryGetProperty("RoomName", out var roomElement)) {
                            roomName = roomElement.GetString() ?? "";
                        }
                        if (root.TryGetProperty("PlayerStateName", out var stateElement)) {
                            playerStateName = stateElement.GetString() ?? "";
                        }

                        // Create and store snapshot for AI decision making
                        latestGameState = new AIDecisionMaker.GameStateSnapshot {
                            PlayerX = posX,
                            PlayerY = posY,
                            PlayerSpeedX = speedX,
                            PlayerSpeedY = speedY,
                            OnGround = onGround,
                            IsHolding = isHolding,
                            JumpTimer = jumpTimer,
                            AutoJump = autoJump,
                            LevelBoundsX = levelBoundsX,
                            LevelBoundsY = levelBoundsY,
                            LevelBoundsW = levelBoundsW,
                            LevelBoundsH = levelBoundsH,
                            WindX = windX,
                            WindY = windY,
                            PlayerStateName = playerStateName
                        };

                        // Mark that game state was updated this frame
                        gameStateUpdatedThisFrame = true;

                        // Log the critical state info for debugging jump behavior
                        DebugLog.Write($"[GameStateMonitor] STATE FETCHED: OnGround={onGround}, Pos({posX:F1},{posY:F1}), Speed({speedX:F2},{speedY:F2}), State={playerStateName}");

                        // Log extracted player info
                        DebugLog.Write($"[GameState] Player: Pos({posX:F1},{posY:F1}) PosRem({posRemX:F1},{posRemY:F1}) Speed({speedX:F2},{speedY:F2}) StarFlyLerp={starFlySpeedLerp:F2}");
                        DebugLog.Write($"[GameState] Player: OnGround={onGround} Holding={isHolding} JumpTimer={jumpTimer} AutoJump={autoJump} MaxFall={maxFall}");
                        DebugLog.Write($"[GameState] Level: Bounds({levelBoundsX},{levelBoundsY},{levelBoundsW}x{levelBoundsH}) Wind({windX:F2},{windY:F2})");
                        DebugLog.Write($"[GameState] DeltaTime={deltaTime:F6} ChapterTime={chapterTime} Room={roomName} State={playerStateName}");
                    }
                } else {
                    // CelesteTAS endpoint not available - silently skip
                }
            } catch (HttpRequestException) {
                // Connection failed - CelesteTAS probably not running, silently skip
            } catch (TaskCanceledException) {
                // Timeout - request took too long, silently skip
            } catch (JsonException ex) {
                DebugLog.Write($"[GameStateMonitor] JSON parse error: {ex.Message}");
            } catch (Exception ex) {
                DebugLog.Write($"[GameStateMonitor] Unexpected error: {ex.Message}");
            }
        }

        internal static void SetEnabled(bool enabled) {
            monitoringEnabled = enabled;
            if (enabled) {
                DebugLog.Write("[GameStateMonitor] Monitoring ENABLED");
            } else {
                DebugLog.Write("[GameStateMonitor] Monitoring DISABLED");
            }
        }

        internal static bool IsEnabled => monitoringEnabled;

        /// Get the latest game state snapshot for AI decision making
        internal static AIDecisionMaker.GameStateSnapshot GetLatestGameState() {
            return latestGameState;
        }

        /// Check if game state was updated this frame (safe for AI to use latest state)
        internal static bool WasUpdatedThisFrame() {
            return gameStateUpdatedThisFrame;
        }

        /// Clear the update flag after AI has processed the state
        internal static void ClearUpdateFlag() {
            gameStateUpdatedThisFrame = false;
            DebugLog.Write("[GameStateMonitor] Update flag cleared after AI processing");
        }
    }
}
