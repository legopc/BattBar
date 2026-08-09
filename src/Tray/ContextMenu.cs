using BattBar.Core;
using System.Diagnostics;
using System.Reflection;

namespace BattBar.Tray;

/// <summary>
/// Builds the right-click menu used by every BatteryIcon.
/// Both tray icons share the same ContextMenuStrip.
/// </summary>
public sealed class ContextMenu
{
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _autostartItem;

    public ContextMenu()
    {
        _menu = new ContextMenuStrip();

        var openSettings = new ToolStripMenuItem("Open Windows Battery Settings");
        openSettings.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:battery") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ErrorLog.Write($"Open battery settings failed: {ex.Message}");
            }
        };

        _autostartItem = new ToolStripMenuItem("Run on startup") { CheckOnClick = true, Checked = Autostart.IsEnabled() };
        _autostartItem.CheckedChanged += (_, _) =>
        {
            try
            {
                if (_autostartItem.Checked) Autostart.Enable(Environment.ProcessPath ?? "");
                else Autostart.Disable();
            }
            catch (Exception ex)
            {
                ErrorLog.Write($"Autostart toggle failed: {ex.Message}");
                _autostartItem.Checked = Autostart.IsEnabled(); // revert
                MessageBox.Show($"Could not change autostart setting:\n\n{ex.Message}",
                    "BattBar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var about = new ToolStripMenuItem("About BattBar");
        about.Click += (_, _) =>
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?";
            MessageBox.Show(
                $"BattBar v{version}\n\n" +
                $"Path: {Environment.ProcessPath}\n\n" +
                $"Logs: %LOCALAPPDATA%\\BattBar\\error.log\n\n" +
                $"For the Dell Latitude 5430 Rugged.",
                "BattBar", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        var quit = new ToolStripMenuItem("Quit");
        quit.Click += (_, _) => Application.Exit();

        _menu.Items.Add(openSettings);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_autostartItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(about);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(quit);
    }

    public ContextMenuStrip Strip => _menu;

    public void RefreshAutostartChecked() => _autostartItem.Checked = Autostart.IsEnabled();
}