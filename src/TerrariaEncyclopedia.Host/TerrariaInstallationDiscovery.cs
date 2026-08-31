using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TerrariaEncyclopedia.Host
{
    public sealed class TerrariaInstallationDiscovery
    {
        private readonly TerrariaInstallationValidator validator;

        public TerrariaInstallationDiscovery()
            : this(new TerrariaInstallationValidator())
        {
        }

        public TerrariaInstallationDiscovery(TerrariaInstallationValidator validator)
        {
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public TerrariaDiscoveryResult Discover(TerrariaDiscoveryOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            PlatformInstallationLayout layout;
            if (!PlatformInstallationLayout.TryGet(options.Platform, options.Architecture, out layout))
            {
                return new TerrariaDiscoveryResult(
                    null,
                    new[]
                    {
                        new TerrariaDiagnostic(
                            "unsupported-platform",
                            "Terraria hosting is not supported for " + options.Platform + " " + options.Architecture +
                            ". Supported targets are Windows x64, Linux x64, and macOS x64 or arm64.")
                    },
                    Array.Empty<string>());
            }

            if (!string.IsNullOrWhiteSpace(options.ExplicitInstallationPath))
                return validator.Validate(options.ExplicitInstallationPath, options.Platform, options.Architecture);

            var candidateDiagnostics = new List<TerrariaDiagnostic>();
            var checkedPaths = new List<string>();
            IReadOnlyList<string> candidates = GetStandardInstallationPaths(options, candidateDiagnostics);
            var diagnostics = new List<TerrariaDiagnostic>(candidateDiagnostics);

            foreach (string candidate in candidates)
            {
                TerrariaDiscoveryResult result = validator.Validate(candidate, options.Platform, options.Architecture);
                AddDistinct(checkedPaths, result.CheckedPaths);
                if (result.Success)
                {
                    return new TerrariaDiscoveryResult(
                        result.Installation,
                        candidateDiagnostics.Concat(result.Diagnostics),
                        checkedPaths);
                }
                diagnostics.AddRange(result.Diagnostics);
            }

            string target = options.Platform + " " + options.Architecture;
            string paths = candidates.Count == 0
                ? "no standard paths were available"
                : string.Join(", ", candidates.Select(path => "'" + path + "'"));
            diagnostics.Add(new TerrariaDiagnostic(
                "installation-not-found",
                "No compatible Terraria " + TerrariaInstallationValidator.SupportedVersion + " installation was found for " +
                target + ". Checked " + paths + ". Supply ExplicitInstallationPath for a non-standard Steam library."));

            return new TerrariaDiscoveryResult(null, diagnostics, checkedPaths);
        }

        public IReadOnlyList<string> GetStandardInstallationPaths(TerrariaDiscoveryOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return GetStandardInstallationPaths(options, new List<TerrariaDiagnostic>());
        }

        private static IReadOnlyList<string> GetStandardInstallationPaths(
            TerrariaDiscoveryOptions options,
            ICollection<TerrariaDiagnostic> diagnostics)
        {
            var candidates = new List<string>();
            string home = options.HomeDirectory;
            if (string.IsNullOrWhiteSpace(home))
                home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (options.Platform == TerrariaPlatform.Windows)
            {
                string programFilesX86 = options.ProgramFilesX86Directory;
                if (string.IsNullOrWhiteSpace(programFilesX86))
                    programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (!string.IsNullOrWhiteSpace(programFilesX86))
                {
                    AddStandardRoot(
                        candidates,
                        diagnostics,
                        programFilesX86,
                        "ProgramFilesX86Directory",
                        "Supply a valid ProgramFilesX86Directory override or ExplicitInstallationPath.",
                        "Steam");
                }
            }
            else if (options.Platform == TerrariaPlatform.Linux && !string.IsNullOrWhiteSpace(home))
            {
                AddStandardRoot(
                    candidates,
                    diagnostics,
                    home,
                    "HomeDirectory",
                    "Supply a valid HomeDirectory override or ExplicitInstallationPath.",
                    ".local", "share", "Steam");
                AddStandardRoot(candidates, null, home, null, null, ".steam", "steam");
                AddStandardRoot(candidates, null, home, null, null, ".steam", "debian-installation");
            }
            else if (options.Platform == TerrariaPlatform.MacOS && !string.IsNullOrWhiteSpace(home))
            {
                AddStandardRoot(
                    candidates,
                    diagnostics,
                    home,
                    "HomeDirectory",
                    "Supply a valid HomeDirectory override or ExplicitInstallationPath.",
                    "Library", "Application Support", "Steam");
            }

            if (options.AdditionalSteamLibraryPaths != null)
            {
                foreach (string path in options.AdditionalSteamLibraryPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
                {
                    AddStandardRoot(
                        candidates,
                        diagnostics,
                        path,
                        "AdditionalSteamLibraryPaths",
                        "Remove or correct this AdditionalSteamLibraryPaths entry, or supply ExplicitInstallationPath.");
                }
            }

            StringComparer comparer = options.Platform == TerrariaPlatform.Windows
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

            return candidates.Distinct(comparer).ToArray();
        }

        private static void AddStandardRoot(
            ICollection<string> candidates,
            ICollection<TerrariaDiagnostic> diagnostics,
            string rootPath,
            string optionName,
            string remedy,
            params string[] relativeSteamRootSegments)
        {
            try
            {
                string steamRoot = relativeSteamRootSegments.Length == 0
                    ? rootPath
                    : Path.Combine(new[] { rootPath }.Concat(relativeSteamRootSegments).ToArray());
                candidates.Add(Path.GetFullPath(Path.Combine(steamRoot, "steamapps", "common", "Terraria")));
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is NotSupportedException ||
                exception is PathTooLongException)
            {
                if (diagnostics != null)
                {
                    diagnostics.Add(new TerrariaDiagnostic(
                        "invalid-installation-path",
                        "The " + optionName + " path used for Steam discovery is invalid: " + exception.Message + " " + remedy,
                        rootPath));
                }
            }
        }

        private static void AddDistinct(ICollection<string> target, IEnumerable<string> values)
        {
            foreach (string value in values)
            {
                if (!target.Contains(value))
                    target.Add(value);
            }
        }
    }
}
