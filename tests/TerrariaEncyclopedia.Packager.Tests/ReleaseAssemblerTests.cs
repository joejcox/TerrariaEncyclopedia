using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using TerrariaEncyclopedia.Packager;
using Xunit;

namespace TerrariaEncyclopedia.Packager.Tests;

public sealed class ReleaseAssemblerTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "terraria-encyclopedia-packager-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Verify_accepts_complete_project_only_release_layout()
    {
        CreateReleaseSources();

        IReadOnlyList<string> errors = new ReleaseAssembler().Verify(root);

        Assert.Empty(errors);
    }

    [Fact]
    public void Verify_reports_missing_release_file()
    {
        CreateReleaseSources();
        File.Delete(Path.Combine(root, "src", "TerrariaEncyclopedia.Host.Mono", "bin", "Release", "net48", "TerrariaEncyclopedia.dll"));

        IReadOnlyList<string> errors = new ReleaseAssembler().Verify(root);

        Assert.Contains(errors, error => error.Contains("TerrariaEncyclopedia.dll", StringComparison.Ordinal));
    }

    [Fact]
    public void Verify_rejects_missing_third_party_licence_text()
    {
        CreateReleaseSources();
        WriteFile(Path.Combine(root, "THIRD_PARTY_NOTICES.md"), "incomplete notice\n");

        IReadOnlyList<string> errors = new ReleaseAssembler().Verify(root);

        Assert.Contains(errors, error => error.Contains("licence text", StringComparison.Ordinal));
    }

    [Fact]
    public void Verify_checks_optional_macos_installation_path_without_packaging_game_files()
    {
        CreateReleaseSources();
        string terraria = Path.Combine(root, "Terraria");
        WriteFile(Path.Combine(terraria, "Terraria.app", "Contents", "MacOS", "Terraria"));
        WriteFile(Path.Combine(terraria, "Terraria.app", "Contents", "MacOS", "Terraria.bin.osx"));
        WriteFile(Path.Combine(terraria, "Terraria.app", "Contents", "Resources", "Terraria.exe"));

        IReadOnlyList<string> errors = new ReleaseAssembler().Verify(root, terraria);

        Assert.Empty(errors);
    }

    [Fact]
    public void Assemble_writes_only_manifested_project_files()
    {
        CreateReleaseSources();
        string archivePath = Path.Combine(root, "output", "release.zip");

        new ReleaseAssembler().Assemble(root, archivePath);

        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        Assert.Equal(ReleaseManifest.Files.Length, archive.Entries.Count);
        Assert.All(archive.Entries, entry =>
        {
            Assert.StartsWith(ReleaseManifest.ArchiveRoot + "/", entry.FullName, StringComparison.Ordinal);
            Assert.DoesNotContain("Terraria.exe", entry.FullName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TerrariaInjector", entry.FullName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TerrariaModder", entry.FullName, StringComparison.OrdinalIgnoreCase);
        });
        ZipArchiveEntry launcher = Assert.Single(archive.Entries, entry => entry.FullName.EndsWith("/launch-macos.sh", StringComparison.Ordinal));
        Assert.Equal(unchecked((int)0x81ED0000), launcher.ExternalAttributes);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    private void CreateReleaseSources()
    {
        foreach (ReleaseFile file in ReleaseManifest.Files)
        {
            string contents = file.SourcePath == "THIRD_PARTY_NOTICES.md"
                ? "TerrariaModder Core UI concepts\nMIT License\n"
                : "project-owned release content\n";
            WriteFile(Path.Combine(root, file.SourcePath), contents);
        }
    }

    private static void WriteFile(string path, string contents = "")
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }
}
