using BattBar.Core;
using System.ComponentModel;

namespace BattBar.Tray;

public sealed class TrayHost : Form
{
    private readonly Dictionary<string, NotifyIconEntry> _icons = new();
    private readonly BatteryReader _reader = new();
    private DeviceChangeWatcher? _watcher;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly ContextMenu _menu;
    private bool _apiFailed;

    public TrayHost()
    {
        // This form is invisible — it exists only to host the message loop for tray icons.
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.None;
        Opacity = 0;
        ShowIcon = false;
        Visible = false;
        Load += OnLoad;
        FormClosing += OnClosing;

        _menu = new ContextMenu();

        _pollTimer = new System.Windows.Forms.Timer { Interval = 15_000 };
        _pollTimer.Tick += (_, _) => RefreshFromReader();
    }

    private void OnLoad(object? sender, EventArgs e)
    {
        StartupLog("TrayHost.OnLoad entered");

        // Create the device-change watcher once the form (and its message loop) exists.
        _watcher = new DeviceChangeWatcher();
        _watcher.DeviceChanged += (_, _) => RefreshFromReader();

        // One-time, idempotent first-run setup.
        try { SystemTraySettings.SetAllIconsVisible(true); } // value=true means "show all"
        catch (Exception ex)
        {
            ErrorLog.Write($"SetAllIconsVisible failed: {ex.Message}");
            MessageBox.Show($"BattBar could not enable 'show all tray icons':\n\n{ex.Message}\n\n" +
                "Open Taskbar settings manually if you want all icons visible.",
                "BattBar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        try { Autostart.Enable(Environment.ProcessPath ?? ""); }
        catch (Exception ex)
        {
            ErrorLog.Write($"Autostart enable on startup failed: {ex.Message}");
            MessageBox.Show($"BattBar could not enable autostart:\n\n{ex.Message}",
                "BattBar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // First read.
        IReadOnlyList<BatteryStatus>? initial = null;
        try
        {
            initial = _reader.ReadAll();
        }
        catch (Win32Exception ex)
        {
            _apiFailed = true;
            ErrorLog.Write($"Battery API unavailable on startup: {ex.Message}");
            SpawnApiFailureIcon();
        }

        if (!_apiFailed && initial != null)
        {
            foreach (var s in initial) SpawnOneIcon(s);
            StartupLog($"Spawned {initial.Count} initial icons");
            BatteryIconRegistry.Set(initial);
            BatteryIconRegistry.RefreshCheckedAction = _menu.RefreshAutostartChecked;
            _watcher!.DeviceChanged += (_, _) => BatteryIconRegistry.Set(_reader.ReadAll());
            _pollTimer.Start();
            StartupLog("Poll timer started");
            _reader.LogCompositeDivergenceIfAny(initial);
        }

        // If API failed, we deliberately do NOT start _pollTimer or wire device change,
        // per Edge Case #10 in the spec.
    }

    private void SpawnOneIcon(BatteryStatus status)
    {
        if (!status.IsPresent) return; // Edge Case #14: no phantom icon for empty bays.

        if (_icons.ContainsKey(status.DeviceId)) return;

        var ni = new NotifyIcon
        {
            Icon = SystemIcons.Application, // overwritten by BatteryIcon ctor
            Visible = true,
            ContextMenuStrip = _menu.Strip,
            Text = TooltipFormatter.Format(new[] { status })
        };
        var renderer = new BatteryIcon(ni, status);
        _icons[status.DeviceId] = new NotifyIconEntry(ni, renderer);
    }

    private void SpawnApiFailureIcon()
    {
        var ni = new NotifyIcon
        {
            Icon = SystemIcons.Warning,
            Visible = true,
            Text = "BattBar: battery API unavailable \u2014 right-click to quit",
            ContextMenuStrip = _menu.Strip
        };
        _icons["__api_failure__"] = new NotifyIconEntry(ni, null);
    }

    private void RemoveOneIcon(string deviceId)
    {
        if (!_icons.TryGetValue(deviceId, out var entry)) return;
        entry.Renderer?.Dispose();
        entry.NotifyIcon.Visible = false;
        entry.NotifyIcon.Dispose();
        _icons.Remove(deviceId);
    }

    private void RefreshFromReader()
    {
        if (_apiFailed) return;

        IReadOnlyList<BatteryStatus> fresh;
        try { fresh = _reader.ReadAll(); }
        catch (Exception ex)
        {
            ErrorLog.Write($"Periodic read failed: {ex.Message}");
            return;
        }

        // Add new icons.
        foreach (var s in fresh)
            if (s.IsPresent && !_icons.ContainsKey(s.DeviceId))
                SpawnOneIcon(s);

        // Remove gone icons.
        var goneIds = _icons.Keys.Where(k => k != "__api_failure__").ToList();
        foreach (var id in goneIds)
            if (!fresh.Any(s => s.DeviceId == id))
                RemoveOneIcon(id);

        // Update existing.
        BatteryIconRegistry.Set(fresh);
        foreach (var s in fresh)
            if (_icons.TryGetValue(s.DeviceId, out var entry) && entry.Renderer != null)
                entry.Renderer.Update(s);

        _reader.LogCompositeDivergenceIfAny(fresh);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _pollTimer.Stop();
        foreach (var entry in _icons.Values)
        {
            entry.Renderer?.Dispose();
            entry.NotifyIcon.Visible = false;
            entry.NotifyIcon.Dispose();
        }
        _icons.Clear();
        _watcher?.Dispose();
    }

    private sealed record NotifyIconEntry(NotifyIcon NotifyIcon, BatteryIcon? Renderer);

    private static readonly string StartupLogPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "BattBar-startup.log");

    private static void StartupLog(string msg)
    {
        try
        {
            System.IO.File.AppendAllText(StartupLogPath,
                $"[{DateTime.Now:O}] [TrayHost] {msg}{Environment.NewLine}");
        }
        catch { }
    }
}