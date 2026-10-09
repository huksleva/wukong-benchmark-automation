using Wukong.Core;

namespace Wukong.Automation;

/// <summary>Semantic keys needed by the benchmark menus; adapters map them to their OS.</summary>
public enum BenchmarkKey { Enter, Escape, Left, Right, Apply }

/// <summary>Access to one selected benchmark window, with client-relative coordinates.</summary>
public interface IBenchmarkWindow
{
    void Capture(string path);
    void Click(double x, double y);
    void Scroll(int ticks);
    void Key(BenchmarkKey key);
}

/// <summary>Local image recognition returning unscaled, client-relative word geometry.</summary>
public interface IBenchmarkOcr : IDisposable
{
    Task<OcrPage> ReadAsync(string path, CancellationToken token, bool numericOnly = false, int targetWidth = 1920);
    Task<OcrPage> ReadResultAsync(string path, CancellationToken token);
    Task<OcrPage> ReadRegionAsync(string path, Box region, CancellationToken token, bool numericOnly = false, int targetWidth = 1920);
}
