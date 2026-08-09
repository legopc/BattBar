using System.IO;

namespace BattBar.Core;

internal static class ErrorLog
{
    private static readonly object Lock = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BattBar",
        "error.log");

    public static void Write(string message)
    {
        try
        {
            lock (Lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Cannot log, do not propagate.
        }
    }
}