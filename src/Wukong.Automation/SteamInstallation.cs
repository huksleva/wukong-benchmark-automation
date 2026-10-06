using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Wukong.Automation;

public sealed record SteamInstallation(string SteamExe, string GameDirectory, string? BuildId)
{
    public const string AppId = "3132990";
    public static SteamInstallation Discover(RunnerOptions options)
    {
        var steamExe = options.SteamPath;
        if (steamExe is null)
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            steamExe = key?.GetValue("SteamExe") as string;
            steamExe ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steam.exe");
        }
        if (Directory.Exists(steamExe)) steamExe = Path.Combine(steamExe, "steam.exe");
        if (!File.Exists(steamExe)) throw new FileNotFoundException("Steam was not found. Set steamPath in runner.local.json.", steamExe);
        steamExe = Path.GetFullPath(steamExe);
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Path.GetDirectoryName(steamExe)! };
        var folders = Path.Combine(libraries.First(), "steamapps", "libraryfolders.vdf");
        if (File.Exists(folders))
            foreach (Match m in Regex.Matches(File.ReadAllText(folders), "\"path\"\\s*\"([^\"]+)\""))
                libraries.Add(m.Groups[1].Value.Replace(@"\\", @"\"));
        foreach (var library in libraries)
        {
            var manifestPath = Path.Combine(library, "steamapps", $"appmanifest_{AppId}.acf");
            if (!File.Exists(manifestPath)) continue;
            var manifest = File.ReadAllText(manifestPath);
            var directory = options.InstallationDirectory ?? Path.Combine(library, "steamapps", "common", Field(manifest, "installdir")
                ?? throw new InvalidDataException("Steam manifest has no installation directory."));
            VerifyDirectory(directory);
            return new(steamExe, Path.GetFullPath(directory), Field(manifest, "buildid"));
        }
        if (options.InstallationDirectory is { } explicitPath)
        {
            VerifyDirectory(explicitPath);
            return new(steamExe, Path.GetFullPath(explicitPath), null);
        }
        throw new FileNotFoundException("Install the FREE Black Myth: Wukong Benchmark Tool in Steam (AppID 3132990). The full game is not required.");
    }

    private static string? Field(string vdf, string name)
    {
        var m = Regex.Match(vdf, $"\"{Regex.Escape(name)}\"\\s*\"([^\"]+)\"");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static void VerifyDirectory(string path)
    {
        if (!Directory.Exists(Path.Combine(path, "b1")) || !File.Exists(Path.Combine(path, "b1_benchmark.exe")))
            throw new DirectoryNotFoundException($"Not a Benchmark Tool installation: {path} (expected b1_benchmark.exe and b1/).");
    }

    public List<Process> GameProcesses()
    {
        var result = new List<Process>();
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (!p.HasExited && p.ProcessName.StartsWith("b1", StringComparison.OrdinalIgnoreCase)
                    && p.MainModule?.FileName is { } exe
                    && exe.StartsWith(GameDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                { result.Add(p); continue; }
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException) { }
            p.Dispose();
        }
        return result;
    }

    public void Launch(BenchmarkProfile profile)
    {
        var info = new ProcessStartInfo(SteamExe) { UseShellExecute = false };
        foreach (var arg in new[] { "-applaunch", AppId, "-culture=en", profile.Name == "GPU" ? "-fullscreen" : "-windowed", $"-ResX={profile.Width}", $"-ResY={profile.Height}", "-NoSplash" })
            info.ArgumentList.Add(arg);
        using var launcher = Process.Start(info) ?? throw new InvalidOperationException("Steam could not be launched.");
    }
}
