using System.Diagnostics;
using System.Globalization;
using Wukong.Core;

namespace Wukong.Automation;

/// <summary>Report presentation is optional; failure to open a browser does not invalidate a completed run.</summary>
public static class ReportPresentation
{
    public static bool Show(string directory, IReadOnlyList<PassReport> passes, Action<string> write,
        Action<string>? openHtml = null)
    {
        var html = Path.GetFullPath(Path.Combine(directory, "report.html"));
        var json = Path.GetFullPath(Path.Combine(directory, "report.json"));
        foreach (var pass in passes)
            write(string.Create(CultureInfo.InvariantCulture,
                $"{pass.Name}: средний / average {pass.Metrics.AverageFps:0.##} FPS; min {pass.Metrics.MinimumFps:0.##}; max {pass.Metrics.MaximumFps:0.##}"));
        write("HTML: " + html);
        write("JSON: " + json);
        try
        {
            if (openHtml is not null) openHtml(html);
            else
            {
                using var browser = Process.Start(new ProcessStartInfo(html) { UseShellExecute = true });
            }
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            write("Тест завершён, отчёты сохранены. Не удалось открыть HTML / Results saved; HTML could not be opened: " + ex.Message);
            write("FPS показаны выше. HTML можно открыть позже в браузере; JSON — в текстовом редакторе.");
            return false;
        }
    }
}
