using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.AutoPlayer;

/// Handles input injection for the autoplayer
internal static class Playback {
    private static InputController inputController = new();
    private static bool hooksApplied = false;

    /// Initialize the playback system
    internal static void Initialize() {
        if (hooksApplied) {
            return;
        }

        using (new DetourConfigContext(new DetourConfig("AutoPlayer", priority: int.MaxValue)).Use()) {
            // Hook into MInput.Update to inject our inputs
            On.Monocle.MInput.Update += On_MInput_Update;
        }

        hooksApplied = true;
    }

    /// Cleanup the playback system
    internal static void Cleanup() {
        if (!hooksApplied) {
            return;
        }

        On.Monocle.MInput.Update -= On_MInput_Update;
        inputController.Stop();
        hooksApplied = false;
    }

    private static void On_MInput_Update(On.Monocle.MInput.orig_Update orig) {
        // Call the original update first
        orig();

        // If the autoplayer is running, inject inputs
        if (inputController.IsRunning) {
            var action = inputController.GetCurrentInput();
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
        }
        if (action.HasFlag(Actions.Left)) {
            stickX = -1.0f;
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
        }
        if (action.HasFlag(Actions.Dash)) {
            buttonFlags |= Buttons.X;
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

    /// Start the autoplayer with the given input sequence
    internal static void StartAutoplay(params InputFrame[] sequence) {
        inputController.Initialize(sequence);
        Logger.Log(nameof(AutoPlayerModule), "AutoPlayer started");
    }

    /// Stop the autoplayer
    internal static void StopAutoplay() {
        inputController.Stop();
        Logger.Log(nameof(AutoPlayerModule), "AutoPlayer stopped");
    }
}
