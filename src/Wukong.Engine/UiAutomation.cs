using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Wukong.Core;

namespace Wukong.Automation;

public sealed class UiAutomation(IBenchmarkWindow window, IBenchmarkOcr ocr, RunnerOptions options, string directory, Action<string> log)
{
    private int captureIndex;
    private string? latestCapture;
    public async Task<OcrPage> ObserveAsync(string name, CancellationToken token)
    {
        var slot = captureIndex++ % options.MaxDiagnosticFrames;
        var path = Path.Combine(directory, $"diagnostic-{slot:0000}.png");
        for (var attempt = 0; ; attempt++)
        {
            token.ThrowIfCancellationRequested();
            try { window.Capture(path); break; }
            catch (InvalidOperationException ex) when (attempt < 3 && ex.Message.StartsWith("Cannot focus", StringComparison.Ordinal))
            { log("Waiting for access to the benchmark window..."); await Task.Delay(2000, token); }
        }
        latestCapture = path;
        var page = name == "benchmark" ? await ocr.ReadResultAsync(path, token) : await ocr.ReadAsync(path, token);
        File.WriteAllText(Path.ChangeExtension(path, ".json"), JsonSerializer.Serialize(new
        { page.Width, page.Height, page.Lines, step = name, observedAt = DateTimeOffset.Now }, JsonDefaults.Options));
        return page;
    }

    private async Task Pause(CancellationToken token) => await Task.Delay(options.PollIntervalMilliseconds, token);
    private string[] Labels(string key) => options.Labels[key];
    private static bool Exact(string a, string b) => OcrPage.Normalize(a) == OcrPage.Normalize(b);
    private OcrLine? Label(OcrPage page, string key)
    {
        var isSetting = key is not ("start" or "continue" or "settings" or "confirm" or "apply");
        var candidates = page.Lines.Where(line => !isSetting || line.Bounds.X < page.Width * .62)
            .Where(line => key != "apply" || (!OcrPage.Normalize(line.Text).Contains("recommended", StringComparison.Ordinal)
                && !OcrPage.Normalize(line.Text).Contains("рекомендуем", StringComparison.Ordinal)))
            .Where(line => key != "rayTracing" || (line.Bounds.X >= page.Width * .17
                && !OcrPage.Normalize(line.Text).Contains("nv", StringComparison.Ordinal)))
            .Where(line => !(key == "superResolutionScale" && new[] { "sampling", "технология", "выборка" }.Any(word => OcrPage.Normalize(line.Text).Contains(word, StringComparison.Ordinal)))
                && !(key == "rayTracing" && new[] { "level", "quality", "уровень", "качество" }.Any(word => OcrPage.Normalize(line.Text).Contains(word, StringComparison.Ordinal)))).ToArray();
        return candidates.FirstOrDefault(line => Labels(key).Any(label => Exact(line.Text, label)))
            ?? candidates.FirstOrDefault(line => Labels(key).Any(label => UiLabels.Matches(line.Text, label)))
            ?? (!isSetting ? page.Find(Labels(key)) : null);
    }

    private OcrLine? Confirmation(OcrPage page) => UiDialogs.Confirmation(page, Labels("confirm"), Labels("apply"));

    public async Task WaitForMenuAsync(CancellationToken token)
    {
        var time = Stopwatch.StartNew();
        var continues = 0;
        while (time.Elapsed.TotalSeconds < options.StartupTimeoutSeconds)
        {
            await Pause(token);
            var page = await ObserveAsync("startup", token);
            if (StartupScreen.NeedsManualAgreement(page))
                throw new InvalidOperationException("First launch requires your agreement in Benchmark Tool. Open it once, review the terms yourself, then close it and restart the runner.");
            if (Label(page, "settings") is not null || Label(page, "preset") is not null || Label(page, "start") is not null)
                return;
            if (StartupScreen.NeedsContinue(page, Labels("continue")) && continues < 5)
            {
                var prompt = Label(page, "continue")!;
                log("Startup: continue prompt detected; sending input to the benchmark window.");
                if (continues++ < 2) window.Key(BenchmarkKey.Enter);
                else window.Click(prompt.Bounds.CenterX, prompt.Bounds.CenterY);
            }
            if ((int)time.Elapsed.TotalSeconds % 30 < options.PollIntervalMilliseconds / 1000)
                log($"Waiting for loading/shaders/main menu: {(int)time.Elapsed.TotalSeconds}s.");
        }
        throw new TimeoutException("Startup screen was not recognized. See startup screenshots; English and Russian menus are supported.");
    }

    public async Task<(List<SettingEvidence> Settings, List<string> Warnings)> ApplyProfileAsync(BenchmarkProfile profile, CancellationToken token)
    {
        log($"Configuring {profile.Name} profile in Benchmark Tool...");
        var settings = new List<SettingEvidence>();
        var warnings = new List<string>();
        if (profile.Name == "CPU") warnings.Add("CPU-biased settings do not prove a CPU bottleneck. GPU utilization is not measured; a weak GPU can still limit this pass.");
        var page = await ObserveAsync("menu", token);
        if (Label(page, "settings") is { } button && Label(page, "preset") is null)
        {
            window.Click(button.Bounds.CenterX, button.Bounds.CenterY);
            await Pause(token);
        }
        await ClickTabAsync("displayTab", token);
        settings.Add(await SetChoiceAsync("vsync", ["Off"], token));
        settings.Add(await SetChoiceAsync("frameCap", ["Off", "Unlimited", "No Limit", "Unlimited FPS"], token));
        settings.Add(await SetChoiceAsync("motionBlur", ["Off"], token));
        var resolution = await ReadRowAsync("resolution", token);
        var digits = new string(resolution.Value.Where(char.IsDigit).ToArray());
        var requested = $"{profile.Width}{profile.Height}";
        if (!digits.Contains(requested, StringComparison.Ordinal))
        {
            await SetChoiceAsync("resolution", [$"{profile.Width}x{profile.Height}"], token, steps: 24);
            resolution = await ReadRowAsync("resolution", token);
            if (!new string(resolution.Value.Where(char.IsDigit).ToArray()).Contains(requested, StringComparison.Ordinal))
                throw new InvalidOperationException($"The benchmark did not offer the requested resolution {profile.Width}x{profile.Height}.");
        }
        settings.Add(new("Display resolution", resolution.Value, "UI verified"));
        await ClickTabAsync("graphicsTab", token);
        settings.Add(await SetChoiceAsync("preset", [profile.Preset], token));
        settings.Add(await SetChoiceAsync("superResolutionMode", ["TSR"], token));
        settings.Add(await SetChoiceAsync("frameGeneration", ["Off"], token));
        settings.Add(await SetChoiceAsync("superResolutionScale", [profile.ResolutionScale.ToString(CultureInfo.InvariantCulture)], token, steps: 110));
        if (profile.Name == "CPU") settings.Add(await SetChoiceAsync("viewDistance", ["High"], token));
        var rt = await FindRowAsync("rayTracing", token, optional: true);
        if (rt is not null && string.IsNullOrWhiteSpace(OcrPage.Normalize(rt.Value)))
        {
            window.Click(rt.Label.Bounds.CenterX, rt.Label.Bounds.CenterY);
            await Pause(token);
            var support = await ObserveAsync("ray-tracing-support", token);
            if (support.Find("your graphics card does not support", "ваша видеокарта не поддерживает") is not null) rt = null;
        }
        if (rt is null)
        {
            if (profile.RayTracing) warnings.Add("Full ray tracing was not available as a verifiable UI control on this hardware/build; GPU pass uses cinematic raster graphics.");
            settings.Add(new("Full ray tracing", "Unavailable", "No available UI control was found"));
        }
        else
        {
            if (profile.RayTracing)
            {
                // A supported option must visibly change; unsupported hardware may expose
                // a disabled row. Never report ray tracing as active without that evidence.
                var enabled = await TrySetChoiceAsync("rayTracing", ["On", "Very High"], token, 6);
                if (enabled is null)
                {
                    settings.Add(await SetChoiceAsync("rayTracing", ["Off"], token));
                    warnings.Add("Full ray tracing could not be enabled; GPU pass uses cinematic raster graphics.");
                }
                else
                {
                    settings.Add(enabled);
                    if (await FindRowAsync("rayTracingQuality", token, optional: true) is not null)
                        settings.Add(await SetChoiceAsync("rayTracingQuality", ["Very High"], token));
                }
            }
            else settings.Add(await SetChoiceAsync("rayTracing", ["Off"], token));
        }
        await ApplyAsync(token);
        log("Graphics applied; checking final settings...");
        // A preset or RT switch can override another choice. Check the final state
        // after Apply rather than accepting each intermediate selection as effective.
        await ClickTabAsync("displayTab", token);
        await VerifyChoiceAsync("vsync", ["Off"], token);
        await VerifyChoiceAsync("frameCap", ["Off", "Unlimited", "No Limit", "Unlimited FPS"], token);
        await VerifyChoiceAsync("motionBlur", ["Off"], token);
        var finalResolution = await ReadRowAsync("resolution", token);
        if (!new string(finalResolution.Value.Where(char.IsDigit).ToArray()).Contains(requested, StringComparison.Ordinal))
            throw new InvalidOperationException("Display resolution changed after Apply.");
        SaveEvidence("display-settings");
        await ClickTabAsync("graphicsTab", token);
        await VerifyChoiceAsync("superResolutionMode", ["TSR"], token);
        await VerifyChoiceAsync("superResolutionScale", [profile.ResolutionScale.ToString(CultureInfo.InvariantCulture)], token);
        await VerifyChoiceAsync("frameGeneration", ["Off"], token);
        if (profile.Name == "CPU") await VerifyChoiceAsync("viewDistance", ["High"], token);
        var recordedRt = settings.First(s => Exact(s.Label, Labels("rayTracing")[0]));
        if (recordedRt.Value != "Unavailable") await VerifyChoiceAsync("rayTracing", [recordedRt.Value], token);
        var recordedRtQuality = settings.FirstOrDefault(s => Exact(s.Label, Labels("rayTracingQuality")[0]));
        if (recordedRtQuality is not null) await VerifyChoiceAsync("rayTracingQuality", [recordedRtQuality.Value], token);
        var finalPreset = await ReadRowAsync("preset", token);
        // High view distance intentionally turns the Low preset into Custom.
        if (profile.Name == "GPU" && !Matches(finalPreset.Value, ["Cinematic", "Custom"]))
            throw new InvalidOperationException("GPU graphics preset changed unexpectedly after Apply.");
        settings.Add(new("Effective graphics preset", finalPreset.Value, "UI verified after Apply"));
        SaveEvidence("graphics-settings");
        log($"{profile.Name} profile applied; final values verified after Apply.");
        return (settings, warnings);
    }

    private void SaveEvidence(string name)
    {
        if (latestCapture is null) return;
        File.Copy(latestCapture, Path.Combine(directory, name + ".png"), true);
        File.Copy(Path.ChangeExtension(latestCapture, ".json"), Path.Combine(directory, name + ".json"), true);
    }

    private async Task VerifyChoiceAsync(string key, string[] choices, CancellationToken token)
    {
        var row = await ReadRowAsync(key, token);
        if (!Matches(row.Value, choices)) throw new InvalidOperationException($"After Apply, {key} is '{row.Value}', expected {string.Join(" / ", choices)}.");
    }

    private async Task ClickTabAsync(string key, CancellationToken token)
    {
        // Category navigation is in the left column. OCR may split a word
        // (actual Russian 'Граф И Ка'); never match the panel heading instead.
        OcrLine? FindTab(OcrPage page) => UiTabs.Find(page, Labels(key));
        var page = await ObserveAsync(key, token);
        var tab = FindTab(page);
        for (var attempt = 0; tab is null && attempt < 4; attempt++)
        {
            // A pending apply dialog is another navigation layer. Confirm it
            // before leaving the panel; never discard the requested profile.
            if (Confirmation(page) is { } pending)
                window.Click(pending.Bounds.CenterX, pending.Bounds.CenterY);
            else window.Key(BenchmarkKey.Escape);
            await Pause(token);
            page = await ObserveAsync(key + "-navigation", token);
            tab = FindTab(page);
        }
        if (tab is null) throw new InvalidOperationException($"Cannot find {key} tab.");
        window.Click(tab.Bounds.CenterX, tab.Bounds.CenterY);
        await Pause(token);
        window.Scroll(20);
        await Pause(token);
    }

    private sealed record Row(OcrPage Page, OcrLine Label, string Value, double ValueX);
    private Row? RowFrom(OcrPage page, string key)
    {
        var label = Label(page, key);
        if (label is null) return null;
        var value = UiRows.ReadValue(page, label, Labels(key));
        return new(page, label, value.Text, value.CenterX ?? page.Width * options.ValueColumnX);
    }

    private async Task<Row?> FindRowAsync(string key, CancellationToken token, bool optional = false)
    {
        window.Scroll(20);
        await Pause(token);
        for (var attempt = 0; attempt < 7; attempt++)
        {
            var page = await ObserveAsync(key, token);
            if (RowFrom(page, key) is { } row)
            {
                return await EnrichRowAsync(row, key, token);
            }
            window.Scroll(-3);
            await Pause(token);
        }
        if (optional) return null;
        throw new InvalidOperationException($"Cannot find setting '{key}'. Review screenshots and configure labels for this build.");
    }

    private async Task<Row> EnrichRowAsync(Row row, string key, CancellationToken token)
    {
        if (latestCapture is null) return row;
        if (key == "superResolutionScale")
        {
            // Windows OCR is sensitive to the context around this small numeric
            // badge. Use several padded row crops; accept only agreeing digits.
            var readings = new List<UiRowValue>();
            foreach (var fraction in new[] { 1d / 12, 1d / 7.2, 1d / 9 })
            {
                var height = row.Page.Height * fraction;
                var region = new Box(row.Page.Width * .15625, Math.Max(0, row.Label.Bounds.CenterY - height / 2), row.Page.Width * .43, height);
                var detail = await ocr.ReadRegionAsync(latestCapture, region, token, numericOnly: true);
                var value = UiRows.ReadValue(detail, row.Label, Labels(key));
                if (int.TryParse(value.Text.Trim().TrimEnd('%'), out var number) && number is >= 10 and <= 100)
                    readings.Add(value);
            }
            var distinct = readings.Select(r => r.Text).Distinct().ToArray();
            if (distinct.Length > 1) throw new InvalidOperationException("Render scale OCR is ambiguous; refusing to guess the setting.");
            if (readings.Count >= 2) row = row with { Value = readings[0].Text, ValueX = readings[0].CenterX ?? row.ValueX };
        }
        else if (string.IsNullOrWhiteSpace(row.Value))
        {
            var height = Math.Max(40, row.Label.Bounds.Height * 2);
            var region = new Box(row.Page.Width * .16, Math.Max(0, row.Label.Bounds.CenterY - height / 2), row.Page.Width * .46, height);
            var detail = await ocr.ReadRegionAsync(latestCapture, region, token);
            var value = UiRows.ReadValue(detail, row.Label, Labels(key));
            if (!string.IsNullOrWhiteSpace(value.Text)) row = row with { Value = value.Text, ValueX = value.CenterX ?? row.ValueX };
        }
        return row;
    }

    private async Task<Row> ReadRowAsync(string key, CancellationToken token) =>
        await FindRowAsync(key, token) ?? throw new InvalidOperationException(key);

    private static bool Matches(string value, string[] choices) => choices.Any(c => UiValues.Normalize(value) == UiValues.Normalize(c));
    private async Task<SettingEvidence?> TrySetChoiceAsync(string key, string[] choices, CancellationToken token, int steps)
    {
        var row = await ReadRowAsync(key, token);
        if (Matches(row.Value, choices)) return new(Labels(key)[0], row.Value, "UI verified");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Numeric sliders are moved left to their lower bound before incrementing.
        if (choices.Length == 1 && int.TryParse(choices[0], out var target))
        {
            window.Click(row.ValueX, row.Label.Bounds.CenterY);
            // 100 is the render-scale upper bound. Reach the bound first, then
            // verify through OCR; avoid 75 full captures for individual increments.
            if (key == "superResolutionScale" && target == 100)
            {
                for (var i = 0; i < 110; i++) { window.Key(BenchmarkKey.Right); await Task.Delay(60, token); }
                await Pause(token);
                var maximum = await ReadRowAsync(key, token);
                return Matches(maximum.Value, choices) ? new(Labels(key)[0], maximum.Value, "UI verified") : null;
            }
            for (var i = 0; i < 110; i++) { window.Key(BenchmarkKey.Left); await Task.Delay(12, token); }
            for (var i = 0; i < steps; i++)
            {
                var current = RowFrom(await ObserveAsync(key + "-slider", token), key);
                if (current is null) break;
                current = await EnrichRowAsync(current, key, token);
                if (Matches(current.Value, choices)) return new(Labels(key)[0], current.Value, "UI verified");
                if (int.TryParse(current.Value.Trim().TrimEnd('%'), out var numeric) && numeric > target) break;
                window.Key(BenchmarkKey.Right);
                await Task.Delay(60, token);
            }
            return null;
        }
        for (var direction = 0; direction < 2; direction++)
        {
            seen.Clear();
            for (var i = 0; i < steps; i++)
            {
                if (Matches(row.Value, choices)) return new(Labels(key)[0], row.Value, "UI verified");
                if (!seen.Add(row.Value)) break;
                window.Click(row.ValueX, row.Label.Bounds.CenterY);
                window.Key(direction == 0 ? BenchmarkKey.Right : BenchmarkKey.Left);
                await Pause(token);
                var page = await ObserveAsync(key + "-change", token);
                // RT toggles may display a known restart/confirmation dialog.
                if (Confirmation(page) is { } confirm)
                {
                    window.Click(confirm.Bounds.CenterX, confirm.Bounds.CenterY);
                    await Pause(token);
                    page = await ObserveAsync(key + "-confirmed", token);
                }
                row = RowFrom(page, key) ?? throw new InvalidOperationException($"Setting {key} disappeared after input.");
            }
        }
        if (Matches(row.Value, choices)) return new(Labels(key)[0], row.Value, "UI verified");
        return null;
    }

    private async Task<SettingEvidence> SetChoiceAsync(string key, string[] choices, CancellationToken token, int steps = 12)
    {
        var evidence = await TrySetChoiceAsync(key, choices, token, steps)
            ?? throw new InvalidOperationException($"Cannot set '{key}' to {string.Join(" / ", choices)}. See OCR evidence.");
        log($"Setting verified: {evidence.Label} = {evidence.Value}.");
        return evidence;
    }

    private async Task ApplyAsync(CancellationToken token)
    {
        var page = await ObserveAsync("before-apply", token);
        if (Label(page, "apply") is { } apply)
        {
            // The bottom Apply caption is a keyboard hint, not a clickable button.
            // Use the game's T command only after recognizing that hint.
            if (apply.Bounds.CenterY > page.Height * .8) window.Key(BenchmarkKey.Apply);
            else window.Click(apply.Bounds.CenterX, apply.Bounds.CenterY);
            await Pause(token);
            var confirmation = await ObserveAsync("after-apply", token);
            if (Confirmation(confirmation) is { } confirm)
            { window.Click(confirm.Bounds.CenterX, confirm.Bounds.CenterY); await Pause(token); }
        }
    }

    public async Task<(BenchmarkMetrics Metrics, string Screenshot, string Ocr)> RunBenchmarkAsync(CancellationToken token)
    {
        var page = await ObserveAsync("before-start", token);
        bool InSettings(OcrPage observed) => observed.Lines.SelectMany(line => line.Words).Any(word =>
            Labels("displayTab").Concat(Labels("graphicsTab")).Any(label => Exact(word.Text, label)))
            || Label(observed, "preset") is not null;
        // Escape leaves a category, then the settings list. Do not click the
        // similarly named Benchmark category while still inside Settings.
        for (var attempt = 0; attempt < 3 && (InSettings(page) || Label(page, "settings") is null); attempt++)
        {
            window.Key(BenchmarkKey.Escape);
            await Pause(token);
            page = await ObserveAsync("main-menu", token);
        }
        if (InSettings(page)) throw new InvalidOperationException("Could not return from Settings to the main menu.");
        var start = Label(page, "start") ?? throw new InvalidOperationException("Cannot find Run/Start Benchmark on the main menu.");
        window.Click(start.Bounds.CenterX, start.Bounds.CenterY);
        await Pause(token);
        page = await ObserveAsync("start-confirmation", token);
        if (Confirmation(page) is { } confirmation)
            window.Click(confirmation.Bounds.CenterX, confirmation.Bounds.CenterY);
        log("Benchmark requested; waiting for its built-in result screen...");
        var time = Stopwatch.StartNew();
        var leftMenu = false;
        BenchmarkMetrics? previous = null;
        while (time.Elapsed.TotalSeconds < options.BenchmarkTimeoutSeconds)
        {
            await Pause(token);
            page = await ObserveAsync("benchmark", token);
            if (!leftMenu) leftMenu = Label(page, "start") is null && Label(page, "preset") is null;
            if (!leftMenu) continue;
            if (!ResultParser.TryParse(page, out var metrics)) { previous = null; continue; }
            if (metrics != previous) { previous = metrics; continue; }
            var screenshot = Path.Combine(directory, "result.png");
            window.Capture(screenshot);
            var final = await ocr.ReadResultAsync(screenshot, token);
            if (!ResultParser.TryParse(final, out var verified) || verified != metrics)
            { previous = null; continue; }
            var raw = Path.Combine(directory, "result-ocr.json");
            File.WriteAllText(raw, JsonSerializer.Serialize(final, JsonDefaults.Options));
            log($"Completed: average {metrics!.AverageFps}, min {metrics.MinimumFps}, max {metrics.MaximumFps} FPS.");
            return (metrics, screenshot, raw);
        }
        throw new TimeoutException("Benchmark result did not appear before the deadline. See screenshots; no FPS values were fabricated.");
    }
}
