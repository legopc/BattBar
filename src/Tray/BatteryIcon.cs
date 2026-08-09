using BattBar.Core;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.Versioning;

namespace BattBar.Tray;

/// <summary>
/// Renders one battery as a numeric-badge tray icon.
/// Pair: one BatteryIcon per NotifyIcon, paired by DeviceId.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class BatteryIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private BatteryStatus _last;
    // Holds the icon and its source bitmap together so the HICON handle stays valid
    // for as long as the icon is in use. Both are released when this entry is replaced.
    private (Icon icon, Bitmap bmp32)? _current;

    public string DeviceId { get; }

    public BatteryIcon(NotifyIcon notifyIcon, BatteryStatus initial)
    {
        _notifyIcon = notifyIcon;
        DeviceId = initial.DeviceId;
        _last = initial;
        _current = Render(initial);
        _notifyIcon.Icon = _current.Value.icon;
        _notifyIcon.Visible = true;
        _notifyIcon.Text = TooltipFormatter.Format(new[] { initial });
    }

    public void Update(BatteryStatus next)
    {
        if (!BatteryIconPredicates.ShouldRedraw(_last, next))
        {
            _notifyIcon.Text = TooltipFormatter.Format(GetAllFromTray());
            return;
        }
        _last = next;
        _current = Render(next);
        _notifyIcon.Icon = _current.Value.icon;
        _notifyIcon.Text = TooltipFormatter.Format(GetAllFromTray());
    }

    private IReadOnlyList<BatteryStatus> GetAllFromTray()
    {
        return BatteryIconRegistry.SnapshotStatuses();
    }

    private static (Icon icon, Bitmap bmp32) Render(BatteryStatus s)
    {
        // Render at 32x32. Windows 11 tray icons default to 16x16 — the system will
        // downscale for display. We accept the cosmetic tradeoff and document it
        // in the README; for the v1 numeric-badge use case, downscale quality is
        // good enough that two-digit percentages stay readable.
        // Icon.FromHandle wraps an existing HICON; the handle is owned by the
        // bitmap, so we keep the bitmap alongside the Icon. Dispose on the Icon
        // is unsafe here — it would DestroyIcon and break any active _notifyIcon reference.
        var bmp32 = DrawBadge(32, s);
        var iconHandle = bmp32.GetHicon();
        var icon = Icon.FromHandle(iconHandle);
        return (icon, bmp32);
    }

    private static Bitmap DrawBadge(int size, BatteryStatus s)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);

        var borderColor = BorderColor(s);
        // Text is always a fixed dark color so it stays readable against any tray background.
        // Only critical-discharging (<5% while discharging) flips text to red to draw the eye.
        var textColor = s.ChargePercent is < 5 && s.State == BatteryState.Discharging
            ? Color.Red
            : Color.FromArgb(20, 20, 20);

        // Rounded rectangle border
        using var pen = new Pen(borderColor, Math.Max(1f, size / 16f));
        float inset = size * 0.06f;
        var rect = new RectangleF(inset, inset, size - 2 * inset, size - 2 * inset);
        using var path = RoundedRect(rect, size * 0.18f);
        g.DrawPath(pen, path);

        // Text
        string text;
        if (!s.IsPresent || s.State == BatteryState.NotPresent)
            text = "\u2014";
        else if (s.ChargePercent is null)
            text = "??";
        else
            text = s.ChargePercent.Value.ToString();

        // Render the number large enough to fill the badge. At 32x32 with a 0.55
        // font ratio we get ~17px Bold Arial, which is readable even when Windows
        // downscales the icon for display.
        float fontPx = Math.Max(8f, size * 0.55f);
        using var font = new Font("Arial", fontPx, FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(textColor);
        var textSize = g.MeasureString(text, font);
        float x = (size - textSize.Width) / 2f;
        float y = (size - textSize.Height) / 2f;
        g.DrawString(text, font, brush, x, y);

        return bmp;
    }

    private static Color BorderColor(BatteryStatus s)
    {
        if (!s.IsPresent || s.State == BatteryState.NotPresent || s.State == BatteryState.Unknown) return Color.Gray;
        if (s.State == BatteryState.Charging) return Color.FromArgb(34, 197, 94); // green-500
        if (s.ChargePercent is < 5) return Color.FromArgb(239, 68, 68);             // red-500
        if (s.ChargePercent is < 20 && s.State == BatteryState.Discharging) return Color.FromArgb(250, 204, 21); // yellow-400
        return Color.FromArgb(59, 130, 246); // blue-500
    }

    private static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        float d = radius * 2f;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Icon?.Dispose();
        _notifyIcon.Icon = null;
    }
}