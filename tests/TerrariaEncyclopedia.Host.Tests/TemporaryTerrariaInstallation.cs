using System;
using System.Collections.Generic;
using System.IO;

namespace TerrariaEncyclopedia.Host.Tests
{
    internal sealed class TemporaryTerrariaInstallation : IDisposable
    {
        private static readonly IReadOnlyDictionary<string, string[]> RequiredFiles =
            new Dictionary<string, string[]>
            {
                ["Windows-X64"] = new[] { "Terraria.exe" },
                ["Linux-X64"] = new[]
                {
                    "Terraria.exe",
                    "Terraria",
                    "Terraria.bin.x86_64",
                    Path.Combine("lib64", "libFAudio.so.0"),
                    Path.Combine("lib64", "libFNA3D.so.0"),
                    Path.Combine("lib64", "libSDL3.so.0")
                },
                ["MacOS-X64"] = MacFiles(),
                ["MacOS-Arm64"] = MacFiles()
            };

        public TemporaryTerrariaInstallation()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "TerrariaEncyclopedia.Host.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; private set; }

        public string CreateLayout(TerrariaPlatform platform, TerrariaArchitecture architecture, string rootPath = null)
        {
            string installationRoot = rootPath ?? RootPath;
            Directory.CreateDirectory(installationRoot);
            foreach (string relativePath in RequiredFiles[platform + "-" + architecture])
                CreateFile(Path.Combine(installationRoot, relativePath));
            return installationRoot;
        }

        public void CreateFile(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, "test fixture");
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
                Directory.Delete(RootPath, true);
        }

        private static string[] MacFiles()
        {
            string contents = Path.Combine("Terraria.app", "Contents");
            string macOS = Path.Combine(contents, "MacOS");
            string native = Path.Combine(macOS, "osx");
            return new[]
            {
                Path.Combine(contents, "Resources", "Terraria.exe"),
                Path.Combine(macOS, "Terraria"),
                Path.Combine(macOS, "Terraria.bin.osx"),
                Path.Combine(native, "libFAudio.0.dylib"),
                Path.Combine(native, "libFNA3D.0.dylib"),
                Path.Combine(native, "libSDL3.0.dylib")
            };
        }
    }

    internal sealed class StubFileVersionReader : IFileVersionReader
    {
        private readonly string version;

        public StubFileVersionReader(string version)
        {
            this.version = version;
        }

        public string GetFileVersion(string path)
        {
            return version;
        }
    }
}
