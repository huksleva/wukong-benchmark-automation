using System.Text.Json;
using Wukong.Automation;
using Wukong.Core;

// These tests exercise the shared state machine with adapter doubles, not a game.
var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures");
var actual = JsonSerializer.Deserialize<OcrPage>(File.ReadAllText(Path.Combine(fixture, "result-ocr.json")), JsonDefaults.Options)!;
var failures = 0;
var count = 0;
static OcrLine Line(string text, double y) => new(text, [new(text, new(100, y, 400, 30))]);
static OcrPage Page(params string[] lines) => new(1280, 720, lines.Select((text, i) => Line(text, 100 + i * 50)).ToArray());
var menu = Page("Settings", "Start Benchmark");

async Task Test(string name, Func<UiAutomation, WindowDouble, Task> check, params OcrPage[] pages)
{
    count++;
    var tempRoot = Path.GetFullPath(Path.GetTempPath());
    var directory = Directory.CreateTempSubdirectory("wukong-engine-").FullName;
    try
    {
        var window = new WindowDouble(Path.Combine(fixture, "result.png"));
        using var ocr = new OcrDouble(pages);
        var engine = new UiAutomation(window, ocr, new RunnerOptions { PollIntervalMilliseconds = 500 }, directory, _ => { });
        await check(engine, window);
        Console.WriteLine("PASS " + name);
    }
    catch (Exception ex) { failures++; Console.Error.WriteLine("FAIL " + name + ": " + ex.Message); }
    finally
    {
        if (!Path.GetFullPath(directory).StartsWith(tempRoot, StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected test path");
        Directory.Delete(directory, true);
    }
}
static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

foreach (var prompt in new[] { "Press any key", "Нажмите любую клавишу" })
    await Test("continue input only after known prompt: " + prompt, async (engine, window) =>
    {
        await engine.WaitForMenuAsync(CancellationToken.None);
        Require(window.Keys.SequenceEqual(new[] { BenchmarkKey.Enter }), "Expected only the continue key; shader/loading screens must receive no input");
        Require(window.Clicks == 0, "Unexpected click");
    }, Page("Compiling shaders"), Page(prompt), menu);

await Test("agreement is handed to the user without input", async (engine, window) =>
{
    try { await engine.WaitForMenuAsync(CancellationToken.None); throw new Exception("Agreement was silently accepted"); }
    catch (InvalidOperationException ex) when (ex.Message.Contains("agreement")) { }
    Require(window.Keys.Count == 0 && window.Clicks == 0, "Agreement received input");
}, Page("User agreement", "Press any key"));

await Test("cancel before input or OCR", async (engine, window) =>
{
    using var token = new CancellationTokenSource(); token.Cancel();
    try { await engine.WaitForMenuAsync(token.Token); throw new Exception("Cancellation ignored"); }
    catch (OperationCanceledException) { }
    Require(window.Keys.Count == 0 && window.Clicks == 0 && window.Captures == 0, "Cancelled scenario touched the desktop");
});

var transient = Page("Average FPS 28", "Minimum FPS 22", "Maximum FPS 32", "95% FPS above 24");
await Test("transient result is rejected until the screen stabilizes", async (engine, window) =>
{
    var result = await engine.RunBenchmarkAsync(CancellationToken.None);
    Require(result.Metrics == new BenchmarkMetrics(27, 22, 32, 24), "Returned an unstable or altered result");
    Require(File.Exists(result.Screenshot) && File.Exists(result.Ocr), "Missing evidence");
    Require(window.Clicks == 1, "Benchmark was not started exactly once");
}, menu, Page(), actual, transient, actual, actual, actual);
Console.WriteLine($"{count - failures}/{count} portable engine checks passed. No benchmark was launched.");
return failures == 0 ? 0 : 1;

sealed class WindowDouble(string fixture) : IBenchmarkWindow
{
    public List<BenchmarkKey> Keys { get; } = [];
    public int Clicks { get; private set; }
    public int Captures { get; private set; }
    public void Capture(string path) { Captures++; File.Copy(fixture, path, true); }
    public void Click(double x, double y) { Clicks++; }
    public void Scroll(int ticks) { }
    public void Key(BenchmarkKey key) => Keys.Add(key);
}
sealed class OcrDouble(OcrPage[] pages) : IBenchmarkOcr
{
    private readonly Queue<OcrPage> frames = new(pages);
    public Task<OcrPage> ReadAsync(string path, CancellationToken token, bool numericOnly = false, int targetWidth = 1920)
    { token.ThrowIfCancellationRequested(); return Task.FromResult(frames.Dequeue()); }
    public Task<OcrPage> ReadResultAsync(string path, CancellationToken token) => ReadAsync(path, token);
    public Task<OcrPage> ReadRegionAsync(string path, Box region, CancellationToken token, bool numericOnly = false, int targetWidth = 1920) => ReadAsync(path, token);
    public void Dispose() { }
}
