#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace AERO_Console.UI
{
    /// <summary>Semantic application icons.</summary>
    public enum AppIcon
    {
        // General
        OpenFolder, NewProject, Save, Package, Terminal, Settings,
        Help, Info, Refresh, Palette, Sun, Moon, Checkmark, Close, Eye,

        // Actions
        LoadGeometry, LoadMass, LoadRun, GeometryDesigner,
        Play, FormatCode, Clear, Undo, Redo, Add, Indent, Outdent,

        // Canvas / View
        ThreeD, ZoomIn, ZoomOut, FitAll, Space, Hover, Move,
        Layers, FontIncrease, FontDecrease, Chart, Table,
        Weights, Airflow, Pulse, ChartScatter
    }

    /// <summary>
    /// Draws vector icons directly from Windows' native icon fonts (Segoe Fluent Icons /
    /// Segoe MDL2 Assets) so every icon scales sharply at any DPI and takes theme colors.
    /// </summary>
    public static class Icons
    {
        private static readonly string? FontFamilyName = ResolveFontFamily();

        // Cached by (icon, size, color)
        private static readonly Dictionary<(AppIcon, int, int), Image> Cache = new();

        public static bool Available => FontFamilyName != null;

        private static string? ResolveFontFamily()
        {
            foreach (var candidate in new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" })
            {
                try
                {
                    using var family = new FontFamily(candidate);
                    return candidate;
                }
                catch (ArgumentException)
                {
                    // Not installed, try next
                }
            }
            return null;
        }

        private static char Glyph(AppIcon icon) => icon switch
        {
            AppIcon.OpenFolder => '\uE838',        // Folder open
            AppIcon.NewProject => '\uE8F4',        // Folder with plus
            AppIcon.Save => '\uE74E',              // Floppy disk
            AppIcon.Package => '\uEDE1',           // Package box
            AppIcon.Terminal => '\uE756',          // Terminal console
            AppIcon.Settings => '\uE713',          // Gear
            AppIcon.Help => '\uE897',              // Question circle
            AppIcon.Info => '\uE946',              // 'i' circle
            AppIcon.Refresh => '\uE72C',           // Circular arrow
            AppIcon.Palette => '\uE790',           // Palette
            AppIcon.Sun => '\uE706',               // Brightness / Sun
            AppIcon.Moon => '\uE708',              // Moon / Dark mode
            AppIcon.Checkmark => '\uE73E',         // Checkmark
            AppIcon.Close => '\uE711',             // Cross
            AppIcon.Eye => '\uE890',               // Eye preview
            AppIcon.LoadGeometry => '\uE8B7',      // Folder / Document
            AppIcon.LoadMass => '\uE80A',          // Table / Grid
            AppIcon.LoadRun => '\uE768',           // Play
            AppIcon.GeometryDesigner => '\uE943',  // Code / CAD designer
            AppIcon.Play => '\uE768',              // Play
            AppIcon.FormatCode => '\uE8C9',        // Document / format lines
            AppIcon.Clear => '\uE74D',             // Trash can
            AppIcon.Undo => '\uE7A7',              // Curved undo
            AppIcon.Redo => '\uE7A6',              // Curved redo
            AppIcon.Add => '\uE710',               // Plus sign
            AppIcon.Indent => '\uE7FD',            // Increase indent
            AppIcon.Outdent => '\uE7FE',           // Decrease indent
            AppIcon.ThreeD => '\uE92D',            // 3D / Expand
            AppIcon.ZoomIn => '\uE8A3',            // Magnifier plus
            AppIcon.ZoomOut => '\uE71F',           // Magnifier minus
            AppIcon.FitAll => '\uE9A6',            // Corner brackets / Fit
            AppIcon.Space => '\uE7A8',             // Spacing / Ruler
            AppIcon.Hover => '\uE8B0',             // Cursor pointer
            AppIcon.Move => '\uE7C2',              // 4-way move
            AppIcon.Layers => '\uE81E',            // Stacked layers
            AppIcon.FontIncrease => '\uE8D2',      // Font size up
            AppIcon.FontDecrease => '\uE8D3',      // Font size down
            AppIcon.Chart => '\uE9D9',             // Analytics chart
            AppIcon.Table => '\uE80A',             // Table
            AppIcon.Weights => '\uE805',           // Scale / balance (Loads)
            AppIcon.Airflow => '\uEC15',           // Airflow / pressure waves (FE)
            AppIcon.Pulse => '\uE95D',             // Pulse / oscillation (Dynamics)
            AppIcon.ChartScatter => '\uF158',      // Scatter / polar chart
            _ => '\uE7C3'
        };

        /// <summary>
        /// Retrieves an icon rendered at <paramref name="size"/> pixels square in <paramref name="colour"/>.
        /// The returned bitmap is cached in memory.
        /// </summary>
        public static Image? Get(AppIcon icon, int size, Color colour)
        {
            if (size <= 0) return null;

            var key = (icon, size, colour.ToArgb());
            if (Cache.TryGetValue(key, out var cached)) return cached;

            // Direct vector rendering for tab indent/outdent to guarantee sharp, miss-render free icons
            if (icon == AppIcon.Outdent || icon == AppIcon.Indent)
            {
                var vbmp = new Bitmap(size, size);
                vbmp.SetResolution(96, 96);
                using (var vg = Graphics.FromImage(vbmp))
                {
                    vg.SmoothingMode = SmoothingMode.AntiAlias;
                    float penWidth = Math.Max(1.5f, size / 11f);
                    using var pen = new Pen(colour, penWidth);
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    if (icon == AppIcon.Outdent)
                    {
                        float barX = size * 0.22f;
                        vg.DrawLine(pen, barX, size * 0.20f, barX, size * 0.80f);
                        float midY = size * 0.50f;
                        vg.DrawLine(pen, size * 0.78f, midY, size * 0.35f, midY);
                        vg.DrawLine(pen, size * 0.56f, size * 0.28f, size * 0.35f, midY);
                        vg.DrawLine(pen, size * 0.56f, size * 0.72f, size * 0.35f, midY);
                    }
                    else
                    {
                        float barX = size * 0.78f;
                        vg.DrawLine(pen, barX, size * 0.20f, barX, size * 0.80f);
                        float midY = size * 0.50f;
                        vg.DrawLine(pen, size * 0.22f, midY, size * 0.65f, midY);
                        vg.DrawLine(pen, size * 0.44f, size * 0.28f, size * 0.65f, midY);
                        vg.DrawLine(pen, size * 0.44f, size * 0.72f, size * 0.65f, midY);
                    }
                }
                Cache[key] = vbmp;
                return vbmp;
            }

            if (FontFamilyName is null) return null;

            var bitmap = new Bitmap(size, size);
            bitmap.SetResolution(96, 96);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                using var font = new Font(FontFamilyName, size * 0.72f, FontStyle.Regular, GraphicsUnit.Pixel);
                using var brush = new SolidBrush(colour);
                using var format = new StringFormat(StringFormatFlags.NoWrap)
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                };

                g.DrawString(Glyph(icon).ToString(), font, brush, new RectangleF(0, 0, size, size), format);
            }

            Cache[key] = bitmap;
            return bitmap;
        }

        /// <summary>Calculates the standard icon size scaled for the given control's DPI.</summary>
        public static int SizeFor(Control control, int baseSize = 16)
        {
            if (control == null) return baseSize;
            float factor = control.DeviceDpi / 96f;
            return (int)Math.Round(baseSize * factor);
        }
    }
}
