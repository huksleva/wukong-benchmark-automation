using System.Diagnostics;
using System.Drawing.Imaging;
using System.Text.Json;
using Wukong.Core;

namespace Wukong.Automation;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var launchedWithoutArguments = args.Length == 0 && !Console.IsInputRedirected;
        if (args.Length == 0) args = ["start"];
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        NativeWindow.EnableDpiAwareness();
        using var cancel = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancel.Cancel(); };
        try
        {
            if (args[0] is "help" or "--help" or "-h") { Help(); return 0; }
            if (args[0] is "doctor" or "parse") return ExecuteAsync(args, cancel.Token).GetAwaiter().GetResult();
            using var mutex = new Mutex(false, @"Local\WukongBenchmarkAutomation3132990");
            bool acquired;
            try { acquired = mutex.WaitOne(0); } catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) throw new InvalidOperationException("Another runner is active. Wait for it to finish.");
            try { return ExecuteAsync(args, cancel.Token).GetAwaiter().GetResult(); }
            finally { mutex.ReleaseMutex(); }
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Cancelled."); return 130; }
        catch (Exception ex) { Console.Error.WriteLine("ERROR: " + ex.Message); return 1; }
        finally
        {
            if (launchedWithoutArguments)
            {
                Console.WriteLine("Press Enter to close this window.");
                Console.ReadLine();
            }
        }
    }

    private static async Task<int> ExecuteAsync(string[] args, CancellationToken token)
    {
        var command = args[0];
        string? Value(string flag)
        {
            var index = Array.IndexOf(args, flag);
            if (index < 0) return null;
            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"{flag} requires a value.");
            return args[index + 1];
        }
        var accepted = command == "parse" ? new[] { "--image", "--ocr" } : new[] { "--config" };
        for (var i = 1; i < args.Length; i += 2)
            if (!accepted.Contains(args[i]) || i + 1 >= args.Length) throw new ArgumentException($"Unknown/incomplete argument: {args[i]}");
        if (command == "parse")
        {
            var image = Value("--image"); var raw = Value("--ocr");
            if ((image is null) == (raw is null)) throw new ArgumentException("Use exactly one of --image or --ocr.");
            OcrPage page;
            if (raw is not null) page = JsonSerializer.Deserialize<OcrPage>(File.ReadAllText(raw), JsonDefaults.Options)
                ?? throw new InvalidDataException("Empty OCR page.");
            else
            {
                using var resultOcr = new WindowsOcr();
                page = await resultOcr.ReadResultAsync(image!, token);
            }
            Console.WriteLine(JsonSerializer.Serialize(ResultParser.Parse(page), JsonDefaults.Options));
            return 0;
        }
        var options = RunnerOptions.Load(Value("--config"));
        if (command == "doctor") return await DoctorAsync(options, token);
        if (command == "start")
        {
            var installation = GuidedSetup.EnsureInstalled(options, token);
            GuidedSetup.WaitForBenchmarkToClose(installation, token);
            if (await DoctorAsync(options, token) != 0)
            {
                Console.Error.WriteLine("Setup checks failed; the benchmark has not been launched and settings were not changed. See the messages above and docs/USAGE.md.");
                return 1;
            }
        }
        else if (command != "run") throw new ArgumentException($"Unknown command: {command}");
        return await RunAsync(options, token);
    }

    private static async Task<int> DoctorAsync(RunnerOptions options, CancellationToken token)
    {
        Console.WriteLine("Checking installation, OCR and hardware (does not launch the benchmark)...");
        var errors = 0;
        try
        {
            var install = SteamInstallation.Discover(options);
            Console.WriteLine($"Steam: {install.SteamExe}\nBenchmark: {install.GameDirectory}\nSteam build: {install.BuildId ?? "unknown"}");
        }
        catch (Exception ex) { Console.Error.WriteLine("Installation: " + ex.Message); errors++; }
        var scratch = Path.Combine(Path.GetTempPath(), "wukong-ocr-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            using (var image = new Bitmap(800, 200))
            using (var g = Graphics.FromImage(image))
            using (var font = new Font("Arial", 32))
            {
                g.Clear(Color.White);
                g.DrawString("Average FPS 123", font, Brushes.Black, 20, 50);
                image.Save(scratch, ImageFormat.Png);
            }
            using var ocr = new WindowsOcr();
            var page = await ocr.ReadAsync(scratch, token);
            if (!page.Text.Contains("123", StringComparison.Ordinal)) throw new InvalidOperationException("OCR smoke test failed.");
            using var numeric = new NumericOcr();
            Console.WriteLine("Local numeric OCR (Tesseract/model/native libraries): OK");
            Console.WriteLine("English Windows OCR: OK");
            Console.WriteLine("Russian Windows OCR: " + (ocr.SupportsRussian ? "available (automatic UI recognition)" : "not installed; select English in Benchmark Tool"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { Console.Error.WriteLine("OCR: " + ex.Message); errors++; }
        finally { if (File.Exists(scratch)) File.Delete(scratch); }
        var machine = await HardwareProbe.ReadAsync(token);
        Console.WriteLine(JsonSerializer.Serialize(machine, JsonDefaults.Options));
        if (machine.Warnings.Length > 0) errors++;
        return errors == 0 ? 0 : 1;
    }

    private static async Task<int> RunAsync(RunnerOptions options, CancellationToken token)
    {
        var installation = SteamInstallation.Discover(options);
        using var ocr = new WindowsOcr();
        var existing = installation.GameProcesses();
        try { if (existing.Count != 0) throw new InvalidOperationException("Close the existing Benchmark Tool before running automation."); }
        finally { existing.ForEach(p => p.Dispose()); }
        var machine = await HardwareProbe.ReadAsync(token);
        if (machine.Cpu.Length == 0 || machine.Gpu.Length == 0 || machine.RamGiB is null)
            throw new InvalidOperationException("CPU/GPU/RAM probe failed; run doctor first. " + string.Join("; ", machine.Warnings));
        var screen = Screen.PrimaryScreen?.Bounds ?? throw new InvalidOperationException("No interactive desktop.");
        var profiles = new[] { BenchmarkProfile.Cpu(options), BenchmarkProfile.Gpu(options, screen.Width, screen.Height) };
        var configDir = options.ConfigDirectory ?? Path.Combine(installation.GameDirectory, "b1", "Saved", "Config", "Windows");
        var output = Path.Combine(Path.GetFullPath(options.OutputDirectory), DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(output);
        using var logFile = new StreamWriter(Path.Combine(output, "runner.log")) { AutoFlush = true };
        void Log(string text) { var line = $"[{DateTimeOffset.Now:HH:mm:ss}] {text}"; Console.WriteLine(line); logFile.WriteLine(line); }
        var backup = new SettingsBackup(configDir, output);
        var reports = new List<PassReport>();
        var started = DateTimeOffset.Now;
        var status = "failed";
        string? error = null;
        var launched = false;
        File.WriteAllText(Path.Combine(output, "run-config.json"), JsonSerializer.Serialize(new
        { options, installation, profiles, configDir }, JsonDefaults.Options));
        Log($"Results: {output}");
        try
        {
            foreach (var profile in profiles)
            {
                token.ThrowIfCancellationRequested();
                var passStarted = DateTimeOffset.Now;
                var directory = Path.Combine(output, profile.Name.ToLowerInvariant());
                Directory.CreateDirectory(directory);
                var settings = backup.ApplyKnownIni(profile).ToList();
                Log($"Launching {profile.Name}: {profile.Width}x{profile.Height}.");
                installation.Launch(profile);
                launched = true;
                var window = await WaitForWindowAsync(installation, options.StartupTimeoutSeconds, token);
                var ui = new UiAutomation(window, ocr, options, directory, Log);
                await ui.WaitForMenuAsync(token);
                var applied = await ui.ApplyProfileAsync(profile, token);
                settings.AddRange(applied.Settings);
                var result = await ui.RunBenchmarkAsync(token);
                await CloseGameAsync(installation, Log);
                launched = false;
                backup.SaveEffective(Path.Combine(directory, "effective-config"));
                reports.Add(new(profile.Name, passStarted, DateTimeOffset.Now, profile.Description, result.Metrics,
                    settings, Path.GetRelativePath(output, result.Screenshot).Replace('\\', '/'),
                    Path.GetRelativePath(output, result.Ocr).Replace('\\', '/'), applied.Warnings));
                ReportWriter.Write(output, new("running", started, machine, reports, null));
                await Task.Delay(2000, token);
            }
            status = "completed";
        }
        catch (Exception ex)
        {
            error = ex.Message;
            status = ex is OperationCanceledException ? "cancelled" : "failed";
            Log(status + ": " + error);
        }
        finally
        {
            try
            {
                if (launched) await CloseGameAsync(installation, Log);
                backup.Restore();
                Log("Original INI settings restored.");
            }
            catch (Exception ex)
            {
                status = "failed";
                error = (error is null ? "" : error + "; ") + "Cleanup failed: " + ex.Message;
                Log("Cleanup failed. Backups are in " + Path.Combine(output, "backup"));
            }
            ReportWriter.Write(output, new(status, started, machine, reports, error));
        }
        Log($"{status}. {Path.Combine(output, "report.html")}");
        if (status == "completed")
        {
            try { using var browser = Process.Start(new ProcessStartInfo(Path.Combine(output, "report.html")) { UseShellExecute = true }); }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            { Log("Open report.html manually: " + ex.Message); }
        }
        if (status == "cancelled") return 130;
        return status == "completed" ? 0 : 1;
    }

    private static async Task<NativeWindow> WaitForWindowAsync(SteamInstallation installation, int timeout, CancellationToken token)
    {
        var time = Stopwatch.StartNew();
        while (time.Elapsed.TotalSeconds < timeout)
        {
            token.ThrowIfCancellationRequested();
            var processes = installation.GameProcesses();
            try
            {
                foreach (var p in processes.OrderByDescending(p => p.StartTime))
                {
                    p.Refresh();
                    if (p.MainWindowHandle != 0)
                    {
                        var window = new NativeWindow(p.MainWindowHandle);
                        var bounds = window.ClientBounds();
                        if (bounds.Width >= 640 && bounds.Height >= 360) return window;
                    }
                }
            }
            finally { processes.ForEach(p => p.Dispose()); }
            await Task.Delay(1000, token);
        }
        throw new TimeoutException("Benchmark window did not appear. Steam must be logged in and all startup dialogs closed.");
    }

    private static async Task CloseGameAsync(SteamInstallation installation, Action<string> log)
    {
        var processes = installation.GameProcesses().OrderByDescending(p => p.MainWindowHandle != 0).ToList();
        try
        {
            foreach (var p in processes)
            {
                if (p.HasExited) continue;
                p.CloseMainWindow();
            }
            foreach (var p in processes)
            {
                if (p.HasExited) continue;
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                try { await p.WaitForExitAsync(timeout.Token); }
                catch (OperationCanceledException)
                {
                    log($"Closing owned Benchmark Tool process {p.Id} after timeout.");
                    p.Kill(true);
                    using var killTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    await p.WaitForExitAsync(killTimeout.Token);
                }
            }
            // Killing/waiting on a launcher does not wait for every descendant.
            // Poll the actual installation's processes before restoring INIs.
            var exitDeadline = Stopwatch.StartNew();
            while (true)
            {
                var remaining = installation.GameProcesses();
                var count = remaining.Count;
                remaining.ForEach(p => p.Dispose());
                if (count == 0) break;
                if (exitDeadline.Elapsed.TotalSeconds >= 10)
                    throw new InvalidOperationException("Benchmark processes are still running; settings were not restored yet.");
                await Task.Delay(250);
            }
        }
        finally { processes.ForEach(p => p.Dispose()); }
    }

    private static void Help() => Console.WriteLine("""
        Wukong Benchmark Automation (Windows 10/11, Steam AppID 3132990)
        Commands:
          start  [--config runner.local.json]   Guided setup checks, then both profiles (default).
          doctor [--config runner.local.json]   Check installation, OCR, CPU/GPU/RAM.
          run    [--config runner.local.json]   Run CPU and GPU profiles automatically.
          parse  --image result.png             Parse an existing English or Russian result image.
          parse  --ocr result-ocr.json           Parse saved OCR data without a game.
        Ctrl+C cancels; settings are backed up and restored after closing the tool.
        Do not use the keyboard/mouse during a run; keep the benchmark unobscured.
        """);
}
