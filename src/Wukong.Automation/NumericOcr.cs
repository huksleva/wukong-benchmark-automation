using System.Globalization;
using System.Text.RegularExpressions;
using Tesseract;
using Wukong.Core;

namespace Wukong.Automation;

/// <summary>Local numeric fallback for stylized FPS digits missed by Windows OCR.</summary>
public sealed class NumericOcr : IDisposable
{
    private readonly TesseractEngine engine = new(Path.Combine(AppContext.BaseDirectory, "tessdata"), "eng", EngineMode.LstmOnly);
    private static readonly Regex ValuePattern = new(@"^\s*(\d+(?:[.,]\d+)?)\s*[FfPpSs]*\s*$");

    public NumericOcr() => engine.SetVariable("tessedit_char_whitelist", "0123456789.,FPSfps");
    public OcrLine? ReadConsensus(string path, Box region)
    {
        using var source = new Bitmap(path);
        var bounds = Rectangle.Intersect(new((int)region.X, (int)region.Y, (int)region.Width, (int)region.Height), new(0, 0, source.Width, source.Height));
        var readings = new List<double>();
        var positions = new List<Box>();
        var temp = Path.Combine(Path.GetTempPath(), "wukong-numeric-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            foreach (var threshold in new[] { 140, 180 })
            {
                using (var crop = source.Clone(bounds, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    for (var y = 0; y < crop.Height; y++)
                        for (var x = 0; x < crop.Width; x++)
                        {
                            var color = crop.GetPixel(x, y);
                            crop.SetPixel(x, y, (color.R + color.G + color.B) / 3 > threshold ? Color.Black : Color.White);
                        }
                    crop.Save(temp, System.Drawing.Imaging.ImageFormat.Png);
                }
                using var image = Pix.LoadFromFile(temp);
                using var page = engine.Process(image, PageSegMode.SingleLine);
                var match = ValuePattern.Match(page.GetText());
                if (match.Success)
                {
                    readings.Add(double.Parse(match.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture));
                    using var iterator = page.GetIterator();
                    iterator.Begin();
                    do
                    {
                        if (ValuePattern.IsMatch(iterator.GetText(PageIteratorLevel.Word) ?? "")
                            && iterator.TryGetBoundingBox(PageIteratorLevel.Word, out var box))
                        { positions.Add(new(bounds.X + box.X1, bounds.Y + box.Y1, box.Width, box.Height)); break; }
                    } while (iterator.Next(PageIteratorLevel.Word));
                }
            }
            if (readings.Count != 2 || readings[0] != readings[1]) return null;
            var text = readings[0].ToString(CultureInfo.InvariantCulture) + " FPS";
            return positions.Count == 0 ? null : new(text, [new(text, positions[0])], "Tesseract numeric consensus; two contrast thresholds");
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public void Dispose() => engine.Dispose();
}
