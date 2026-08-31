using System;
using System.Diagnostics;

namespace TerrariaEncyclopedia.Compatibility
{
    /// <summary>
    /// The exact Terraria build this mod has been verified against. Phase 1 must never
    /// initialize UI/patches against an unverified version — fail safe instead
    /// (see project.md: "Do NOT let an unsupported version blindly run patches").
    /// </summary>
    public static class GameVersionGuard
    {
        public const string ExpectedVersion = "1.4.5.8";

        public static string DetectedVersion { get; private set; } = "unknown";
        public static bool IsSupported { get; private set; }

        public static bool Check()
        {
            try
            {
                string location = typeof(Terraria.Main).Assembly.Location;
                DetectedVersion = FileVersionInfo.GetVersionInfo(location).FileVersion ?? "unknown";
            }
            catch (Exception)
            {
                DetectedVersion = "unknown";
            }

            IsSupported = DetectedVersion == ExpectedVersion;
            return IsSupported;
        }
    }
}
