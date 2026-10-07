using System.Text.Json;
using System.Text.RegularExpressions;

namespace Wukong.Core;

public sealed record Box(double X, double Y, double Width, double Height)
{
    public double CenterX => X + Width / 2;
    public double CenterY => Y + Height / 2;
}

public sealed record OcrWord(string Text, Box Bounds);
public sealed record OcrLine(string Text, OcrWord[] Words, string? Source = null)
{
    public Box Bounds => Words.Length == 0 ? new(0, 0, 0, 0) : new(
        Words.Min(w => w.Bounds.X), Words.Min(w => w.Bounds.Y),
        Words.Max(w => w.Bounds.X + w.Bounds.Width) - Words.Min(w => w.Bounds.X),
        Words.Max(w => w.Bounds.Y + w.Bounds.Height) - Words.Min(w => w.Bounds.Y));
}

public sealed record OcrPage(int Width, int Height, OcrLine[] Lines)
{
    public string Text => string.Join(Environment.NewLine, Lines.Select(l => l.Text));
    public static string Normalize(string text) => Regex.Replace(text.ToLowerInvariant(), @"[^\p{L}\p{Nd}%]+", " ").Trim();
    public OcrLine? Find(params string[] labels) => Lines.FirstOrDefault(l => labels.Any(s => !string.IsNullOrWhiteSpace(s) &&
        Normalize(l.Text).Contains(Normalize(s), StringComparison.Ordinal)));
}

public sealed record BenchmarkMetrics(double AverageFps, double MinimumFps, double MaximumFps,
    double? Fps95PercentAbove = null);
public sealed record SettingEvidence(string Label, string Value, string Source);
public sealed record PassReport(string Name, DateTimeOffset StartedAt, DateTimeOffset FinishedAt,
    string ProfileDescription, BenchmarkMetrics Metrics, IReadOnlyList<SettingEvidence> Settings,
    string ResultScreenshot, string RawOcr, IReadOnlyList<string> Warnings);
public sealed record MachineInfo(string Os, string[] Cpu, string[] Gpu, double? RamGiB,
    string[] DriverVersions, int LogicalProcessors, string Architecture, string[] Warnings);
public sealed record SessionReport(string Status, DateTimeOffset CreatedAt, MachineInfo Machine,
    IReadOnlyList<PassReport> Passes, string? Error);

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
