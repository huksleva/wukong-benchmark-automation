using System.Text.Json;

namespace Wukong.Automation;

public sealed class RunnerOptions
{
    public string? SteamPath { get; set; }
    public string? InstallationDirectory { get; set; }
    public string? ConfigDirectory { get; set; }
    public string OutputDirectory { get; set; } = "results";
    public int StartupTimeoutSeconds { get; set; } = 900;
    public int BenchmarkTimeoutSeconds { get; set; } = 600;
    public int PollIntervalMilliseconds { get; set; } = 2000;
    public int MaxDiagnosticFrames { get; set; } = 40;
    public double ValueColumnX { get; set; } = .75;
    public int CpuWidth { get; set; } = 1280;
    public int CpuHeight { get; set; } = 720;
    public int? GpuWidth { get; set; }
    public int? GpuHeight { get; set; }
    public bool EnableGpuRayTracing { get; set; } = true;
    public Dictionary<string, string[]> Labels { get; set; } = new()
    {
        ["settings"] = ["settings"],
        ["displayTab"] = ["display"],
        ["graphicsTab"] = ["graphics"],
        ["preset"] = ["graphics preset", "graphics quality", "overall graphics quality"],
        ["viewDistance"] = ["view distance quality", "view distance"],
        ["superResolutionMode"] = ["super resolution sampling"],
        ["superResolutionScale"] = ["super resolution sharpness", "super resolution"],
        ["frameGeneration"] = ["frame generation"],
        ["rayTracing"] = ["full ray tracing"],
        ["rayTracingQuality"] = ["full ray tracing level", "ray tracing quality"],
        ["vsync"] = ["v sync", "vsync", "vertical sync"],
        ["frameCap"] = ["frame rate cap", "framerate cap", "frame rate limit"],
        ["resolution"] = ["display resolution", "resolution"],
        ["apply"] = ["apply settings", "apply"],
        ["start"] = ["start benchmark", "run benchmark", "begin benchmark", "benchmark test"],
        ["confirm"] = ["confirm", "yes", "keep changes"],
        ["continue"] = ["press any key", "press any button", "press enter", "press to continue", "click to continue", "click anywhere"]
    };

    public void Validate()
    {
        if (StartupTimeoutSeconds < 10 || BenchmarkTimeoutSeconds < 30 || PollIntervalMilliseconds < 500)
            throw new ArgumentException("Timeouts must be >=10s / >=30s and polling >=500ms.");
        if (MaxDiagnosticFrames is < 5 or > 500) throw new ArgumentException("maxDiagnosticFrames must be between 5 and 500.");
        if (ValueColumnX < .5 || ValueColumnX > .95 || CpuWidth < 640 || CpuHeight < 360
            || (GpuWidth.HasValue != GpuHeight.HasValue) || GpuWidth is < 640 || GpuHeight is < 360)
            throw new ArgumentException("Invalid resolution or value-column position.");
        var defaults = new RunnerOptions();
        foreach (var key in defaults.Labels.Keys)
            if (!Labels.TryGetValue(key, out var labels) || labels.Length == 0 || labels.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException($"Missing UI labels: {key}");
    }

    public static RunnerOptions Load(string? file)
    {
        var options = file is null ? new RunnerOptions() : JsonSerializer.Deserialize<RunnerOptions>(
            File.ReadAllText(file), Wukong.Core.JsonDefaults.Options) ?? throw new InvalidDataException("Empty configuration.");
        options.Validate();
        return options;
    }
}

public sealed record BenchmarkProfile(string Name, int Width, int Height, string Preset, int ResolutionScale,
    bool RayTracing, string Description)
{
    public static BenchmarkProfile Cpu(RunnerOptions o) => new("CPU", o.CpuWidth, o.CpuHeight, "Low", 50, false,
        "720p by default, low graphics, 50% internal resolution, high view distance, ray tracing and frame generation off; VSync and the frame cap off. Reduces GPU work while retaining scene submission work on the CPU.");
    public static BenchmarkProfile Gpu(RunnerOptions o, int nativeWidth, int nativeHeight) => new("GPU",
        o.GpuWidth ?? nativeWidth, o.GpuHeight ?? nativeHeight, "Cinematic", 100, o.EnableGpuRayTracing,
        "Native display resolution by default, cinematic graphics, TSR at 100%, highest available full ray tracing, frame generation off; VSync and the frame cap off. Unsupported ray tracing is explicitly recorded as a fallback.");
}
