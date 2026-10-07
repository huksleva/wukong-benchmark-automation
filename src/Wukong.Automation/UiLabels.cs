using Wukong.Core;

namespace Wukong.Automation;

/// <summary>Allows a small OCR error budget only for long, anchored setting labels.</summary>
public static class UiLabels
{
    public static bool Matches(string observed, string expected)
    {
        var text = OcrPage.Normalize(observed);
        var label = OcrPage.Normalize(expected);
        if (label.Length == 0) return false;
        if (text == label || text.StartsWith(label + " ", StringComparison.Ordinal)) return true;
        if (label.Length < 12) return false;
        var budget = Math.Min(3, label.Length / 8);
        for (var length = Math.Max(1, label.Length - budget); length <= Math.Min(text.Length, label.Length + budget); length++)
        {
            if (length < text.Length && text[length] != ' ') continue;
            if (Distance(text[..length], label) <= budget) return true;
        }
        return false;
    }

    private static int Distance(string a, string b)
    {
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var current = new int[b.Length + 1]; current[0] = i;
            for (var j = 1; j <= b.Length; j++)
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            previous = current;
        }
        return previous[^1];
    }
}
