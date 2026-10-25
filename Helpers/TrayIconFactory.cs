using System.Drawing;
using System.Drawing.Drawing2D;

namespace FocusHeure.Helpers;

public static class TrayIconFactory
{
    public static Icon Create(bool active)
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        var fill = active ? Color.FromArgb(17, 17, 17) : Color.FromArgb(180, 180, 180);
        using var brush = new SolidBrush(fill);
        g.FillEllipse(brush, 2, 2, 28, 28);

        using var font = new Font("Segoe UI", 14, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textBrush = new SolidBrush(Color.White);
        var text = "F";
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, textBrush, (32 - size.Width) / 2f, (32 - size.Height) / 2f - 1);

        var handle = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);
}
