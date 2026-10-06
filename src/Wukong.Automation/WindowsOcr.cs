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

    public async Task<OcrPage> ReadAsync(string path, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var file = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(path));
        using var stream = await file.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var scale = Math.Min(1, (double)OcrEngine.MaxImageDimension / Math.Max(decoder.PixelWidth, decoder.PixelHeight));
        var transform = new BitmapTransform
        {
            ScaledWidth = (uint)(decoder.PixelWidth * scale), ScaledHeight = (uint)(decoder.PixelHeight * scale)
        };
        using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, transform,
            ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
        var result = await engine.RecognizeAsync(bitmap);
        token.ThrowIfCancellationRequested();
        return new((int)decoder.PixelWidth, (int)decoder.PixelHeight, result.Lines.Select(l => new Wukong.Core.OcrLine(l.Text,
            l.Words.Select(w => new Wukong.Core.OcrWord(w.Text, new(w.BoundingRect.X / scale, w.BoundingRect.Y / scale,
                w.BoundingRect.Width / scale, w.BoundingRect.Height / scale))).ToArray())).ToArray());
    }
}
