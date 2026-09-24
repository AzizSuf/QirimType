using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace QirimType.Utils;

public static class IconHelper
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Icon CreateAppIcon(bool enabled, int size = 32)
    {
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Background colors
            Color bgColor = enabled ? Color.FromArgb(0, 122, 217) : Color.FromArgb(120, 130, 140);
            Color fgColor = enabled ? Color.FromArgb(255, 215, 0) : Color.FromArgb(210, 215, 220); // Crimean gold or muted silver
            Color letterColor = Color.White;

            // Draw rounded background
            using (var brush = new SolidBrush(bgColor))
            {
                using var path = CreateRoundedRectanglePath(new Rectangle(1, 1, size - 2, size - 2), size / 4);
                g.FillPath(brush, path);
            }

            // Draw Crimean golden accent dot/arc at top right
            if (enabled)
            {
                using var accentBrush = new SolidBrush(fgColor);
                int dotSize = Math.Max(4, size / 6);
                g.FillEllipse(accentBrush, size - dotSize - 3, 3, dotSize, dotSize);
            }

            // Draw 'Q' letter in center
            float fontSize = size * 0.58f;
            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(letterColor);

            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            var rect = new RectangleF(0, 1, size, size);
            g.DrawString("Q", font, textBrush, rect, stringFormat);

            // If disabled, draw a subtle slash or mute badge
            if (!enabled)
            {
                using var mutePen = new Pen(Color.FromArgb(220, 50, 50), Math.Max(2, size / 10));
                g.DrawLine(mutePen, size * 0.2f, size * 0.8f, size * 0.8f, size * 0.2f);
            }
        }

        IntPtr hIcon = bitmap.GetHicon();
        Icon icon;
        try
        {
            // Clone into a managed Icon so we can immediately release the native handle
            using var tempIcon = Icon.FromHandle(hIcon);
            icon = (Icon)tempIcon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }

        return icon;
    }

    private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int diameter = radius * 2;
        var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static Bitmap CreateAppBitmap(bool enabled, int size = 32)
    {
        var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Color bgColor = enabled ? Color.FromArgb(0, 122, 217) : Color.FromArgb(120, 130, 140);
            Color fgColor = enabled ? Color.FromArgb(255, 215, 0) : Color.FromArgb(210, 215, 220);
            Color letterColor = Color.White;

            using (var brush = new SolidBrush(bgColor))
            {
                using var path = CreateRoundedRectanglePath(new Rectangle(1, 1, size - 2, size - 2), Math.Max(2, size / 4));
                g.FillPath(brush, path);
            }

            if (enabled)
            {
                using var accentBrush = new SolidBrush(fgColor);
                int dotSize = Math.Max(3, size / 6);
                g.FillEllipse(accentBrush, size - dotSize - Math.Max(2, size / 10), Math.Max(2, size / 10), dotSize, dotSize);
            }

            float fontSize = size * 0.58f;
            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(letterColor);

            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            var rect = new RectangleF(0, size * 0.04f, size, size);
            g.DrawString("Q", font, textBrush, rect, stringFormat);

            if (!enabled)
            {
                using var mutePen = new Pen(Color.FromArgb(220, 50, 50), Math.Max(2, size / 10));
                g.DrawLine(mutePen, size * 0.2f, size * 0.8f, size * 0.8f, size * 0.2f);
            }
        }
        return bitmap;
    }

    public static void SaveMultiResolutionIcon(string filePath, int[] sizes)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        var pngBuffers = new List<byte[]>();
        foreach (int sz in sizes)
        {
            using var bmp = CreateAppBitmap(true, sz);
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            pngBuffers.Add(ms.ToArray());
        }

        // ICONDIR Header
        writer.Write((ushort)0); // idReserved
        writer.Write((ushort)1); // idType = 1 (Icon)
        writer.Write((ushort)sizes.Length); // idCount

        int offset = 6 + (16 * sizes.Length);

        // ICONDIRENTRY entries
        for (int i = 0; i < sizes.Length; i++)
        {
            int sz = sizes[i];
            byte widthByte = (sz >= 256) ? (byte)0 : (byte)sz;
            byte heightByte = (sz >= 256) ? (byte)0 : (byte)sz;

            writer.Write(widthByte);
            writer.Write(heightByte);
            writer.Write((byte)0); // bColorCount
            writer.Write((byte)0); // bReserved
            writer.Write((ushort)1); // wPlanes
            writer.Write((ushort)32); // wBitCount
            writer.Write((uint)pngBuffers[i].Length); // dwBytesInRes
            writer.Write((uint)offset); // dwImageOffset

            offset += pngBuffers[i].Length;
        }

        // Image byte data
        for (int i = 0; i < sizes.Length; i++)
        {
            writer.Write(pngBuffers[i]);
        }
    }
}
