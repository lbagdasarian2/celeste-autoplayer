using System;
using System.Reflection;

namespace Celeste.Mod.AutoPlayer;

public class AutoPlayerModule : EverestModule {
    public static AutoPlayerModule Instance { get; private set; }

    public override Type SettingsType => typeof(AutoPlayerModuleSettings);
    public static AutoPlayerModuleSettings Settings => (AutoPlayerModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(AutoPlayerModuleSession);
    public static AutoPlayerModuleSession Session => (AutoPlayerModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(AutoPlayerModuleSaveData);
    public static AutoPlayerModuleSaveData SaveData => (AutoPlayerModuleSaveData) Instance._SaveData;

    public AutoPlayerModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(AutoPlayerModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(AutoPlayerModule), LogLevel.Info);
#endif
    }

    public override void Load() {
        // Log version on load
        var version = typeof(AutoPlayerModule).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                     ?? typeof(AutoPlayerModule).Assembly.GetName().Version?.ToString()
                     ?? "unknown";
        Logger.Log(nameof(AutoPlayerModule), $"AutoPlayer v{version} loaded");

        // Initialize the playback system hooks
        Playback.Initialize();

        // Initialize the autoplayer sequence: move right for 15 frames, then jump for 1 frame
        Playback.StartAutoplay(
            new InputFrame(Actions.Right, 15),
            new InputFrame(Actions.Jump, 1)
        );
    }

    public override void Unload() {
        // Stop autoplay and cleanup hooks on unload
        Playback.StopAutoplay();
        Playback.Cleanup();
    }
}