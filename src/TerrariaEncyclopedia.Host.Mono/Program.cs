using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TerrariaEncyclopedia.Host.Mono
{
    internal static class Program
    {
        private static string hostDirectory;

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
            hostDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            AppDomain.CurrentDomain.AssemblyResolve += ResolveHostAssembly;
            Console.WriteLine(
                "Terraria Encyclopedia Mono host loaded before game entry point; Terraria loaded: " +
                terrariaLoaded.ToString().ToLowerInvariant());

            if (args.Length == 0)
            {
#if ENCYCLOPEDIA_FEATURE
                GameHost.Configure(null);
#endif
                return 0;
            }

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
#if ENCYCLOPEDIA_FEATURE
            GameHost.Configure(markerPath + ".encyclopedia");
#endif
            return 0;
        }

        private static System.Reflection.Assembly ResolveHostAssembly(object sender, ResolveEventArgs args)
        {
            string name = new System.Reflection.AssemblyName(args.Name).Name + ".dll";
            string path = Path.Combine(hostDirectory, name);
            return File.Exists(path) ? System.Reflection.Assembly.LoadFrom(path) : null;
        }
    }
}
