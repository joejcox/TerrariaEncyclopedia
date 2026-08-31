namespace TerrariaEncyclopedia.Packager;

public static class ReleaseManifest
{
    public const string ArchiveRoot = "TerrariaEncyclopedia-macos";

    public static readonly ReleaseFile[] Files =
    {
        new("tools/Packager/macos-release-launcher.sh", "launch-macos.sh"),
        new("src/TerrariaEncyclopedia.Host.Mono/bin/Release/net48/TerrariaEncyclopedia.Host.Mono.exe", "TerrariaEncyclopedia.Host.Mono.exe"),
        new("src/TerrariaEncyclopedia.Host.Mono/bin/Release/net48/TerrariaEncyclopedia.Runtime.dll", "TerrariaEncyclopedia.Runtime.dll"),
        new("src/TerrariaEncyclopedia.Host.Mono/bin/Release/net48/TerrariaEncyclopedia.dll", "TerrariaEncyclopedia.dll"),
        new("src/TerrariaEncyclopedia.Host.Mono/bin/Release/net48/WikiData/bosses.json", "WikiData/bosses.json"),
        new("src/TerrariaEncyclopedia.Host.Mono/bin/Release/net48/WikiData/events.json", "WikiData/events.json"),
        new("src/TerrariaEncyclopedia.Host.Mono/bin/Release/net48/WikiData/hardmode-items.json", "WikiData/hardmode-items.json"),
        new("src/TerrariaEncyclopedia.Host.Mono/bin/Release/net48/WikiData/hardmode-npcs.json", "WikiData/hardmode-npcs.json"),
        new("src/TerrariaEncyclopedia.Host.Mono/bin/Release/net48/WikiData/npc-preferences.json", "WikiData/npc-preferences.json"),
        new("README.md", "README.md"),
        new("THIRD_PARTY_NOTICES.md", "THIRD_PARTY_NOTICES.md")
    };

    public static readonly string[] RequiredNoticePhrases =
    {
        "MIT License",
        "TerrariaModder Core UI concepts"
    };

    public static readonly string[] ForbiddenFileNames =
    {
        "Terraria.exe",
        "FNA.dll",
        "TerrariaInjector.exe",
        "0Harmony.dll",
        "Mono.Cecil.dll",
        "Mono.Cecil.Mdb.dll",
        "Mono.Cecil.Pdb.dll",
        "Mono.Cecil.Rocks.dll"
    };
}

public sealed record ReleaseFile(string SourcePath, string ArchivePath);
