using System.IO;

namespace BattBar.Core;

/// <summary>
/// Lightweight diagnostic logger that writes to %USERPROFILE%\BattBar-diag.log.
/// Intended for laptop-side debugging when the normal ErrorLog (in %LOCALAPPDATA%) is not helpful.
/// </summary>
internal static class Diag
{
    private static readonly object Lock = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "BattBar-diag.log");

    public static void Log(string message)
    {
        try
        {
            lock (Lock)
            {
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Cannot log; do not propagate.
        }
    }
}