using Wukong.Core;

namespace Wukong.Automation;

public sealed record UiRowValue(string Text, double? CenterX);

public static class UiRows
{
    public static UiRowValue ReadValue(OcrPage page, OcrLine label, string[] aliases)
    {
        var words = page.Lines.SelectMany(line => line.Words)
            .Where(word => word.Bounds.X > label.Bounds.X + label.Bounds.Width
                && word.Bounds.X < page.Width * .62
                && Math.Abs(word.Bounds.CenterY - label.Bounds.CenterY) < Math.Max(14, label.Bounds.Height * .8))
            .OrderBy(word => word.Bounds.X).ToArray();
        if (words.Length == 0)
        {
            // OCR may merge both columns into the same line. Split the label's own
            // words at the recognized alias rather than excluding its whole bounds.
            var alias = aliases.OrderByDescending(a => a.Length).FirstOrDefault(a =>
                OcrPage.Normalize(label.Text).StartsWith(OcrPage.Normalize(a) + " ", StringComparison.Ordinal));
            if (alias is not null)
            {
                var length = OcrPage.Normalize(alias).Length;
                var consumed = 0;
                while (consumed < label.Words.Length && OcrPage.Normalize(string.Join(" ", label.Words.Take(consumed).Select(w => w.Text))).Length < length)
                    consumed++;
                words = label.Words.Skip(consumed).ToArray();
            }
        }
        return new(string.Join(" ", words.Select(w => w.Text)), words.Length == 0 ? null :
            (words.Min(w => w.Bounds.X) + words.Max(w => w.Bounds.X + w.Bounds.Width)) / 2);
    }
}
