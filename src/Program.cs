using System.Runtime.InteropServices;
using BattBar.Core;
using BattBar.Tray;
using Microsoft.Win32;

namespace BattBar;

internal static class Program
{
    private const string MutexName = "Local\\battbar-single-instance-v1";
    private const string PidKey = @"Software\BattBar";
    private const string PidValue = "pid";
    private static readonly string StartupLogPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "BattBar-startup.log");

    private static void Log(string msg)
    {
        try
        {
            System.IO.File.AppendAllText(StartupLogPath,
                $"[{DateTime.Now:O}] {msg}{Environment.NewLine}");
        }
        catch { /* swallow */ }
    }

    [STAThread]
    private static void Main()
    {
        // Enable per-monitor DPI awareness so taskbar/tray icons render at the
        // user's actual scale, not at the legacy 96-DPI assumption. This makes
        // the 32x32 numeric badge legible at 125%, 150%, and 175% scales.
        TryEnablePerMonitorDpi();

        Log($"Main entered. PID={Environment.ProcessId} ProcessPath={Environment.ProcessPath}");
        Log($"Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA> = {System.Runtime.InteropServices.Marshal.SizeOf<BattBar.Core.BatteryApi.SP_DEVICE_INTERFACE_DATA>()}");
        Log($"Marshal.SizeOf<SP_DEVICE_INTERFACE_DETAIL_DATA> = {System.Runtime.InteropServices.Marshal.SizeOf<BattBar.Core.BatteryApi.SP_DEVICE_INTERFACE_DETAIL_DATA>()}");
        Log($"IntPtr.Size = {IntPtr.Size}");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log($"UNHANDLED: {e.ExceptionObject}");
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) =>
            Log($"UNTASK: {e.Exception}");

        try
        {
            bool createdNew;
            using var mutex = new System.Threading.Mutex(true, MutexName, out createdNew);
            Log($"Mutex acquired. createdNew={createdNew}");

            if (!createdNew)
            {
                if (TryTakeOverFromDeadInstance())
                {
                    Log("Takeover from dead instance: proceeding");
                    createdNew = true;
                }
                else
                {
                    Log("Another live instance is running; exiting");
                    return;
                }
            }

            WriteOurPid();
            Log("WriteOurPid done");
            AppDomain.CurrentDomain.ProcessExit += (_, _) => ClearOurPid();

            Log("About to ApplicationConfiguration.Initialize");
            ApplicationConfiguration.Initialize();
            Log("ApplicationConfiguration.Initialize done");

            Log("About to construct TrayHost");
            var host = new TrayHost();
            Log("TrayHost constructed");

            Log("About to Application.Run");
            Application.Run(host);
            Log("Application.Run returned");
        }
        catch (Exception ex)
        {
            Log($"TOP-LEVEL EXCEPTION: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
        finally
        {
            ClearOurPid();
            Log("Main exiting");
        }
    }

    private static bool TryTakeOverFromDeadInstance()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PidKey, writable: false);
            if (key?.GetValue(PidValue) is not int storedPid) return true;
            try
            {
                using var probe = System.Diagnostics.Process.GetProcessById(storedPid);
                if (!probe.HasExited) return false;
            }
            catch (ArgumentException) { /* PID gone — safe to take over */ }
            Log($"Stored PID {storedPid} is dead; taking over");
        }
        catch (Exception ex)
        {
            Log($"Takeover check failed: {ex.Message}");
        }
        return true;
    }

    private static void WriteOurPid()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(PidKey, writable: true);
            key.SetValue(PidValue, Environment.ProcessId, RegistryValueKind.DWord);
        }
        catch (Exception ex)
        {
            Log($"WriteOurPid failed: {ex.Message}");
        }
    }

    private static void ClearOurPid()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PidKey, writable: true);
            key?.DeleteValue(PidValue, throwOnMissingValue: false);
        }
        catch { /* best effort */ }
    }

    private static void TryEnablePerMonitorDpi()
    {
        try
        {
            // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4.
            SetProcessDpiAwarenessContext((IntPtr)(-4));
        }
        catch
        {
            // Older Windows or non-Windows test host: ignore. The process will
            // fall back to system DPI awareness, which is still legible.
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiAwarenessContext);
}