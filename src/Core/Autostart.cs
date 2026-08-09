using Microsoft.Win32;

namespace BattBar.Core;

public static class Autostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "battbar";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
        catch (Exception ex)
        {
            ErrorLog.Write($"Autostart.IsEnabled failed: {ex.Message}");
            return false;
        }
    }

    public static void Enable(string exePath)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
            key.SetValue(ValueName, exePath, RegistryValueKind.String);
        }
        catch (Exception ex)
        {
            ErrorLog.Write($"Autostart.Enable failed: {ex.Message}");
            throw;
        }
    }

    public static void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            ErrorLog.Write($"Autostart.Disable failed: {ex.Message}");
            throw;
        }
    }
}