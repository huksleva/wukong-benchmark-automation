namespace Wukong.Core;

/// <summary>Edits only named sections and retains unrelated settings and comments.</summary>
public sealed class IniDocument
{
    private readonly List<string> lines;
    private readonly string newline;
    public IniDocument(string text)
    {
        newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        lines = text.Replace("\r\n", "\n").Split('\n').ToList();
    }

    public string? FindSectionContaining(string key)
    {
        string? current = null;
        foreach (var line in lines)
        {
            if (Section(line) is { } s) current = s;
            if (IsKey(line, key)) return current;
        }
        return null;
    }

    public string? Get(string section, string key)
    {
        string? current = null;
        foreach (var line in lines)
        {
            if (Section(line) is { } s) current = s;
            if (string.Equals(current, section, StringComparison.OrdinalIgnoreCase) && IsKey(line, key))
                return line[(line.IndexOf('=') + 1)..].Trim();
        }
        return null;
    }

    public void Set(string section, string key, string value)
    {
        var start = lines.FindIndex(l => string.Equals(Section(l), section, StringComparison.OrdinalIgnoreCase));
        if (start < 0)
        {
            if (lines.Count > 0 && lines[^1].Length > 0) lines.Add("");
            lines.Add($"[{section}]");
            lines.Add($"{key}={value}");
            return;
        }
        var end = lines.FindIndex(start + 1, l => Section(l) is not null);
        if (end < 0) end = lines.Count;
        var matching = Enumerable.Range(start + 1, end - start - 1).Where(i => IsKey(lines[i], key)).ToArray();
        if (matching.Length == 0) lines.Insert(end, $"{key}={value}");
        else
        {
            lines[matching[0]] = $"{key}={value}";
            foreach (var index in matching.Skip(1).Reverse()) lines.RemoveAt(index);
        }
    }

    public override string ToString() => string.Join(newline, lines).TrimEnd('\r', '\n') + newline;
    private static string? Section(string line) => line.Trim() is var s && s.StartsWith('[') && s.EndsWith(']') ? s[1..^1] : null;
    private static bool IsKey(string line, string key) => !line.TrimStart().StartsWith(';')
        && line.IndexOf('=') is var i && i >= 0 && string.Equals(line[..i].Trim(), key, StringComparison.OrdinalIgnoreCase);
}
