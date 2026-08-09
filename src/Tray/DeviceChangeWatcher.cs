using System.Windows.Forms;

namespace BattBar.Core;

/// <summary>
/// Listens for WM_DEVICECHANGE / DBT_DEVNODES_CHANGED via a hidden message-only window,
/// coalesces events within 250ms, and raises <see cref="DeviceChanged"/> on a UI timer.
/// </summary>
public sealed class DeviceChangeWatcher : IDisposable
{
    private const int WM_DEVICECHANGE = 0x0219;
    private const int DBT_DEVNODES_CHANGED = 0x0007;

    private readonly HiddenWindow _window;
    private readonly System.Windows.Forms.Timer _debounce;
    private bool _disposed;

    public event EventHandler? DeviceChanged;

    public DeviceChangeWatcher()
    {
        _window = new HiddenWindow(this);
        _debounce = new System.Windows.Forms.Timer { Interval = 250 };
        _debounce.Tick += (s, e) =>
        {
            _debounce.Stop();
            DeviceChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    internal void OnDeviceChange()
    {
        _debounce.Stop();
        _debounce.Start();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _debounce.Dispose();
        _window.DestroyHandle();
    }

    private sealed class HiddenWindow : NativeWindow
    {
        private readonly DeviceChangeWatcher _owner;

        public HiddenWindow(DeviceChangeWatcher owner)
        {
            _owner = owner;
            var cp = new CreateParams
            {
                Caption = "BattBarDeviceWatcher",
                X = 0, Y = 0, Width = 0, Height = 0,
                Style = 0,
                ExStyle = 0
            };
            CreateHandle(cp);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_DEVICECHANGE && m.WParam.ToInt32() == DBT_DEVNODES_CHANGED)
            {
                _owner.OnDeviceChange();
            }
            base.WndProc(ref m);
        }
    }
}