# AutoPlayer

A Celeste mod that automatically plays back pre-recorded input sequences. Press F7 to toggle playback.

## Overview

AutoPlayer is a sophisticated input injection system that hooks into Celeste's input update cycle and replays stored button sequences frame-by-frame. It emulates the input injection pattern used by CelesteTAS.

## How It Works

### Architecture

The mod consists of several key components working together:

**HotkeyHandler** → Detects F7 key press → **Playback** → Injects inputs → **Game reads inputs**

### Core Flow

1. **HotkeyHandler** continuously monitors keyboard input
   - When F7 is pressed, it creates an input sequence
   - Calls `Playback.StartAutoplay()` with a list of `InputFrame` objects

2. **InputController** manages the playback sequence
   - Tracks which action should be performed at each frame
   - Advances through the sequence one frame at a time
   - Each `InputFrame` specifies an action and how many frames to hold it

3. **Playback** hooks into `MInput.Update` (the game's input system)
   - On each frame, gets the current action from InputController
   - Injects the action by modifying gamepad and keyboard states
   - Calls `MInput.UpdateVirtualInputs()` to apply the injected inputs

4. **Game** reads the virtual button states
   - Virtual buttons reflect our injected inputs
   - Game logic processes them normally (movement, jumping, etc.)

### Frame-by-Frame Execution

```
Each Game Frame:
┌─────────────────────────────────────────┐
│ 1. MInput.Update hook fires             │
│ 2. Configure input system               │
│ 3. Call original MInput.Update()        │
│ 4. Get current action from sequence     │
│ 5. Create GamePadState with action      │
│ 6. Update MInput.GamePads[0]            │
│ 7. Update MInput.Keyboard               │
│ 8. Call MInput.UpdateVirtualInputs()    │
└─────────────────────────────────────────┘
         ↓
    Game reads virtual buttons
         ↓
    Player moves/jumps/dashes
```

## Input Definition

Actions are defined as bit flags:

```csharp
[Flags]
public enum Actions {
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Jump = 1 << 2,
    Dash = 1 << 3,
    Up = 1 << 4,
    Down = 1 << 5,
}
```

Input sequences are stored as `InputFrame` records:

```csharp
public record InputFrame {
    public Actions Action { get; init; }  // What to press
    public int Frames { get; init; }      // How many frames
}
```

Example sequence (from HotkeyHandler):
```csharp
new InputFrame(Actions.Right, 60)  // Move right for 60 frames (1 second at 60 FPS)
new InputFrame(Actions.Jump, 1)    // Jump for 1 frame
```

## Key Technical Details

### Hook Execution Order (CRITICAL)

The hook must execute in this exact order:

1. **Call `orig()`** - Let the game's normal input reading happen first
2. **Inject inputs** - Override with our button states
3. **Call `UpdateVirtualInputs()`** - Apply to the virtual button system

This ensures our injected inputs take precedence and are properly registered.

### Why We Set Both DPad and Buttons

```csharp
// GamePadDPad: the actual directional pad state
var dpad = new GamePadDPad(
    action.HasFlag(Actions.Up) ? ButtonState.Pressed : ButtonState.Released,
    action.HasFlag(Actions.Down) ? ButtonState.Pressed : ButtonState.Released,
    action.HasFlag(Actions.Left) ? ButtonState.Pressed : ButtonState.Released,
    action.HasFlag(Actions.Right) ? ButtonState.Pressed : ButtonState.Released
);

// Buttons: the button flags for the same actions
Buttons buttonFlags = 0;
if (action.HasFlag(Actions.Right)) buttonFlags |= Buttons.DPadRight;
if (action.HasFlag(Actions.Left)) buttonFlags |= Buttons.DPadLeft;
// ... etc
```

Celeste uses both the DPad state (physical) and button flags (logical). We must set both to ensure input is properly recognized.

### Lazy Initialization

Input system configuration happens on first use, not at module load:

```csharp
private static void ConfigureInputSystem() {
    if (inputSystemConfigured) return;

    try {
        MInput.ControllerHasFocus = true;   // Tell game controller is active
        MInput.GamePads[0].Attached = true; // Mark gamepad as connected
        inputSystemConfigured = true;
    } catch (Exception ex) {
        DebugLog.Write($"Warning: {ex.Message}");
    }
}
```

This avoids NullReferenceExceptions that occur if MInput isn't fully initialized yet.

## File Structure

```
Source/
├── AutoPlayer.cs          - EverestModule entry point
├── AutoPlayer.csproj      - Project configuration
├── Playback.cs            - Input injection and hooking
├── HotkeyHandler.cs       - F7 key detection
├── InputFrame.cs          - Input sequence data structures
├── InputController.cs     - Playback state management
└── DebugLog.cs            - Debug logging system
```

## Build Instructions

```bash
cd "/Users/bagdl002/Library/Application Support/Steam/steamapps/common/Celeste/Celeste.app/Contents/Resources/Mods/AutoPlayer/Source"
~/.dotnet/dotnet build --configuration Release
```

The built DLL and zip package will be created automatically.

## Version History

- **v1.0.13** - Fixed hook execution order (inject AFTER orig, not before)
- **v1.0.12** - Fixed NullReferenceException with lazy initialization
- **v1.0.11** - Added MInput.ControllerHasFocus and GamePad.Attached configuration
- **v1.0.10+** - Complete input injection system with DPad and button flags

## Debug Logging

AutoPlayer writes debug logs to:
```
~/Library/Application Support/AutoPlayer_debug.log
```

This file contains detailed information about:
- Module loading
- Hook attachment/detachment
- Input system configuration
- Each frame's input injection
- Any errors encountered

## Future Enhancements

To add new actions:
1. Add to the `Actions` enum in `InputFrame.cs`
2. Handle in `ApplyInput()` method in `Playback.cs`
3. Create input sequences in `HotkeyHandler.cs`

Example: Adding a `Grab` action:
```csharp
// In InputFrame.cs
Grab = 1 << 6,

// In Playback.cs ApplyInput()
if (action.HasFlag(Actions.Grab)) {
    buttonFlags |= Buttons.LeftStick;
    keysToPress.Add(Keys.C);
}

// In HotkeyHandler.cs
Playback.StartAutoplay(
    new InputFrame(Actions.Grab, 30),
    // ... more frames
);
```
