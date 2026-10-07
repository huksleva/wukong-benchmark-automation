using System.Globalization;
using System.Text.Json;
using Wukong.Core;

// Documentation only: deliberately synthetic data; never launches Steam.
var output = Path.GetFullPath(args.Length > 0 ? args[0] : "docs/examples");
Directory.CreateDirectory(output);
var timestamp = DateTimeOffset.Parse("2026-10-07T00:00:00+03:00", CultureInfo.InvariantCulture);
var passes = new[]
{
    Demo("CPU", new(72.5, 44, 98), "1280 × 720", "Low / High view distance", "50%"),
    Demo("GPU", new(40, 25, 62), "1920 × 1080", "Cinematic", "100%")
};
ReportWriter.Write(output, new("demo", timestamp,
    new("Windows (example)", ["Sample CPU — synthetic"], ["Sample GPU — synthetic"],
        16, ["Example driver"], 8, "X64", ["Documentation preview; not a hardware measurement."]),
    passes, "DEMONSTRATION ONLY: synthetic OCR fixtures, not real benchmark runs."));
Console.WriteLine("Synthetic preview written to " + output);

PassReport Demo(string name, BenchmarkMetrics values, string resolution, string quality, string scale)
{
    static OcrLine Line(string text, double x) => new(text, [new(text, new(x, 100, 280, 30))]);
    static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    var fixture = new OcrPage(1200, 240,
    [
        Line("Average FPS " + N(values.AverageFps), 50),
        Line("Minimum FPS " + N(values.MinimumFps), 450),
        Line("Maximum FPS " + N(values.MaximumFps), 850)
    ]);
    var stem = name.ToLowerInvariant() + "-fixture";
    File.WriteAllText(Path.Combine(output, stem + ".json"), JsonSerializer.Serialize(fixture, JsonDefaults.Options));
    var svg = $"""
        <svg xmlns="http://www.w3.org/2000/svg" width="1200" height="240" viewBox="0 0 1200 240">
          <rect width="1200" height="240" rx="12" fill="#10151e"/>
          <g font-family="Segoe UI,Arial,sans-serif" fill="#e7edf5">
            <text x="50" y="50" font-size="18" fill="#ffd18a">SYNTHETIC OCR FIXTURE · {name} · NOT A BENCHMARK SCREEN</text>
            <text x="50" y="130" font-size="25">Average FPS {N(values.AverageFps)}</text>
            <text x="450" y="130" font-size="25">Minimum FPS {N(values.MinimumFps)}</text>
            <text x="850" y="130" font-size="25">Maximum FPS {N(values.MaximumFps)}</text>
            <text x="50" y="205" font-size="17" fill="#a6c0df">Fixture values illustrate parsing and report layout only.</text>
          </g>
        </svg>
        """;
    File.WriteAllText(Path.Combine(output, stem + ".svg"), svg);
    return new(name + " · demo", timestamp, timestamp.AddMinutes(2),
        "Synthetic documentation example. Requested settings shown for illustration.",
        ResultParser.Parse(fixture),
        [new("Resolution", resolution, "Demo fixture"), new("Graphics preset", quality, "Demo fixture"),
         new("Super resolution", "TSR / " + scale, "Demo fixture"),
         new("Frame generation / VSync / FPS cap", "Off", "Demo fixture")],
        stem + ".svg", stem + ".json", ["Synthetic values; this pass was not executed in Benchmark Tool."]);
}
