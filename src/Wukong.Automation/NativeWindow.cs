using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Wukong.Automation;

/// <summary>Win32 access is confined to the selected Benchmark Tool window.</summary>
public sealed class NativeWindow(nint handle)
{
    public nint Handle { get; } = handle;
    private const uint InputKeyboard = 1, KeyUp = 2;
    private const uint MouseLeftDown = 2, MouseLeftUp = 4;

    public Rectangle ClientBounds()
    {
        if (!IsWindow(Handle) || !GetClientRect(Handle, out var rect)) throw new InvalidOperationException("Benchmark window disappeared.");
        var origin = new PointNative();
        if (!ClientToScreen(Handle, ref origin)) throw new Win32Exception();
        return new(origin.X, origin.Y, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    public void Focus()
    {
        if (IsIconic(Handle)) ShowWindow(Handle, 9);
        if (GetForegroundWindow() != Handle) SetForegroundWindow(Handle);
        if (GetForegroundWindow() != Handle)
            throw new InvalidOperationException("Cannot focus the Benchmark Tool. Close Steam dialogs/overlays and keep the benchmark window in front.");
    }

    public void Capture(string path)
    {
        Focus();
        var b = ClientBounds();
        if (b.Width < 640 || b.Height < 360) throw new InvalidOperationException("Benchmark window is too small for OCR.");
        if (!Screen.AllScreens.Any(s => s.Bounds.Contains(b)))
            throw new InvalidOperationException("The entire benchmark window must fit on one monitor. Use borderless/fullscreen or a smaller requested resolution.");
        using var bitmap = new Bitmap(b.Width, b.Height);
        using var g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(b.Location, Point.Empty, b.Size, CopyPixelOperation.SourceCopy);
        bitmap.Save(path, ImageFormat.Png);
    }

    public void Click(double x, double y)
    {
        Focus();
        var bounds = ClientBounds();
        if (x < 0 || y < 0 || x >= bounds.Width || y >= bounds.Height) throw new ArgumentOutOfRangeException(nameof(x));
        if (!SetCursorPos(bounds.X + (int)x, bounds.Y + (int)y)) throw new Win32Exception();
        Send([new() { Type = 0, Data = new() { Mouse = new() { Flags = MouseLeftDown } } },
              new() { Type = 0, Data = new() { Mouse = new() { Flags = MouseLeftUp } } }]);
    }

    public void Key(ushort key)
    {
        Focus();
        Send([new() { Type = InputKeyboard, Data = new() { Keyboard = new() { VirtualKey = key } } },
              new() { Type = InputKeyboard, Data = new() { Keyboard = new() { VirtualKey = key, Flags = KeyUp } } }]);
    }

    public void Scroll(int ticks)
    {
        Focus();
        var bounds = ClientBounds();
        SetCursorPos(bounds.X + (int)(bounds.Width * .7), bounds.Y + bounds.Height / 2);
        Send([new() { Type = 0, Data = new() { Mouse = new() { MouseData = unchecked((uint)(ticks * 120)), Flags = 0x800 } } }]);
    }

    private static void Send(Input[] inputs)
    {
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Input injection failed. Run Steam and the runner at the same privilege level.");
    }

    public static void EnableDpiAwareness() => SetProcessDpiAwarenessContext(new nint(-4));
    public const ushort Enter = 0x0D, Escape = 0x1B, Tab = 0x09, Left = 0x25, Right = 0x27;
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct PointNative { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion
    {
        [FieldOffset(0)] public MouseInput Mouse;
        [FieldOffset(0)] public KeyboardInput Keyboard;
    }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput
    { public int X, Y; public uint MouseData, Flags, Time; public nuint ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput
    { public ushort VirtualKey, Scan; public uint Flags, Time; public nuint ExtraInfo; }
    [DllImport("user32.dll")] private static extern bool GetClientRect(nint hWnd, out Rect rect);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(nint hWnd, ref PointNative point);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hWnd);
    [DllImport("user32.dll")] private static extern bool IsWindow(nint hWnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint hWnd, int command);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(nint context);
}
