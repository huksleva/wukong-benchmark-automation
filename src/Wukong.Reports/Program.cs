using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Wukong.Core;

Console.OutputEncoding = Encoding.UTF8;
try
{
    if (args.Length == 0)
    {
        Console.WriteLine("Wukong Reports: previously saved benchmark results. No new benchmark is launched.");
        Show(Path.Combine(AppContext.BaseDirectory, "sample", "report.json"), false);
        Console.WriteLine("Use --help for commands; use show --report /path/to/report.json for your own result.");
        return 0;
    }
    if (args.Length == 1 && args[0] is "--help" or "-h" or "help")
    {
        Console.WriteLine("""
            Wukong Reports (Linux / macOS / Windows, .NET included in release packages)
            No arguments: show the bundled, previously measured Windows result.
            show  --report report.json [--json]  Read an existing session report.
            parse --ocr result-ocr.json          Parse saved OCR data; does not recognize images.
            doctor                              Show platform and bundled sample availability.
            This program does NOT launch Steam or run CPU/GPU tests.
            Full benchmark automation currently requires the separate Windows runner.
            """);
        return 0;
    }
    if (args.Length == 1 && args[0] == "doctor")
    {
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            os = RuntimeInformation.OSDescription,
            architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            mode = "saved-reports-only",
            benchmarkAutomation = false,
            bundledSample = File.Exists(Path.Combine(AppContext.BaseDirectory, "sample", "report.json"))
        }, JsonDefaults.Options));
        return 0;
    }
    if (args[0] is "run" or "start")
        throw new ArgumentException("Wukong Reports reads saved results. It cannot run the benchmark. Use Wukong.Automation on Windows for CPU/GPU tests; Linux/macOS benchmark automation is not implemented.");
    if (args[0] == "show" && args.Length is 3 or 4 && args[1] == "--report" && (args.Length == 3 || args[3] == "--json"))
    {
        Show(args[2], args.Length == 4);
        return 0;
    }
    if (args[0] == "parse" && args.Length == 3 && args[1] == "--ocr")
    {
        var page = Load<OcrPage>(args[2]);
        if (page.Width <= 0 || page.Height <= 0 || page.Lines is null || page.Lines.Any(line => line is null || line.Text is null || line.Words is null || line.Words.Any(word => word is null || word.Text is null || word.Bounds is null)))
            throw new InvalidDataException("Invalid OCR lines.");
        Console.WriteLine(JsonSerializer.Serialize(ResultParser.Parse(page), JsonDefaults.Options));
        return 0;
    }
    throw new ArgumentException("Unknown/incomplete command. Use --help.");
}
catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException)
{
    Console.Error.WriteLine("ERROR: " + ex.Message);
    return 1;
}

static T Load<T>(string path) where T : class
{
    var info = new FileInfo(path);
    if (!info.Exists) throw new FileNotFoundException("Input file not found: " + Path.GetFullPath(path));
    if (info.Length > 20 * 1024 * 1024) throw new InvalidDataException("JSON input must be at most 20 MiB.");
    return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonDefaults.Options)
        ?? throw new InvalidDataException("Empty JSON input.");
}

static void Show(string path, bool json)
{
    var report = Load<SessionReport>(path);
    if (string.IsNullOrWhiteSpace(report.Status) || report.Machine?.Cpu is null || report.Machine.Gpu is null
        || report.Passes is null || report.Passes.Count == 0)
        throw new InvalidDataException("Missing report status, machine or passes.");
    foreach (var pass in report.Passes)
    {
        var metrics = pass?.Metrics;
        if (pass is null || string.IsNullOrWhiteSpace(pass.Name) || metrics is null
            || !double.IsFinite(metrics.AverageFps) || !double.IsFinite(metrics.MinimumFps) || !double.IsFinite(metrics.MaximumFps)
            || metrics.MinimumFps < 0 || metrics.MinimumFps > metrics.AverageFps || metrics.AverageFps > metrics.MaximumFps
            || metrics.AverageFps <= 0 || metrics.MaximumFps > 10000
            || (metrics.Fps95PercentAbove is double percentile && (!double.IsFinite(percentile) || percentile < metrics.MinimumFps || percentile > metrics.MaximumFps)))
            throw new InvalidDataException("Invalid or inconsistent pass metrics.");
    }
    if (json) { Console.WriteLine(JsonSerializer.Serialize(report, JsonDefaults.Options)); return; }
    Console.WriteLine("Saved report: " + Path.GetFullPath(path));
    Console.WriteLine($"Measured at: {report.CreatedAt:u}; status: {report.Status}");
    Console.WriteLine("CPU: " + string.Join("; ", report.Machine.Cpu));
    Console.WriteLine("GPU: " + string.Join("; ", report.Machine.Gpu));
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"RAM: {report.Machine.RamGiB:0.##} GiB"));
    foreach (var pass in report.Passes)
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{pass.Name}: average {pass.Metrics.AverageFps:0.##} FPS; min {pass.Metrics.MinimumFps:0.##}; max {pass.Metrics.MaximumFps:0.##}; 95% above {pass.Metrics.Fps95PercentAbove:0.##}"));
    if (!string.IsNullOrWhiteSpace(report.Error)) Console.WriteLine("Report note: " + report.Error);
    Console.WriteLine("HTML: " + Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, "report.html"));
}
