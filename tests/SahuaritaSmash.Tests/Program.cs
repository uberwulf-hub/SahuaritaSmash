// Sahuarita Smash! (Sauce Edition) - the checks: dotnet run --project tests/SahuaritaSmash.Tests -c Release [-Only a,b] [-Snapshot dir]
// They are App Hub's Classic Games checks for the game (same names, same numbers), run without a window and without
// sound. The exit code is the number of failures (0 = all passed).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Headless;

namespace ClassicGames
{
    public static partial class Tests
    {
        static int checks, failed;
        static readonly List<string> failures = new List<string>();
        static string group = "";
        /// <summary>-Snapshot dir: where the pictures are saved (null = not saved).</summary>
        static string SnapshotDir;

        static void Check(string name, bool ok, string detail)
        {
            checks++;
            if (ok) return;
            failed++;
            string line = "FAIL " + name + (string.IsNullOrEmpty(detail) ? "" : "  (" + detail + ")");
            failures.Add(line);
            Console.WriteLine(line);
        }

        static void Run(string name, HashSet<string> only, Action a)
        {
            if (only != null && !only.Contains(name)) return;
            group = name;
            Console.WriteLine("-- " + name);
            var sw = Stopwatch.StartNew();
            try { a(); }
            catch (Exception ex) { Check(name + ": threw", false, ex.GetType().Name + ": " + ex.Message + " " + ex.StackTrace?.Split('\n').FirstOrDefault()); }
            Console.WriteLine("   " + sw.Elapsed.TotalSeconds.ToString("0.0") + " s");
        }

        public static int Main(string[] args)
        {
            HashSet<string> only = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-Only" && i + 1 < args.Length) only = new HashSet<string>(args[++i].Split(',').Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
                else if (args[i] == "-Snapshot" && i + 1 < args.Length) SnapshotDir = System.IO.Path.GetFullPath(args[++i]);
            }
            var sw = Stopwatch.StartNew();
            // drawing without a window: Avalonia's headless platform with Skia (real pictures)
            Avalonia.AppBuilder.Configure<SahuaritaSmash.App>().UseSkia().UseHeadless(new Avalonia.Headless.AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
            SahuaritaSmash.App.Testing = true;
            SahuaritaSmash.Services.Data.UseTestFolder();
            BrawlMusic.Enabled = false;
            Run("brawl", only, BrawlChecks);
            Run("brawl-saves", only, BrawlSaveChecks);
            Run("brawl-snapshots", only, BrawlSnapshotChecks);
            Run("sound", only, SoundOutChecks);
            Console.WriteLine(failed == 0 ? "ALL PASSED (" + checks + " checks, " + sw.Elapsed.TotalSeconds.ToString("0.0") + " s)" : "FAILED " + failed + " of " + checks + " checks:");
            foreach (string f in failures.Take(50)) Console.WriteLine("  " + f);
            return failed;
        }
    }
}
