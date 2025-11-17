using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.AutoPlayer {
    /// Handles hotkey input for toggling AutoPlayer
    internal static class HotkeyHandler {
    private static KeyboardState previousKeyboardState;
    private static bool hooksApplied = false;

    internal static void Initialize() {
        if (hooksApplied) {
            return;
        }

        try {
            DebugLog.Write("[HotkeyHandler.Initialize] Setting up keyboard hook...");
            Logger.Log(nameof(AutoPlayerModule), "[HotkeyHandler.Initialize] Setting up keyboard hook...");
            using (new DetourConfigContext(new DetourConfig("AutoPlayer", priority: int.MaxValue)).Use()) {
                On.Monocle.Engine.Update += On_Engine_Update;
            }
            hooksApplied = true;
            DebugLog.Write("[HotkeyHandler.Initialize] Keyboard hook applied");
            Logger.Log(nameof(AutoPlayerModule), "[HotkeyHandler.Initialize] Keyboard hook applied");
        } catch (Exception ex) {
            DebugLog.WriteException("HotkeyHandler.Initialize", ex);
            Logger.Log(nameof(AutoPlayerModule), $"[HotkeyHandler.Initialize] ERROR: {ex}");
        }
    }

    internal static void Cleanup() {
        if (!hooksApplied) {
            return;
        }

        try {
            On.Monocle.Engine.Update -= On_Engine_Update;
            hooksApplied = false;
            Logger.Log(nameof(AutoPlayerModule), "[HotkeyHandler.Cleanup] Keyboard hook removed");
        } catch (Exception ex) {
            Logger.Log(nameof(AutoPlayerModule), $"[HotkeyHandler.Cleanup] ERROR: {ex}");
        }
    }

    private static void On_Engine_Update(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime) {
        // Call original first
        orig(self, gameTime);

        try {
            var currentKeyboardState = Keyboard.GetState();

            // Check if F7 was just pressed
            if (currentKeyboardState.IsKeyDown(Keys.F7) && !previousKeyboardState.IsKeyDown(Keys.F7)) {
                ToggleAutoplay();
            }

            previousKeyboardState = currentKeyboardState;
        } catch (Exception ex) {
            Logger.Log(nameof(AutoPlayerModule), $"[HotkeyHandler] ERROR: {ex}");
        }
    }

    private static void ToggleAutoplay() {
        if (Playback.IsRunning) {
            DebugLog.Write("[F7] AutoPlayer DISABLED");
            Logger.Log(nameof(AutoPlayerModule), "[F7] AutoPlayer DISABLED");
            Playback.StopAutoplay();
        } else {
            DebugLog.Write("[F7] AutoPlayer ENABLED");
            Logger.Log(nameof(AutoPlayerModule), "[F7] AutoPlayer ENABLED");
            // Start the autoplayer sequence: move right for 60 frames (1 second), then jump for 1 frame
            Playback.StartAutoplay(
                new InputFrame(Actions.Right, 60),
                new InputFrame(Actions.Jump, 1)
            );
        }
    }
    }
}
