using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.AutoPlayer;

/// Handles input injection for the autoplayer
internal static class Playback {
    private static InputController inputController = new();
    private static bool hooksApplied = false;

    /// Initialize the playback system (can be called multiple times safely)
    internal static void Initialize() {
        if (hooksApplied) {
            return;
        }

        try {
            DebugLog.Write("[Playback.Initialize] Setting up hooks...");
            Logger.Log(nameof(AutoPlayerModule), "[Playback.Initialize] Setting up hooks...");
            using (new DetourConfigContext(new DetourConfig("AutoPlayer", priority: int.MaxValue)).Use()) {
                // Hook into MInput.Update to inject our inputs
                On.Monocle.MInput.Update += On_MInput_Update;
            }

            hooksApplied = true;
            DebugLog.Write("[Playback.Initialize] Hooks applied successfully");
            Logger.Log(nameof(AutoPlayerModule), "[Playback.Initialize] Hooks applied successfully");
        } catch (Exception ex) {
            DebugLog.WriteException("Playback.Initialize", ex);
            Logger.Log(nameof(AutoPlayerModule), $"[Playback.Initialize] ERROR: {ex}");
        }
    }

    /// Cleanup the playback system
    internal static void Cleanup() {
        if (!hooksApplied) {
            return;
        }

        try {
            On.Monocle.MInput.Update -= On_MInput_Update;
            inputController.Stop();
            hooksApplied = false;
            Logger.Log(nameof(AutoPlayerModule), "[Playback.Cleanup] Hooks removed and state reset");
        } catch (Exception ex) {
            Logger.Log(nameof(AutoPlayerModule), $"[Playback.Cleanup] ERROR: {ex}");
        }
    }

    private static void On_MInput_Update(On.Monocle.MInput.orig_Update orig) {
        // Call the original update first
        orig();

        // If the autoplayer is running, inject inputs
        if (inputController.IsRunning) {
            var action = inputController.GetCurrentInput();
            if (action != Actions.None) {
                Logger.Log(nameof(AutoPlayerModule), $"[v{GetVersion()}] Injecting input: {action}");
            }
            ApplyInput(action);
        }
    }

    /// Apply the input action to the game
    private static void ApplyInput(Actions action) {
        // Build stick position
        var stickX = 0.0f;
        var stickY = 0.0f;

        if (action.HasFlag(Actions.Right)) {
            stickX = 1.0f;
            Logger.Log(nameof(AutoPlayerModule), $"[v{GetVersion()}] Move RIGHT - stickX: {stickX}");
        }
        if (action.HasFlag(Actions.Left)) {
            stickX = -1.0f;
            Logger.Log(nameof(AutoPlayerModule), $"[v{GetVersion()}] Move LEFT - stickX: {stickX}");
        }

        var sticks = new GamePadThumbSticks(new Vector2(stickX, stickY), Vector2.Zero);

        // Build DPad
        var dpad = new GamePadDPad(
            ButtonState.Released,
            ButtonState.Released,
            ButtonState.Released,
            ButtonState.Released
        );

        // Build buttons flags
        Buttons buttonFlags = 0;
        if (action.HasFlag(Actions.Jump)) {
            buttonFlags |= Buttons.A;
            Logger.Log(nameof(AutoPlayerModule), $"[v{GetVersion()}] JUMP - pressing A button");
        }
        if (action.HasFlag(Actions.Dash)) {
            buttonFlags |= Buttons.X;
            Logger.Log(nameof(AutoPlayerModule), $"[v{GetVersion()}] DASH - pressing X button");
        }

        var buttons = new GamePadButtons(buttonFlags);

        var gamePadState = new GamePadState(
            sticks,
            new GamePadTriggers(0, 0),
            buttons,
            dpad
        );

        // Update MInput gamepad - use gamepad 0
        var gamepadData = MInput.GamePads[0];
        gamepadData.PreviousState = gamepadData.CurrentState;
        gamepadData.CurrentState = gamePadState;
    }

    private static string GetVersion() {
        return typeof(Playback).Assembly.GetName().Version?.ToString() ?? "unknown";
    }

    /// Start the autoplayer with the given input sequence
    internal static void StartAutoplay(params InputFrame[] sequence) {
        DebugLog.Write("AutoPlayer started");
        inputController.Initialize(sequence);
        Logger.Log(nameof(AutoPlayerModule), "AutoPlayer started");
    }

    /// Stop the autoplayer
    internal static void StopAutoplay() {
        DebugLog.Write("AutoPlayer stopped");
        inputController.Stop();
        Logger.Log(nameof(AutoPlayerModule), "AutoPlayer stopped");
    }

    /// Check if the autoplayer is currently running
    internal static bool IsRunning => inputController.IsRunning;
}
