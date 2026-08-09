using System;
using System.Drawing;
using System.Windows.Forms;

namespace AeroPlot
{
    /// <summary>
    /// The shared interactive plot window of the AeroPlot engine. It provides the shell only --
    /// a resizable window with a toolbar (Export PNG/SVG/PDF, Zoom/Fit, optional Light/Dark) and
    /// mouse-drag + wheel navigation -- and delegates rendering + export to an <see cref="IPlotSource"/>.
    ///
    /// Navigation is always crisp (the plot is re-rendered at every step, never a scaled raster).
    /// A 3D source (CanRotate/CanZoomFit) drives its own camera: drag orbits, wheel zooms the model.
    /// A flat 2D source (Trefftz, Cp, ...) is re-rendered through a <see cref="PlotView"/> that
    /// matches the docked plots exactly: Scale floored at 1.0 and pan clamped to the plot extent, so
    /// zoom/pan moves the CONTENT within a fixed frame rather than floating the whole image.
    /// </summary>
    public sealed class frmPlotWindow : Form
    {
        private readonly IPlotSource _source;
        private readonly PictureBox _canvas;
        private readonly ToolStrip _toolbar;
        private readonly ToolStripButton _themeButton;
        private readonly System.Windows.Forms.Timer _resizeDebounce;
        private Point _lastMouse;
        private MouseButtons _dragButton = MouseButtons.None;
        private Bitmap _lastBitmap;
        private bool _autoFitPending;

        // Flat-plot viewport, in native (unzoomed) pixel space == canvas pixels: Scale >= 1 and
        // (PanX,PanY) is the native point at the canvas top-left, clamped so content never leaves
        // the frame. Identity for 3D sources (they navigate their own camera).
        private double _viewScale = 1.0;
        private double _viewPanX;
        private double _viewPanY;
        // Native pan captured at drag start (native coords, like the docked plots' drag).
        private double _dragPanStartX;
        private double _dragPanStartY;

        // Window theme (chrome + what we ask the source to render in). Follows the source's app
        // theme initially so the pop-out opens matching the docked view.
        private bool _dark;

        // Label font-size multiplier, adjustable via the A- / A+ toolbar buttons within sane bounds.
        private double _fontScale = 1.0;
        private const double FontScaleMin = 0.6;
        private const double FontScaleMax = 2.0;
        private const double FontScaleStep = 0.1;

        // True for flat 2D plots (navigate the vector viewport here); false for the 3D geometry
        // source (navigates its own camera). CanZoomFit is true only for that 3D source.
        private bool RasterNav => !_source.CanZoomFit;

        public frmPlotWindow(IPlotSource source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _autoFitPending = source.CanZoomFit; // 3D fits its model to the window on first render
            _dark = source.SupportsThemeToggle ? source.InitialDark : true;

            Text = source.Title;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(820, 620);
            MinimumSize = new Size(360, 300);
            KeyPreview = true;
            DoubleBuffered = true;

            // ---- toolbar ----
            // Built as [Export] | [Zoom+ Zoom- Fit] | [Light/Dark]. A cleanup pass drops any
            // separator left leading/trailing/doubled (e.g. if a group has no buttons), and there
            // is deliberately NO Close button -- the window's own title-bar close already does that.
            _toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };

            var exportMenu = new ToolStripDropDownButton("Export"); // the control draws its own ▾
            exportMenu.DropDownItems.Add("Export as PNG...", null, (s, e) => _source.Export("PNG", _canvas.Width, _canvas.Height));
            exportMenu.DropDownItems.Add("Export as SVG...", null, (s, e) => _source.Export("SVG", _canvas.Width, _canvas.Height));
            exportMenu.DropDownItems.Add("Export as PDF...", null, (s, e) => _source.Export("PDF", _canvas.Width, _canvas.Height));
            _toolbar.Items.Add(exportMenu);
            _toolbar.Items.Add(new ToolStripSeparator());

            var zoomIn = new ToolStripButton("Zoom +");
            zoomIn.Click += (s, e) => ZoomStep(1);
            var zoomOut = new ToolStripButton("Zoom -");
            zoomOut.Click += (s, e) => ZoomStep(-1);
            var btnFit = new ToolStripButton("Fit / Reset");
            btnFit.Click += (s, e) => FitOrReset();
            _toolbar.Items.Add(zoomIn);
            _toolbar.Items.Add(zoomOut);
            _toolbar.Items.Add(btnFit);

            if (_source.SupportsFontScale)
            {
                _toolbar.Items.Add(new ToolStripSeparator());
                var fontSmaller = new ToolStripButton("A-") { ToolTipText = "Decrease label font size" };
                fontSmaller.Click += (s, e) => StepFontScale(-1);
                var fontLarger = new ToolStripButton("A+") { ToolTipText = "Increase label font size" };
                fontLarger.Click += (s, e) => StepFontScale(1);
                _toolbar.Items.Add(fontSmaller);
                _toolbar.Items.Add(fontLarger);
            }

            _toolbar.Items.Add(new ToolStripSeparator());
            _themeButton = new ToolStripButton(_dark ? "Light" : "Dark") { ToolTipText = "Toggle light / dark theme" };
            _themeButton.Click += (s, e) => ToggleTheme();
            if (_source.SupportsThemeToggle)
                _toolbar.Items.Add(_themeButton);

            RemoveRedundantSeparators(_toolbar);

            // ---- canvas ----
            _canvas = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
            };
            _canvas.Paint += Canvas_Paint;
            _canvas.MouseDown += Canvas_MouseDown;
            _canvas.MouseMove += Canvas_MouseMove;
            _canvas.MouseUp += Canvas_MouseUp;
            _canvas.MouseWheel += Canvas_MouseWheel;
            _canvas.MouseEnter += (s, e) => _canvas.Focus();

            Controls.Add(_canvas);
            Controls.Add(_toolbar);

            // Resizing fires a burst of Resize events; re-render once it settles.
            _resizeDebounce = new System.Windows.Forms.Timer { Interval = 60 };
            _resizeDebounce.Tick += (s, e) => { _resizeDebounce.Stop(); RequestRender(); };
            _canvas.Resize += (s, e) => { _resizeDebounce.Stop(); _resizeDebounce.Start(); };

            // Tell the source which theme to render in before the first paint.
            if (_source.SupportsThemeToggle)
                _source.SetDarkTheme(_dark);
            ApplyWindowTheme();

            Shown += (s, e) => RequestRender();
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        }

        // Drops separators that end up at the start, at the end, or next to another separator --
        // so a toolbar group with no visible buttons leaves no orphaned divider.
        private static void RemoveRedundantSeparators(ToolStrip strip)
        {
            bool prevWasSep = true; // treat "start of strip" as a preceding separator
            for (int i = 0; i < strip.Items.Count;)
            {
                var it = strip.Items[i];
                bool isSep = it is ToolStripSeparator;
                if (isSep && prevWasSep)
                {
                    strip.Items.RemoveAt(i);
                    continue; // don't advance; re-test the shifted item
                }
                prevWasSep = isSep;
                i++;
            }
            // Trailing separator, if any.
            while (strip.Items.Count > 0 && strip.Items[strip.Items.Count - 1] is ToolStripSeparator)
                strip.Items.RemoveAt(strip.Items.Count - 1);
        }

        private void ToggleTheme()
        {
            _dark = !_dark;
            _themeButton.Text = _dark ? "Light" : "Dark";
            _source.SetDarkTheme(_dark);
            ApplyWindowTheme();
            RequestRender();
        }

        // Themes the window chrome (toolbar + canvas margins) to match the plot's theme so the
        // shell doesn't clash with the rendered plot.
        private void ApplyWindowTheme()
        {
            Color bg = _dark ? Color.FromArgb(32, 32, 32) : Color.FromArgb(238, 238, 238);
            Color fg = _dark ? Color.White : Color.Black;
            BackColor = bg;
            _toolbar.BackColor = bg;
            _toolbar.ForeColor = fg;
            foreach (ToolStripItem it in _toolbar.Items)
                it.ForeColor = fg;
            _canvas.BackColor = _dark ? Color.Black : Color.White;
            _canvas.Invalidate();
        }

        private void FitToWindow()
        {
            if (!_source.CanZoomFit || _canvas.Width < 1 || _canvas.Height < 1)
                return;
            try { _source.Fit(_canvas.Width, _canvas.Height); }
            catch { }
        }

        /// <summary>Renders the plot at the current canvas size and (for flat plots) view.</summary>
        public void RequestRender()
        {
            if (_canvas.Width < 1 || _canvas.Height < 1 || IsDisposed)
                return;
            if (_autoFitPending)
            {
                _autoFitPending = false;
                FitToWindow();
            }
            // Set the theme right before each render so windows sharing one host source (e.g. the
            // XFOIL Cp + BL pop-outs) each render in their own theme, even on resize/zoom re-renders.
            if (_source.SupportsThemeToggle)
                _source.SetDarkTheme(_dark);
            // Likewise the font scale, so each window keeps its own size on every re-render.
            if (_source.SupportsFontScale)
                _source.SetFontScale(_fontScale);
            var view = RasterNav ? new PlotView(_viewScale, _viewPanX, _viewPanY) : PlotView.Identity;
            Bitmap bmp;
            try
            {
                bmp = _source.Render(_canvas.Width, _canvas.Height, view);
            }
            catch
            {
                return; // a transient render failure shouldn't take down the window
            }
            var old = _lastBitmap;
            _lastBitmap = bmp;
            if (!ReferenceEquals(old, bmp))
                old?.Dispose();
            _canvas.Invalidate();
        }

        private void ResetView()
        {
            _viewScale = 1.0;
            _viewPanX = 0.0;
            _viewPanY = 0.0;
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(_canvas.BackColor);
            if (_lastBitmap is not null)
                e.Graphics.DrawImageUnscaled(_lastBitmap, 0, 0); // already rendered at the right view
        }

        // Zoom In/Out toolbar buttons. 3D zooms its model; flat plots zoom the content about the
        // canvas center; both re-render crisply.
        private void ZoomStep(int direction)
        {
            if (_source.CanZoomFit)
            {
                _source.Zoom(direction > 0 ? 120 : -120);
                RequestRender();
            }
            else
            {
                ZoomViewAbout(_canvas.Width / 2f, _canvas.Height / 2f, direction > 0 ? 1.25 : 1.0 / 1.25);
            }
        }

        // Adjust the label font size by one step, clamped to [FontScaleMin, FontScaleMax], then
        // re-render so the plot's text redraws crisply at the new size.
        private void StepFontScale(int direction)
        {
            double next = Math.Round(_fontScale + direction * FontScaleStep, 2);
            next = Math.Max(FontScaleMin, Math.Min(FontScaleMax, next));
            if (Math.Abs(next - _fontScale) < 1e-9)
                return;
            _fontScale = next;
            RequestRender();
        }

        private void FitOrReset()
        {
            if (_source.CanZoomFit)
                FitToWindow();
            else
                ResetView();
            RequestRender();
        }

        // Scale the flat-plot content about (cx,cy) in canvas pixels, keeping that point fixed, then
        // re-render crisply. Mirrors the docked plots: Scale is floored at 1.0 (never smaller than
        // fit, so no empty margins) and pan is clamped so content stays within the frame.
        private void ZoomViewAbout(float cx, float cy, double factor)
        {
            double oldScale = _viewScale;
            double newScale = Math.Max(1.0, Math.Min(20.0, oldScale * factor));
            if (newScale == oldScale)
                return;
            double nativeX = _viewPanX + cx / oldScale;
            double nativeY = _viewPanY + cy / oldScale;
            _viewScale = newScale;
            _viewPanX = nativeX - cx / newScale;
            _viewPanY = nativeY - cy / newScale;
            ClampPan();
            RequestRender();
        }

        // Pins pan so the visible native window (canvas / scale) can never extend past the plot's
        // [0, canvas size] extent -- at Scale=1 the window equals that extent, so pan pins to (0,0)
        // until the user zooms in.
        private void ClampPan()
        {
            double viewW = _canvas.Width / _viewScale;
            double viewH = _canvas.Height / _viewScale;
            _viewPanX = viewW >= _canvas.Width ? 0.0 : Math.Max(0.0, Math.Min(_viewPanX, _canvas.Width - viewW));
            _viewPanY = viewH >= _canvas.Height ? 0.0 : Math.Max(0.0, Math.Min(_viewPanY, _canvas.Height - viewH));
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            _dragButton = e.Button;
            _lastMouse = e.Location;      // drag-start location (native pan is anchored to this)
            _dragPanStartX = _viewPanX;
            _dragPanStartY = _viewPanY;
            _canvas.Cursor = Cursors.SizeAll;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragButton == MouseButtons.None)
                return;

            if (_source.CanRotate)
            {
                float dx = e.X - _lastMouse.X;
                float dy = e.Y - _lastMouse.Y;
                _lastMouse = e.Location;   // 3D orbits incrementally, so advance the anchor
                // 3D: left = yaw+pitch, right = pitch, middle = roll (mirrors the docked 3D view).
                if (_dragButton == MouseButtons.Left)
                    _source.Rotate(dx * 0.5f, -dy * 0.5f, 0f);
                else if (_dragButton == MouseButtons.Right)
                    _source.Rotate(0f, dx * 0.5f, 0f);
                else if (_dragButton == MouseButtons.Middle)
                    _source.Rotate(0f, 0f, dx * 0.5f);
                RequestRender();
            }
            else if (RasterNav)
            {
                // Flat plot: pan the content in native coords relative to the drag-start pan (the
                // anchor stays fixed at mouse-down), then re-render crisply -- the same math the
                // docked plots use, so content stays framed instead of the image floating.
                _viewPanX = _dragPanStartX - (e.X - _lastMouse.X) / _viewScale;
                _viewPanY = _dragPanStartY - (e.Y - _lastMouse.Y) / _viewScale;
                ClampPan();
                RequestRender();
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            _dragButton = MouseButtons.None;
            _canvas.Cursor = Cursors.Default;
        }

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_source.CanZoomFit)
            {
                _source.Zoom(e.Delta);
                RequestRender();
            }
            else
            {
                ZoomViewAbout(e.X, e.Y, e.Delta > 0 ? 1.15 : 1.0 / 1.15);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _resizeDebounce?.Dispose();
                _lastBitmap?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
