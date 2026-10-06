using System.Globalization;
using System.Text.RegularExpressions;

namespace Wukong.Core;

/// <summary>Reads the tool's summary, never estimates FPS from an external frame counter.</summary>
public static class ResultParser
{
    private static readonly Regex Number = new(@"(?<![\w.])\d{1,4}(?:[.,]\d{1,2})?(?![\w.])", RegexOptions.Compiled);
    private static readonly (string Name, string[] Labels)[] Required =
    [
        ("average", ["average fps", "average frame rate", "average framerate"]),
        ("minimum", ["minimum fps", "min fps", "minimum frame rate", "lowest fps"]),
        ("maximum", ["maximum fps", "max fps", "maximum frame rate", "highest fps"])
    ];

    public static BenchmarkMetrics Parse(OcrPage page)
    {
        var values = Required.Select(item => ReadMetric(page, item.Labels)
            ?? throw new InvalidDataException($"Cannot read {item.Name} FPS from the benchmark summary.")).ToArray();
        if (values.Any(v => v <= 0 || v > 10000) || values[1] > values[0] || values[0] > values[2])
            throw new InvalidDataException("FPS values are inconsistent (expected minimum <= average <= maximum).");
        return new(values[0], values[1], values[2], ReadMetric(page, ["95%", "95 %"], exclude95: true));
    }

    public static bool TryParse(OcrPage page, out BenchmarkMetrics? metrics)
    {
        try { metrics = Parse(page); return true; }
        catch (InvalidDataException) { metrics = null; return false; }
    }

    private static double? ReadMetric(OcrPage page, string[] labels, bool exclude95 = false)
    {
        var labelLine = page.Find(labels);
        if (labelLine is null) return null;
        // The summary can put a number beside, above or below its label. Geometry
        // prevents a different metric, graph axis or hardware name from being read.
        var ownNumbers = Numbers(labelLine.Text).Where(v => !exclude95 || v != 95).ToArray();
        if (ownNumbers.Length == 1) return ownNumbers[0];
        var bounds = labelLine.Bounds;
        var candidates = page.Lines.Where(l => !ReferenceEquals(l, labelLine))
            .Select(l => new { Line = l, Numbers = Numbers(l.Text).ToArray(), Bounds = l.Bounds })
            .Where(c => c.Numbers.Length == 1 && Regex.IsMatch(c.Line.Text.Trim(), @"^\d+(?:[.,]\d+)?(?:\s*FPS)?$", RegexOptions.IgnoreCase))
            .Where(c => Math.Abs(c.Bounds.CenterX - bounds.CenterX) < Math.Max(bounds.Width, page.Width * .09)
                && Math.Abs(c.Bounds.CenterY - bounds.CenterY) < page.Height * .15)
            .OrderBy(c => Math.Abs(c.Bounds.CenterY - bounds.CenterY) + Math.Abs(c.Bounds.CenterX - bounds.CenterX) * .5)
            .ToArray();
        if (candidates.Length == 0) return null;
        if (candidates.Length > 1)
        {
            static double Distance(Box a, Box b) => Math.Abs(a.CenterY - b.CenterY) + Math.Abs(a.CenterX - b.CenterX) * .5;
            if (Math.Abs(Distance(candidates[0].Bounds, bounds) - Distance(candidates[1].Bounds, bounds)) < 8)
                return null; // Ambiguous OCR must fail rather than silently invent a result.
        }
        return candidates[0].Numbers[0];
    }

    private static IEnumerable<double> Numbers(string text) => Number.Matches(text).Select(m =>
        double.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture));
}
