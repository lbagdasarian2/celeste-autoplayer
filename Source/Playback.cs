using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.AutoPlayer {
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

    /// Configure input system on first actual input injection (lazy initialization)
    private static bool inputSystemConfigured = false;
    private static void ConfigureInputSystem() {
        if (inputSystemConfigured) {
            return;
        }

        try {
            // Ensure controller has focus and gamepad is marked as attached (CelesteTAS pattern)
            // This is done lazily when we first inject inputs to ensure MInput is fully initialized
            MInput.ControllerHasFocus = true;
            MInput.GamePads[0].Attached = true;
            inputSystemConfigured = true;
            DebugLog.Write("[Playback] Input system configured: ControllerHasFocus=true, GamePad[0].Attached=true");
        } catch (Exception ex) {
            DebugLog.Write($"[Playback] Warning: Could not configure input system: {ex.Message}");
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
        // Configure input system early
        ConfigureInputSystem();

        // Call the original update first
        orig();

        // If the autoplayer is running, inject inputs AFTER original update processes keyboard/mouse
        if (inputController.IsRunning) {
            var action = inputController.GetCurrentInput();
            if (action != Actions.None) {
                DebugLog.Write($"MInput.Update: action={action} (AFTER orig)");
                Logger.Log(nameof(AutoPlayerModule), $"[v{GetVersion()}] Injecting input: {action}");
            }
            ApplyInput(action);
        }
    }

    /// Apply the input action to the game
    private static void ApplyInput(Actions action) {
        if (action != Actions.None) {
            DebugLog.Write($"ApplyInput called with action={action}");
        }

        // Track which keys should be pressed for keyboard input
        var keysToPress = new List<Keys>();

        // Build DPad based on movement actions (this is the critical part!)
        var dpad = new GamePadDPad(
            action.HasFlag(Actions.Up) ? ButtonState.Pressed : ButtonState.Released,
            action.HasFlag(Actions.Down) ? ButtonState.Pressed : ButtonState.Released,
            action.HasFlag(Actions.Left) ? ButtonState.Pressed : ButtonState.Released,
            action.HasFlag(Actions.Right) ? ButtonState.Pressed : ButtonState.Released
        );

        if (action.HasFlag(Actions.Right)) {
            keysToPress.Add(Keys.Right);
            DebugLog.Write($"Move RIGHT - DPad Right button pressed");
            Logger.Log(nameof(AutoPlayerModule), $"[v{GetVersion()}] Move RIGHT - DPad Right button");
        }
        if (action.HasFlag(Actions.Left)) {
            keysToPress.Add(Keys.Left);
            DebugLog.Write($"Move LEFT - DPad Left button pressed");
            Logger.Log(nameof(AutoPlayerModule), $"[v{GetVersion()}] Move LEFT - DPad Left button");
        }
        if (action.HasFlag(Actions.Up)) {
            keysToPress.Add(Keys.Up);
            DebugLog.Write($"Move UP - DPad Up button pressed");
            Logger.Log(nameof(AutoPlayerModule), $"[v{GetVersion()}] Move UP - DPad Up button");
        }
        if (action.HasFlag(Actions.Down)) {
            keysToPress.Add(Keys.Down);
            DebugLog.Write($"Move DOWN - DPad Down button pressed");
            Logger.Log(nameof(AutoPlayerModule), $"[v{GetVersion()}] Move DOWN - DPad Down button");
        }

        // Sticks set to zero when using DPad (following CelesteTAS pattern)
        var sticks = new GamePadThumbSticks(Vector2.Zero, Vector2.Zero);

        // Build buttons flags - include movement buttons AND action buttons
        Buttons buttonFlags = 0;

        // Movement buttons (DPad)
        if (action.HasFlag(Actions.Up)) {
            buttonFlags |= Buttons.DPadUp;
            DebugLog.Write($"UP button flag added");
        }
        if (action.HasFlag(Actions.Down)) {
            buttonFlags |= Buttons.DPadDown;
            DebugLog.Write($"DOWN button flag added");
        }
        if (action.HasFlag(Actions.Left)) {
            buttonFlags |= Buttons.DPadLeft;
            DebugLog.Write($"LEFT button flag added");
        }
        if (action.HasFlag(Actions.Right)) {
            buttonFlags |= Buttons.DPadRight;
            DebugLog.Write($"RIGHT button flag added");
        }

        // Action buttons
        if (action.HasFlag(Actions.Jump)) {
            buttonFlags |= Buttons.A;
            keysToPress.Add(Keys.Z);
            DebugLog.Write($"JUMP - pressing A button and Z key");
            Logger.Log(nameof(AutoPlayerModule), $"[v{GetVersion()}] JUMP - pressing A button");
        }
        if (action.HasFlag(Actions.Dash)) {
            buttonFlags |= Buttons.X;
            keysToPress.Add(Keys.X);
            DebugLog.Write($"DASH - pressing X button and X key");
            Logger.Log(nameof(AutoPlayerModule), $"[v{GetVersion()}] DASH - pressing X button");
        }

        var buttons = new GamePadButtons(buttonFlags);

        var gamePadState = new GamePadState(
            sticks,
            new GamePadTriggers(0, 0),
            buttons,
            dpad
        );

        // Update MInput gamepad - use gamepad 0 (following CelesteTAS pattern)
        var gamepadData = MInput.GamePads[0];
        gamepadData.PreviousState = gamepadData.CurrentState;
        gamepadData.CurrentState = gamePadState;

        // Update keyboard state if needed
        if (keysToPress.Count > 0) {
            var currentKeyboardState = Keyboard.GetState();
            var pressedKeys = currentKeyboardState.GetPressedKeys().ToList();

            // Add our injected keys
            foreach (var key in keysToPress) {
                if (!pressedKeys.Contains(key)) {
                    pressedKeys.Add(key);
                }
            }

            // Create new keyboard state with our keys pressed
            var newKeyboardState = new KeyboardState(pressedKeys.ToArray());
            MInput.Keyboard.PreviousState = MInput.Keyboard.CurrentState;
            MInput.Keyboard.CurrentState = newKeyboardState;
        }

        // CRITICAL: Call UpdateVirtualInputs to apply the state to virtual buttons
        MInput.UpdateVirtualInputs();
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

    /// Start autoplayer in dynamic AI mode
    internal static void StartDynamicAutoplay() {
        DebugLog.Write("AutoPlayer started in Dynamic AI mode");
        inputController.InitializeDynamicAI();
        Logger.Log(nameof(AutoPlayerModule), "AutoPlayer started in Dynamic AI mode");
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
}
