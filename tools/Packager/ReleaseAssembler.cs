using System.IO.Compression;

namespace TerrariaEncyclopedia.Packager;

public sealed class ReleaseAssembler
{
    public IReadOnlyList<string> Verify(string repositoryRoot, string? terrariaPath = null)
    {
        if (string.IsNullOrWhiteSpace(repositoryRoot))
            throw new ArgumentException("A repository root is required.", nameof(repositoryRoot));

        var errors = new List<string>();
        string root = Path.GetFullPath(repositoryRoot);

        foreach (ReleaseFile file in ReleaseManifest.Files)
        {
            string sourcePath = Path.Combine(root, file.SourcePath);
            if (!File.Exists(sourcePath))
                errors.Add("Required release file is missing: " + file.SourcePath);
            if (IsForbidden(file.ArchivePath))
                errors.Add("Release manifest contains a forbidden file: " + file.ArchivePath);
        }

        string noticesPath = Path.Combine(root, "THIRD_PARTY_NOTICES.md");
        if (File.Exists(noticesPath))
        {
            string notices = File.ReadAllText(noticesPath);
            foreach (string phrase in ReleaseManifest.RequiredNoticePhrases)
            {
                if (!notices.Contains(phrase, StringComparison.Ordinal))
                    errors.Add("Third-party notices are missing required licence text: " + phrase);
            }
        }

        if (!string.IsNullOrWhiteSpace(terrariaPath))
            VerifyTerrariaInstallation(terrariaPath, errors);

        return errors;
    }

    public void Assemble(string repositoryRoot, string outputArchivePath, string? terrariaPath = null)
    {
        IReadOnlyList<string> errors = Verify(repositoryRoot, terrariaPath);
        if (errors.Count != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));

        string root = Path.GetFullPath(repositoryRoot);
        string archivePath = Path.GetFullPath(outputArchivePath);
        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);

        if (File.Exists(archivePath))
            File.Delete(archivePath);

        using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            foreach (ReleaseFile file in ReleaseManifest.Files)
            {
                string entryName = ReleaseManifest.ArchiveRoot + "/" + file.ArchivePath.Replace(Path.DirectorySeparatorChar, '/');
                ZipArchiveEntry entry = archive.CreateEntryFromFile(
                    Path.Combine(root, file.SourcePath), entryName, CompressionLevel.Optimal);
                if (string.Equals(file.ArchivePath, "launch-macos.sh", StringComparison.Ordinal))
                    entry.ExternalAttributes = unchecked((int)0x81ED0000);
            }
        }

        using ZipArchive completedArchive = ZipFile.OpenRead(archivePath);
        VerifyArchive(completedArchive);
    }

    public static void VerifyArchive(ZipArchive archive)
    {
        var expected = new HashSet<string>(ReleaseManifest.Files.Select(file =>
            ReleaseManifest.ArchiveRoot + "/" + file.ArchivePath.Replace(Path.DirectorySeparatorChar, '/')), StringComparer.Ordinal);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (!expected.Remove(entry.FullName))
                throw new InvalidOperationException("Archive contains an unapproved file: " + entry.FullName);
            if (IsForbidden(entry.FullName))
                throw new InvalidOperationException("Archive contains a forbidden file: " + entry.FullName);
        }

        if (expected.Count != 0)
            throw new InvalidOperationException("Archive is missing required files: " + string.Join(", ", expected));
    }

    private static void VerifyTerrariaInstallation(string terrariaPath, ICollection<string> errors)
    {
        string root = Path.GetFullPath(terrariaPath);
        string launcher = Path.Combine(root, "Terraria.app", "Contents", "MacOS", "Terraria");
        string runtime = Path.Combine(root, "Terraria.app", "Contents", "MacOS", "Terraria.bin.osx");
        string managedAssembly = Path.Combine(root, "Terraria.app", "Contents", "Resources", "Terraria.exe");

        foreach (string requiredFile in new[] { launcher, runtime, managedAssembly })
        {
            if (!File.Exists(requiredFile))
                errors.Add("The supplied macOS Terraria installation is missing: " + requiredFile);
        }
    }

    private static bool IsForbidden(string path)
    {
        string fileName = Path.GetFileName(path.Replace('/', Path.DirectorySeparatorChar));
        return ReleaseManifest.ForbiddenFileNames.Contains(fileName, StringComparer.OrdinalIgnoreCase) ||
               path.Contains("TerrariaModder", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("TerrariaInjector", StringComparison.OrdinalIgnoreCase);
    }
}
