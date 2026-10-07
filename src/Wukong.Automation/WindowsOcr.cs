using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Wukong.Core;

namespace Wukong.Automation;

public sealed class WindowsOcr
{
    private readonly OcrEngine engine = OcrEngine.TryCreateFromLanguage(new Language("en-US"))
        ?? throw new InvalidOperationException("Windows English OCR is not installed. Add English (United States) in Windows Settings > Language, including Basic typing.");

    private readonly OcrEngine? russianEngine = OcrEngine.TryCreateFromLanguage(new Language("ru-RU"));
    private bool useRussian;
    public bool SupportsRussian => russianEngine is not null;

    public async Task<OcrPage> ReadAsync(string path, CancellationToken token, bool numericOnly = false)
    {
        token.ThrowIfCancellationRequested();
        var file = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(path));
        using var stream = await file.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var scale = Math.Min(Math.Max(1, 1920d / decoder.PixelWidth),
            (double)OcrEngine.MaxImageDimension / Math.Max(decoder.PixelWidth, decoder.PixelHeight));
        var transform = new BitmapTransform
        {
            ScaledWidth = (uint)(decoder.PixelWidth * scale), ScaledHeight = (uint)(decoder.PixelHeight * scale)
        };
        using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, transform,
            ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
        var result = await engine.RecognizeAsync(bitmap);
        if (!numericOnly && russianEngine is not null)
        {
            var localized = await russianEngine.RecognizeAsync(bitmap);
            // A saved Russian UI can override Unreal's command-line culture. Select
            // its OCR page only when recognizable Russian interface text is present.
            var text = OcrPage.Normalize(localized.Text);
            if (new[] { "нажмите", "настройки", "быстродействия", "суперразрешение", "разрешение", "трассировка", "средний fps", "конфиденциальности", "экран", "графика", "язык", "звук", "подтвердить" }
                .Any(label => text.Contains(label, StringComparison.Ordinal))) useRussian = true;
            if (useRussian) result = localized;
        }
        token.ThrowIfCancellationRequested();
        return new((int)decoder.PixelWidth, (int)decoder.PixelHeight, result.Lines.Select(l => new Wukong.Core.OcrLine(l.Text,
            l.Words.Select(w => new Wukong.Core.OcrWord(w.Text, new(w.BoundingRect.X / scale, w.BoundingRect.Y / scale,
                w.BoundingRect.Width / scale, w.BoundingRect.Height / scale))).ToArray())).ToArray());
    }
    public async Task<OcrPage> ReadRegionAsync(string path, Box region, CancellationToken token, bool numericOnly = false)
    {
        using var source = new Bitmap(path);
        var bounds = Rectangle.Intersect(new((int)region.X, (int)region.Y, (int)region.Width, (int)region.Height), new(0, 0, source.Width, source.Height));
        if (bounds.Width <= 0 || bounds.Height <= 0) throw new ArgumentException("Empty OCR region.");
        var temp = Path.Combine(Path.GetTempPath(), "wukong-row-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            using (var crop = source.Clone(bounds, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                crop.Save(temp, System.Drawing.Imaging.ImageFormat.Png);
            var detail = await ReadAsync(temp, token, numericOnly);
            return new(source.Width, source.Height, detail.Lines.Select(line => new Wukong.Core.OcrLine(line.Text,
                line.Words.Select(word => new Wukong.Core.OcrWord(word.Text, word.Bounds with { X = word.Bounds.X + bounds.X, Y = word.Bounds.Y + bounds.Y })).ToArray())).ToArray());
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

}
