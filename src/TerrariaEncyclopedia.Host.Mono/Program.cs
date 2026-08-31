using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TerrariaEncyclopedia.Host.Mono
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args == null || args.Length > 1 || (args.Length == 1 && string.IsNullOrWhiteSpace(args[0])))
            {
                Console.Error.WriteLine("Terraria Encyclopedia Mono host accepts at most one non-empty marker path.");
                return 2;
            }

            bool terrariaLoaded = AppDomain.CurrentDomain.GetAssemblies()
                .Any(assembly => string.Equals(assembly.GetName().Name, "Terraria", StringComparison.Ordinal));

            int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            Console.WriteLine(
                "Terraria Encyclopedia Mono host loaded before game entry point; Terraria loaded: " +
                terrariaLoaded.ToString().ToLowerInvariant());

            if (args.Length == 0)
                return 0;

            string markerPath = Path.GetFullPath(args[0]);
            string[] marker =
            {
                "marker=terraria-encyclopedia-mono-host",
                "phase=mono-agent-main",
                "terraria-loaded=" + terrariaLoaded.ToString().ToLowerInvariant(),
                "process-id=" + processId.ToString(CultureInfo.InvariantCulture),
                "architecture=" + (Environment.Is64BitProcess ? "64-bit" : "32-bit")
            };

            string temporaryMarkerPath = markerPath + "." + processId.ToString(CultureInfo.InvariantCulture) + ".tmp";
            File.WriteAllLines(temporaryMarkerPath, marker);
            File.Move(temporaryMarkerPath, markerPath);
            Console.WriteLine("Terraria Encyclopedia Mono host marker written: " + markerPath);
            return 0;
        }
    }
}
