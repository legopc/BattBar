using Microsoft.Win32;

namespace BattBar.Core;

public static class SystemTraySettings
{
    // EnableAutoTray=0 means "show all tray icons in the visible cluster" (no overflow arrow).
    // EnableAutoTray=1 (default) means "hide unused icons in the overflow arrow cluster".
    private const string AdvancedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string ValueName = "EnableAutoTray";

    public static bool IsAllIconsVisible()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(AdvancedKey);
            var v = key?.GetValue(ValueName);
            // Default is 1 (overflow cluster). Treat anything other than 0 as "not all visible".
            return v is int i && i == 0;
        }
        catch (Exception ex)
        {
            ErrorLog.Write($"SystemTraySettings.IsAllIconsVisible failed: {ex.Message}");
            return false;
        }
    }

    public static void SetAllIconsVisible(bool value)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(AdvancedKey, writable: true);
            // We store DWORD 0 (visible) or 1 (overflow). Idempotent: skip if already correct.
            int stored = key.GetValue(ValueName) is int i ? i : 1;
            int desired = value ? 0 : 1;
            if (stored != desired)
            {
                key.SetValue(ValueName, desired, RegistryValueKind.DWord);
            }
        }
        catch (Exception ex)
        {
            ErrorLog.Write($"SystemTraySettings.SetAllIconsVisible({value}) failed: {ex.Message}");
            throw;
        }
    }
}