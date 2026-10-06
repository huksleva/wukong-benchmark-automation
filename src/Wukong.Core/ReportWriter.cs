using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Wukong.Core;

public static class ReportWriter
{
    public static void Write(string directory, SessionReport report)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "report.json"), JsonSerializer.Serialize(report, JsonDefaults.Options));
        File.WriteAllText(Path.Combine(directory, "report.html"), Html(report), Encoding.UTF8);
    }

    private static string Html(SessionReport r)
    {
        static string E(string? s) => WebUtility.HtmlEncode(s ?? "Unavailable");
        static string N(double? d) => d?.ToString("0.##", CultureInfo.InvariantCulture) ?? "Unavailable";
        var b = new StringBuilder("<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\"><title>Wukong benchmark results</title><style>body{font:16px system-ui;max-width:1100px;margin:40px auto;padding:0 24px;background:#10151e;color:#e7edf5}table{border-collapse:collapse;width:100%;margin:24px 0}td,th{padding:12px;text-align:left;border-bottom:1px solid #344155}th{color:#9bbcea}a{color:#8ecaff}code{overflow-wrap:anywhere}section{margin:36px 0;padding:20px;background:#1b2330;border-radius:12px}img{max-width:100%}.warn{color:#ffd18a}h1,h2{font-weight:600}</style>");
        b.Append($"<h1>Black Myth: Wukong Benchmark Tool</h1><p>Status: <strong>{E(r.Status)}</strong> · {E(r.CreatedAt.ToString("u"))}</p>");
        if (r.Error is not null) b.Append($"<p class=\"warn\">{E(r.Error)}</p>");
        b.Append($"<section><h2>Computer</h2><p>CPU: {E(string.Join("; ", r.Machine.Cpu))}</p><p>GPU: {E(string.Join("; ", r.Machine.Gpu))}</p><p>RAM: {N(r.Machine.RamGiB)} GiB</p><p>{E(r.Machine.Os)} · {E(r.Machine.Architecture)} · {r.Machine.LogicalProcessors} logical processors</p><p>GPU drivers: {E(string.Join("; ", r.Machine.DriverVersions))}</p></section>");
        b.Append("<table><tr><th>Pass</th><th>Average FPS</th><th>Minimum FPS</th><th>Maximum FPS</th><th>95% above</th></tr>");
        foreach (var p in r.Passes)
            b.Append($"<tr><td>{E(p.Name)}</td><td>{N(p.Metrics.AverageFps)}</td><td>{N(p.Metrics.MinimumFps)}</td><td>{N(p.Metrics.MaximumFps)}</td><td>{N(p.Metrics.Fps95PercentAbove)}</td></tr>");
        b.Append("</table><p>The metrics above are parsed from the tool's result screen. CPU/GPU names describe the settings' intent; they do not certify the limiting component.</p>");
        foreach (var p in r.Passes)
        {
            b.Append($"<section><h2>{E(p.Name)} settings</h2><p>{E(p.ProfileDescription)}</p><table><tr><th>Setting</th><th>Value</th><th>Evidence</th></tr>");
            foreach (var s in p.Settings) b.Append($"<tr><td>{E(s.Label)}</td><td>{E(s.Value)}</td><td>{E(s.Source)}</td></tr>");
            b.Append("</table>");
            foreach (var w in p.Warnings) b.Append($"<p class=\"warn\">{E(w)}</p>");
            b.Append($"<a href=\"{E(p.RawOcr)}\">Raw OCR JSON</a><p><a href=\"{E(p.ResultScreenshot)}\"><img src=\"{E(p.ResultScreenshot)}\" alt=\"{E(p.Name)} benchmark result\"></a></p></section>");
        }
        foreach (var w in r.Machine.Warnings) b.Append($"<p class=\"warn\">{E(w)}</p>");
        b.Append("</html>");
        return b.ToString();
    }
}
