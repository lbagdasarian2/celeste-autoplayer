namespace Celeste.Mod.AutoPlayer {

    /// <summary>
    /// Configuration for AI decision source
    /// Controls whether to use the HTTP-based AIDecisionService or the local AIDecisionMaker
    /// </summary>
    internal static class AISourceConfig {
        /// <summary>
        /// If true, use the HTTP-based AIDecisionService on localhost:5001
        /// If false, use the local AIDecisionMaker class
        /// </summary>
        internal static bool UseRemoteAIService { get; set; } = true;

        internal static void Initialize() {
            DebugLog.Write($"[AISourceConfig] AI Source: {(UseRemoteAIService ? "Remote HTTP Service" : "Local AIDecisionMaker")}");
            Logger.Log(nameof(AutoPlayerModule), $"[AISourceConfig] Using {(UseRemoteAIService ? "Remote HTTP Service on http://localhost:5001" : "Local AIDecisionMaker")}");
        }

        internal static void SetUseRemoteService(bool useRemote) {
            UseRemoteAIService = useRemote;
            DebugLog.Write($"[AISourceConfig] AI Source changed to: {(UseRemoteAIService ? "Remote HTTP Service" : "Local AIDecisionMaker")}");
            Logger.Log(nameof(AutoPlayerModule), $"[AISourceConfig] AI Source changed to {(UseRemoteAIService ? "Remote HTTP Service on http://localhost:5001" : "Local AIDecisionMaker")}");
        }
    }
}
