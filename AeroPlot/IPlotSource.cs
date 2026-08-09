using System;
using System.Drawing;

namespace AeroPlot
{
    /// <summary>
    /// The contract a plot implements to be shown in the shared <see cref="frmPlotWindow"/>.
    /// The plot source owns its rendering, interaction, and export (using the app's renderers +
    /// <see cref="SvgGraphics"/>); the window only provides the interactive shell (toolbar,
    /// mouse handling, resize). This keeps the plot engine independent of any particular app:
    /// AVL and XFOIL each supply their own IPlotSource, exactly as avl.exe/xfoil.exe drive one
    /// shared plotting subsystem.
    /// </summary>
    public interface IPlotSource
    {
        /// <summary>Window title.</summary>
        string Title { get; }

        /// <summary>Renders the plot at the given pixel size for on-screen display, applying the
        /// zoom/pan <paramref name="view"/> (flat 2D plots re-render their vectors crisply at that
        /// view; 3D sources ignore it and navigate their own camera).</summary>
        Bitmap Render(int width, int height, PlotView view);

        /// <summary>True if mouse-drag orbits the view (3D plots). Gates drag handling.</summary>
        bool CanRotate { get; }

        /// <summary>True if the plot supports wheel-zoom and fit-to-window (3D plots). Gates the
        /// Zoom/Fit toolbar buttons and the first-render auto-fit.</summary>
        bool CanZoomFit { get; }

        /// <summary>Orbit the view by the given deltas in degrees (no-op if !CanRotate).</summary>
        void Rotate(float dAlpha, float dBeta, float dGamma);

        /// <summary>Zoom by a mouse-wheel delta (no-op if !CanZoomFit).</summary>
        void Zoom(int wheelDelta);

        /// <summary>Fit the view to a viewport of the given size (no-op if !CanZoomFit).</summary>
        void Fit(int width, int height);

        /// <summary>Save the plot as "PNG" | "SVG" | "PDF" at the given size (shows its own
        /// Save dialog).</summary>
        void Export(string format, int width, int height);

        /// <summary>True if the source can re-render in light or dark; gates the window's theme
        /// toggle button.</summary>
        bool SupportsThemeToggle { get; }

        /// <summary>The theme the window's toggle should start in (typically the app's current
        /// theme), so the pop-out opens matching the docked view.</summary>
        bool InitialDark { get; }

        /// <summary>Sets the theme used for the NEXT render (no-op if !SupportsThemeToggle). The
        /// window calls this then RequestRender; it affects only this pop-out, not the docked tab.</summary>
        void SetDarkTheme(bool dark);
    }

    /// <summary>Adapter that builds an <see cref="IPlotSource"/> from plain delegates, so a host
    /// can wire an existing set of render/interaction/export methods without writing a class.
    /// Capability flags derive from which delegates were supplied.</summary>
    public sealed class DelegatePlotSource : IPlotSource
    {
        private readonly Func<int, int, PlotView, Bitmap> _render;
        private readonly Action<float, float, float> _rotate;
        private readonly Action<int> _zoom;
        private readonly Action<int, int> _fit;
        private readonly Action<string, int, int> _export;
        private readonly Action<bool> _setTheme;

        public DelegatePlotSource(
            string title,
            Func<int, int, PlotView, Bitmap> render,
            Action<string, int, int> export,
            Action<float, float, float> rotate = null,
            Action<int> zoom = null,
            Action<int, int> fit = null,
            Action<bool> setTheme = null,
            bool initialDark = false)
        {
            Title = title;
            _render = render ?? throw new ArgumentNullException(nameof(render));
            _export = export;
            _rotate = rotate;
            _zoom = zoom;
            _fit = fit;
            _setTheme = setTheme;
            InitialDark = initialDark;
        }

        public string Title { get; }
        public bool CanRotate => _rotate != null;
        public bool CanZoomFit => _fit != null;
        public bool SupportsThemeToggle => _setTheme != null;
        public bool InitialDark { get; }

        public Bitmap Render(int width, int height, PlotView view) => _render(width, height, view);
        public void Rotate(float dAlpha, float dBeta, float dGamma) => _rotate?.Invoke(dAlpha, dBeta, dGamma);
        public void Zoom(int wheelDelta) => _zoom?.Invoke(wheelDelta);
        public void Fit(int width, int height) => _fit?.Invoke(width, height);
        public void Export(string format, int width, int height) => _export?.Invoke(format, width, height);
        public void SetDarkTheme(bool dark) => _setTheme?.Invoke(dark);
    }
}
