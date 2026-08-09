using AeroPlot;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AERO_Console
{
    // Detached-window support for the AVL 3D geometry view. HeavyRender draws the 3D scene
    // at V3W x V3H (the docked p3d size by default); a resizable pop-out window can override
    // that size and capture the finished frame via RenderGeometry3D, so the same rendering
    // code drives both the docked tab and the standalone interactive window.
    public partial class frmGeometry
    {
        // Non-blocking mutual exclusion for HeavyRender's two entry points: the debounced
        // docked render (background task) and the synchronous pop-out capture below. A plain
        // lock would DEADLOCK -- the background render marshals PictureBox assignments to the
        // UI thread via Invoke, so if the UI thread were blocked waiting on the lock the
        // Invoke could never complete. Interlocked lets whoever is second simply skip its
        // frame instead of blocking. 0 = idle, 1 = a render is in flight.
        private int _renderBusy;

        /// <summary>Runs HeavyRender only if no other render is in flight (else skips this
        /// frame). Used by the debounced docked render path.</summary>
        private void RunHeavyRenderGuarded(CancellationToken token)
        {
            if (Interlocked.CompareExchange(ref _renderBusy, 1, 0) != 0)
                return;
            try { HeavyRender(null, token); }
            finally { Interlocked.Exchange(ref _renderBusy, 0); }
        }

        // Render-size override for the 3D view. When >0 these replace the docked p3d
        // PictureBox dimensions everywhere the 3D projection needs a viewport size.
        private int _ovr3DW;
        private int _ovr3DH;
        private int V3W => _ovr3DW > 0 ? _ovr3DW : Math.Max(1, p3d.Width);
        private int V3H => _ovr3DH > 0 ? _ovr3DH : Math.Max(1, p3d.Height);

        // When set, HeavyRender diverts the finished 3D bitmap here instead of assigning it
        // to the docked p3d PictureBox (see the p3d assignment block in HeavyRender).
        private bool _grab3DMode;
        private Bitmap _grab3D;

        /// <summary>Renders the current 3D geometry view (same camera/toggles as the docked
        /// tab) at an arbitrary pixel size and returns it as a fresh Bitmap, without touching
        /// the docked p3d PictureBox. Call on the UI thread. Used by the detached
        /// interactive plot window (<see cref="frmPlotWindow"/>).</summary>
        public Bitmap RenderGeometry3D(int width, int height)
        {
            if (width < 1) width = 1;
            if (height < 1) height = 1;

            // If the docked background render is mid-flight, skip rather than race/deadlock;
            // the next interaction (or resize-debounce tick) will render this frame.
            if (Interlocked.CompareExchange(ref _renderBusy, 1, 0) != 0)
                return _grab3D ?? new Bitmap(width, height);

            bool prevShow3D = show3D;
            _ovr3DW = width;
            _ovr3DH = height;
            _grab3DMode = true;
            _grab3D = null;
            show3D = true;
            try
            {
                HeavyRender(null, CancellationToken.None);
            }
            finally
            {
                _ovr3DW = 0;
                _ovr3DH = 0;
                _grab3DMode = false;
                show3D = prevShow3D;
                Interlocked.Exchange(ref _renderBusy, 0);
            }
            return _grab3D ?? new Bitmap(width, height);
        }

        /// <summary>Renders the 3D view at the given size WITH vector capture on, returning the
        /// raster bitmap plus the SVG and PDF content-stream strings (same machinery frmGeometry's
        /// ExportView uses for the docked views). Used by the detached window's PNG/SVG/PDF export.</summary>
        private (Bitmap bmp, string svg, string pdf) RenderGeometry3DForExport(int width, int height)
        {
            if (width < 1) width = 1;
            if (height < 1) height = 1;
            if (Interlocked.CompareExchange(ref _renderBusy, 1, 0) != 0)
                return (new Bitmap(width, height), "", "");

            bool prevShow3D = show3D;
            bool prevCapture = _captureVectors;
            _ovr3DW = width;
            _ovr3DH = height;
            _grab3DMode = true;
            _grab3D = null;
            show3D = true;
            _captureVectors = true; // makes the SvgGraphics record vector ops -> p3dSvg/p3dPdf
            try
            {
                HeavyRender(null, CancellationToken.None);
            }
            finally
            {
                _ovr3DW = 0;
                _ovr3DH = 0;
                _grab3DMode = false;
                show3D = prevShow3D;
                _captureVectors = prevCapture;
                Interlocked.Exchange(ref _renderBusy, 0);
            }
            return (_grab3D ?? new Bitmap(width, height), p3dSvg ?? "", p3dPdf ?? "");
        }

        /// <summary>Export handler for the detached 3D window's "Export ▾" menu: renders the
        /// current view at the window size and saves it as PNG, SVG, or PDF -- the same three
        /// formats (and the same WriteVectorPdf writer) frmGeometry's docked Export button uses.</summary>
        public void ExportGeometry3D(string format, int width, int height)
        {
            _popoutRenderActive = true; // render the export at the pop-out's chosen font size
            try
            {
                var (bmp, svg, pdf) = RenderGeometry3DForExport(width, height);

                if (format != "PNG" && string.IsNullOrEmpty(svg))
                {
                    AppMessageBox.Show("The view has not finished rendering. Please try again in a moment.",
                        "Export View", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    bmp?.Dispose();
                    return;
                }

                using var sfd = new SaveFileDialog { Title = $"Export 3D View as {format}" };
                string projName = (projectName ?? "").Trim();
                if (string.IsNullOrEmpty(projName) || projName.Contains("Enter AVL Project") || projName.Contains("Enter NACA"))
                {
                    sfd.FileName = "3D_View";
                }
                else
                {
                    foreach (var c in Path.GetInvalidFileNameChars())
                        projName = projName.Replace(c, '_');
                    sfd.FileName = $"{projName}_3D_View";
                }
                switch (format)
                {
                    case "PNG": sfd.Filter = "PNG Image (*.png)|*.png"; sfd.DefaultExt = "png"; break;
                    case "SVG": sfd.Filter = "SVG Image (*.svg)|*.svg"; sfd.DefaultExt = "svg"; break;
                    case "PDF": sfd.Filter = "PDF Document (*.pdf)|*.pdf"; sfd.DefaultExt = "pdf"; break;
                }

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    switch (format)
                    {
                        case "PNG":
                            using (var copy = new Bitmap(bmp))
                                copy.Save(sfd.FileName, ImageFormat.Png);
                            break;
                        case "SVG":
                            File.WriteAllText(sfd.FileName, svg, Encoding.UTF8);
                            break;
                        case "PDF":
                            WriteVectorPdf(pdf, width, height, sfd.FileName);
                            break;
                    }
                    AppToast.ShowExported($"{format} exported to " + Path.GetFileName(sfd.FileName), sfd.FileName);
                }
                bmp?.Dispose();
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Error exporting file: " + ex.Message, "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _popoutRenderActive = false;
            }
        }

        // ---- Interaction forwarded from the detached 3D window --------------------------
        // These mutate the SHARED view state (viewAlpha/Beta/Gamma, viewDist) and refresh
        // the docked tab too, so the pop-out and the docked p3d stay in sync. Mirror the
        // p3d_MouseMove / p3d_MouseWheel / Fit3D behavior exactly.

        /// <summary>Orbit the 3D view (degrees), matching a left/right/middle drag on p3d.</summary>
        public void Popout3DRotate(float dAlpha, float dBeta, float dGamma)
        {
            viewAlpha += dAlpha;
            viewBeta += dBeta;
            viewGamma += dGamma;
            drawAxes();
        }

        /// <summary>Zoom the 3D view by a mouse-wheel delta, matching p3d_MouseWheel.</summary>
        public void Popout3DZoom(int wheelDelta)
        {
            int scrollAmount = (int)Math.Round(wheelDelta / 120d);
            float fitDist = ComputeFitViewDist();
            float minDist = Math.Max(2.0f, fitDist * 0.1f);
            float maxDist = fitDist * 5.0f;
            float zoomStep = Math.Max(0.5f, fitDist * 0.05f);
            viewDist -= scrollAmount * zoomStep;
            if (viewDist < minDist) viewDist = minDist;
            if (viewDist > maxDist) viewDist = maxDist;
            drawAxes();
        }

        /// <summary>Fit the 3D view to the model for a viewport of the given size (the pop-out
        /// window's canvas), then reset to the isometric. Without the size override, Fit3D would
        /// size the fit to the docked p3d PictureBox instead of the pop-out window, leaving the
        /// geometry too small.</summary>
        public void Popout3DFit(int width, int height)
        {
            _ovr3DW = Math.Max(1, width);
            _ovr3DH = Math.Max(1, height);
            try { Fit3D(); }          // uses ComputeFitViewDist() -> V3W/V3H == the window size
            finally { _ovr3DW = 0; _ovr3DH = 0; }
        }

        // Tracks the live 3D pop-out so re-opening focuses it instead of stacking windows.
        private frmPlotWindow _plot3DWindow;

        /// <summary>Opens (or focuses) the detached, resizable, interactive 3D geometry window.
        /// Co-exists with the docked p3d tab; the two share camera state.</summary>
        public void OpenGeometry3DWindow()
        {
            if (_plot3DWindow is not null && !_plot3DWindow.IsDisposed)
            {
                _plot3DWindow.Focus();
                _plot3DWindow.RequestRender();
                return;
            }

            _plot3DWindow = new frmPlotWindow(new DelegatePlotSource(
                "AVL Geometry - 3D View",
                render: (w, h, _) => PopoutRender(() => RenderGeometry3D(w, h)), // 3D navigates its own camera; ignores PlotView
                export: ExportGeometry3D,
                rotate: Popout3DRotate,
                zoom: Popout3DZoom,
                fit: Popout3DFit,
                setTheme: SetPopoutTheme, initialDark: IsDarkTheme, setFontScale: SetPopoutFontScale))
            {
                Icon = this.Icon,
            };
            _plot3DWindow.FormClosed += (_, __) => _plot3DWindow = null;
            _plot3DWindow.Show(this);
        }

        /// <summary>Entry point for the native-AVL console: when the user types "G" at the
        /// .OPER menu, load the current project's geometry into this form (so the renderer has
        /// data even if the Designer was never opened) and pop the interactive 3D window. This
        /// stands in for avl.exe's OPER geometry-plot window, which the native port omits.</summary>
        public void ShowConsoleGeometryPlot(string avlName, string avlText)
        {
            LoadConsoleGeometryInto(avlName, avlText);
            OpenGeometry3DWindow();
        }

        /// <summary>Loads the geometry the native console just handled into this form so the
        /// renderer/analyses have data, without popping the Designer. Shared by the console "G"
        /// (geometry) and "T" (Trefftz) hooks.</summary>
        internal void LoadConsoleGeometryInto(string avlName, string avlText)
        {
            // The renderer needs the form's controls (p3d, editor, tabs), which are built in
            // frmGeometry_Load -- and that only runs the first time the Designer is shown. If the
            // user has been working purely in the console it hasn't run yet, so build them now.
            EnsureFormInitialized();

            try
            {
                if (!string.IsNullOrWhiteSpace(avlName))
                {
                    // Name the project after whatever the user actually loaded, so save/export
                    // and findPoints' file fallback line up with the loaded file.
                    projectName = Path.GetFileNameWithoutExtension(avlName);
                    loadedProjectName = projectName;
                }

                if (!string.IsNullOrEmpty(avlText) && txt3 is not null)
                {
                    // Feed findPoints the EXACT text the console loaded: put it in the editor and
                    // select the Geometry tab (findPoints only reads txt3 when that tab is active).
                    var geoTab = tc1?.TabPages["Geometry"];
                    if (geoTab is not null)
                        tc1.SelectedTab = geoTab;
                    txt3.Text = avlText;
                    FormatActiveText();
                }
                else
                {
                    // No captured geometry (e.g. typed before any successful load): fall back to
                    // the project's .avl on disk.
                    LoadAVL();
                }
            }
            catch
            {
            }

            try { findPoints(); } catch { }  // parse into points/parsedSurfaces for the renderer
        }

        // txt3 is created in frmGeometry_Load; a null txt3 means Load hasn't run. Build the control
        // tree by invoking the Load handler directly -- it only creates controls and reads settings,
        // needing no visible/handle-created window -- so the Designer never flashes on screen. (The
        // previous Show()/Hide() approach re-showed the form via the WindowState restore.)
        private void EnsureFormInitialized()
        {
            if (txt3 is not null)
                return;
            try
            {
                frmGeometry_Load(this, EventArgs.Empty);
            }
            catch
            {
            }
        }
    }
}
