#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AERO_Console.UI
{
    /// <summary>
    /// A TabControl that custom-paints its tab strip using the application's active Theme,
    /// eliminating the stock WinForms system gray band in Dark Mode and rendering crisp
    /// accent underlines, hover states, vector icons, and dark-themed scroll controls.
    /// </summary>
    public sealed class ThemedTabControl : TabControl
    {
        private const int IconTextGap = 6;
        private int _hoverIndex = -1;
        private readonly Dictionary<TabPage, AppIcon> _icons = new();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public ThemedTabControl()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            DrawMode = TabDrawMode.OwnerDrawFixed;
            SizeMode = TabSizeMode.Normal;
            ItemSize = new Size(0, 32);
            Padding = new Point(14, 4);
        }

        /// <summary>Associates an AppIcon with a specific TabPage.</summary>
        public void SetIcon(TabPage page, AppIcon icon)
        {
            _icons[page] = icon;
            Invalidate();
        }

        public int IndexAt(Point point)
        {
            for (int i = 0; i < TabCount; i++)
            {
                if (GetTabRect(i).Contains(point))
                    return i;
            }
            return -1;
        }

        public int UpDownWidth
        {
            get
            {
                if (!IsHandleCreated) return 0;
                var hwnd = FindWindowEx(Handle, IntPtr.Zero, "msctls_updown32", null);
                if (hwnd != IntPtr.Zero && IsWindowVisible(hwnd) && GetWindowRect(hwnd, out var rect))
                {
                    var w = rect.Right - rect.Left;
                    return w > 0 ? w + 4 : 0;
                }
                return 0;
            }
        }

        private void RepositionUpDown()
        {
            if (!IsHandleCreated) return;
            var hwnd = FindWindowEx(Handle, IntPtr.Zero, "msctls_updown32", null);
            if (hwnd != IntPtr.Zero)
            {
                Theme.ApplyExplorerTheme(hwnd, Theme.Current.IsDark);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.Changed += OnThemeChanged;
            RepositionUpDown();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Theme.Changed -= OnThemeChanged;
            base.OnHandleDestroyed(e);
        }

        private void OnThemeChanged()
        {
            if (IsHandleCreated && !IsDisposed)
            {
                RepositionUpDown();
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int newHover = IndexAt(e.Location);
            if (newHover != _hoverIndex)
            {
                _hoverIndex = newHover;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                int index = IndexAt(e.Location);
                if (index >= 0 && index < TabCount && index != SelectedIndex)
                {
                    SelectedIndex = index;
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var stripHeight = TabCount > 0 ? GetTabRect(0).Bottom : ItemSize.Height;
            if (e.Y <= stripHeight && TabCount > 1)
            {
                if (e.Delta < 0 && SelectedIndex < TabCount - 1)
                {
                    SelectedIndex++;
                    return;
                }
                if (e.Delta > 0 && SelectedIndex > 0)
                {
                    SelectedIndex--;
                    return;
                }
            }
            base.OnMouseWheel(e);
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            RepositionUpDown();
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // Suppress default background erase to eliminate flicker
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var theme = Theme.Current;
            var g = e.Graphics;

            g.Clear(theme.Background);

            // Strip height based on tab rect
            int stripHeight = TabCount > 0 ? GetTabRect(0).Bottom : ItemSize.Height;

            // Paint strip background
            using (var stripBrush = new SolidBrush(theme.Chrome))
            {
                g.FillRectangle(stripBrush, 0, 0, Width, stripHeight);
            }

            // Strictly clip tabs so they do not overlap overflow scroll buttons
            int reservedRight = UpDownWidth;
            int tabClipWidth = Math.Max(0, Width - reservedRight);
            var oldClip = g.Clip;
            g.SetClip(new Rectangle(0, 0, tabClipWidth, stripHeight));

            // Draw each tab
            for (int i = 0; i < TabCount; i++)
            {
                DrawTab(g, i, theme);
            }

            g.Clip = oldClip;

            // Divider line below the tab strip
            using (var borderPen = new Pen(theme.Border))
            {
                g.DrawLine(borderPen, 0, stripHeight - 1, Width, stripHeight - 1);
            }
        }

        private void DrawTab(Graphics g, int index, Theme theme)
        {
            var rect = GetTabRect(index);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            var page = TabPages[index];
            bool selected = index == SelectedIndex;
            bool hovered = index == _hoverIndex;

            // Tab background
            var bg = selected ? theme.Surface : (hovered ? theme.Hover : theme.Chrome);
            using (var brush = new SolidBrush(bg))
            {
                g.FillRectangle(brush, rect);
            }

            // Active tab accent line at the bottom
            if (selected)
            {
                using var accentBrush = new SolidBrush(theme.Accent);
                g.FillRectangle(accentBrush, rect.X, rect.Bottom - 3, rect.Width, 3);
            }

            // Foreground text color
            var fg = selected ? theme.Foreground : (hovered ? theme.Foreground : theme.ForegroundDim);

            // Icon
            int iconSize = Icons.SizeFor(this, 16);
            Image? iconImg = null;
            if (_icons.TryGetValue(page, out var appIcon))
            {
                iconImg = Icons.Get(appIcon, iconSize, fg);
            }

            // Measure text
            int iconWidth = iconImg != null ? iconImg.Width + IconTextGap : 0;
            var textSize = TextRenderer.MeasureText(g, page.Text, Font, new Size(rect.Width, rect.Height), TextFormatFlags.NoPadding);
            int contentWidth = iconWidth + textSize.Width;
            int startX = rect.X + Math.Max(6, (rect.Width - contentWidth) / 2);

            // Draw Icon
            if (iconImg != null)
            {
                int iconY = rect.Y + (rect.Height - iconImg.Height) / 2;
                g.DrawImage(iconImg, startX, iconY);
                startX += iconWidth;
            }

            // Draw Text
            var textRect = new Rectangle(startX, rect.Y, Math.Max(0, rect.Right - startX - 4), rect.Height);
            TextRenderer.DrawText(g, page.Text, Font, textRect, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HSCROLL = 0x0114;
            const int WM_PARENTNOTIFY = 0x0210;
            const int WM_WINDOWPOSCHANGED = 0x0047;
            const int WM_CREATE = 1;

            if (m.Msg == WM_HSCROLL)
            {
                base.WndProc(ref m);
                RepositionUpDown();
                Invalidate();
                Update();
                return;
            }

            if (m.Msg == WM_PARENTNOTIFY)
            {
                var eventId = unchecked((short)(m.WParam.ToInt32() & 0xFFFF));
                if (eventId == WM_CREATE)
                {
                    base.WndProc(ref m);
                    RepositionUpDown();
                    Invalidate();
                    return;
                }
            }

            if (m.Msg == WM_WINDOWPOSCHANGED)
            {
                if (m.LParam != IntPtr.Zero)
                {
                    base.WndProc(ref m);
                }
                RepositionUpDown();
                return;
            }

            base.WndProc(ref m);
        }
    }
}
