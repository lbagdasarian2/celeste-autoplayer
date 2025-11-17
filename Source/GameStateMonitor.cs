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
        private const int FETCH_INTERVAL = 20; // Fetch every 20 frames
        private static bool hooksApplied = false;
        private static bool monitoringEnabled = false;
        private static int frameCounter = 0;

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

                    // Parse to validate JSON and extract key info
                    using (var doc = JsonDocument.Parse(content)) {
                        var root = doc.RootElement;

                        // Extract key player info
                        if (root.TryGetProperty("Player", out var playerElement)) {
                            if (playerElement.TryGetProperty("Position", out var posElement)) {
                                var x = posElement.GetProperty("X").GetSingle();
                                var y = posElement.GetProperty("Y").GetSingle();

                                if (playerElement.TryGetProperty("Speed", out var speedElement)) {
                                    var speedX = speedElement.GetProperty("X").GetSingle();
                                    var speedY = speedElement.GetProperty("Y").GetSingle();
                                    var onGround = playerElement.GetProperty("OnGround").GetBoolean();

                                    DebugLog.Write($"[GameState] Player: Pos({x:F1},{y:F1}) Speed({speedX:F2},{speedY:F2}) OnGround={onGround}");
                                }
                            }
                        }

                        if (root.TryGetProperty("RoomName", out var roomElement)) {
                            var room = roomElement.GetString();
                            if (root.TryGetProperty("ChapterTime", out var timeElement)) {
                                var time = timeElement.GetString();
                                DebugLog.Write($"[GameState] Room: {room} Time: {time}");
                            }
                        }
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
    }
}
