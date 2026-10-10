using System.Text.Json;
using Wukong.Automation;
using Wukong.Core;

var failures = 0;
var count = 0;
void Test(string name, Action action)
{
    count++;
    try { action(); Console.WriteLine("PASS " + name); }
    catch (Exception e) { failures++; Console.Error.WriteLine("FAIL " + name + ": " + e.Message); }
}
static void Equal<T>(T expected, T actual)
{
    if (!Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}.");
}
static void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception("Expected " + typeof(T).Name);
}
static OcrLine Line(string text, double x, double y, double width = 180) =>
    new(text, [new(text, new(x, y, width, 30))]);
static OcrPage Inline(string avg = "72.5", string min = "44", string max = "98") => new(1920, 1080,
    [Line("Average FPS " + avg, 200, 400), Line("Minimum FPS " + min, 600, 400), Line("Maximum FPS " + max, 1000, 400)]);

Test("inline summary decimal FPS", () => Equal(new BenchmarkMetrics(72.5, 44, 98), ResultParser.Parse(Inline())));
Test("decimal comma OCR", () => Equal(72.5, ResultParser.Parse(Inline("72,5")).AverageFps));
Test("numbers above their labels", () =>
{
    var page = new OcrPage(1920, 1080, [Line("72", 260, 350, 60), Line("Average FPS", 200, 400),
        Line("44", 660, 350, 60), Line("Minimum FPS", 600, 400),
        Line("98", 1060, 350, 60), Line("Maximum FPS", 1000, 400)]);
    Equal(new BenchmarkMetrics(72, 44, 98), ResultParser.Parse(page));
});
Test("ignore graph axis and hardware numbers", () =>
{
    var page = Inline() with { Lines = [.. Inline().Lines, Line("120", 100, 900, 50), Line("NVIDIA RTX 4090", 20, 100), Line("32 GB RAM", 20, 130)] };
    Equal(98d, ResultParser.Parse(page).MaximumFps);
});
Test("reject missing minimum", () => Throws<InvalidDataException>(() => ResultParser.Parse(new(1920, 1080, [Inline().Lines[0], Inline().Lines[2]]))));
Test("reject inconsistent min/average/max", () => Throws<InvalidDataException>(() => ResultParser.Parse(Inline("30", "50", "90"))));
Test("reject zero FPS summary", () => Throws<InvalidDataException>(() => ResultParser.Parse(Inline("0", "0", "0"))));
Test("reject ambiguous nearby metric", () =>
{
    var page = new OcrPage(1920, 1080, [Line("Average FPS", 200, 400), Line("70", 260, 350, 60), Line("80", 260, 450, 60),
        Line("Minimum FPS 40", 600, 400), Line("Maximum FPS 100", 1000, 400)]);
    Throws<InvalidDataException>(() => ResultParser.Parse(page));
});
Test("INI preserves unrelated sections and removes duplicate edited key", () =>
{
    var ini = new IniDocument("; keep me\r\n[Other]\r\nFrameRateLimit=60\r\n[Game]\r\nFrameRateLimit=144\r\nFrameRateLimit=30\r\nUnrelated=yes\r\n");
    ini.Set("Game", "FrameRateLimit", "0");
    Equal("60", ini.Get("Other", "FrameRateLimit"));
    Equal("0", ini.Get("Game", "FrameRateLimit"));
    Equal("yes", ini.Get("Game", "Unrelated"));
    if (!ini.ToString().Contains("; keep me\r\n")) throw new Exception("Comment/newline lost.");
});
Test("INI creates missing scalability section", () =>
{
    var ini = new IniDocument("[Game]\nResolutionSizeX=1920\n");
    ini.Set("ScalabilityGroups", "sg.ShadowQuality", "0");
    Equal("0", new IniDocument(ini.ToString()).Get("ScalabilityGroups", "sg.ShadowQuality"));
    Equal("Game", ini.FindSectionContaining("ResolutionSizeX"));
});
Test("original INI restored byte-for-byte including new files", () =>
{
    var root = Path.Combine(Path.GetTempPath(), "wukong-test-" + Guid.NewGuid().ToString("N"));
    try
    {
        var config = Path.Combine(root, "Config"); Directory.CreateDirectory(config);
        var original = System.Text.Encoding.Unicode.GetBytes("[Game]\r\nResolutionSizeX=1920\r\n");
        File.WriteAllBytes(Path.Combine(config, "GameUserSettings.ini"), original);
        var backup = new SettingsBackup(config, Path.Combine(root, "Results"));
        File.WriteAllText(Path.Combine(config, "GameUserSettings.ini"), "changed");
        File.WriteAllText(Path.Combine(config, "Engine.ini"), "created during run");
        backup.Restore();
        if (!File.ReadAllBytes(Path.Combine(config, "GameUserSettings.ini")).SequenceEqual(original)) throw new Exception("Bytes not restored.");
        Equal(false, File.Exists(Path.Combine(config, "Engine.ini")));
    }
    finally { Directory.Delete(root, true); }
});
Test("invalid runner config rejected", () => Throws<ArgumentException>(() => new RunnerOptions { PollIntervalMilliseconds = 1 }.Validate()));
Test("null UI label dictionary is rejected with a configuration error", () =>
{
    var options = JsonSerializer.Deserialize<RunnerOptions>("{\"labels\":null}", JsonDefaults.Options)!;
    Throws<ArgumentException>(options.Validate);
});
Test("null UI label entry is rejected with a configuration error", () =>
{
    var options = new RunnerOptions(); options.Labels["settings"] = null!;
    Throws<ArgumentException>(options.Validate);
});
Test("missing output directory is rejected before running", () =>
{
    foreach (var directory in new string?[] { null, "", "  " })
        Throws<ArgumentException>(() => new RunnerOptions { OutputDirectory = directory! }.Validate());
});
Test("nonfinite value column is rejected before desktop input", () =>
{
    foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        Throws<ArgumentException>(() => new RunnerOptions { ValueColumnX = value }.Validate());
});
Test("out-of-range percentile is rejected instead of reported as FPS", () =>
{
    foreach (var value in new[] { "1", "999" })
    {
        var page = Inline() with { Lines = [.. Inline().Lines, Line("95% FPS above " + value, 200, 600)] };
        Throws<InvalidDataException>(() => ResultParser.Parse(page));
        Equal(false, ResultParser.TryParse(page, out _));
    }
});
Test("percentile bounds and missing optional percentile remain valid", () =>
{
    foreach (var value in new[] { "44", "98" })
    {
        var page = Inline() with { Lines = [.. Inline().Lines, Line("95% FPS above " + value, 200, 600)] };
        Equal(double.Parse(value), ResultParser.Parse(page).Fps95PercentAbove);
    }
    Equal(null, ResultParser.Parse(Inline()).Fps95PercentAbove);
});
Test("report escapes application strings", () =>
{
    var root = Path.Combine(Path.GetTempPath(), "wukong-report-" + Guid.NewGuid().ToString("N"));
    try
    {
        ReportWriter.Write(root, new("failed", DateTimeOffset.UtcNow,
            new("Windows", ["<script>cpu</script>"], ["GPU"], 32, ["1"], 8, "X64", []), [], "<error>"));
        var html = File.ReadAllText(Path.Combine(root, "report.html"));
        if (html.Contains("<script>")) throw new Exception("Unsafe HTML.");
        Equal("failed", JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "report.json"))).RootElement.GetProperty("status").GetString());
    }
    finally { Directory.Delete(root, true); }
});
Test("startup continue prompt recognized", () =>
    Equal(true, StartupScreen.NeedsContinue(new(1280, 720, [Line("Press any key to continue", 100, 600)]), new RunnerOptions().Labels["continue"])));
Test("loading screen never receives continue input", () =>
    Equal(false, StartupScreen.NeedsContinue(new(1280, 720, [Line("Compiling shaders 20%", 100, 600)]), new RunnerOptions().Labels["continue"])));
Test("startup agreement is explicitly handed to the user", () =>
    Equal(true, StartupScreen.NeedsManualAgreement(new(1280, 720, [Line("Privacy Agreement", 100, 100)]))));
Test("missing Steam gives an installation step", () =>
{
    try { SteamInstallation.DiscoverFromSteamPath(new(), Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".exe")); }
    catch (InstallationIssue issue) { Equal(InstallationProblem.MissingSteam, issue.Problem); return; }
    throw new Exception("Expected missing Steam.");
});
Test("Steam without benchmark distinguishes missing and partial installs", () =>
{
    var root = Path.Combine(Path.GetTempPath(), "wukong-steam-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    try
    {
        var steam = Path.Combine(root, "steam.exe"); File.WriteAllBytes(steam, []);
        InstallationProblem? missing = null;
        try { SteamInstallation.DiscoverFromSteamPath(new(), steam); }
        catch (InstallationIssue issue) { missing = issue.Problem; }
        Equal<InstallationProblem?>(InstallationProblem.MissingBenchmark, missing);
        var app = Path.Combine(root, "game"); Directory.CreateDirectory(Path.Combine(app, "b1"));
        File.WriteAllBytes(Path.Combine(app, "b1_benchmark.exe"), []);
        try { SteamInstallation.DiscoverFromSteamPath(new() { InstallationDirectory = app }, steam); }
        catch (InstallationIssue issue) { Equal(InstallationProblem.IncompleteBenchmark, issue.Problem); return; }
        throw new Exception("Expected partial download to fail before launch.");
    }
    finally { Directory.Delete(root, true); }
});
Test("Steam libraries resolve complete benchmark installation", () =>
{
    var root = Path.Combine(Path.GetTempPath(), "wukong-library-" + Guid.NewGuid().ToString("N"));
    try
    {
        Directory.CreateDirectory(Path.Combine(root, "steamapps"));
        var steam = Path.Combine(root, "steam.exe"); File.WriteAllBytes(steam, []);
        var library = Path.Combine(root, "Library");
        var app = Path.Combine(library, "steamapps", "common", "Benchmark");
        Directory.CreateDirectory(Path.Combine(app, "b1", "Binaries", "Win64"));
        File.WriteAllBytes(Path.Combine(app, "b1_benchmark.exe"), []);
        File.WriteAllBytes(Path.Combine(app, "b1", "Binaries", "Win64", "b1-Win64-Shipping.exe"), []);
        File.WriteAllText(Path.Combine(root, "steamapps", "libraryfolders.vdf"), "\"path\" \"" + library.Replace("\\", "\\\\") + "\"");
        File.WriteAllText(Path.Combine(library, "steamapps", "appmanifest_3132990.acf"), "\"installdir\" \"Benchmark\"\n\"buildid\" \"123\"");
        var installed = SteamInstallation.DiscoverFromSteamPath(new(), steam);
        Equal(Path.GetFullPath(app), installed.GameDirectory);
        Equal("123", installed.BuildId);
    }
    finally { Directory.Delete(root, true); }
});
Test("Russian continue prompt recognized without matching unrelated text", () =>
{
    Equal(true, StartupScreen.NeedsContinue(new(1280, 720, [Line("Нажмите любую клавишу, чтобы продолжить", 100, 600)]), new RunnerOptions().Labels["continue"]));
    Equal(false, StartupScreen.NeedsContinue(new(1280, 720, [Line("Компиляция шейдеров", 100, 600)]), new RunnerOptions().Labels["continue"]));
});
Test("Russian FPS labels are preserved and parsed", () =>
{
    var page = new OcrPage(1280, 720, [Line("Средний FPS 30", 100, 400), Line("Минимальный FPS 20", 400, 400), Line("Максимальный FPS 50", 700, 400)]);
    Equal(new BenchmarkMetrics(30, 20, 50), ResultParser.Parse(page));
    Equal(null, page.Find(""));
});
Test("localized UI values and resolution separators compare correctly", () =>
{
    Equal(UiValues.Normalize("Off"), UiValues.Normalize("Выкл."));
    Equal(UiValues.Normalize("High"), UiValues.Normalize("Высокое"));
    Equal(UiValues.Normalize("1280x720"), UiValues.Normalize("1280 × 720"));
});
Test("actual Russian OCR setting typo resolves without confusing other rows", () =>
{
    Equal(true, UiLabels.Matches("Вертикальная синхщ)низация", "Вертикальная синхронизация"));
    Equal(false, UiLabels.Matches("Порог частоты кадров", "Вертикальная синхронизация"));
    Equal(false, UiLabels.Matches("Уровень полной трассировки лучей", "Полная трассировка лучей"));
    Equal(false, UiLabels.Matches("Выйти", "да"));
    Equal(UiValues.Normalize("1280x720"), UiValues.Normalize("1280х720"));
});
Test("actual settings column left of screen center remains readable", () =>
{
    var label = Line("Вертикальная синхронизация", 226, 440, 210);
    var page = new OcrPage(1280, 720, [label, Line("Выкл.", 573, 440, 40), Line("Help text", 800, 440)]);
    var value = UiRows.ReadValue(page, label, ["вертикальная синхронизация"]);
    Equal("Выкл.", value.Text);
    Equal<double?>(593d, value.CenterX);
});
Test("merged OCR label and value have distinct input target", () =>
{
    var label = new OcrLine("Frame Generation Off", [new("Frame", new(200, 400, 60, 20)), new("Generation", new(270, 400, 100, 20)), new("Off", new(570, 400, 40, 20))]);
    var value = UiRows.ReadValue(new(1280, 720, [label]), label, ["frame generation"]);
    Equal("Off", value.Text);
    Equal<double?>(590d, value.CenterX);
});
Test("default and distributed runner configuration can actually launch", () =>
{
    new RunnerOptions().Validate();
    var config = Path.Combine(AppContext.BaseDirectory, "runner.example.json");
    RunnerOptions.Load(config).Validate();
});
Test("actual graphics confirmation applies instead of discarding settings", () =>
{
    var apply = Line("Применить", 550, 400);
    var page = new OcrPage(1280, 720, [Line("Вы точно хотите использовать новые настройки", 300, 300, 600), apply, Line("Закрыть без изменений", 500, 500)]);
    var options = new RunnerOptions();
    Equal(apply, UiDialogs.Confirmation(page, options.Labels["confirm"], options.Labels["apply"]));
});
Test("footer confirmation and legal agreements are never autoaccepted", () =>
{
    var options = new RunnerOptions();
    Equal(null, UiDialogs.Confirmation(new(1280, 720, [Line("Подтвердить", 900, 680)]), options.Labels["confirm"], options.Labels["apply"]));
    Equal(null, UiDialogs.Confirmation(new(1280, 720, [Line("Пользовательское соглашение", 300, 200), Line("Да", 550, 400)]), options.Labels["confirm"], options.Labels["apply"]));
});
Test("missing average never borrows the neighboring maximum", () =>
{
    var page = new OcrPage(1280, 720, [Line("В среднем", 48, 220, 70), Line("Максимум", 48, 292, 70),
        Line("31 FPS", 48, 314, 70), Line("Минимум", 212, 292, 70), Line("22 FPS", 212, 314, 70)]);
    Throws<InvalidDataException>(() => ResultParser.Parse(page));
});
Test("percentile label number is not the percentile value", () =>
{
    var page = Inline() with { Lines = [.. Inline().Lines, Line("5-й перцентиль", 200, 600), Line("64 FPS", 220, 650, 60)] };
    Equal(64d, ResultParser.Parse(page).Fps95PercentAbove);
});
Test("local numeric OCR reads actual stylized average FPS", () =>
{
    using var numeric = new NumericOcr();
    var image = Path.Combine(AppContext.BaseDirectory, "Fixtures", "actual-cpu-summary.png");
    Equal("27 FPS", numeric.ReadConsensus(image, new(40, 210, 160, 75))?.Text);
});
Test("actual split graphics tab is recognized only in navigation column", () =>
{
    var tab = Line("Граф И Ка", 220, 470, 120);
    var page = new OcrPage(1920, 1080, [Line("Графика", 520, 220), tab]);
    Equal(tab, UiTabs.Find(page, ["графика"]));
    Equal(null, UiTabs.Find(new(1920, 1080, [page.Lines[0]]), ["графика"]));
});
Test("actual Russian maximum preset and observed Off glyph read correctly", () =>
{
    Equal(UiValues.Normalize("Cinematic"), UiValues.Normalize("Реалистичн."));
    Equal(UiValues.Normalize("Off"), UiValues.Normalize("Выюл."));
});
Test("zero minimum is valid for a nonzero completed summary", () =>
    Equal(new BenchmarkMetrics(5, 0, 8), ResultParser.Parse(Inline("5", "0", "8"))));
Test("agreeing OCR sources do not create a conflicting average", () =>
{
    var page = new OcrPage(1920, 1080, [Line("Average FPS", 200, 400), Line("28 FPS", 260, 350, 60),
        Line("28 FPS", 261, 350, 60), Line("Minimum FPS 21", 600, 400), Line("Maximum FPS 32", 1000, 400)]);
    Equal(28d, ResultParser.Parse(page).AverageFps);
});
Test("duplicate agreeing sources cannot hide a nearby conflicting value", () =>
{
    var page = new OcrPage(1920, 1080, [Line("Average FPS", 200, 400), Line("28 FPS", 260, 350, 60),
        Line("28 FPS", 261, 350, 60), Line("29 FPS", 262, 350, 60),
        Line("Minimum FPS 21", 600, 400), Line("Maximum FPS 32", 1000, 400)]);
    Throws<InvalidDataException>(() => ResultParser.Parse(page));
});
Test("missing HTML handler preserves console access to completed metrics", () =>
{
    var now = DateTimeOffset.UtcNow;
    PassReport Pass(string name, BenchmarkMetrics metrics) => new(name, now, now, "fixture", metrics,
        [], "result.png", "result-ocr.json", []);
    var output = new List<string>();
    var directory = Path.Combine(Path.GetTempPath(), "wukong browser fallback fixture");
    var opened = ReportPresentation.Show(directory,
        [Pass("CPU", new(27, 22, 32)), Pass("GPU", new(2, 2, 2))], output.Add,
        path => { Equal(Path.Combine(directory, "report.html"), path); throw new System.ComponentModel.Win32Exception(1155); });
    Equal(false, opened);
    Equal(true, output.Any(line => line.Contains("CPU:") && line.Contains("27 FPS") && line.Contains("min 22") && line.Contains("max 32")));
    Equal(true, output.Any(line => line.Contains("GPU:") && line.Contains("2 FPS")));
    Equal(true, output.Any(line => line == "HTML: " + Path.Combine(directory, "report.html")));
    Equal(true, output.Any(line => line == "JSON: " + Path.Combine(directory, "report.json")));
    Equal(true, output.Any(line => line.Contains("Results saved")));
});

Console.WriteLine($"{count - failures}/{count} tests passed.");
return failures == 0 ? 0 : 1;
