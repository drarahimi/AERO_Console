#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AERO_Console.UI
{
    /// <summary>
    /// The colour palette for the application and the recursive engine that applies it
    /// to control trees, status bars, toolbars, buttons, and native OS chrome.
    /// </summary>
    public sealed class Theme
    {
        public bool IsDark { get; init; }

        public Color Background { get; init; }        // Window / panel chrome
        public Color Surface { get; init; }           // Textboxes, lists, trees, inputs
        public Color SurfaceAlt { get; init; }        // Toolbars, headers, button backgrounds
        public Color Border { get; init; }            // Hairlines, outlines, dividers
        public Color Foreground { get; init; }        // Primary text
        public Color ForegroundDim { get; init; }     // Secondary / muted text
        public Color Accent { get; init; }            // Primary brand / action highlight
        public Color AccentHover { get; init; }       // Accent hover state
        public Color Selection { get; init; }         // Highlight / selection fill
        public Color Chrome { get; init; }            // Toolbars, status bar, menu background
        public Color Hover { get; init; }             // Button / menu hover fill
        public Color Pressed { get; init; }           // Button / menu pressed fill
        public Color Error { get; init; }
        public Color Warning { get; init; }
        public Color Success { get; init; }

        public static readonly Theme Dark = new Theme
        {
            IsDark = true,
            Background = Color.FromArgb(0x17, 0x1A, 0x21),
            Surface = Color.FromArgb(0x1E, 0x22, 0x2B),
            SurfaceAlt = Color.FromArgb(0x25, 0x2A, 0x35),
            Border = Color.FromArgb(0x32, 0x38, 0x45),
            Foreground = Color.FromArgb(0xDC, 0xE2, 0xEE),
            ForegroundDim = Color.FromArgb(0x8C, 0x96, 0xA8),
            Accent = Color.FromArgb(0x4C, 0x8D, 0xF6),
            AccentHover = Color.FromArgb(0x6B, 0xA3, 0xF8),
            Selection = Color.FromArgb(0x26, 0x4F, 0x78),
            Chrome = Color.FromArgb(0x1C, 0x20, 0x29),
            Hover = Color.FromArgb(0x2E, 0x34, 0x42),
            Pressed = Color.FromArgb(0x38, 0x40, 0x52),
            Error = Color.FromArgb(0xE0, 0x6C, 0x75),
            Warning = Color.FromArgb(0xE5, 0xC0, 0x7B),
            Success = Color.FromArgb(0x84, 0xCF, 0xB6),
        };

        public static readonly Theme Light = new Theme
        {
            IsDark = false,
            Background = Color.FromArgb(0xF1, 0xF3, 0xF7),
            Surface = Color.White,
            SurfaceAlt = Color.FromArgb(0xE9, 0xEC, 0xF2),
            Border = Color.FromArgb(0xD3, 0xD8, 0xE2),
            Foreground = Color.FromArgb(0x1B, 0x1F, 0x27),
            ForegroundDim = Color.FromArgb(0x5E, 0x67, 0x78),
            Accent = Color.FromArgb(0x1F, 0x6F, 0xE0),
            AccentHover = Color.FromArgb(0x18, 0x5C, 0xC0),
            Selection = Color.FromArgb(0xAD, 0xD6, 0xFF),
            Chrome = Color.FromArgb(0xF7, 0xF8, 0xFB),
            Hover = Color.FromArgb(0xDF, 0xE4, 0xED),
            Pressed = Color.FromArgb(0xD0, 0xD8, 0xE6),
            Error = Color.FromArgb(0xC5, 0x2F, 0x38),
            Warning = Color.FromArgb(0x9A, 0x67, 0x00),
            Success = Color.FromArgb(0x1A, 0x7F, 0x37),
        };

        /// <summary>Raised whenever the global active theme changes.</summary>
        public static event Action? Changed;

        /// <summary>Gets the current active theme based on user settings.</summary>
        public static Theme Current => My.MySettingsProperty.Settings.DarkTheme ? Dark : Light;

        /// <summary>Switches the active theme, saves the setting, and notifies listeners.</summary>
        public static void Apply(bool isDark)
        {
            My.MySettingsProperty.Settings.DarkTheme = isDark;
            My.MySettingsProperty.Settings.Save();
            Changed?.Invoke();
        }

        /// <summary>
        /// Recursively applies the theme to a control tree.
        /// Controls can opt-out by setting Tag="no-theme".
        /// </summary>
        public void ApplyTo(Control root)
        {
            if (root is Form form)
            {
                form.BackColor = Background;
                form.ForeColor = Foreground;
                UseImmersiveDarkMode(form.Handle, IsDark);
            }

            foreach (Control control in root.Controls)
            {
                Paint(control);
            }
        }

        private void Paint(Control control)
        {
            if (control.Tag as string == "no-theme") return;

            switch (control)
            {
                case TextBox textBox:
                    textBox.BackColor = Surface;
                    textBox.ForeColor = Foreground;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    if (textBox.Multiline) ApplyExplorerTheme(textBox.Handle, IsDark);
                    break;

                case TreeView tree:
                    tree.BackColor = Surface;
                    tree.ForeColor = Foreground;
                    tree.LineColor = Border;
                    ApplyExplorerTheme(tree.Handle, IsDark);
                    break;

                case ListView list:
                    list.BackColor = Surface;
                    list.ForeColor = Foreground;
                    ApplyExplorerTheme(list.Handle, IsDark);
                    break;

                case ListBox listBox:
                    listBox.BackColor = Surface;
                    listBox.ForeColor = Foreground;
                    listBox.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ComboBox combo:
                    combo.BackColor = Surface;
                    combo.ForeColor = Foreground;
                    combo.FlatStyle = FlatStyle.Flat;
                    break;

                case Button button:
                    StyleButton(button);
                    break;

                case CheckBox or RadioButton:
                    control.BackColor = Color.Transparent;
                    control.ForeColor = Foreground;
                    break;

                case Label label:
                    label.BackColor = Color.Transparent;
                    label.ForeColor = label.Tag as string == "dim" ? ForegroundDim : Foreground;
                    break;

                case StatusStrip status:
                    status.BackColor = Chrome;
                    status.ForeColor = Foreground;
                    status.Renderer = new ThemedToolStripRenderer(this);
                    break;

                case ToolStrip strip:
                    strip.BackColor = Chrome;
                    strip.ForeColor = Foreground;
                    strip.Renderer = new ThemedToolStripRenderer(this);
                    foreach (ToolStripItem item in strip.Items)
                    {
                        PaintToolStripItem(item);
                    }
                    break;

                case TabControl tabs:
                    tabs.BackColor = Background;
                    tabs.ForeColor = Foreground;
                    tabs.Invalidate();
                    break;

                case SplitContainer split:
                    split.BackColor = Border;
                    split.Panel1.BackColor = Background;
                    split.Panel2.BackColor = Background;
                    break;

                case Panel or TableLayoutPanel or FlowLayoutPanel or TabPage or GroupBox:
                    control.BackColor = Background;
                    control.ForeColor = Foreground;
                    break;

                default:
                    // Avoid overriding custom picture box canvases (e.g. plot boxes)
                    if (control is not PictureBox)
                    {
                        control.BackColor = Background;
                        control.ForeColor = Foreground;
                    }
                    break;
            }

            foreach (Control child in control.Controls)
            {
                Paint(child);
            }
        }

        private void PaintToolStripItem(ToolStripItem item)
        {
            item.ForeColor = Foreground;
            if (item is ToolStripDropDownItem dropDown)
            {
                dropDown.DropDown.BackColor = SurfaceAlt;
                dropDown.DropDown.Padding = new Padding(0, 4, 0, 4);
                dropDown.DropDown.ForeColor = Foreground;
                foreach (ToolStripItem child in dropDown.DropDownItems)
                {
                    PaintToolStripItem(child);
                }
            }
            if (item is ToolStripComboBox combo)
            {
                combo.BackColor = Surface;
                combo.ForeColor = Foreground;
                combo.FlatStyle = FlatStyle.Flat;
            }
            if (item is ToolStripTextBox text)
            {
                text.BackColor = Surface;
                text.ForeColor = Foreground;
                text.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        public void StyleButton(Button button)
        {
            var primary = button.Tag as string == "primary";
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.BackColor = primary ? Accent : SurfaceAlt;
            button.ForeColor = primary ? Color.White : Foreground;
            button.FlatAppearance.BorderColor = primary ? Accent : Border;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = primary ? AccentHover : Blend(SurfaceAlt, Accent, 0.18f);
            button.FlatAppearance.MouseDownBackColor = primary ? AccentHover : Blend(SurfaceAlt, Accent, 0.30f);
            button.Cursor = Cursors.Hand;
        }

        public static Color Blend(Color a, Color b, float amount) => Color.FromArgb(
            (int)(a.R + (b.R - a.R) * amount),
            (int)(a.G + (b.G - a.G) * amount),
            (int)(a.B + (b.B - a.B) * amount));

        // ------------------------------------------------------------ Native Win32

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string? subAppName, string? subIdList);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        /// <summary>Paints the window's title bar to match the theme on Windows 10/11.</summary>
        public static void UseImmersiveDarkMode(IntPtr handle, bool enabled)
        {
            if (handle == IntPtr.Zero) return;
            try
            {
                int value = enabled ? 1 : 0;
                int res = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
                if (res != 0)
                {
                    DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref value, sizeof(int));
                }
            }
            catch { /* Older OS - title bar stays default */ }
        }

        /// <summary>Switches a tree/list/textbox scrollbars to dark or light Explorer style.</summary>
        public static void ApplyExplorerTheme(IntPtr handle, bool dark)
        {
            if (handle == IntPtr.Zero) return;
            try
            {
                SetWindowTheme(handle, dark ? "DarkMode_Explorer" : "Explorer", null);
            }
            catch { /* Visual styles unavailable */ }
        }
    }

    /// <summary>Renders menus and toolbars using the modern flat theme palette.</summary>
    public sealed class ThemedToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly Theme _theme;

        public ThemedToolStripRenderer(Theme theme) : base(new ThemedColorTable(theme))
        {
            _theme = theme;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item?.Enabled == true ? _theme.Foreground : _theme.ForegroundDim;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            var color = e.Item?.Enabled == true ? _theme.Foreground : _theme.ForegroundDim;
            e.ArrowColor = color;

            var r = e.ArrowRectangle;
            if (r.Width <= 0 || r.Height <= 0) return;

            var g = e.Graphics;
            var prevSmoothing = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var pen = new Pen(color, 1.6f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round,
            };

            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;

            switch (e.Direction)
            {
                case ArrowDirection.Down:
                    g.DrawLines(pen, new[]
                    {
                        new PointF(cx - 3.5f, cy - 1.5f),
                        new PointF(cx, cy + 2f),
                        new PointF(cx + 3.5f, cy - 1.5f),
                    });
                    break;
                case ArrowDirection.Right:
                    g.DrawLines(pen, new[]
                    {
                        new PointF(cx - 1.5f, cy - 3.5f),
                        new PointF(cx + 2f, cy),
                        new PointF(cx - 1.5f, cy + 3.5f),
                    });
                    break;
                case ArrowDirection.Up:
                    g.DrawLines(pen, new[]
                    {
                        new PointF(cx - 3.5f, cy + 1.5f),
                        new PointF(cx, cy - 2f),
                        new PointF(cx + 3.5f, cy + 1.5f),
                    });
                    break;
                case ArrowDirection.Left:
                    g.DrawLines(pen, new[]
                    {
                        new PointF(cx + 1.5f, cy - 3.5f),
                        new PointF(cx - 2f, cy),
                        new PointF(cx + 1.5f, cy + 3.5f),
                    });
                    break;
                default:
                    base.OnRenderArrow(e);
                    break;
            }

            g.SmoothingMode = prevSmoothing;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(e.ToolStrip is ToolStripDropDown ? _theme.SurfaceAlt : _theme.Chrome);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            var bounds = e.AffectedBounds;
            using var pen = new Pen(_theme.Border);

            if (e.ToolStrip is ToolStripDropDown)
            {
                e.Graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                return;
            }

            e.Graphics.DrawLine(pen, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var item = e.Item;
            if (item is null) return;

            var fill = item.Pressed ? _theme.Pressed
                : item is ToolStripButton { Checked: true } ? _theme.Pressed
                : item.Selected ? _theme.Hover
                : Color.Empty;

            if (fill == Color.Empty) return;

            FillRounded(e.Graphics, new Rectangle(Point.Empty, item.Size), fill);
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e) =>
            OnRenderButtonBackground(e);

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var item = e.Item;
            if (item is null || !item.Selected || !item.Enabled) return;

            var isTopLevel = e.ToolStrip is MenuStrip;
            var bounds = new Rectangle(Point.Empty, item.Size);
            if (!isTopLevel) bounds = Rectangle.Inflate(bounds, -2, -1);

            FillRounded(e.Graphics, bounds, _theme.Hover);
        }

        private static void FillRounded(Graphics g, Rectangle bounds, Color colour, int radius = 4)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            var prev = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var path = RoundedPath(bounds, radius))
            using (var brush = new SolidBrush(colour))
            {
                g.FillPath(brush, path);
            }

            g.SmoothingMode = prev;
        }

        private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            var path = new GraphicsPath();

            if (diameter <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            var arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new Pen(_theme.Border);
            var b = e.Item.Bounds;
            if (e.Vertical)
            {
                e.Graphics.DrawLine(pen, b.Width / 2, 4, b.Width / 2, b.Height - 4);
            }
            else
            {
                e.Graphics.DrawLine(pen, 8, b.Height / 2, b.Width - 8, b.Height / 2);
            }
        }
    }

    /// <summary>Colour table backing <see cref="ThemedToolStripRenderer"/>.</summary>
    public sealed class ThemedColorTable : ProfessionalColorTable
    {
        private readonly Theme _t;

        public ThemedColorTable(Theme theme)
        {
            _t = theme;
        }

        public override Color MenuStripGradientBegin => _t.Chrome;
        public override Color MenuStripGradientEnd => _t.Chrome;
        public override Color ToolStripGradientBegin => _t.Chrome;
        public override Color ToolStripGradientMiddle => _t.Chrome;
        public override Color ToolStripGradientEnd => _t.Chrome;
        public override Color ToolStripDropDownBackground => _t.SurfaceAlt;
        public override Color ImageMarginGradientBegin => _t.SurfaceAlt;
        public override Color ImageMarginGradientMiddle => _t.SurfaceAlt;
        public override Color ImageMarginGradientEnd => _t.SurfaceAlt;
        public override Color MenuItemSelected => Theme.Blend(_t.SurfaceAlt, _t.Accent, 0.35f);
        public override Color MenuItemSelectedGradientBegin => MenuItemSelected;
        public override Color MenuItemSelectedGradientEnd => MenuItemSelected;
        public override Color MenuItemPressedGradientBegin => _t.SurfaceAlt;
        public override Color MenuItemPressedGradientMiddle => _t.SurfaceAlt;
        public override Color MenuItemPressedGradientEnd => _t.SurfaceAlt;
        public override Color MenuItemBorder => _t.Accent;
        public override Color MenuBorder => _t.Border;
        public override Color ButtonSelectedHighlight => MenuItemSelected;
        public override Color ButtonSelectedGradientBegin => MenuItemSelected;
        public override Color ButtonSelectedGradientMiddle => MenuItemSelected;
        public override Color ButtonSelectedGradientEnd => MenuItemSelected;
        public override Color ButtonPressedGradientBegin => Theme.Blend(_t.SurfaceAlt, _t.Accent, 0.5f);
        public override Color ButtonPressedGradientMiddle => ButtonPressedGradientBegin;
        public override Color ButtonPressedGradientEnd => ButtonPressedGradientBegin;
        public override Color SeparatorDark => _t.Border;
        public override Color SeparatorLight => _t.Border;
        public override Color StatusStripGradientBegin => _t.Chrome;
        public override Color StatusStripGradientEnd => _t.Chrome;
    }
}
