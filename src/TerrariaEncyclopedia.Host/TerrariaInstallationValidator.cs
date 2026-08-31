using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TerrariaEncyclopedia.Host
{
    public sealed class TerrariaInstallationValidator
    {
        public const string SupportedVersion = "1.4.5.8";

        private readonly IFileVersionReader fileVersionReader;

        public TerrariaInstallationValidator()
            : this(new FileVersionReader())
        {
        }

        public TerrariaInstallationValidator(IFileVersionReader fileVersionReader)
        {
            this.fileVersionReader = fileVersionReader ?? throw new ArgumentNullException(nameof(fileVersionReader));
        }

        public TerrariaDiscoveryResult Validate(
            string installationPath,
            TerrariaPlatform platform,
            TerrariaArchitecture architecture)
        {
            var diagnostics = new List<TerrariaDiagnostic>();
            var checkedPaths = new List<string>();

            if (string.IsNullOrWhiteSpace(installationPath))
            {
                diagnostics.Add(new TerrariaDiagnostic(
                    "installation-path-required",
                    "Supply an explicit Terraria installation directory or use standard Steam discovery."));
                return Result(null, diagnostics, checkedPaths);
            }

            PlatformInstallationLayout layout;
            if (!PlatformInstallationLayout.TryGet(platform, architecture, out layout))
            {
                diagnostics.Add(new TerrariaDiagnostic(
                    "unsupported-platform",
                    "Terraria hosting is not supported for " + platform + " " + architecture +
                    ". Supported targets are Windows x64, Linux x64, and macOS x64 or arm64."));
                return Result(null, diagnostics, checkedPaths);
            }

            string rootPath;
            try
            {
                rootPath = Path.GetFullPath(installationPath);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException || exception is PathTooLongException)
            {
                diagnostics.Add(new TerrariaDiagnostic(
                    "invalid-installation-path",
                    "The Terraria installation path is invalid: " + exception.Message,
                    installationPath));
                return Result(null, diagnostics, checkedPaths);
            }

            checkedPaths.Add(rootPath);
            if (!Directory.Exists(rootPath))
            {
                diagnostics.Add(new TerrariaDiagnostic(
                    "installation-directory-missing",
                    "The Terraria installation directory does not exist.",
                    rootPath));
                return Result(null, diagnostics, checkedPaths);
            }

            string managedAssemblyPath = Resolve(rootPath, layout.ManagedAssembly);
            string launcherPath = Resolve(rootPath, layout.Launcher);
            string runtimePath = layout.Runtime == null ? null : Resolve(rootPath, layout.Runtime);
            string[] nativeLibraryPaths = layout.NativeLibraries.Select(path => Resolve(rootPath, path)).ToArray();

            var requiredPaths = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("managed Terraria assembly", managedAssemblyPath),
                new KeyValuePair<string, string>("platform launcher", launcherPath)
            };
            if (runtimePath != null)
                requiredPaths.Add(new KeyValuePair<string, string>("bundled platform runtime", runtimePath));
            requiredPaths.AddRange(nativeLibraryPaths.Select(path =>
                new KeyValuePair<string, string>("required native library", path)));

            foreach (KeyValuePair<string, string> requiredPath in requiredPaths)
            {
                if (!checkedPaths.Contains(requiredPath.Value, StringComparer.Ordinal))
                    checkedPaths.Add(requiredPath.Value);

                if (!File.Exists(requiredPath.Value))
                {
                    diagnostics.Add(new TerrariaDiagnostic(
                        "required-file-missing",
                        "The " + requiredPath.Key + " is missing. Verify the Terraria installation in Steam and try again.",
                        requiredPath.Value));
                }
            }

            if (diagnostics.Count != 0)
                return Result(null, diagnostics, checkedPaths);

            string detectedVersion;
            try
            {
                detectedVersion = fileVersionReader.GetFileVersion(managedAssemblyPath);
            }
            catch (Exception exception)
            {
                diagnostics.Add(new TerrariaDiagnostic(
                    "version-read-failed",
                    "Could not read the Terraria managed assembly file version: " + exception.Message,
                    managedAssemblyPath));
                return Result(null, diagnostics, checkedPaths);
            }

            if (!string.Equals(detectedVersion, SupportedVersion, StringComparison.Ordinal))
            {
                diagnostics.Add(new TerrariaDiagnostic(
                    "unsupported-version",
                    "Terraria file version '" + (detectedVersion ?? "unknown") + "' is unsupported; version " +
                    SupportedVersion + " is required.",
                    managedAssemblyPath));
                return Result(null, diagnostics, checkedPaths);
            }

            var installation = new TerrariaInstallation(
                platform,
                architecture,
                rootPath,
                managedAssemblyPath,
                launcherPath,
                runtimePath,
                nativeLibraryPaths,
                detectedVersion);

            return Result(installation, diagnostics, checkedPaths);
        }

        private static string Resolve(string rootPath, string relativePath)
        {
            return Path.GetFullPath(Path.Combine(rootPath, relativePath));
        }

        private static TerrariaDiscoveryResult Result(
            TerrariaInstallation installation,
            IEnumerable<TerrariaDiagnostic> diagnostics,
            IEnumerable<string> checkedPaths)
        {
            return new TerrariaDiscoveryResult(installation, diagnostics, checkedPaths);
        }
    }
}
