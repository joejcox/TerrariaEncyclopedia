using System;
using System.Collections.Generic;
using System.IO;

namespace TerrariaEncyclopedia.Host
{
    internal sealed class PlatformInstallationLayout
    {
        private PlatformInstallationLayout(
            string managedAssembly,
            string launcher,
            string runtime,
            params string[] nativeLibraries)
        {
            ManagedAssembly = managedAssembly;
            Launcher = launcher;
            Runtime = runtime;
            NativeLibraries = nativeLibraries;
        }

        public string ManagedAssembly { get; private set; }

        public string Launcher { get; private set; }

        public string Runtime { get; private set; }

        public IReadOnlyList<string> NativeLibraries { get; private set; }

        public static bool TryGet(
            TerrariaPlatform platform,
            TerrariaArchitecture architecture,
            out PlatformInstallationLayout layout)
        {
            layout = null;

            if (platform == TerrariaPlatform.Windows && architecture == TerrariaArchitecture.X64)
            {
                layout = new PlatformInstallationLayout("Terraria.exe", "Terraria.exe", null);
                return true;
            }

            if (platform == TerrariaPlatform.Linux && architecture == TerrariaArchitecture.X64)
            {
                layout = new PlatformInstallationLayout(
                    "Terraria.exe",
                    "Terraria",
                    "Terraria.bin.x86_64",
                    Combine("lib64", "libFAudio.so.0"),
                    Combine("lib64", "libFNA3D.so.0"),
                    Combine("lib64", "libSDL3.so.0"));
                return true;
            }

            if (platform == TerrariaPlatform.MacOS &&
                (architecture == TerrariaArchitecture.X64 || architecture == TerrariaArchitecture.Arm64))
            {
                string resources = Combine("Terraria.app", "Contents", "Resources");
                string macOS = Combine("Terraria.app", "Contents", "MacOS");
                string native = Combine(macOS, "osx");
                layout = new PlatformInstallationLayout(
                    Combine(resources, "Terraria.exe"),
                    Combine(macOS, "Terraria"),
                    Combine(macOS, "Terraria.bin.osx"),
                    Combine(native, "libFAudio.0.dylib"),
                    Combine(native, "libFNA3D.0.dylib"),
                    Combine(native, "libSDL3.0.dylib"));
                return true;
            }

            return false;
        }

        private static string Combine(params string[] parts)
        {
            string path = parts[0];
            for (int index = 1; index < parts.Length; index++)
                path = Path.Combine(path, parts[index]);
            return path;
        }
    }
}
