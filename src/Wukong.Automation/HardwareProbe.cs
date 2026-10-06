using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Wukong.Core;

namespace Wukong.Automation;

public static class HardwareProbe
{
    public static async Task<MachineInfo> ReadAsync(CancellationToken token)
    {
        const string script = "[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false); $ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; try { " +
            "$c=@(Get-CimInstance Win32_Processor | ForEach-Object Name); " +
            "$g=@(Get-CimInstance Win32_VideoController); " +
            "$r=(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory; " +
            "@{cpu=$c;gpu=@($g|ForEach-Object Name);driverVersions=@($g|ForEach-Object DriverVersion);ramGiB=$r/1GB} | ConvertTo-Json -Compress " +
            "} catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }";
        var info = new ProcessStartInfo("powershell.exe")
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(script)) })
            info.ArgumentList.Add(arg);
        var cpu = Array.Empty<string>(); var gpu = Array.Empty<string>(); var drivers = Array.Empty<string>();
        double? ram = null;
        var warnings = new List<string>();
        try
        {
            using var process = Process.Start(info) ?? throw new InvalidOperationException("Hardware probe failed to start.");
            var stdout = process.StandardOutput.ReadToEndAsync(token);
            var stderr = process.StandardError.ReadToEndAsync(token);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            try { await process.WaitForExitAsync(deadline.Token); }
            catch (OperationCanceledException) { process.Kill(true); token.ThrowIfCancellationRequested(); throw new TimeoutException("CIM hardware probe timed out."); }
            if (process.ExitCode != 0) throw new InvalidOperationException((await stderr).Trim());
            using var json = JsonDocument.Parse(await stdout);
            var root = json.RootElement;
            cpu = root.GetProperty("cpu").EnumerateArray().Select(v => v.GetString()?.Trim() ?? "Unknown").ToArray();
            gpu = root.GetProperty("gpu").EnumerateArray().Select(v => v.GetString()?.Trim() ?? "Unknown").ToArray();
            drivers = root.GetProperty("driverVersions").EnumerateArray().Select(v => v.GetString()?.Trim() ?? "Unknown").ToArray();
            ram = Math.Round(root.GetProperty("ramGiB").GetDouble(), 2);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { warnings.Add("Hardware details unavailable: " + ex.Message); }
        return new(RuntimeInformation.OSDescription, cpu, gpu, ram, drivers,
            Environment.ProcessorCount, RuntimeInformation.OSArchitecture.ToString(), warnings.ToArray());
    }
}
