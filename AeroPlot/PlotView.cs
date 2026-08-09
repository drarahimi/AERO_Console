using System.Drawing;
using System.Drawing.Drawing2D;

namespace AeroPlot
{
    /// <summary>
    /// A zoom/pan viewport for a flat 2D plot, expressed in the plot's own "native" (unzoomed,
    /// Scale=1) pixel space: <see cref="Scale"/> is the zoom factor and (<see cref="PanX"/>,
    /// <see cref="PanY"/>) is the native-space point shown at the canvas's TOP-LEFT corner --
    /// identical semantics to the docked frmGeometry / frmXfoilAnalysis plots' PlotZoomState.
    ///
    /// It is applied as a single Graphics transform BEFORE the plot draws, so the CONTENT (axes,
    /// curves, labels) is re-rasterized crisply at the zoomed resolution and stays inside the
    /// frame -- the plot zooms/pans its content, rather than the finished image being scaled or
    /// floated around in a void. 3D sources ignore it (they navigate their own camera).
    /// </summary>
    public readonly struct PlotView
    {
        /// <summary>Zoom factor (1.0 = fit; the window never scales below 1.0 for flat plots).</summary>
        public double Scale { get; }

        /// <summary>Native-space X shown at the canvas's left edge.</summary>
        public double PanX { get; }

        /// <summary>Native-space Y shown at the canvas's top edge.</summary>
        public double PanY { get; }

        public PlotView(double scale, double panX, double panY)
        {
            Scale = scale;
            PanX = panX;
            PanY = panY;
        }

        public static PlotView Identity => new PlotView(1.0, 0.0, 0.0);

        public bool IsIdentity => Scale == 1.0 && PanX == 0.0 && PanY == 0.0;

        /// <summary>Applies this viewport to the drawing Graphics (no-op if identity). The mapping
        /// is screen = (native - pan) * scale -- the exact matrix the docked plots use -- so drawing
        /// in native coordinates lands zoomed/panned and crisp.</summary>
        public void ApplyTo(Graphics g)
        {
            if (IsIdentity)
                return;
            g.Transform = new Matrix((float)Scale, 0f, 0f, (float)Scale,
                                     (float)(-PanX * Scale), (float)(-PanY * Scale));
        }
    }
}
