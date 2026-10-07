using System.Diagnostics;

namespace Wukong.Automation;

public enum InstallationProblem { MissingSteam, MissingBenchmark, IncompleteBenchmark }
public sealed class InstallationIssue(InstallationProblem problem, string message) : InvalidOperationException(message)
{
    public InstallationProblem Problem { get; } = problem;
}

internal static class GuidedSetup
{
    public static SteamInstallation EnsureInstalled(RunnerOptions options, CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            try { return SteamInstallation.Discover(options); }
            catch (InstallationIssue issue)
            {
                Console.WriteLine(issue.Message);
                if (Console.IsInputRedirected) throw;
                var address = issue.Problem == InstallationProblem.MissingSteam
                    ? "https://store.steampowered.com/about/"
                    : issue.Problem == InstallationProblem.MissingBenchmark ? "steam://install/3132990" : null;
                if (address is not null)
                {
                    Console.WriteLine("Press O to open the installation page, Enter to retry, or Q to exit.");
                    var answer = Console.ReadLine();
                    if (answer is null || answer.Equals("q", StringComparison.OrdinalIgnoreCase))
                        throw new OperationCanceledException(token);
                    if (answer.Equals("o", StringComparison.OrdinalIgnoreCase))
                    {
                        try { using var process = Process.Start(new ProcessStartInfo(address) { UseShellExecute = true }); }
                        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
                        { Console.WriteLine("Could not open the page. Open this address yourself: " + address); }
                        Console.WriteLine("Complete installation and sign-in yourself. Then press Enter here to check again, or Q to exit.");
                        answer = Console.ReadLine();
                        if (answer is null || answer.Equals("q", StringComparison.OrdinalIgnoreCase))
                            throw new OperationCanceledException(token);
                    }
                }
                else WaitForUser("Wait for the Steam download / file verification, then press Enter to retry, or Q to exit.", token);
            }
        }
    }

    public static void WaitForBenchmarkToClose(SteamInstallation installation, CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var processes = installation.GameProcesses();
            var count = processes.Count;
            processes.ForEach(p => p.Dispose());
            if (count == 0) return;
            WaitForUser("Benchmark Tool is already running. Close it, then press Enter to continue, or Q to exit.", token);
        }
    }

    private static void WaitForUser(string message, CancellationToken token)
    {
        Console.WriteLine(message);
        if (Console.IsInputRedirected) throw new InvalidOperationException(message);
        var answer = Console.ReadLine();
        if (answer is null || answer.Equals("q", StringComparison.OrdinalIgnoreCase))
            throw new OperationCanceledException(token);
    }
}
