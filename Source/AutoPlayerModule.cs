using System;
using System.Reflection;
using Monocle;

namespace Celeste.Mod.AutoPlayer {

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
            DebugLog.Write("[CTOR] AutoPlayerModule constructor called");
            Logger.Log(nameof(AutoPlayerModule), "[CTOR] AutoPlayerModule constructor called");
        }

        public override void Load() {
            try {
                // Log version on load
                var version = typeof(AutoPlayerModule).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                             ?? typeof(AutoPlayerModule).Assembly.GetName().Version?.ToString()
                             ?? "unknown";
                DebugLog.Write($"[LOAD] AutoPlayer v{version} loaded");
                Logger.Log(nameof(AutoPlayerModule), $"[LOAD] AutoPlayer v{version} loaded");

                // Initialize the playback system hooks
                DebugLog.Write("[LOAD] Initializing playback system...");
                Logger.Log(nameof(AutoPlayerModule), "[LOAD] Initializing playback system...");
                Playback.Initialize();
                DebugLog.Write("[LOAD] Playback system initialized");
                Logger.Log(nameof(AutoPlayerModule), "[LOAD] Playback system initialized");

                // Initialize keyboard hotkey handler
                DebugLog.Write("[LOAD] Initializing hotkey handler...");
                Logger.Log(nameof(AutoPlayerModule), "[LOAD] Initializing hotkey handler...");
                HotkeyHandler.Initialize();
                DebugLog.Write("[LOAD] Hotkey handler initialized");
                Logger.Log(nameof(AutoPlayerModule), "[LOAD] Hotkey handler initialized");

                // Initialize game state monitor
                DebugLog.Write("[LOAD] Initializing game state monitor...");
                Logger.Log(nameof(AutoPlayerModule), "[LOAD] Initializing game state monitor...");
                GameStateMonitor.Initialize();
                DebugLog.Write("[LOAD] AutoPlayer ready - press F7 to toggle");
                Logger.Log(nameof(AutoPlayerModule), "[LOAD] AutoPlayer ready - press F7 to toggle");
            } catch (Exception ex) {
                DebugLog.WriteException("Load", ex);
                Logger.Log(nameof(AutoPlayerModule), $"[ERROR] Exception in Load(): {ex}");
            }
        }


        public override void Unload() {
            // Stop autoplay and cleanup hooks on unload
            Playback.StopAutoplay();
            Playback.Cleanup();
            HotkeyHandler.Cleanup();
            GameStateMonitor.Cleanup();
        }
    }
}