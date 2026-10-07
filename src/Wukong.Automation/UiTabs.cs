using Wukong.Core;

namespace Wukong.Automation;

public static class UiTabs
{
    public static OcrLine? Find(OcrPage page, string[] labels)
    {
        static string Compact(string text) => OcrPage.Normalize(text).Replace(" ", "");
        bool Match(string text) => labels.Any(label => Compact(text) == Compact(label));
        var column = page.Lines.Where(line => line.Bounds.CenterX < page.Width * .17).ToArray();
        return column.FirstOrDefault(line => Match(line.Text))
            ?? column.SelectMany(line => line.Words).Where(word => Match(word.Text))
                .Select(word => new OcrLine(word.Text, [word])).FirstOrDefault();
    }
}
