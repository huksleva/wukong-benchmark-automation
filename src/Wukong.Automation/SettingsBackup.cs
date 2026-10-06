using System.Text;
using System.Text.Json;
using Wukong.Core;

namespace Wukong.Automation;

public sealed class SettingsBackup
{
    private readonly string directory;
    private readonly string backupDirectory;
    private readonly Dictionary<string, byte[]> originals = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> touched = new(StringComparer.OrdinalIgnoreCase);
    public string DirectoryPath => directory;

    public SettingsBackup(string directory, string output)
    {
        this.directory = Path.GetFullPath(directory);
        backupDirectory = Path.Combine(output, "backup");
        Directory.CreateDirectory(backupDirectory);
        if (Directory.Exists(this.directory))
            foreach (var path in Directory.GetFiles(this.directory, "*.ini", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(path);
                originals[name] = File.ReadAllBytes(path);
                File.WriteAllBytes(Path.Combine(backupDirectory, name), originals[name]);
            }
        File.WriteAllText(Path.Combine(backupDirectory, "manifest.json"), JsonSerializer.Serialize(new
        { configDirectory = this.directory, existedFiles = originals.Keys.ToArray() }, JsonDefaults.Options));
    }

    public IReadOnlyList<SettingEvidence> ApplyKnownIni(BenchmarkProfile profile)
    {
        var path = Path.Combine(directory, "GameUserSettings.ini");
        if (!File.Exists(path)) return [];
        var ini = new IniDocument(File.ReadAllText(path));
        var section = ini.FindSectionContaining("ResolutionSizeX");
        if (section is null) return []; // Do not guess a game's custom settings class.
        var values = new Dictionary<string, string>
        {
            ["ResolutionSizeX"] = profile.Width.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["ResolutionSizeY"] = profile.Height.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["LastUserConfirmedResolutionSizeX"] = profile.Width.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["LastUserConfirmedResolutionSizeY"] = profile.Height.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["FullscreenMode"] = profile.Name == "GPU" ? "1" : "2", ["LastConfirmedFullscreenMode"] = profile.Name == "GPU" ? "1" : "2",
            ["bUseVSync"] = "False", ["FrameRateLimit"] = "0.000000", ["bUseDynamicResolution"] = "False"
        };
        foreach (var (k, v) in values) ini.Set(section, k, v);
        foreach (var key in new[] { "ViewDistance", "AntiAliasing", "Shadow", "GlobalIllumination", "Reflection", "PostProcess", "Texture", "Effects", "Foliage", "Shading" })
            ini.Set("ScalabilityGroups", $"sg.{key}Quality", profile.Name == "GPU" ? "4" : key == "ViewDistance" ? "3" : "0");
        touched.Add("GameUserSettings.ini");
        File.WriteAllText(path, ini.ToString(), new UTF8Encoding(false));
        return values.Select(v => new SettingEvidence(v.Key, v.Value, "INI request; effective settings are verified separately in the UI")).ToArray();
    }

    public void SaveEffective(string destination)
    {
        Directory.CreateDirectory(destination);
        if (!Directory.Exists(directory)) return;
        foreach (var file in Directory.GetFiles(directory, "*.ini", SearchOption.TopDirectoryOnly))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
    }

    public void Restore()
    {
        // The application may write settings via its UI, including newly-created INIs.
        if (Directory.Exists(directory))
            foreach (var path in Directory.GetFiles(directory, "*.ini", SearchOption.TopDirectoryOnly))
                touched.Add(Path.GetFileName(path));
        foreach (var name in touched.Union(originals.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var path = Path.Combine(directory, name);
            if (originals.TryGetValue(name, out var bytes)) File.WriteAllBytes(path, bytes);
            else if (File.Exists(path)) File.Delete(path);
        }
    }
}
