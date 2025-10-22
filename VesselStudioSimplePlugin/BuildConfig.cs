namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Build configuration helpers for DEV vs RELEASE builds
    /// </summary>
    public static class BuildConfig
    {
#if DEV
        public const string CommandPrefix = "Dev";
        public const string DisplayName = "Vessel Studio DEV";
        public const bool IsDev = true;
#else
        public const string CommandPrefix = "";
        public const string DisplayName = "Vessel Studio";
        public const bool IsDev = false;
#endif
        
        /// <summary>
        /// Get command name with appropriate prefix
        /// </summary>
        public static string GetCommandName(string baseName)
        {
            return CommandPrefix + baseName;
        }
    }
}
