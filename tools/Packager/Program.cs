namespace TerrariaEncyclopedia.Packager;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            Options options = Options.Parse(args);
            var assembler = new ReleaseAssembler();
            IReadOnlyList<string> errors = assembler.Verify(options.RepositoryRoot, options.TerrariaPath);
            if (errors.Count != 0)
            {
                foreach (string error in errors)
                    Console.Error.WriteLine(error);
                return 1;
            }

            if (options.VerifyOnly)
            {
                Console.WriteLine("macOS release manifest and licence checks passed.");
                return 0;
            }

            assembler.Assemble(options.RepositoryRoot, options.OutputArchivePath!, options.TerrariaPath);
            Console.WriteLine("Created " + options.OutputArchivePath);
            return 0;
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private sealed class Options
    {
        public string RepositoryRoot { get; private init; } = FindRepositoryRoot(AppContext.BaseDirectory);
        public string? TerrariaPath { get; private init; }
        public string? OutputArchivePath { get; private init; }
        public bool VerifyOnly { get; private init; }

        public static Options Parse(string[] args)
        {
            string root = FindRepositoryRoot(AppContext.BaseDirectory);
            string? terrariaPath = null;
            string? output = null;
            bool verifyOnly = false;

            for (int index = 0; index < args.Length; index++)
            {
                switch (args[index])
                {
                    case "--repository-root":
                        root = RequireValue(args, ref index, "--repository-root");
                        break;
                    case "--terraria-path":
                        terrariaPath = RequireValue(args, ref index, "--terraria-path");
                        break;
                    case "--output":
                        output = RequireValue(args, ref index, "--output");
                        break;
                    case "--verify-only":
                        verifyOnly = true;
                        break;
                    case "--help":
                    case "-h":
                        throw new ArgumentException("Usage: dotnet run --project tools/Packager -- [--verify-only] [--terraria-path PATH] [--output ARCHIVE] [--repository-root PATH]");
                    default:
                        throw new ArgumentException("Unknown option: " + args[index]);
                }
            }

            if (!verifyOnly && string.IsNullOrWhiteSpace(output))
                output = Path.Combine(root, "artifacts", "TerrariaEncyclopedia-macos.zip");

            return new Options { RepositoryRoot = root, TerrariaPath = terrariaPath, OutputArchivePath = output, VerifyOnly = verifyOnly };
        }

        private static string RequireValue(string[] args, ref int index, string option)
        {
            if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
                throw new ArgumentException(option + " requires a value.");
            return args[index];
        }

        private static string FindRepositoryRoot(string startingDirectory)
        {
            for (DirectoryInfo? directory = new DirectoryInfo(startingDirectory); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "README.md")) &&
                    (Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git"))))
                    return directory.FullName;
            }

            throw new ArgumentException("Could not locate the repository root. Pass --repository-root PATH.");
        }
    }
}
