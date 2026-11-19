using System.Drawing;
using System.Drawing.Drawing2D;
using QweNumpad.App.Interop;
using QweNumpad.Core;

namespace QweNumpad.App.Ui;

/// <summary>
/// Tray icons drawn at runtime, so the repository carries no binary assets.
/// Colour tells the state at a glance: grey off, green auto-on, blue forced on, red forced off.
/// </summary>
internal sealed class TrayIcons : IDisposable
{
    private readonly Icon _inactive = Create(Color.FromArgb(120, 125, 133));
    private readonly Icon _autoActive = Create(Color.FromArgb(32, 160, 90));
    private readonly Icon _forcedOn = Create(Color.FromArgb(40, 115, 215));
    private readonly Icon _forcedOff = Create(Color.FromArgb(190, 60, 60));

    public Icon For(ModeState state) => state.Override switch
    {
        ModeOverride.ForcedOn => _forcedOn,
        ModeOverride.ForcedOff => _forcedOff,
        _ => state.IsActive ? _autoActive : _inactive,
    };

    private static Icon Create(Color background)
    {
        using var bitmap = new Bitmap(32, 32);

        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var fill = new SolidBrush(background);
            g.FillEllipse(fill, 0, 0, 31, 31);

            // A 3x3 dot grid: the numpad digit block the left hand is standing in for.
            using var dots = new SolidBrush(Color.White);
            for (var row = 0; row < 3; row++)
            for (var col = 0; col < 3; col++)
                g.FillEllipse(dots, 7 + col * 7, 7 + row * 7, 4, 4);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }

    public void Dispose()
    {
        _inactive.Dispose();
        _autoActive.Dispose();
        _forcedOn.Dispose();
        _forcedOff.Dispose();
    }
}
