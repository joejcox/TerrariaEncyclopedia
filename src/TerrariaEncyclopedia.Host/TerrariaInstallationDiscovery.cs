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

            var diagnostics = new List<TerrariaDiagnostic>();
            var checkedPaths = new List<string>();
            IReadOnlyList<string> candidates = GetStandardInstallationPaths(options);

            foreach (string candidate in candidates)
            {
                TerrariaDiscoveryResult result = validator.Validate(candidate, options.Platform, options.Architecture);
                AddDistinct(checkedPaths, result.CheckedPaths);
                if (result.Success)
                    return new TerrariaDiscoveryResult(result.Installation, result.Diagnostics, checkedPaths);
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

            var steamLibraryPaths = new List<string>();
            string home = options.HomeDirectory;
            if (string.IsNullOrWhiteSpace(home))
                home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (options.Platform == TerrariaPlatform.Windows)
            {
                string programFilesX86 = options.ProgramFilesX86Directory;
                if (string.IsNullOrWhiteSpace(programFilesX86))
                    programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (!string.IsNullOrWhiteSpace(programFilesX86))
                    steamLibraryPaths.Add(Path.Combine(programFilesX86, "Steam"));
            }
            else if (options.Platform == TerrariaPlatform.Linux && !string.IsNullOrWhiteSpace(home))
            {
                steamLibraryPaths.Add(Path.Combine(home, ".local", "share", "Steam"));
                steamLibraryPaths.Add(Path.Combine(home, ".steam", "steam"));
                steamLibraryPaths.Add(Path.Combine(home, ".steam", "debian-installation"));
            }
            else if (options.Platform == TerrariaPlatform.MacOS && !string.IsNullOrWhiteSpace(home))
            {
                steamLibraryPaths.Add(Path.Combine(home, "Library", "Application Support", "Steam"));
            }

            if (options.AdditionalSteamLibraryPaths != null)
                steamLibraryPaths.AddRange(options.AdditionalSteamLibraryPaths.Where(path => !string.IsNullOrWhiteSpace(path)));

            StringComparer comparer = options.Platform == TerrariaPlatform.Windows
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

            return steamLibraryPaths
                .Select(path => Path.GetFullPath(Path.Combine(path, "steamapps", "common", "Terraria")))
                .Distinct(comparer)
                .ToArray();
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
