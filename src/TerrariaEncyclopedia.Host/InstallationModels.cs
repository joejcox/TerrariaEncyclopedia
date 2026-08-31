using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TerrariaEncyclopedia.Host
{
    public enum TerrariaPlatform
    {
        Windows,
        Linux,
        MacOS
    }

    public enum TerrariaArchitecture
    {
        X64,
        Arm64
    }

    public sealed class TerrariaInstallation
    {
        internal TerrariaInstallation(
            TerrariaPlatform platform,
            TerrariaArchitecture architecture,
            string rootPath,
            string managedAssemblyPath,
            string launcherPath,
            string runtimePath,
            IEnumerable<string> nativeLibraryPaths,
            string version)
        {
            Platform = platform;
            Architecture = architecture;
            RootPath = rootPath;
            ManagedAssemblyPath = managedAssemblyPath;
            LauncherPath = launcherPath;
            RuntimePath = runtimePath;
            NativeLibraryPaths = ReadOnly(nativeLibraryPaths);
            Version = version;
        }

        public TerrariaPlatform Platform { get; private set; }

        public TerrariaArchitecture Architecture { get; private set; }

        public string RootPath { get; private set; }

        public string ManagedAssemblyPath { get; private set; }

        public string LauncherPath { get; private set; }

        /// <summary>
        /// The bundled runtime executable, when the platform ships one separately from its launcher.
        /// Windows uses the managed launcher directly, so this value is null there.
        /// </summary>
        public string RuntimePath { get; private set; }

        public IReadOnlyList<string> NativeLibraryPaths { get; private set; }

        public string Version { get; private set; }

        private static IReadOnlyList<string> ReadOnly(IEnumerable<string> values)
        {
            return new ReadOnlyCollection<string>(values.ToArray());
        }
    }

    public sealed class TerrariaDiagnostic
    {
        public TerrariaDiagnostic(string code, string message, string path = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("A diagnostic code is required.", nameof(code));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("A diagnostic message is required.", nameof(message));

            Code = code;
            Message = message;
            Path = path;
        }

        public string Code { get; private set; }

        public string Message { get; private set; }

        public string Path { get; private set; }

        public override string ToString()
        {
            return Path == null ? Message : Message + " Path: '" + Path + "'.";
        }
    }

    public sealed class TerrariaDiscoveryResult
    {
        internal TerrariaDiscoveryResult(
            TerrariaInstallation installation,
            IEnumerable<TerrariaDiagnostic> diagnostics,
            IEnumerable<string> checkedPaths)
        {
            Installation = installation;
            Diagnostics = new ReadOnlyCollection<TerrariaDiagnostic>(diagnostics.ToArray());
            CheckedPaths = new ReadOnlyCollection<string>(checkedPaths.ToArray());
        }

        public bool Success { get { return Installation != null; } }

        public TerrariaInstallation Installation { get; private set; }

        public IReadOnlyList<TerrariaDiagnostic> Diagnostics { get; private set; }

        public IReadOnlyList<string> CheckedPaths { get; private set; }
    }

    public sealed class TerrariaDiscoveryOptions
    {
        public TerrariaDiscoveryOptions(TerrariaPlatform platform, TerrariaArchitecture architecture)
        {
            Platform = platform;
            Architecture = architecture;
            AdditionalSteamLibraryPaths = Array.Empty<string>();
        }

        public TerrariaPlatform Platform { get; private set; }

        public TerrariaArchitecture Architecture { get; private set; }

        /// <summary>A Terraria installation directory. When set, standard locations are not searched.</summary>
        public string ExplicitInstallationPath { get; set; }

        /// <summary>Overrides the current home directory, primarily for deterministic discovery and tests.</summary>
        public string HomeDirectory { get; set; }

        /// <summary>Overrides Program Files (x86) when discovering a Windows Steam installation.</summary>
        public string ProgramFilesX86Directory { get; set; }

        /// <summary>Additional Steam library roots containing a steamapps directory.</summary>
        public IEnumerable<string> AdditionalSteamLibraryPaths { get; set; }
    }
}
