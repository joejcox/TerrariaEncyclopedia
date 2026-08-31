using System;
using System.IO;
using System.Linq;
using Xunit;

namespace TerrariaEncyclopedia.Host.Tests
{
    public sealed class TerrariaInstallationDiscoveryTests
    {
        [Theory]
        [InlineData(TerrariaPlatform.Windows, TerrariaArchitecture.X64)]
        [InlineData(TerrariaPlatform.Linux, TerrariaArchitecture.X64)]
        [InlineData(TerrariaPlatform.MacOS, TerrariaArchitecture.X64)]
        [InlineData(TerrariaPlatform.MacOS, TerrariaArchitecture.Arm64)]
        public void Explicit_path_resolves_supported_platform_layouts(
            TerrariaPlatform platform,
            TerrariaArchitecture architecture)
        {
            using (var fixture = new TemporaryTerrariaInstallation())
            {
                fixture.CreateLayout(platform, architecture);
                var discovery = CreateDiscovery(TerrariaInstallationValidator.SupportedVersion);

                TerrariaDiscoveryResult result = discovery.Discover(new TerrariaDiscoveryOptions(platform, architecture)
                {
                    ExplicitInstallationPath = fixture.RootPath
                });

                Assert.True(result.Success);
                Assert.Empty(result.Diagnostics);
                Assert.Equal(Path.GetFullPath(fixture.RootPath), result.Installation.RootPath);
                Assert.Equal(platform, result.Installation.Platform);
                Assert.Equal(architecture, result.Installation.Architecture);
                Assert.Equal(TerrariaInstallationValidator.SupportedVersion, result.Installation.Version);
                Assert.Contains(result.Installation.ManagedAssemblyPath, result.CheckedPaths);
                Assert.Contains(result.Installation.LauncherPath, result.CheckedPaths);
            }
        }

        [Fact]
        public void Linux_record_contains_runtime_and_native_library_paths()
        {
            using (var fixture = new TemporaryTerrariaInstallation())
            {
                fixture.CreateLayout(TerrariaPlatform.Linux, TerrariaArchitecture.X64);

                TerrariaDiscoveryResult result = CreateDiscovery("1.4.5.8").Discover(
                    new TerrariaDiscoveryOptions(TerrariaPlatform.Linux, TerrariaArchitecture.X64)
                    {
                        ExplicitInstallationPath = fixture.RootPath
                    });

                Assert.EndsWith("Terraria.bin.x86_64", result.Installation.RuntimePath);
                Assert.Equal(3, result.Installation.NativeLibraryPaths.Count);
                Assert.All(result.Installation.NativeLibraryPaths, path => Assert.True(File.Exists(path)));
            }
        }

        [Theory]
        [InlineData(TerrariaPlatform.Windows, TerrariaArchitecture.X64)]
        [InlineData(TerrariaPlatform.Linux, TerrariaArchitecture.X64)]
        [InlineData(TerrariaPlatform.MacOS, TerrariaArchitecture.Arm64)]
        public void Standard_steam_layout_is_discovered(
            TerrariaPlatform platform,
            TerrariaArchitecture architecture)
        {
            using (var fixture = new TemporaryTerrariaInstallation())
            {
                string steamRoot = GetSteamRoot(fixture.RootPath, platform);
                string installationRoot = Path.Combine(steamRoot, "steamapps", "common", "Terraria");
                fixture.CreateLayout(platform, architecture, installationRoot);
                var options = new TerrariaDiscoveryOptions(platform, architecture)
                {
                    HomeDirectory = fixture.RootPath,
                    ProgramFilesX86Directory = fixture.RootPath
                };

                TerrariaDiscoveryResult result = CreateDiscovery("1.4.5.8").Discover(options);

                Assert.True(result.Success);
                Assert.Equal(Path.GetFullPath(installationRoot), result.Installation.RootPath);
            }
        }

        [Fact]
        public void Additional_steam_library_is_discovered()
        {
            using (var fixture = new TemporaryTerrariaInstallation())
            {
                string library = Path.Combine(fixture.RootPath, "external-library");
                string installation = Path.Combine(library, "steamapps", "common", "Terraria");
                fixture.CreateLayout(TerrariaPlatform.Linux, TerrariaArchitecture.X64, installation);

                TerrariaDiscoveryResult result = CreateDiscovery("1.4.5.8").Discover(
                    new TerrariaDiscoveryOptions(TerrariaPlatform.Linux, TerrariaArchitecture.X64)
                    {
                        HomeDirectory = Path.Combine(fixture.RootPath, "empty-home"),
                        AdditionalSteamLibraryPaths = new[] { library }
                    });

                Assert.True(result.Success);
                Assert.Equal(Path.GetFullPath(installation), result.Installation.RootPath);
                Assert.Empty(result.Diagnostics);
            }
        }

        [Fact]
        public void Invalid_additional_library_does_not_prevent_later_valid_library_discovery()
        {
            using (var fixture = new TemporaryTerrariaInstallation())
            {
                const string invalidLibrary = "invalid\0library";
                string validLibrary = Path.Combine(fixture.RootPath, "valid-library");
                string installation = Path.Combine(validLibrary, "steamapps", "common", "Terraria");
                fixture.CreateLayout(TerrariaPlatform.Linux, TerrariaArchitecture.X64, installation);

                TerrariaDiscoveryResult result = CreateDiscovery("1.4.5.8").Discover(
                    new TerrariaDiscoveryOptions(TerrariaPlatform.Linux, TerrariaArchitecture.X64)
                    {
                        HomeDirectory = Path.Combine(fixture.RootPath, "empty-home"),
                        AdditionalSteamLibraryPaths = new[] { invalidLibrary, validLibrary }
                    });

                Assert.True(result.Success);
                Assert.Equal(Path.GetFullPath(installation), result.Installation.RootPath);
                TerrariaDiagnostic diagnostic = Assert.Single(result.Diagnostics);
                Assert.Equal("invalid-installation-path", diagnostic.Code);
                Assert.Equal(invalidLibrary, diagnostic.Path);
                Assert.Contains("AdditionalSteamLibraryPaths", diagnostic.Message);
                Assert.Contains("correct", diagnostic.Message);
            }
        }

        [Fact]
        public void Invalid_home_directory_override_returns_actionable_diagnostic()
        {
            const string invalidHome = "invalid\0home";

            TerrariaDiscoveryResult result = CreateDiscovery("1.4.5.8").Discover(
                new TerrariaDiscoveryOptions(TerrariaPlatform.Linux, TerrariaArchitecture.X64)
                {
                    HomeDirectory = invalidHome
                });

            Assert.False(result.Success);
            TerrariaDiagnostic diagnostic = Assert.Single(
                result.Diagnostics,
                item => item.Code == "invalid-installation-path");
            Assert.Equal(invalidHome, diagnostic.Path);
            Assert.Contains("HomeDirectory", diagnostic.Message);
            Assert.Contains("valid HomeDirectory", diagnostic.Message);
            Assert.Contains(result.Diagnostics, item => item.Code == "installation-not-found");
            Assert.Empty(result.CheckedPaths);
        }

        [Fact]
        public void Invalid_program_files_override_returns_actionable_diagnostic()
        {
            const string invalidProgramFiles = "invalid\0program-files";

            TerrariaDiscoveryResult result = CreateDiscovery("1.4.5.8").Discover(
                new TerrariaDiscoveryOptions(TerrariaPlatform.Windows, TerrariaArchitecture.X64)
                {
                    ProgramFilesX86Directory = invalidProgramFiles
                });

            Assert.False(result.Success);
            TerrariaDiagnostic diagnostic = Assert.Single(
                result.Diagnostics,
                item => item.Code == "invalid-installation-path");
            Assert.Equal(invalidProgramFiles, diagnostic.Path);
            Assert.Contains("ProgramFilesX86Directory", diagnostic.Message);
            Assert.Contains("valid ProgramFilesX86Directory", diagnostic.Message);
            Assert.Contains(result.Diagnostics, item => item.Code == "installation-not-found");
            Assert.Empty(result.CheckedPaths);
        }

        [Fact]
        public void Missing_required_file_reports_its_checked_path_and_remedy()
        {
            using (var fixture = new TemporaryTerrariaInstallation())
            {
                fixture.CreateLayout(TerrariaPlatform.Linux, TerrariaArchitecture.X64);
                string missingPath = Path.Combine(fixture.RootPath, "lib64", "libFNA3D.so.0");
                File.Delete(missingPath);

                TerrariaDiscoveryResult result = CreateDiscovery("1.4.5.8").Discover(
                    new TerrariaDiscoveryOptions(TerrariaPlatform.Linux, TerrariaArchitecture.X64)
                    {
                        ExplicitInstallationPath = fixture.RootPath
                    });

                Assert.False(result.Success);
                TerrariaDiagnostic diagnostic = Assert.Single(result.Diagnostics);
                Assert.Equal("required-file-missing", diagnostic.Code);
                Assert.Equal(Path.GetFullPath(missingPath), diagnostic.Path);
                Assert.Contains("Verify the Terraria installation in Steam", diagnostic.Message);
                Assert.Contains(Path.GetFullPath(missingPath), result.CheckedPaths);
            }
        }

        [Fact]
        public void Wrong_managed_assembly_file_version_is_rejected()
        {
            using (var fixture = new TemporaryTerrariaInstallation())
            {
                fixture.CreateLayout(TerrariaPlatform.Windows, TerrariaArchitecture.X64);

                TerrariaDiscoveryResult result = CreateDiscovery("1.4.5.7").Discover(
                    new TerrariaDiscoveryOptions(TerrariaPlatform.Windows, TerrariaArchitecture.X64)
                    {
                        ExplicitInstallationPath = fixture.RootPath
                    });

                Assert.False(result.Success);
                TerrariaDiagnostic diagnostic = Assert.Single(result.Diagnostics);
                Assert.Equal("unsupported-version", diagnostic.Code);
                Assert.Contains("1.4.5.7", diagnostic.Message);
                Assert.Contains("1.4.5.8", diagnostic.Message);
                Assert.Equal(result.CheckedPaths.Single(path => path.EndsWith("Terraria.exe")), diagnostic.Path);
            }
        }

        [Theory]
        [InlineData(TerrariaPlatform.Windows, TerrariaArchitecture.Arm64)]
        [InlineData(TerrariaPlatform.Linux, TerrariaArchitecture.Arm64)]
        public void Unsupported_platform_architecture_returns_actionable_diagnostic(
            TerrariaPlatform platform,
            TerrariaArchitecture architecture)
        {
            using (var fixture = new TemporaryTerrariaInstallation())
            {
                TerrariaDiscoveryResult result = CreateDiscovery("1.4.5.8").Discover(
                    new TerrariaDiscoveryOptions(platform, architecture)
                    {
                        ExplicitInstallationPath = fixture.RootPath
                    });

                Assert.False(result.Success);
                TerrariaDiagnostic diagnostic = Assert.Single(result.Diagnostics);
                Assert.Equal("unsupported-platform", diagnostic.Code);
                Assert.Contains("Supported targets are Windows x64, Linux x64, and macOS x64 or arm64", diagnostic.Message);
            }
        }

        [Fact]
        public void Unsupported_platform_is_reported_before_standard_path_discovery()
        {
            TerrariaDiscoveryResult result = CreateDiscovery("1.4.5.8").Discover(
                new TerrariaDiscoveryOptions(TerrariaPlatform.Windows, TerrariaArchitecture.Arm64)
                {
                    HomeDirectory = string.Empty,
                    ProgramFilesX86Directory = string.Empty
                });

            Assert.False(result.Success);
            TerrariaDiagnostic diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("unsupported-platform", diagnostic.Code);
            Assert.Empty(result.CheckedPaths);
        }

        [Fact]
        public void Missing_standard_layout_lists_candidate_paths_and_explicit_path_remedy()
        {
            using (var fixture = new TemporaryTerrariaInstallation())
            {
                TerrariaDiscoveryResult result = CreateDiscovery("1.4.5.8").Discover(
                    new TerrariaDiscoveryOptions(TerrariaPlatform.MacOS, TerrariaArchitecture.X64)
                    {
                        HomeDirectory = fixture.RootPath
                    });

                Assert.False(result.Success);
                TerrariaDiagnostic summary = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == "installation-not-found");
                string candidate = Path.Combine(
                    fixture.RootPath,
                    "Library",
                    "Application Support",
                    "Steam",
                    "steamapps",
                    "common",
                    "Terraria");
                Assert.Contains(Path.GetFullPath(candidate), summary.Message);
                Assert.Contains("ExplicitInstallationPath", summary.Message);
                Assert.Contains(Path.GetFullPath(candidate), result.CheckedPaths);
            }
        }

        private static TerrariaInstallationDiscovery CreateDiscovery(string version)
        {
            return new TerrariaInstallationDiscovery(
                new TerrariaInstallationValidator(new StubFileVersionReader(version)));
        }

        private static string GetSteamRoot(string root, TerrariaPlatform platform)
        {
            if (platform == TerrariaPlatform.Windows)
                return Path.Combine(root, "Steam");
            if (platform == TerrariaPlatform.Linux)
                return Path.Combine(root, ".local", "share", "Steam");
            return Path.Combine(root, "Library", "Application Support", "Steam");
        }
    }
}
