using System;
using System.IO;

namespace Celeste.Mod.AutoPlayer {

    /// Simple debug logger that writes directly to a file
    internal static class DebugLog {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutoPlayer_debug.log"
        );

        static DebugLog() {
            // Create log file on first use
            try {
                File.WriteAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] AutoPlayer Debug Log Started\n");
            } catch {
                // If we can't write, that's ok - just skip logging
            }
        }

        internal static void Write(string message) {
            try {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                File.AppendAllText(LogPath, $"[{timestamp}] {message}\n");
            } catch {
                // Silently fail if we can't write
            }
        }

        internal static void WriteException(string context, Exception ex) {
            Write($"[ERROR {context}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
