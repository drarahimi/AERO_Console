using AeroPlot;
using System;
using System.Collections.Generic;

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Xfoil.Core.Geometry;
using Xfoil.Core.Solver;
using Xfoil.Core.Solver.Bl;

namespace AERO_Console
{

    /// <summary>
/// Standalone XFOIL analysis window: owns its own dedicated xfoil.exe child process
/// (independent of frmMain's engine selector / process), runs alpha-sweep polars and
/// single-point Cp/boundary-layer snapshots, and renders the results as native .NET
/// plots the same way frmGeometry's AVL "Polar" tab does (GDI+ via SvgGraphics, with
/// PNG/SVG/PDF export).
/// </summary>
    public class frmXfoilAnalysis : Form
    {

        #region Process management

        private Process p;
        private Thread bt;
        private bool Leaving = false;
        private readonly StringBuilder _logBuffer = new StringBuilder();
        private readonly object _logBufferLock = new object();
        private System.Windows.Forms.Timer _logFlushTimer;

        // Debounce timers for the plot Resize handlers below: a TableLayoutPanel/TabControl
        // layout pass can fire a control's Resize event more than once with different
        // intermediate sizes before settling (a known WinForms quirk), and re-rendering
        // synchronously on every one of those intermediate events was causing the plot to
        // visibly thrash between a stale small size and the correct final size. Collapsing
        // a burst of Resize events into a single render of whatever size is current when
        // the burst goes quiet avoids that.
        private System.Windows.Forms.Timer _polarResizeTimer;
        private System.Windows.Forms.Timer _cpResizeTimer;
        private System.Windows.Forms.Timer _blResizeTimer;
        private System.Windows.Forms.Timer _geomResizeTimer;

        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "aeroconsole_xfoil");
        // Regenerated fresh (unique filename) at the start of every run rather than reused -
        // if a just-exited prior run's process hadn't fully released its file handle yet,
        // XFOIL would hit its own "Output file exists. Overwrite? Y" prompt on the SAME
        // filename and sit there waiting for a keystroke we never send, silently hanging
        // with no error. A fresh name every time makes that class of hang impossible.
        private string _polFile;
        private string _cpFile;
        private string _blFile;
        private string _geomFile;

        private string NewTempFile(string baseName)
        {
            return Path.Combine(_tempDir, baseName + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".txt");
        }

        /// <summary>
    /// Reads a specific process's stdout, passed in explicitly rather than read back off
    /// the shared "p" field. "p" gets reassigned to a brand-new Process on every run (see
    /// StartProcess - a fresh process is started per analysis run), and a background
    /// thread whose loop condition instead re-reads that shared field would silently
    /// retarget itself onto whatever process "p" currently points to the next time its
    /// loop condition is evaluated - i.e. an old run's leftover thread could end up
    /// racing the new run's thread to read the NEW process's stdout, each stealing chunks
    /// of the other's output. Capturing the exact Process instance as a parameter at
    /// thread-start time makes each thread bound to the run it was created for.
    /// </summary>
        private void ReadThread(Process proc)
        {
            lock (_logBufferLock)
                _logBuffer.Append($"*** Reader thread started for pid {proc.Id} (blocking on first read...)" + Environment.NewLine);
            try
            {
                var buffer = new char[4097];
                while (!Leaving && !proc.HasExited)
                {
                    int bytesRead = proc.StandardOutput.Read(buffer, 0, buffer.Length);
                    lock (_logBufferLock)
                        _logBuffer.Append($"*** [read returned {bytesRead}]" + Environment.NewLine);
                    if (bytesRead <= 0)
                        break;
                    string textChunk = new string(buffer, 0, bytesRead);
                    lock (_logBufferLock)
                        _logBuffer.Append(textChunk);
                }
            }
            catch (Exception ex)
            {
                // Surface this instead of swallowing it silently - a read failure here (broken
                // pipe, access denied, etc.) previously looked identical in the log to "XFOIL
                // just isn't saying anything yet", making a real failure indistinguishable from
                // XFOIL still being busy.
                lock (_logBufferLock)
                    _logBuffer.Append(Environment.NewLine + "*** stdout read failed: " + ex.Message + Environment.NewLine);
            }
        }

        /// <summary>Appends a line to the log box directly - only call this from the UI thread.</summary>
        private void LogLine(string text)
        {
            if (txtLog is null)
                return;
            txtLog.AppendText(text + Environment.NewLine);
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private void _logFlushTimer_Tick(object sender, EventArgs e)
        {
            string pending = null;
            lock (_logBufferLock)
            {
                if (_logBuffer.Length > 0)
                {
                    pending = _logBuffer.ToString();
                    _logBuffer.Clear();
                }
            }
            if (pending is null)
                return;
            txtLog.AppendText(pending);
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private void _polarResizeTimer_Tick(object sender, EventArgs e)
        {
            _polarResizeTimer.Stop();
            RenderPolarPlot();
        }

        private void _cpResizeTimer_Tick(object sender, EventArgs e)
        {
            _cpResizeTimer.Stop();
            RenderCpPlot();
        }

        private void _blResizeTimer_Tick(object sender, EventArgs e)
        {
            _blResizeTimer.Stop();
            RenderBlPlot();
        }

        private void _geomResizeTimer_Tick(object sender, EventArgs e)
        {
            _geomResizeTimer.Stop();
            RenderGeometryPlot();
        }

        private void StartProcess(string appPath)
        {
            if (p is not null)
            {
                try
                {
                    if (!p.HasExited)
                    {
                        p.Kill();
                        // Wait for the OS to fully tear down the killed process (release its
                        // stdio pipe handles etc.) before starting a new one - starting the
                        // next process immediately after Kill() (which only requests
                        // termination, asynchronously) was observed to make the *next* run's
                        // xfoil.exe produce no output at all, even though the same sequence
                        // works fine as the first run in a session.
                        p.WaitForExit(2000);
                    }
                    p.Dispose();
                }
                catch
                {
                }
                p = null;
            }

            // Defensive: StartProcess can in principle run before the constructor's
            // Directory.CreateDirectory(_tempDir) has taken effect, or the folder could have
            // been deleted externally - a WorkingDirectory that doesn't exist makes p.Start()
            // throw, which used to be swallowed with no visible sign anything went wrong.
            Directory.CreateDirectory(_tempDir);

            p = new Process();
            var startinfo = new ProcessStartInfo();
            startinfo.FileName = appPath;
            startinfo.Arguments = "";
            // XFOIL's own SAVE/PACC/CPWR/DUMP file-name prompts are read into a fixed-length
            // Fortran CHARACTER buffer - confirmed against the real xfoil.exe that a long
            // absolute path (e.g. under %TEMP%\aeroconsole_xfoil\...) silently fails to open
            // the file at all (no error, just nothing written), while the same command with
            // a short path works fine. Running with _tempDir as the working directory lets
            // every *short, relative* filename we pass for those commands resolve correctly
            // regardless of how deep the user's actual %TEMP% happens to be nested.
            startinfo.WorkingDirectory = _tempDir;
            startinfo.RedirectStandardError = false;
            startinfo.RedirectStandardOutput = true;
            startinfo.RedirectStandardInput = true;
            startinfo.UseShellExecute = false;
            startinfo.CreateNoWindow = true;
            startinfo.EnvironmentVariables["GFORTRAN_UNBUFFERED_ALL"] = "y";
            startinfo.EnvironmentVariables["GFORTRAN_UNBUFFERED_PRECONNECTED"] = "y";
            p.StartInfo = startinfo;
            p.EnableRaisingEvents = true;

            try
            {
                p.Start();
                p.StandardInput.AutoFlush = true;
            }
            catch (Exception ex)
            {
                LogLine("*** Failed to start xfoil.exe: " + ex.Message);
                throw;
            }

            txtLog.Clear();
            LogLine($"*** Started xfoil.exe (pid {p.Id}), working dir: {_tempDir}");

            var startedProcess = p;
            bt = new Thread(() => ReadThread(startedProcess));
            bt.IsBackground = true;
            bt.Start();

            lblStatus.Text = "Status: XFOIL running";
        }

        /// <summary>
    /// Starts (or restarts) the dedicated xfoil.exe process if needed, downloading it
    /// first via frmMain's shared downloader if it isn't present in appdata\.
    /// </summary>
        private async Task<bool> EnsureXfoilReadyAsync()
        {
            string appPath = Path.Combine(Application.StartupPath, "appdata", "xfoil.exe");
            if (!File.Exists(appPath))
            {
                bool downloaded = await My.MyProject.Forms.frmMain.DownloadXfoilAsync();
                if (!downloaded || !File.Exists(appPath))
                {
                    AppMessageBox.Show("XFOIL executable was not found and could not be downloaded.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            // Always start a fresh XFOIL process for each analysis run so every run starts cleanly
            // at the top-level XFOIL prompt rather than inheriting a leftover OPER/VPAR submenu state.
            try
            {
                StartProcess(appPath);
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Could not start xfoil.exe: " + ex.Message, "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (p is null || p.HasExited)
            {
                AppMessageBox.Show("XFOIL process exited immediately! Exit code: " + p.ExitCode, "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        /// <summary>
    /// Sends a command to XFOIL and echoes it into the log box (prefixed "&gt;&gt;&gt;",
    /// or "(blank)" for an empty line - blank lines are meaningful XFOIL input, they pop
    /// back up a menu level) so the log shows the full conversation, not just XFOIL's
    /// side of it. Makes it possible to tell exactly which command a stalled run is stuck
    /// after: the last ">>>" line with no XFOIL response beneath it.
    /// </summary>
        private void WriteCmd(string text)
        {
            LogLine(">>> " + (string.IsNullOrEmpty(text) ? "(blank)" : text));
            try
            {
                p.StandardInput.WriteLine(text);
            }
            catch (Exception ex)
            {
                // If the pipe is already broken (process died, handle closed, etc.) this would
                // otherwise fail completely silently from the caller's point of view - the run
                // just sits there forever waiting for a response that was never actually sent.
                // Deliberately not re-thrown: the caller's subsequent WaitForFileAsync will just
                // time out and show its own "check the log" message, which will now actually
                // explain why, instead of risking an unhandled-exception crash on the UI thread.
                LogLine("*** Failed to send command: " + ex.Message);
            }
        }

        private void FlushCmd()
        {
            try
            {
                p.StandardInput.Flush();
            }
            catch (Exception ex)
            {
                LogLine("*** Failed to flush input: " + ex.Message);
            }
        }

        /// <summary>
    /// Closes XFOIL's stdin, which forces the Fortran runtime to hit end-of-file on its
    /// next read and terminate - regardless of which menu level ("OPER", "OPERv", etc.)
    /// it's currently sitting at. Confirmed against the real xfoil.exe: a literal "quit"
    /// typed while inside the OPER menu is rejected ("QUIT command not recognized"),
    /// which left the process hung waiting at that prompt forever in real interactive use
    /// (the earlier "quit"-based approach only ever appeared to work because each
    /// WaitForXFileAsync falls back to a plain File.Exists check once its timeout expires -
    /// every run was silently burning its full timeout). Closing the pipe is exit-path-
    /// agnostic and actually lets the completion checks that look at p.HasExited fire
    /// promptly instead of always waiting out the clock.
    /// </summary>
        private void CloseXfoilInput()
        {
            try
            {
                p.StandardInput.Close();
            }
            catch
            {
            }
        }

        /// <summary>
    /// Polls a file XFOIL is writing until it parses to non-empty data (optionally also
    /// requiring the process to have exited, for commands issued as the last step of a
    /// run), or times out.
    /// </summary>
        private async Task<bool> WaitForFileAsync(string path, int timeoutMs, Func<string, bool> hasData, bool requireProcessExit = true)
        {
            long start = Environment.TickCount64;
            do
            {
                await Task.Delay(150);
                if (File.Exists(path))
                {
                    bool ok = false;
                    try
                    {
                        ok = hasData(path);
                    }
                    catch
                    {
                    }
                    if (ok && (!requireProcessExit || p is null || p.HasExited))
                        return true;
                }
            }
            while (Environment.TickCount64 - start < timeoutMs);

            if (File.Exists(path))
            {
                try
                {
                    return hasData(path);
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        #endregion

        #region UI fields

        private TextBox txtAirfoil;
        private Button btnBrowseAirfoil;
        private Button btnLoadAirfoil;
        private ComboBox cmbTheme;
        private TextBox txtRe;
        private TextBox txtMach;
        private TextBox txtNcrit;
        private TextBox txtAlphaMin;
        private TextBox txtAlphaMax;
        private TextBox txtAlphaStep;
        private TextBox txtSingleAlpha;
        private Button btnRunPolar;
        private Button btnRunPoint;
        private Label lblStatus;
        private TextBox txtLog;
        private TabControl tc;
        private TabPage tabPolar;
        private TabPage tabCp;
        private TabPage tabBl;
        private TabPage tabGeom;
        private PictureBox pPolar;
        private PictureBox pCp;
        private PictureBox pBl;
        private PictureBox pGeom;
        private CheckedListBox lstPolarRuns;
        private Button btnClearRuns;
        private ComboBox cmbBlQuantity;
        private Button btnExplainBl;

        #endregion

        #region Plot zoom/pan

        /// <summary>
    /// Per-view zoom/pan state, in "native" (unzoomed, Scale=1) pixel coordinates:
    /// PanX/PanY is the native-space point shown at the PictureBox's top-left corner.
    /// Rather than stretching a fixed-resolution bitmap (which blurs when zoomed in),
    /// every zoom/pan change re-renders the plot with this transform applied directly to
    /// the drawing Graphics - so lines and text are always drawn crisp at the exact
    /// effective resolution, the same way e.g. a PDF viewer stays sharp at any zoom.
    /// </summary>
        private class PlotZoomState
        {
            public double Scale = 1.0d;
            public double PanX = 0.0d;
            public double PanY = 0.0d;
        }

        private readonly ToolTip _plotTip = new ToolTip();
        // Keyed by PictureBox rather than stored as named fields (pPolar/pCp/pBl/pGeom) so the
        // shared Paint/mouse/render-dispatch handlers below can stay generic across all four
        // plot views.
        private readonly Dictionary<PictureBox, Bitmap> _plotBitmaps = new Dictionary<PictureBox, Bitmap>();
        private readonly Dictionary<PictureBox, PlotZoomState> _plotZooms = new Dictionary<PictureBox, PlotZoomState>();
        private readonly Dictionary<PictureBox, Action> _plotRenderers = new Dictionary<PictureBox, Action>();
        private PictureBox _dragPb = null;
        private Point _dragStart;
        private PointF _dragPanStart;

        /// <summary>
    /// Replaces a view's rendered bitmap and repaints it. The bitmap is always exactly
    /// PictureBox-sized (never scaled up/down for zoom - see PlotZoomState) so painting
    /// it is a plain 1:1 blit, never a stretch.
    /// </summary>
        private void SetPlotBitmap(PictureBox pb, Bitmap bmp)
        {
            Bitmap old = null;
            _plotBitmaps.TryGetValue(pb, out old);
            _plotBitmaps[pb] = bmp;
            old?.Dispose();
            pb.Invalidate();
        }

        /// <summary>Re-renders whichever plot owns this PictureBox, picking up its current
    /// zoom/pan state - the only way zoom/pan changes actually become visible.</summary>
        private void RedrawPlot(PictureBox pb)
        {
            Action renderer = null;
            if (_plotRenderers.TryGetValue(pb, out renderer))
                renderer();
        }

        // ---- Detached AeroPlot windows driven by the native XFOIL console (CPX / VPLO) ----------
        private frmPlotWindow _cpWindow;
        private frmPlotWindow _blWindow;

        /// <summary>Native-XFOIL console "CPX": show the Cp-vs-x plot from what the session
        /// computed (OPER ALFA), in the shared AeroPlot window.</summary>
        public void ShowConsoleXfoilCpPlot(
            System.Collections.Generic.IReadOnlyList<(double x, double cp, double? cpi)> cp,
            System.Collections.Generic.IReadOnlyList<(double x, double y)> airfoil,
            System.Collections.Generic.IReadOnlyList<Xfoil.Core.Solver.Bl.BlStationPoint> bl,
            (double alpha, double cl, double cm, double cd, double re, double ncrit, double xtrTop, double xtrBot, double mach)? info)
        {
            if (cp is null) return;
            _cpPoints = new List<XfoilCpPoint>();
            foreach (var c in cp)
                _cpPoints.Add(new XfoilCpPoint() { X = c.x, Cp = c.cp, CpInv = c.cpi });

            // Airfoil shape band under the Cp curve (xfoil.exe's CPX shows the section here).
            _airfoilCoords = new List<XfoilGeomPoint>();
            if (airfoil is not null)
                foreach (var a in airfoil)
                    _airfoilCoords.Add(new XfoilGeomPoint() { X = a.x, Y = a.y });

            // Boundary-layer / wake displacement overlay on that band (viscous only), split
            // top/bottom by y-sign exactly like RunPointNative so the dashed BL edge draws.
            _blTop.Clear();
            _blBottom.Clear();
            if (bl is not null)
            {
                foreach (var b in bl)
                {
                    var p = new XfoilBLPoint() { X = b.X, Y = b.Y, Ue = b.Ue, Dstar = b.Dstar, Theta = b.Theta, Cf = b.Cf, H = b.H, N = b.N, Ctau = b.Ctau, Cd = b.Cd };
                    if (b.Y >= 0d) _blTop.Add(p); else _blBottom.Add(p);
                }
            }

            ApplyXfoilPointInfo(info);
            RenderCpPlot();
            OpenCpWindow();
        }

        /// <summary>Native-XFOIL console VPLO menu: show the chosen boundary-layer variable
        /// (quantityIndex, matching cmbBlQuantity) from what the session computed.</summary>
        public void ShowConsoleXfoilBlPlot(
            System.Collections.Generic.IReadOnlyList<Xfoil.Core.Solver.Bl.BlStationPoint> bl,
            (double alpha, double cl, double cm, double cd, double re, double ncrit, double xtrTop, double xtrBot, double mach)? info,
            int quantityIndex)
        {
            if (bl is null) return;
            _blTop.Clear();
            _blBottom.Clear();
            foreach (var b in bl)
            {
                var p = new XfoilBLPoint() { X = b.X, Y = b.Y, Ue = b.Ue, Dstar = b.Dstar, Theta = b.Theta, Cf = b.Cf, H = b.H, N = b.N, Ctau = b.Ctau, Cd = b.Cd };
                if (b.Y >= 0d) _blTop.Add(p); else _blBottom.Add(p);
            }
            ApplyXfoilPointInfo(info);

            // Select the BL variable the user picked in the VPLO menu (the docked combo drives
            // BuildBlBitmap's quantity, and setting it also keeps the docked tab in sync).
            if (cmbBlQuantity is not null && quantityIndex >= 0 && quantityIndex < cmbBlQuantity.Items.Count)
                cmbBlQuantity.SelectedIndex = quantityIndex;

            RenderBlPlot();
            OpenBlWindow();
        }

        private void ApplyXfoilPointInfo((double alpha, double cl, double cm, double cd, double re, double ncrit, double xtrTop, double xtrBot, double mach)? info)
        {
            if (info is null) return;
            var i = info.Value;
            bool visc = i.re > 0d;
            _lastPointAlpha = i.alpha;
            _lastPointMach = i.mach;
            _lastPointCL = i.cl;
            _lastPointCM = i.cm;
            _lastPointCD = i.cd;
            _lastPointRe = visc ? i.re : (double?)null;
            _lastPointNcrit = visc ? i.ncrit : (double?)null;
            _lastTopXtr = visc ? i.xtrTop : (double?)null;
            _lastBotXtr = visc ? i.xtrBot : (double?)null;
        }

        private Bitmap BuildCpBitmap(int w, int h, AeroPlot.PlotView view, bool captureVectors, out string svg, out string pdf)
        {
            var bmp = BuildXfoilPlotBitmap(w, h, view, () => RenderCpPlot(captureVectors));
            svg = _cpSvg; pdf = _cpPdf;
            return bmp;
        }

        private Bitmap BuildBlBitmap(int w, int h, AeroPlot.PlotView view, bool captureVectors, out string svg, out string pdf)
        {
            var bmp = BuildXfoilPlotBitmap(w, h, view, () => RenderBlPlot(captureVectors));
            svg = _blSvg; pdf = _blPdf;
            return bmp;
        }

        // Renders one plot at an arbitrary size into a fresh Bitmap via the size-override + grab
        // seam, without disturbing the docked PictureBox. `view` is the pop-out window's crisp
        // zoom/pan, applied (in grab mode) by ApplyPlotZoomTransform so vectors re-render sharp.
        private Bitmap BuildXfoilPlotBitmap(int w, int h, AeroPlot.PlotView view, Action render)
        {
            _ovrPlotW = Math.Max(1, w);
            _ovrPlotH = Math.Max(1, h);
            _grabPlotMode = true;
            _grabbedPlot = null;
            _xfoilPlotView = view;
            try { render(); }
            finally { _ovrPlotW = 0; _ovrPlotH = 0; _grabPlotMode = false; _xfoilPlotView = AeroPlot.PlotView.Identity; }
            return _grabbedPlot ?? new Bitmap(Math.Max(1, w), Math.Max(1, h));
        }

        public void OpenCpWindow()
        {
            if (_cpWindow is not null && !_cpWindow.IsDisposed) { _cpWindow.Focus(); _cpWindow.RequestRender(); return; }
            _cpWindow = new frmPlotWindow(new DelegatePlotSource("XFOIL Cp Distribution",
                (w, h, view) => BuildCpBitmap(w, h, view, false, out _, out _), ExportCp,
                setTheme: SetPopoutTheme, initialDark: _isDarkTheme, setFontScale: SetPopoutFontScale)) { Icon = this.Icon };
            _cpWindow.FormClosed += (_, __) => _cpWindow = null;
            _cpWindow.Show(this);
        }

        public void OpenBlWindow()
        {
            if (_blWindow is not null && !_blWindow.IsDisposed) { _blWindow.Focus(); _blWindow.RequestRender(); return; }
            _blWindow = new frmPlotWindow(new DelegatePlotSource("XFOIL Boundary Layer",
                (w, h, view) => BuildBlBitmap(w, h, view, false, out _, out _), ExportBl,
                setTheme: SetPopoutTheme, initialDark: _isDarkTheme, setFontScale: SetPopoutFontScale)) { Icon = this.Icon };
            _blWindow.FormClosed += (_, __) => _blWindow = null;
            _blWindow.Show(this);
        }

        private void ExportCp(string format, int width, int height)
            => XfoilPlotExport(format, width, height, "XFOIL_Cp", (w, h, cap) => { var b = BuildCpBitmap(w, h, AeroPlot.PlotView.Identity, cap, out var svg, out var pdf); return (b, svg, pdf); });

        private void ExportBl(string format, int width, int height)
            => XfoilPlotExport(format, width, height, "XFOIL_BoundaryLayer", (w, h, cap) => { var b = BuildBlBitmap(w, h, AeroPlot.PlotView.Identity, cap, out var svg, out var pdf); return (b, svg, pdf); });

        // ---- "Explain trends": educational read-out of the BL solution --------------------------

        /// <summary>Reads the last viscous BL solution (_blTop/_blBottom plus the run parameters)
        /// and pops up a plain-language explanation of WHY the boundary layer behaves the way the
        /// plots show it - laminar run, transition, separation/bubbles, and how that drives drag.
        /// Purely descriptive: everything is derived from the data already plotted on this tab.</summary>
        private void ShowBlExplanation()
        {
            bool haveData = (_blTop != null && _blTop.Count > 0) || (_blBottom != null && _blBottom.Count > 0);
            if (!haveData)
            {
                AppMessageBox.Show(
                    "No boundary-layer data yet.\n\nRun a VISCOUS point analysis first (set a Reynolds number and a single alpha, then \"Run Point Analysis\"), or use OPER / VISC / ALFA in the console. The explanation is built from that solution - an inviscid run has no boundary layer to describe.",
                    "Explain BL Trends", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ShowTextPopup("Boundary-Layer Trends – what the flow is doing and why", BuildBlExplanation());
        }

        private string BuildBlExplanation()
        {
            var sb = new StringBuilder();
            double? re = _lastPointRe, ncrit = _lastPointNcrit, alpha = _lastPointAlpha;
            double? cl = _lastPointCL, cd = _lastPointCD;

            sb.AppendLine("HOW TO READ THIS");
            sb.AppendLine("An automatic, plain-language reading of the boundary-layer (BL) solution");
            sb.AppendLine("behind the plots on this tab. It follows the edge velocity (Ue), skin");
            sb.AppendLine("friction (Cf), shape factor (H) and momentum thickness (theta) along each");
            sb.AppendLine("surface and describes what the flow is doing, and why.");
            sb.AppendLine();

            sb.AppendLine("RUN CONDITIONS");
            sb.AppendLine("  Reynolds number Re = " + (re.HasValue && re.Value > 0 ? Sci(re.Value) : "(inviscid - no BL)"));
            if (alpha.HasValue) sb.AppendLine("  Angle of attack    = " + F2(alpha.Value) + " deg");
            if (ncrit.HasValue) sb.AppendLine("  Ncrit              = " + F2(ncrit.Value) + "  (e^N transition threshold)");
            if (cl.HasValue) sb.AppendLine("  CL                 = " + cl.Value.ToString("0.####", CultureInfo.InvariantCulture));
            if (cd.HasValue) sb.AppendLine("  CD                 = " + cd.Value.ToString("0.#####", CultureInfo.InvariantCulture));
            if (cl.HasValue && cd.HasValue && cd.Value != 0.0) sb.AppendLine("  L/D                = " + (cl.Value / cd.Value).ToString("0.#", CultureInfo.InvariantCulture));
            sb.AppendLine();

            ExplainSurface(sb, "UPPER (SUCTION) SURFACE", PrepSurface(_blTop), _lastTopXtr, re);
            ExplainSurface(sb, "LOWER (PRESSURE) SURFACE", PrepSurface(_blBottom), _lastBotXtr, re);
            AppendGlobalNotes(sb, re, ncrit);

            sb.AppendLine("GLOSSARY");
            sb.AppendLine("  Ue     edge velocity. Rising Ue = accelerating (favourable) flow; falling");
            sb.AppendLine("         Ue = decelerating (adverse) flow, which thickens the BL.");
            sb.AppendLine("  Cf     skin-friction coefficient. Cf>0 attached; Cf=0 is the point of");
            sb.AppendLine("         separation; Cf<0 means reversed (separated) flow.");
            sb.AppendLine("  H      shape factor = dstar/theta. ~2.6 laminar, ~1.4-1.9 turbulent; H");
            sb.AppendLine("         rises sharply as the BL approaches separation.");
            sb.AppendLine("  theta  momentum thickness. Its value at the trailing edge sets profile drag.");
            return sb.ToString();
        }

        // Airfoil-surface BL stations only (drop the wake, x/c > 1), ordered LE -> TE by x.
        private static List<XfoilBLPoint> PrepSurface(List<XfoilBLPoint> pts)
        {
            var list = new List<XfoilBLPoint>();
            if (pts != null)
                foreach (var p in pts)
                    if (p.X <= 1.001) list.Add(p);
            list.Sort((a, b) => a.X.CompareTo(b.X));
            return list;
        }

        private void ExplainSurface(StringBuilder sb, string title, List<XfoilBLPoint> s, double? xtr, double? re)
        {
            sb.AppendLine("=== " + title + " ===");
            if (s.Count < 3)
            {
                sb.AppendLine("  (not enough BL data on this surface to analyse)");
                sb.AppendLine();
                return;
            }

            // 1) pressure gradient, read from where Ue peaks (the suction peak).
            int iPeak = 0;
            for (int i = 1; i < s.Count; i++) if (s[i].Ue > s[iPeak].Ue) iPeak = i;
            sb.AppendLine("  Pressure gradient:");
            sb.AppendLine("    Ue peaks at x/c = " + F2(s[iPeak].X) + " (the suction peak). Ahead of it the flow");
            sb.AppendLine("    accelerates - a favourable gradient with a thin, stable BL. Behind it the");
            sb.AppendLine("    flow decelerates into an adverse gradient that thickens the BL and, if");
            sb.AppendLine("    strong enough, drives transition and then separation.");

            // 2) transition.
            sb.AppendLine("  Transition (laminar -> turbulent):");
            if (xtr.HasValue && xtr.Value < 0.999 && re.HasValue && re.Value > 0.0)
            {
                double xt = xtr.Value;
                double reTheta = re.Value * InterpField(s, xt, p => p.Ue) * InterpField(s, xt, p => p.Theta);
                if (xt <= 0.03)
                    sb.AppendLine("    Transition sits right at the leading edge (x/c = " + F3(xt) + "): the BL is");
                else
                    sb.AppendLine("    The BL trips from laminar to turbulent at about x/c = " + F3(xt) + ". Upstream it is");
                sb.AppendLine("    laminar (low Cf, H ~ 2.6); downstream it is turbulent (higher Cf, fuller");
                sb.AppendLine("    profile, H ~ 1.5). Re_theta at transition is about " + F0(reTheta) + " - the");
                sb.AppendLine("    disturbances have amplified ~e^Ncrit and broken down.");
            }
            else
            {
                sb.AppendLine("    No on-surface transition was reached - the BL stays laminar essentially to");
                sb.AppendLine("    the trailing edge. At low Re the flow often separates while still laminar");
                sb.AppendLine("    before it can transition on the surface.");
            }

            // 3) separation / reattachment, from the sign of Cf.
            sb.AppendLine("  Separation / reattachment:");
            int iSep = -1;
            for (int i = 0; i < s.Count; i++) if (s[i].Cf <= 0.0) { iSep = i; break; }
            if (iSep < 0)
            {
                sb.AppendLine("    Cf stays positive everywhere - the flow remains ATTACHED all the way to");
                sb.AppendLine("    the trailing edge. Best case for drag and for holding lift.");
            }
            else
            {
                double xSep = s[iSep].X;
                int iReat = -1;
                for (int i = iSep + 1; i < s.Count; i++) if (s[i].Cf > 0.0) { iReat = i; break; }
                bool laminarSep = !xtr.HasValue || xtr.Value >= 0.999 || xSep < xtr.Value;
                if (iReat >= 0 && s[iReat].X < 0.999)
                {
                    double xReat = s[iReat].X;
                    sb.AppendLine("    Cf goes negative at x/c = " + F3(xSep) + " then positive again at x/c = " + F3(xReat) + ":");
                    sb.AppendLine("    a LAMINAR SEPARATION BUBBLE. The laminar BL cannot climb the adverse");
                    sb.AppendLine("    gradient and lifts off; the free shear layer transitions to turbulent,");
                    sb.AppendLine("    re-energises, and reattaches. Bubble length ~ " + F3(xReat - xSep) + " c. These bubbles");
                    sb.AppendLine("    are the hallmark of low-Re airfoils and add pressure (form) drag.");
                }
                else if (laminarSep)
                {
                    sb.AppendLine("    The laminar BL separates at x/c = " + F3(xSep) + " and does NOT reattach (open");
                    sb.AppendLine("    laminar separation). Typical at low Re / high loading - it collapses lift");
                    sb.AppendLine("    and adds a lot of pressure drag.");
                }
                else
                {
                    sb.AppendLine("    The turbulent BL separates at x/c = " + F3(xSep) + " and stays separated to the");
                    sb.AppendLine("    trailing edge (trailing-edge stall). The adverse gradient finally");
                    sb.AppendLine("    overwhelms even the turbulent BL's extra near-wall momentum.");
                }
            }

            // 4) shape factor trend.
            int iHmax = 0;
            for (int i = 1; i < s.Count; i++) if (s[i].H > s[iHmax].H) iHmax = i;
            sb.AppendLine("  Shape factor H:");
            sb.AppendLine("    H peaks at " + F2(s[iHmax].H) + " near x/c = " + F2(s[iHmax].X) + ". Rising H means a less-full,");
            sb.AppendLine("    more inflected velocity profile - the BL being pushed toward separation.");
            sb.AppendLine("    H past ~3.5 (laminar) or ~2.5-3 (turbulent) is the run-up to letting go.");

            // 5) momentum thickness growth -> drag.
            sb.AppendLine("  Momentum thickness theta:");
            sb.AppendLine("    theta grows from " + Sci(s[0].Theta) + " near the LE to " + Sci(s[s.Count - 1].Theta) + " at the TE.");
            sb.AppendLine("    It only ever grows along a surface and jumps fastest across transition and");
            sb.AppendLine("    through any separated region. The TE value on each surface is what sets");
            sb.AppendLine("    this airfoil's profile drag.");
            sb.AppendLine();
        }

        private void AppendGlobalNotes(StringBuilder sb, double? re, double? ncrit)
        {
            sb.AppendLine("=== WHY, IN ONE PICTURE ===");
            if (re.HasValue && re.Value > 0.0 && re.Value < 1.0e5)
            {
                sb.AppendLine("  This is a LOW-REYNOLDS-NUMBER case (Re < 1e5). Viscosity dominates: the");
                sb.AppendLine("  laminar BL is thick and weak, so it tends to separate before it can");
                sb.AppendLine("  transition, forming laminar separation bubbles (or open separation). That");
                sb.AppendLine("  is why the plots look 'busy' near the trailing edge, H runs high, and drag");
                sb.AppendLine("  is large / L/D poor versus the same airfoil at high Re. Small changes in");
                sb.AppendLine("  Re, Ncrit or angle move the bubble a lot - the flow is delicate.");
            }
            else if (re.HasValue && re.Value > 0.0)
            {
                sb.AppendLine("  At this Reynolds number the BL carries enough momentum to transition on");
                sb.AppendLine("  the surface and usually stay attached over most of the chord, so drag is");
                sb.AppendLine("  low and L/D high - until the adverse gradient finally separates the flow");
                sb.AppendLine("  near the trailing edge at higher angles.");
            }
            if (ncrit.HasValue)
            {
                sb.AppendLine("  Ncrit = " + F2(ncrit.Value) + ": lowering it (noisier air / rougher surface) moves");
                sb.AppendLine("  transition forward and can suppress bubbles; raising it (clean tunnel)");
                sb.AppendLine("  delays transition and can enlarge them. It is the single biggest 'knob'");
                sb.AppendLine("  on where transition sits.");
            }
            sb.AppendLine();
        }

        private static string F0(double v) => v.ToString("0", CultureInfo.InvariantCulture);
        private static string F2(double v) => v.ToString("0.00", CultureInfo.InvariantCulture);
        private static string F3(double v) => v.ToString("0.000", CultureInfo.InvariantCulture);
        private static string Sci(double v) => v.ToString("0.###E+0", CultureInfo.InvariantCulture);

        // Linear interpolation of a BL field at an arbitrary x/c on an LE->TE-ordered surface.
        private static double InterpField(List<XfoilBLPoint> s, double x, Func<XfoilBLPoint, double> f)
        {
            if (s.Count == 0) return 0.0;
            if (x <= s[0].X) return f(s[0]);
            if (x >= s[s.Count - 1].X) return f(s[s.Count - 1]);
            for (int i = 1; i < s.Count; i++)
                if (s[i].X >= x)
                {
                    double t = (x - s[i - 1].X) / (s[i].X - s[i - 1].X + 1e-30);
                    return f(s[i - 1]) + t * (f(s[i]) - f(s[i - 1]));
                }
            return f(s[s.Count - 1]);
        }

        // Simple themed, scrollable, read-only text dialog (with Copy) for the explanation.
        private void ShowTextPopup(string title, string body)
        {
            var dlg = new Form()
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(760, 640),
                MinimumSize = new Size(480, 360),
                ShowInTaskbar = false,
                Icon = this.Icon,
                BackColor = ThemeBackColor
            };

            var box = new RichTextBox()
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 10f),
                BackColor = ThemeBackColor,
                ForeColor = ThemeForeColor,
                WordWrap = true,
                DetectUrls = false,
                Text = body
            };
            box.Select(0, 0);

            var bottom = new Panel() { Dock = DockStyle.Bottom, Height = 44, BackColor = ThemeBackColor };
            var btnCopy = new Button() { Text = "Copy", Width = 90, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Color.Black, Cursor = Cursors.Hand };
            var btnClose = new Button() { Text = "Close", Width = 90, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Color.Black, Cursor = Cursors.Hand };
            btnCopy.FlatAppearance.BorderColor = Color.LightGray;
            btnClose.FlatAppearance.BorderColor = Color.LightGray;
            btnCopy.Click += (s, e) => { try { Clipboard.SetText(body); } catch { } };
            btnClose.Click += (s, e) => dlg.Close();
            void layoutButtons()
            {
                btnClose.Location = new Point(bottom.ClientSize.Width - btnClose.Width - 12, 8);
                btnCopy.Location = new Point(btnClose.Left - btnCopy.Width - 8, 8);
            }
            bottom.Resize += (s, e) => layoutButtons();
            bottom.Controls.Add(btnCopy);
            bottom.Controls.Add(btnClose);

            dlg.Controls.Add(box);      // Fill added first so the Bottom panel (added next) reserves its strip.
            dlg.Controls.Add(bottom);
            dlg.AcceptButton = btnClose;
            layoutButtons();
            dlg.ShowDialog(this);
        }

        // PNG/SVG/PDF save for the detached XFOIL plot windows (PDF via frmGeometry.WriteVectorPdf,
        // the same writer the docked XFOIL export uses).
        private void XfoilPlotExport(string format, int width, int height, string baseName,
            Func<int, int, bool, (Bitmap bmp, string svg, string pdf)> render)
        {
            Bitmap bmp = null;
            try
            {
                var (b, svg, pdf) = render(width, height, true);
                bmp = b;
                if (format != "PNG" && string.IsNullOrEmpty(svg))
                {
                    AppMessageBox.Show("The view has not finished rendering. Please try again in a moment.",
                        "Export View", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                using var sfd = new SaveFileDialog { Title = $"Export {baseName} as {format}", FileName = baseName };
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
                            frmGeometry.WriteVectorPdf(pdf, width, height, sfd.FileName);
                            break;
                    }
                    AppToast.ShowExported($"{format} exported to " + Path.GetFileName(sfd.FileName), sfd.FileName);
                }
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Error exporting file: " + ex.Message, "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                bmp?.Dispose();
            }
        }

        /// <summary>
    /// Applies the current zoom/pan as a direct transform on the real drawing surface
    /// (g.g - the SvgGraphics wrapper's underlying System.Drawing.Graphics), so every
    /// subsequent DrawLine/DrawString/etc. in the caller draws already at the correct
    /// zoomed screen position and resolution - true vector re-rendering, not a bitmap
    /// stretch, which is what keeps lines and text crisp at any zoom level. No-op for
    /// PNG/SVG/PDF export (captureVectors = True), which always renders the full
    /// un-zoomed view regardless of the on-screen zoom state.
    /// </summary>
        private void ApplyPlotZoomTransform(SvgGraphics g, PictureBox pb, bool captureVectors)
        {
            if (captureVectors)
                return;
            // Pop-out (grab) render: apply the detached window's crisp zoom/pan and skip the
            // docked interactive zoom entirely (the docked PlotZoomState isn't in play here).
            if (_grabPlotMode)
            {
                _xfoilPlotView.ApplyTo(g.g);
                return;
            }
            PlotZoomState zoom = null;
            if (!_plotZooms.TryGetValue(pb, out zoom))
                return;
            if (zoom.Scale == 1.0d && zoom.PanX == 0.0d && zoom.PanY == 0.0d)
                return;
            g.g.Transform = new Matrix((float)zoom.Scale, 0f, 0f, (float)zoom.Scale, (float)(-zoom.PanX * zoom.Scale), (float)(-zoom.PanY * zoom.Scale));
        }

        /// <summary>
    /// Clamps pan so the visible native-space window can never extend past the plot's own
    /// [0, native size] extent - at Scale = 1 the window exactly equals that extent, so
    /// this always pins pan to (0,0) until the user has zoomed in.
    /// </summary>
        private void ClampPan(PictureBox pb, PlotZoomState zoom)
        {
            double viewW = pb.Width / zoom.Scale;
            double viewH = pb.Height / zoom.Scale;
            zoom.PanX = viewW >= pb.Width ? 0.0d : Math.Max(0.0d, Math.Min(zoom.PanX, pb.Width - viewW));
            zoom.PanY = viewH >= pb.Height ? 0.0d : Math.Max(0.0d, Math.Min(zoom.PanY, pb.Height - viewH));
        }

        private void PlotPictureBox_Paint(object sender, PaintEventArgs e)
        {
            PictureBox pb = (PictureBox)sender;
            Bitmap bmp = null;
            if (!_plotBitmaps.TryGetValue(pb, out bmp) || bmp is null)
                return;
            e.Graphics.DrawImageUnscaled(bmp, 0, 0);
        }

        /// <summary>Mouse-wheel zoom, keeping the native-space point under the cursor fixed
    /// on screen, then immediately re-renders at the new effective resolution.</summary>
        private void PlotPictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            PictureBox pb = (PictureBox)sender;
            PlotZoomState zoom = null;
            if (!_plotZooms.TryGetValue(pb, out zoom))
                return;
            if (pb.Width <= 0 || pb.Height <= 0)
                return;

            double oldScale = zoom.Scale;
            double factor = e.Delta > 0 ? 1.15d : 1.0d / 1.15d;
            double newScale = Math.Max(1.0d, Math.Min(20.0d, oldScale * factor));
            if (newScale == oldScale)
                return;

            double nativeX = zoom.PanX + e.X / oldScale;
            double nativeY = zoom.PanY + e.Y / oldScale;
            zoom.Scale = newScale;
            zoom.PanX = nativeX - e.X / newScale;
            zoom.PanY = nativeY - e.Y / newScale;
            ClampPan(pb, zoom);
            RedrawPlot(pb);
        }

        private void PlotPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            PictureBox pb = (PictureBox)sender;
            PlotZoomState zoom = null;
            if (!_plotZooms.TryGetValue(pb, out zoom))
                return;
            _dragPb = pb;
            _dragStart = e.Location;
            _dragPanStart = new PointF((float)zoom.PanX, (float)zoom.PanY);
            pb.Cursor = Cursors.SizeAll;
        }

        private void PlotPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            PictureBox pb = (PictureBox)sender;
            if (!ReferenceEquals(_dragPb, pb))
                return;
            var zoom = _plotZooms[pb];
            zoom.PanX = (double)_dragPanStart.X - (e.X - _dragStart.X) / zoom.Scale;
            zoom.PanY = (double)_dragPanStart.Y - (e.Y - _dragStart.Y) / zoom.Scale;
            ClampPan(pb, zoom);
            RedrawPlot(pb);
        }

        private void PlotPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            PictureBox pb = (PictureBox)sender;
            if (ReferenceEquals(_dragPb, pb))
            {
                _dragPb = null;
                pb.Cursor = Cursors.Default;
            }
        }

        private void PlotPictureBox_DoubleClick(object sender, EventArgs e)
        {
            ResetPlotZoom((PictureBox)sender);
        }

        private void ResetPlotZoom(PictureBox pb)
        {
            PlotZoomState zoom = null;
            if (!_plotZooms.TryGetValue(pb, out zoom))
                return;
            zoom.Scale = 1.0d;
            zoom.PanX = 0.0d;
            zoom.PanY = 0.0d;
            RedrawPlot(pb);
        }

        #endregion

        #region Data

        private static readonly Color[] _runColorPalette = new[] { Color.DodgerBlue, Color.Crimson, Color.ForestGreen, Color.DarkOrange, Color.Purple, Color.Teal, Color.SaddleBrown, Color.DeepPink };

        // Boundary-layer quantities available on the BL tab, shown one at a time - matching
        // XFOIL's own VPLO submenu names/order as closely as the data this app actually has
        // access to allows. VPLO also offers CD (dissipation coefficient), N (amplification
        // ratio), and CT (max shear coefficient), but those aren't in the "dump" file XFOIL
        // writes (confirmed against a real run: dump is fixed at s/x/y/Ue/Dstar/Theta/Cf/H,
        // and unlike the interactive on-screen VPLO plots, XFOIL has no command to write
        // those three to a file this app can parse) - RT/RTL are still derivable, though,
        // since Re_theta = Re_chord * (Ue/Vinf) * (Theta/c), all of which ARE in the dump.
        // 
        // Indices 0/1/2/5/6 are single-quantity "one line per surface" plots, rendered via
        // BlQuantityValue + the normal top/bottom DrawBlSeries path. Indices 3/4 (DT/DB) are
        // XFOIL's own combined Dstar+Theta-vs-x plot for ONE surface only (not one quantity
        // across both surfaces) - RenderBlPlot special-cases those two instead of going
        // through BlQuantityValue. Keep BlQuantityDualIndex in sync with their position here.
        // Indices 7-9 (N/CT/CD) mirror XFOIL's VPLO amplification / shear-coefficient /
        // dissipation plots. They only carry data from the native engine (the external
        // xfoil.exe dump file omits them) - see XfoilBLPoint.
        private static readonly string[] _blQuantityNames = new[] { "H (shape parameter)", "UE (edge velocity)", "CF (skin friction)", "DT (top: Dstar & Theta)", "DB (bottom: Dstar & Theta)", "RT (Re_theta)", "RTL (log Re_theta)", "N (amplification)", "CT (max shear coeff)", "CD (dissipation)" };
        private const int BlQuantityIndexDT = 3;
        private const int BlQuantityIndexDB = 4;
        // Axis label for each single-quantity entry, as (main character, superscript/
        // subscript) - matches XFOIL's own naming (e.g. the shape parameter is "H" with
        // subscript "k", read "Hk"). Unused (blank) for the DT/DB dual-quantity entries,
        // which draw their own small two-color legend instead - see RenderBlPlot.
        private static readonly string[] _blQuantityAxisMain = new[] { "H", "U", "C", "", "", "Re", "logRe", "n", "C", "C" };
        private static readonly string[] _blQuantityAxisSub = new[] { "k", "e", "f", "", "", "θ", "θ", "", "τ", "D" };

        // Dark ("XFOIL", black background/white content, matches the real xfoil.exe's own
        // plot windows) is the default; Light swaps background/foreground only - the
        // upper/lower surface accent colors stay the same in both so they're never the same
        // color as whatever the background happens to be.
        private bool _isDarkTheme = true;
        // Theme chosen by a detached AeroPlot pop-out (Cp/BL). Applied ONLY while rendering in grab
        // mode, so a pop-out can be light while the docked tab stays dark (or vice-versa). Null =
        // follow the docked/app theme.
        private bool? _popoutDark;
        private bool EffectiveDark => (_grabPlotMode && _popoutDark.HasValue) ? _popoutDark.Value : _isDarkTheme;
        // Called by the pop-out window's Light/Dark toggle before each render.
        private void SetPopoutTheme(bool dark) => _popoutDark = dark;

        // Label font-size multiplier chosen by a pop-out's A-/A+ buttons (1 = default). Applied to
        // the SvgGraphics only while rendering in grab mode (a pop-out), so the docked tabs keep
        // their normal size.
        private double _popoutFontScale = 1.0;
        private void SetPopoutFontScale(double scale) => _popoutFontScale = scale;
        private float CurrentPlotFontScale => _grabPlotMode ? (float)_popoutFontScale : 1f;
        private Color ThemeBackColor
        {
            get
            {
                return EffectiveDark ? Color.Black : Color.White;
            }
        }
        private Color ThemeForeColor
        {
            get
            {
                return EffectiveDark ? Color.White : Color.Black;
            }
        }
        private Color ThemeGridColor
        {
            get
            {
                return EffectiveDark ? Color.FromArgb(90, 90, 90) : Color.LightGray;
            }
        }
        private Color ThemeUpperColor
        {
            get
            {
                return Color.OrangeRed;
            }
        }
        private Color ThemeLowerColor
        {
            get
            {
                return Color.DeepSkyBlue;
            }
        }

        private List<XfoilPolarRun> _polarRuns = new List<XfoilPolarRun>();
        private List<XfoilCpPoint> _cpPoints = new List<XfoilCpPoint>();
        private List<XfoilBLPoint> _blTop = new List<XfoilBLPoint>();
        private List<XfoilBLPoint> _blBottom = new List<XfoilBLPoint>();
        private List<XfoilGeomPoint> _airfoilCoords = new List<XfoilGeomPoint>();

        // Transition location for the single-point (Cp/BL) run, read from XFOIL's console
        // echo ("Side 1/2 free transition at x/c = ..."). Not available from the .pol polar
        // sweep dump itself for this per-point view (that data - Top_Xtr/Bot_Xtr - lives in
        // XfoilPolarPoint instead, one pair per alpha).
        private double? _lastTopXtr = default;
        private double? _lastBotXtr = default;

        // Converged CL/CM/CD for the single-point run, read from XFOIL's console echo
        // ("a =  5.000   CL =  0.5571" / "Cm =  0.0019  CD = 0.00848 => CDf = ... CDp = ..."),
        // for the parameter block on the Cp/BL plots (matches XFOIL's own CPX view). Re and
        // Ncrit aren't parsed from the log - they're just the run's own input parameters,
        // captured directly in btnRunPoint_Click.
        private double? _lastPointCL = default;
        private double? _lastPointCM = default;
        private double? _lastPointCD = default;
        private double? _lastPointAlpha = default;
        private double? _lastPointRe = default;
        private double? _lastPointNcrit = default;
        private double? _lastPointMach = default;

        private string _polarSvg = "";
        private string _polarPdf = "";
        private string _cpSvg = "";
        private string _cpPdf = "";
        private string _blSvg = "";
        private string _blPdf = "";

        // Render-size override + capture seam so the detached AeroPlot windows can render a plot at
        // their own size without touching the docked PictureBoxes (mirrors frmGeometry's approach).
        // Only one plot renders in grab mode at a time, so these are shared across Cp/BL.
        private int _ovrPlotW;
        private int _ovrPlotH;
        private bool _grabPlotMode;
        // The detached window's crisp zoom/pan, applied when rendering in grab mode (see
        // ApplyPlotZoomTransform). Identity for the docked tabs and for vector-capture exports.
        private AeroPlot.PlotView _xfoilPlotView = AeroPlot.PlotView.Identity;
        private Bitmap _grabbedPlot;
        private string _geomSvg = "";
        private string _geomPdf = "";

        #endregion

        public frmXfoilAnalysis()
        {
            _logFlushTimer = new System.Windows.Forms.Timer() { Interval = 75 };
            _polarResizeTimer = new System.Windows.Forms.Timer() { Interval = 120 };
            _cpResizeTimer = new System.Windows.Forms.Timer() { Interval = 120 };
            _blResizeTimer = new System.Windows.Forms.Timer() { Interval = 120 };
            _geomResizeTimer = new System.Windows.Forms.Timer() { Interval = 120 };
            Directory.CreateDirectory(_tempDir);
            Icon = My.MyProject.Forms.frmMain.Icon;
            InitializeUi();
            // _logFlushTimer is only declared/wired (New Timer With {...}, Handles ... .Tick) above -
            // a WinForms Timer does NOT start ticking just from being constructed, Enabled defaults
            // to False. Without this, _logBuffer accumulates every byte XFOIL ever prints (and any
            // Reader-thread/status lines appended to it) forever, but none of it ever reaches the
            // visible txtLog box - looks exactly like "XFOIL isn't saying anything" even when the
            // underlying process communication is working fine.
            _logFlushTimer.Start();
            FormClosing += frmXfoilAnalysis_FormClosing;
            // Keep the "active engine" in the title current: it follows frmMain's XFOIL engine
            // selection (native solvers vs external xfoil.exe), which can change while open.
            Activated += (s, e) => UpdateEngineTitle();
            UpdateEngineTitle();
            _logFlushTimer.Tick += _logFlushTimer_Tick;
            _polarResizeTimer.Tick += _polarResizeTimer_Tick;
            _cpResizeTimer.Tick += _cpResizeTimer_Tick;
            _blResizeTimer.Tick += _blResizeTimer_Tick;
            _geomResizeTimer.Tick += _geomResizeTimer_Tick;
        }

        #region UI layout

        // Sets the window title to include the active XFOIL engine (native solvers or xfoil.exe),
        // mirroring frmMain's selection. Safe to call repeatedly (ctor + on Activated).
        private void UpdateEngineTitle()
        {
            string engine = My.MyProject.Forms.frmMain.UseNativeXfoil ? "XFOIL (native)" : "XFOIL (xfoil.exe)";
            Text = $"XFOIL Analysis  •  Engine: {engine}";
        }

        private void InitializeUi()
        {
            Text = "XFOIL Analysis";
            Size = My.MyProject.Forms.frmMain.Size;
            WindowState = My.MyProject.Forms.frmMain.WindowState;
            StartPosition = FormStartPosition.CenterScreen;
            Font = frmMain.systemFont;

            var outer = new TableLayoutPanel();
            outer.Dock = DockStyle.Fill;
            outer.ColumnCount = 1;
            outer.RowCount = 3;
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 200.0f));
            Controls.Add(outer);

            outer.Controls.Add(BuildParamsPanel(), 0, 0);

            tc = new TabControl();
            tc.Dock = DockStyle.Fill;

            tabPolar = new TabPage("Polar");
            tabCp = new TabPage("Cp Distribution");
            tabBl = new TabPage("Boundary Layer");
            tabGeom = new TabPage("Geometry");
            tc.Controls.Add(tabPolar);
            tc.Controls.Add(tabCp);
            tc.Controls.Add(tabBl);
            tc.Controls.Add(tabGeom);
            outer.Controls.Add(tc, 0, 1);

            pPolar = BuildPlotTab(tabPolar, "Polar");
            pCp = BuildPlotTab(tabCp, "Cp");

            var lblBlQty = new Label()
            {
                Text = "Quantity:",
                AutoSize = true,
                Font = frmMain.systemFont,
                TextAlign = ContentAlignment.MiddleLeft
            };
            cmbBlQuantity = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = frmMain.systemFont,
                Width = 230,
                Height = 23
            };
            cmbBlQuantity.Items.AddRange(_blQuantityNames);
            cmbBlQuantity.SelectedIndex = 0;
            cmbBlQuantity.SelectedIndexChanged += (s, e) => RenderBlPlot();
            _plotTip.SetToolTip(cmbBlQuantity, "Which boundary-layer quantity to plot (from the last viscous point-analysis run)");

            btnExplainBl = new Button()
            {
                Text = "Explain trends",
                Font = frmMain.systemFont,
                Width = 120,
                Height = 25,
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExplainBl.FlatAppearance.BorderSize = 1;
            btnExplainBl.FlatAppearance.BorderColor = Color.LightGray;
            btnExplainBl.Click += (s, e) => ShowBlExplanation();
            _plotTip.SetToolTip(btnExplainBl, "Plain-language explanation of the boundary-layer trends in this run (transition, separation, drag) - for learning");
            pBl = BuildPlotTab(tabBl, "BL", new[] { (Control)lblBlQty, cmbBlQuantity, btnExplainBl });

            pGeom = BuildPlotTab(tabGeom, "Geometry");

            _plotRenderers[pPolar] = () => RenderPolarPlot();
            _plotRenderers[pCp] = () => RenderCpPlot();
            _plotRenderers[pBl] = () => RenderBlPlot();
            _plotRenderers[pGeom] = () => RenderGeometryPlot();

            BuildPolarRunsPanel(tabPolar);

            // Resizing invalidates any active zoom/pan (its offsets are in the old size's pixel
            // space), so reset to fit rather than leaving the view stale or oddly cropped.
            pPolar.Resize += (s, e) =>
                {
                    ResetPlotZoom(pPolar);
                    _polarResizeTimer.Stop();
                    _polarResizeTimer.Start();
                };
            pCp.Resize += (s, e) =>
                {
                    ResetPlotZoom(pCp);
                    _cpResizeTimer.Stop();
                    _cpResizeTimer.Start();
                };
            pBl.Resize += (s, e) =>
                {
                    ResetPlotZoom(pBl);
                    _blResizeTimer.Stop();
                    _blResizeTimer.Start();
                };
            pGeom.Resize += (s, e) =>
                {
                    ResetPlotZoom(pGeom);
                    _geomResizeTimer.Stop();
                    _geomResizeTimer.Start();
                };

            // Re-render the newly active tab when the user switches tabs: WinForms only finishes
            // laying out a TabPage's Dock=Fill children a moment after that page actually becomes
            // selected, so a tab that was never selected before can still report a stale/undersized
            // PictureBox if read synchronously in this same handler. BeginInvoke defers the render
            // to the next message-loop tick, by which point the resize/layout cascade has settled.
            tc.SelectedIndexChanged += (s, e) => BeginInvoke(() => { switch (tc.SelectedIndex) { case 0: { RenderPolarPlot(); break; } case 1: { RenderCpPlot(); break; } case 2: { RenderBlPlot(); break; } case 3: { RenderGeometryPlot(); break; } } });

            // Re-render the initially-selected tab (Polar) once more after the window is actually
            // shown, deferred a tick via BeginInvoke so it runs after WinForms finishes the resize/
            // layout cascade to the form's final size - the construction-time render below can run
            // before that cascade is done and land on a stale, too-small size. Deliberately only
            // the Polar tab here, not all four: rendering every tab together before any of them has
            // ever been the selected tab causes them to fight over the shared TabControl's layout
            // pass and land on stale sizes. The others render correctly on their own the moment the
            // user actually selects them, via the SelectedIndexChanged handler above.
            Shown += (s, e) => BeginInvoke(() => RenderPolarPlot());

            RenderPolarPlot();
            RenderCpPlot();
            RenderBlPlot();
            RenderGeometryPlot();

            outer.Controls.Add(BuildLogPanel(), 0, 2);
        }

        /// <summary>
    /// Raw XFOIL console log, so a hung/misbehaving run can actually be diagnosed:
    /// every command WE send is echoed here (prefixed "&gt;&gt;&gt;") interleaved with
    /// whatever XFOIL prints back, in the order it happens. If a run stalls, the last
    /// few lines show exactly which command it stalled after.
    /// </summary>
        private Panel BuildLogPanel()
        {
            var outer = new TableLayoutPanel();
            outer.Dock = DockStyle.Fill;
            outer.ColumnCount = 1;
            outer.RowCount = 2;
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 34.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));

            var header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.WhiteSmoke;
            header.Controls.Add(new Label()
            {
                Text = "XFOIL Console Log",
                Dock = DockStyle.Left,
                Width = 200,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0),
                Font = new Font(frmMain.systemFont, FontStyle.Bold)
            });
            var btnCopyLog = new Button()
            {
                Text = "Copy",
                Dock = DockStyle.Right,
                Width = 70,
                Height = 26,
                Margin = new Padding(4),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCopyLog.Click += (s, e) => { if (txtLog.TextLength > 0) { Clipboard.SetText(txtLog.Text); AppToast.Show("Log copied to clipboard"); } };
            var btnClearLog = new Button()
            {
                Text = "Clear",
                Dock = DockStyle.Right,
                Width = 70,
                Height = 26,
                Margin = new Padding(4),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnClearLog.Click += (s, e) => txtLog.Clear();
            var btnCheckStale = new Button()
            {
                Text = "Check XFOIL Processes",
                Dock = DockStyle.Right,
                Width = 150,
                Height = 26,
                Margin = new Padding(4),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCheckStale.Click += btnCheckStale_Click;
            _plotTip.SetToolTip(btnCheckStale, "List every xfoil.exe process on this machine and offer to kill any left over from a crashed session");
            header.Controls.Add(btnClearLog);
            header.Controls.Add(btnCopyLog);
            header.Controls.Add(btnCheckStale);
            outer.Controls.Add(header, 0, 0);

            txtLog = new TextBox();
            txtLog.Dock = DockStyle.Fill;
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Font = frmMain.systemFont;
            txtLog.BackColor = Color.Black;
            txtLog.ForeColor = Color.White;
            txtLog.BorderStyle = BorderStyle.FixedSingle;
            outer.Controls.Add(txtLog, 0, 1);

            return outer;
        }

        private Panel BuildParamsPanel()
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Top;
            panel.AutoSize = true;
            panel.BackColor = Color.WhiteSmoke;
            panel.Padding = new Padding(8, 6, 8, 6);

            var rows = new TableLayoutPanel();
            rows.AutoSize = true;
            rows.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            rows.ColumnCount = 1;
            rows.RowCount = 3;
            panel.Controls.Add(rows);

            // Row 1: airfoil source
            var row1 = new FlowLayoutPanel();
            row1.AutoSize = true;
            row1.WrapContents = false;
            row1.Controls.Add(NewLabel("Airfoil (NACA e.g. 0012, or .dat path):"));
            txtAirfoil = new TextBox() { Width = 320, Text = "0012", Margin = new Padding(4, 3, 4, 3) };
            row1.Controls.Add(txtAirfoil);
            btnBrowseAirfoil = NewButton("Browse...", 80);
            btnBrowseAirfoil.Click += btnBrowseAirfoil_Click;
            row1.Controls.Add(btnBrowseAirfoil);
            btnLoadAirfoil = NewButton("Load Airfoil", 100);
            btnLoadAirfoil.Click += btnLoadAirfoil_Click;
            row1.Controls.Add(btnLoadAirfoil);
            row1.Controls.Add(NewLabel("      Plot theme:"));
            cmbTheme = new ComboBox() { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(4, 3, 4, 3) };
            cmbTheme.Items.Add("Dark (XFOIL)");
            cmbTheme.Items.Add("Light");
            cmbTheme.SelectedIndex = 0;
            cmbTheme.SelectedIndexChanged += cmbTheme_SelectedIndexChanged;
            row1.Controls.Add(cmbTheme);
            rows.Controls.Add(row1, 0, 0);

            // Row 2: flow conditions
            var row2 = new FlowLayoutPanel();
            row2.AutoSize = true;
            row2.WrapContents = false;
            row2.Controls.Add(NewLabel("Re:"));
            txtRe = new TextBox() { Width = 70, Text = "1e6", Margin = new Padding(4, 3, 12, 3) };
            _plotTip.SetToolTip(txtRe, "Reynolds number. Accepts scientific notation (e.g. 1e6). 0 runs an inviscid (no boundary-layer) analysis.");
            row2.Controls.Add(txtRe);
            row2.Controls.Add(NewLabel("Mach:"));
            txtMach = new TextBox() { Width = 50, Text = "0", Margin = new Padding(4, 3, 12, 3) };
            row2.Controls.Add(txtMach);
            row2.Controls.Add(NewLabel("Ncrit:"));
            txtNcrit = new TextBox() { Width = 40, Text = "9", Margin = new Padding(4, 3, 12, 3) };
            _plotTip.SetToolTip(txtNcrit, "Ncrit: how 'clean' (low-turbulence) the airflow is, which controls how early the boundary" + Constants.vbCrLf + "layer transitions from smooth (laminar) to turbulent flow." + Constants.vbCrLf + Constants.vbCrLf + "XFOIL predicts transition with the e^N method: tiny disturbances in the laminar boundary" + Constants.vbCrLf + "layer grow exponentially with distance; transition is assumed to occur once that growth" + Constants.vbCrLf + "reaches a factor of e^Ncrit. A lower Ncrit means transition (and more drag) happens sooner." + Constants.vbCrLf + Constants.vbCrLf + "Typical values: ~4-5 for a noisy/turbulent wind tunnel or a dirty/bumpy wing surface," + Constants.vbCrLf + "9 for a smooth low-turbulence wind tunnel (XFOIL's default, used here), 11-14 for very" + Constants.vbCrLf + "clean free-flight/sailplane conditions." + Constants.vbCrLf + Constants.vbCrLf + "Only affects viscous runs (Re > 0) - ignored for an inviscid run (Re = 0).");
            row2.Controls.Add(txtNcrit);
            row2.Controls.Add(NewLabel("(leave Re = 0 for an inviscid run)"));
            rows.Controls.Add(row2, 0, 1);

            // Row 3: alpha sweep + single-point + run buttons
            var row3 = new FlowLayoutPanel();
            row3.AutoSize = true;
            row3.WrapContents = false;
            row3.Controls.Add(NewLabel("Alpha min:"));
            txtAlphaMin = new TextBox() { Width = 40, Text = "-4", Margin = new Padding(4, 3, 8, 3) };
            row3.Controls.Add(txtAlphaMin);
            row3.Controls.Add(NewLabel("max:"));
            txtAlphaMax = new TextBox() { Width = 40, Text = "12", Margin = new Padding(4, 3, 8, 3) };
            row3.Controls.Add(txtAlphaMax);
            row3.Controls.Add(NewLabel("step:"));
            txtAlphaStep = new TextBox() { Width = 35, Text = "1", Margin = new Padding(4, 3, 12, 3) };
            row3.Controls.Add(txtAlphaStep);
            btnRunPolar = NewButton("Run Polar Sweep", 130);
            btnRunPolar.Click += btnRunPolar_Click;
            row3.Controls.Add(btnRunPolar);

            row3.Controls.Add(NewLabel("      Single alpha (Cp/BL):"));
            txtSingleAlpha = new TextBox() { Width = 40, Text = "5", Margin = new Padding(4, 3, 12, 3) };
            row3.Controls.Add(txtSingleAlpha);
            btnRunPoint = NewButton("Run Point Analysis", 140);
            btnRunPoint.Click += btnRunPoint_Click;
            row3.Controls.Add(btnRunPoint);
            rows.Controls.Add(row3, 0, 2);

            lblStatus = new Label() { AutoSize = true, Text = "Status: idle", Margin = new Padding(4, 6, 4, 0) };
            rows.Controls.Add(lblStatus, 0, 3);
            rows.RowCount = 4;

            return panel;
        }

        private Label NewLabel(string text)
        {
            return new Label() { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 4, 3) };
        }

        private Button NewButton(string text, int w)
        {
            var b = new Button();
            b.Text = text;
            b.Size = new Size(w, 25);
            b.Margin = new Padding(4, 2, 4, 3);
            b.FlatStyle = FlatStyle.Flat;
            b.BackColor = Color.White;
            b.Cursor = Cursors.Hand;
            return b;
        }

        /// <summary>
    /// Builds a tab's content: a thin top strip (hosts the export button) plus a
    /// fill PictureBox, mirroring frmGeometry's AddExportButtonToPanel pattern so
    /// each plot gets the same PNG/SVG/PDF export UX as the AVL polar tab.
    /// </summary>
        private PictureBox BuildPlotTab(TabPage tab, string viewName, IEnumerable<Control> leftControls = null)
        {
            var outer = new TableLayoutPanel();
            outer.Dock = DockStyle.Fill;
            outer.ColumnCount = 1;
            outer.RowCount = 2;
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 36.0f));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f));
            tab.Controls.Add(outer);

            var controlPanel = new Panel();
            controlPanel.Dock = DockStyle.Fill;
            controlPanel.BackColor = Color.WhiteSmoke;
            outer.Controls.Add(controlPanel, 0, 0);

            var pb = new PictureBox();
            pb.Dock = DockStyle.Fill;
            pb.BackColor = ThemeBackColor;
            // Never set pb.Image directly - the rendered bitmap is kept in _plotBitmaps and
            // drawn manually in PlotPictureBox_Paint (with the zoom/pan transform applied),
            // so PictureBox's own built-in Image drawing would just double-paint underneath it.
            pb.SizeMode = PictureBoxSizeMode.Normal;
            _plotZooms[pb] = new PlotZoomState();
            pb.Paint += PlotPictureBox_Paint;
            pb.MouseWheel += PlotPictureBox_MouseWheel;
            pb.MouseDown += PlotPictureBox_MouseDown;
            pb.MouseMove += PlotPictureBox_MouseMove;
            pb.MouseUp += PlotPictureBox_MouseUp;
            pb.DoubleClick += PlotPictureBox_DoubleClick;
            _plotTip.SetToolTip(pb, "Scroll to zoom, drag to pan, double-click to reset");
            outer.Controls.Add(pb, 0, 1);

            if (leftControls is not null)
            {
                int leftX = 8;
                foreach (var ctrl in leftControls)
                {
                    ctrl.Location = new Point(leftX, (36 - ctrl.Height) / 2);
                    ctrl.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    controlPanel.Controls.Add(ctrl);
                    leftX += ctrl.Width + 8;
                }
            }

            AddExportButtonToPanel(controlPanel, pb, viewName);
            return pb;
        }

        /// <summary>
    /// Adds a right-docked side panel to the Polar tab listing every polar sweep run
    /// so far, with a checkbox to toggle its visibility in the plot (multi-run overlay/
    /// comparison) and a button to clear the run history. Added after BuildPlotTab has
    /// already added the Fill-docked plot area to tabPolar.Controls - a Right-docked
    /// sibling still gets its own space and the Fill sibling takes whatever remains,
    /// regardless of add order (DockStyle.Fill is always resolved last).
    /// </summary>
        private void BuildPolarRunsPanel(TabPage tab)
        {
            var panel = new Panel();
            panel.Dock = DockStyle.Right;
            panel.Width = 190;
            panel.BackColor = Color.WhiteSmoke;

            var header = new Label()
            {
                Text = "Polar Runs",
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                Font = new Font(frmMain.systemFont, FontStyle.Bold)
            };
            panel.Controls.Add(header);

            btnClearRuns = new Button()
            {
                Text = "Clear Runs",
                Dock = DockStyle.Bottom,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnClearRuns.Click += btnClearRuns_Click;
            panel.Controls.Add(btnClearRuns);

            lstPolarRuns = new CheckedListBox();
            lstPolarRuns.Dock = DockStyle.Fill;
            lstPolarRuns.CheckOnClick = true;
            lstPolarRuns.BorderStyle = BorderStyle.None;
            _plotTip.SetToolTip(lstPolarRuns, "Every completed polar sweep. Check/uncheck a run to show/hide it on the plot.");
            lstPolarRuns.ItemCheck += lstPolarRuns_ItemCheck;
            panel.Controls.Add(lstPolarRuns);

            tab.Controls.Add(panel);
        }

        private void AddExportButtonToPanel(Panel panel, PictureBox pb, string viewName)
        {
            var btnExport = new Button();
            btnExport.Text = "Export ▾";
            btnExport.Font = frmMain.systemFont;
            btnExport.BackColor = Color.White;
            btnExport.ForeColor = Color.Black;
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.FlatAppearance.BorderSize = 1;
            btnExport.FlatAppearance.BorderColor = Color.LightGray;
            btnExport.Size = new Size(75, 25);
            btnExport.Top = 6;
            btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExport.Cursor = Cursors.Hand;

            var menu = new ContextMenuStrip();
            menu.Items.Add(new ToolStripMenuItem("Export as PNG...", null, (s, ev) => ExportView(pb, "PNG", viewName)));
            menu.Items.Add(new ToolStripMenuItem("Export as SVG...", null, (s, ev) => ExportView(pb, "SVG", viewName)));
            menu.Items.Add(new ToolStripMenuItem("Export as PDF...", null, (s, ev) => ExportView(pb, "PDF", viewName)));

            btnExport.Click += (s, ev) => menu.Show(btnExport, new Point(0, btnExport.Height));

            void reposition() => btnExport.Left = Math.Max(0, panel.ClientSize.Width - btnExport.Width - 10);
            panel.Resize += (s, ev) => reposition();

            panel.Controls.Add(btnExport);
            btnExport.BringToFront();
            reposition();
        }

        private void ExportView(PictureBox pb, string format, string viewName)
        {
            string svgContent = "";
            string pdfContent = "";

            if (ReferenceEquals(pb, pPolar))
            {
                if (format != "PNG")
                    RenderPolarPlot(true);
                svgContent = _polarSvg;
                pdfContent = _polarPdf;
            }
            else if (ReferenceEquals(pb, pCp))
            {
                if (format != "PNG")
                    RenderCpPlot(true);
                svgContent = _cpSvg;
                pdfContent = _cpPdf;
            }
            else if (ReferenceEquals(pb, pBl))
            {
                if (format != "PNG")
                    RenderBlPlot(true);
                svgContent = _blSvg;
                pdfContent = _blPdf;
            }
            else if (ReferenceEquals(pb, pGeom))
            {
                if (format != "PNG")
                    RenderGeometryPlot(true);
                svgContent = _geomSvg;
                pdfContent = _geomPdf;
            }

            Bitmap plotBmp = null;
            if (format == "PNG")
            {
                _plotBitmaps.TryGetValue(pb, out plotBmp);
                if (plotBmp is null)
                {
                    AppMessageBox.Show("There is no image to export.", "Export View", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (string.IsNullOrEmpty(svgContent))
            {
                AppMessageBox.Show("The view has not finished rendering. Please wait a moment and try again.", "Export View", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var sfd = new SaveFileDialog();
            sfd.Title = $"Export {viewName} as {format}";
            string baseName = "xfoil_" + viewName.ToLower().Replace(" ", "_");
            sfd.FileName = baseName;

            switch (format ?? "")
            {
                case "PNG":
                    {
                        sfd.Filter = "PNG Image (*.png)|*.png";
                        sfd.DefaultExt = "png";
                        break;
                    }
                case "SVG":
                    {
                        sfd.Filter = "SVG Image (*.svg)|*.svg";
                        sfd.DefaultExt = "svg";
                        break;
                    }
                case "PDF":
                    {
                        sfd.Filter = "PDF Document (*.pdf)|*.pdf";
                        sfd.DefaultExt = "pdf";
                        break;
                    }
            }

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    switch (format ?? "")
                    {
                        case "PNG":
                            {
                                using (Image imgCopy = (Image)plotBmp.Clone())
                                {
                                    imgCopy.Save(sfd.FileName, ImageFormat.Png);
                                }

                                break;
                            }
                        case "SVG":
                            {
                                File.WriteAllText(sfd.FileName, svgContent, Encoding.UTF8);
                                break;
                            }
                        case "PDF":
                            {
                                frmGeometry.WriteVectorPdf(pdfContent, pb.Width, pb.Height, sfd.FileName);
                                break;
                            }
                    }
                    AppToast.ShowExported($"{format} exported to " + Path.GetFileName(sfd.FileName), sfd.FileName);
                }
                catch (Exception ex)
                {
                    AppMessageBox.Show("Error exporting file: " + ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnBrowseAirfoil_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Title = "Select airfoil coordinate file";
            ofd.Filter = "Airfoil files (*.dat)|*.dat|All files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtAirfoil.Text = ofd.FileName;
            }
        }

        private async void btnLoadAirfoil_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAirfoil.Text))
            {
                AppMessageBox.Show("Enter a NACA code (e.g. 0012) or browse to an airfoil .dat file.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            btnLoadAirfoil.Enabled = false;
            btnRunPolar.Enabled = false;
            btnRunPoint.Enabled = false;
            try
            {
                lblStatus.Text = "Status: loading airfoil...";
                bool ok = await EnsureAirfoilLoadedAsync();
                lblStatus.Text = ok ? "Status: airfoil loaded" : "Status: failed to load airfoil (check log below)";
                if (ok)
                    tc.SelectedIndex = 3;
            }
            finally
            {
                btnLoadAirfoil.Enabled = true;
                btnRunPolar.Enabled = true;
                btnRunPoint.Enabled = true;
            }
        }

        private void cmbTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            _isDarkTheme = cmbTheme.SelectedIndex == 0;
            pPolar.BackColor = ThemeBackColor;
            pCp.BackColor = ThemeBackColor;
            pBl.BackColor = ThemeBackColor;
            pGeom.BackColor = ThemeBackColor;
            RenderPolarPlot();
            RenderCpPlot();
            RenderBlPlot();
            RenderGeometryPlot();
        }

        private void lstPolarRuns_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _polarRuns.Count)
                return;
            var run = _polarRuns[e.Index];
            // ItemCheck fires before the check state is applied, so use e.NewValue.
            run.Visible = e.NewValue == CheckState.Checked;
            BeginInvoke(() => RenderPolarPlot());
        }

        private void btnClearRuns_Click(object sender, EventArgs e)
        {
            _polarRuns.Clear();
            lstPolarRuns.Items.Clear();
            RenderPolarPlot();
        }

        /// <summary>
    /// Lists every "xfoil" process currently running on the machine (not just ones this
    /// form started) - a stale one left over from a crashed/killed earlier session can
    /// hold a lock on appdata\xfoil.exe or the temp working folder, or otherwise interfere
    /// with a fresh run in ways that look identical to "XFOIL just isn't responding".
    /// Offers to kill any that aren't the one this form is actively using right now.
    /// </summary>
        private void btnCheckStale_Click(object sender, EventArgs e)
        {
            Process[] all = Process.GetProcessesByName("xfoil");
            try
            {
                if (all.Length == 0)
                {
                    LogLine("*** No xfoil.exe processes found running on this machine.");
                    return;
                }

                int ownPid = -1;
                if (p is not null)
                {
                    try
                    {
                        if (!p.HasExited)
                            ownPid = p.Id;
                    }
                    catch
                    {
                    }
                }

                LogLine($"*** Found {all.Length} xfoil.exe process(es) running:");
                var stale = new List<Process>();
                foreach (var proc in all)
                {
                    bool isOwn = proc.Id == ownPid;
                    string startInfo = "";
                    try
                    {
                        startInfo = $", started {proc.StartTime:HH:mm:ss}, {proc.TotalProcessorTime.TotalSeconds:0.0}s CPU time";
                    }
                    catch
                    {
                        // Access to some process info can be denied depending on how it was launched.
                    }
                    LogLine($"    PID {proc.Id}{startInfo}{(isOwn ? "  <- this window's current process" : "")}");
                    if (!isOwn)
                        stale.Add(proc);
                }

                if (stale.Count == 0)
                {
                    LogLine("*** All of them are the one this window is currently using - nothing stale.");
                    return;
                }

                string msg = $"Found {stale.Count} xfoil.exe process(es) not associated with this window (possibly left over from a crashed or force-closed session). Kill them?";
                if (AppMessageBox.Show(msg, "Check XFOIL Processes", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    foreach (var proc in stale)
                    {
                        try
                        {
                            proc.Kill();
                            LogLine($"*** Killed stale xfoil.exe (PID {proc.Id}).");
                        }
                        catch (Exception ex)
                        {
                            LogLine($"*** Failed to kill PID {proc.Id}: {ex.Message}");
                        }
                    }
                }
            }
            finally
            {
                foreach (var proc in all)
                    proc.Dispose();
            }
        }

        #endregion

        #region Loading the airfoil + geometry

        private void LoadAirfoilCommands()
        {
            string text = txtAirfoil.Text.Trim();
            if (Regex.IsMatch(text, @"^\d{4,5}$"))
            {
                WriteCmd($"naca {text}");
            }
            else
            {
                WriteCmd($"load {text}");
            }
        }

        /// <summary>
    /// Loads the airfoil and captures its buffer coordinates via XFOIL's top-level SAVE
    /// command (verified against the real xfoil.exe: a title line followed by plain
    /// "x y" rows, TE -> upper -> LE -> lower -> TE) into _airfoilCoords, which backs
    /// both the Geometry tab and the airfoil-shape overlay on the Cp plot.
    /// 
    /// ALWAYS closes stdin (forcing the process to exit) before checking for the file.
    /// Confirmed directly against xfoil.exe: the file SAVE writes is not flushed to disk
    /// until the process itself terminates - it stays sitting in an unflushed buffer for
    /// as long as the process is kept alive to receive more commands (reproduced outside
    /// this app entirely, with a bare .NET Process and no reader thread at all, so it's a
    /// genuine behavior of this XFOIL build's Fortran I/O, not a bug in how this app reads
    /// output). That means this step can never share a process with whatever analysis
    /// commands come after it - the caller must start a fresh process afterward.
    /// </summary>
        private async Task<bool> EnsureAirfoilLoadedAsync()
        {
            if (!await EnsureXfoilReadyAsync())
                return false;

            _geomFile = NewTempFile("geom");

            LoadAirfoilCommands();
            WriteCmd($"save {Path.GetFileName(_geomFile)}");
            FlushCmd();
            CloseXfoilInput();

            bool ok = await WaitForFileAsync(_geomFile, 15000, path => ParseGeomFile(path).Count > 0, requireProcessExit: true);
            if (ok)
            {
                _airfoilCoords = ParseGeomFile(_geomFile);
                RenderGeometryPlot();
                RenderCpPlot();
            }
            return ok;
        }

        #endregion

        #region Run polar sweep

        private async void btnRunPolar_Click(object sender, EventArgs e)
        {
            double re;
            double mach;
            double ncrit;
            double aMin;
            double aMax;
            double aStep;
            if (!double.TryParse(txtRe.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out re))
                re = 0d;
            if (!double.TryParse(txtMach.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out mach))
                mach = 0d;
            if (!double.TryParse(txtNcrit.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out ncrit))
                ncrit = 9d;
            if (!double.TryParse(txtAlphaMin.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out aMin) || !double.TryParse(txtAlphaMax.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out aMax) || !double.TryParse(txtAlphaStep.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out aStep) || aStep == 0d)
            {
                AppMessageBox.Show("Enter valid numeric alpha min/max/step values.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtAirfoil.Text))
            {
                AppMessageBox.Show("Enter a NACA code (e.g. 0012) or browse to an airfoil .dat file.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Native XFOIL engine: compute the polar in-process with Xfoil.Core instead of
            // driving external xfoil.exe. Honors the engine chosen in frmMain.
            if (My.MyProject.Forms.frmMain.UseNativeXfoil)
            {
                RunPolarNative(re, mach, ncrit, aMin, aMax, aStep);
                return;
            }

            btnRunPolar.Enabled = false;
            btnRunPoint.Enabled = false;
            btnLoadAirfoil.Enabled = false;
            try
            {
                lblStatus.Text = "Status: loading airfoil...";
                if (!await EnsureAirfoilLoadedAsync())
                    return;

                // EnsureAirfoilLoadedAsync always closes its own process once the geometry file
                // is safely flushed to disk (see its comment) - start a fresh one here for the
                // actual analysis run, re-sending the airfoil load since it's a brand new session.
                if (!await EnsureXfoilReadyAsync())
                    return;
                LoadAirfoilCommands();

                _polFile = NewTempFile("polar");

                lblStatus.Text = "Status: running polar sweep...";

                WriteCmd("oper");
                if (re > 0d)
                {
                    WriteCmd("visc " + re.ToString("F0", CultureInfo.InvariantCulture));
                    WriteCmd("iter 200");
                    WriteCmd("vpar");
                    WriteCmd("n " + ncrit.ToString("0.###", CultureInfo.InvariantCulture));
                    WriteCmd("");
                }
                WriteCmd("mach " + mach.ToString("0.###", CultureInfo.InvariantCulture));
                WriteCmd("pacc");
                WriteCmd(Path.GetFileName(_polFile));
                WriteCmd("");
                WriteCmd($"aseq {aMin.ToString("0.###", CultureInfo.InvariantCulture)} {aMax.ToString("0.###", CultureInfo.InvariantCulture)} {aStep.ToString("0.###", CultureInfo.InvariantCulture)}");
                WriteCmd("pacc");
                FlushCmd();
                CloseXfoilInput();

                int numPoints = Math.Max(1, (int)Math.Round(Math.Abs((aMax - aMin) / aStep)) + 1);
                int timeoutMs = Math.Max(10000, numPoints * 2000);
                bool ok = await WaitForFileAsync(_polFile, timeoutMs, path => ParsePolarFile(path).Count > 0);
                if (!ok)
                {
                    lblStatus.Text = "Status: polar sweep did not produce output (check log below)";
                    AppMessageBox.Show("XFOIL did not produce a polar file in time. Check the log for errors (e.g. an airfoil that failed to load, or a non-converging viscous run).", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var points = ParsePolarFile(_polFile);
                var run = new XfoilPolarRun()
                {
                    Label = $"{txtAirfoil.Text.Trim()}  Re={FormatRe(re)}  M={mach:0.00}",
                    Color = _runColorPalette[_polarRuns.Count % _runColorPalette.Length],
                    Points = points,
                    Visible = true
                };
                _polarRuns.Add(run);
                lstPolarRuns.Items.Add(run.Label, true);

                RenderPolarPlot();
                lblStatus.Text = $"Status: polar sweep complete - {points.Count} point(s)";
                tc.SelectedIndex = 0;
            }
            finally
            {
                btnRunPolar.Enabled = true;
                btnRunPoint.Enabled = true;
                btnLoadAirfoil.Enabled = true;
            }
        }

        private string FormatRe(double re)
        {
            if (re <= 0d)
                return "inviscid";
            return (re / 1000000.0d).ToString("0.0#", CultureInfo.InvariantCulture) + "e6";
        }

        // ---- Native (in-process Xfoil.Core) analysis paths -------------------------------

        /// <summary>Builds a paneled airfoil from the airfoil input box using Xfoil.Core:
        /// a NACA code (e.g. "0012"/"2412") or a coordinate .dat file path. Returns null if
        /// it can't be built.</summary>
        private PanelAirfoil BuildPanelAirfoilNative()
        {
            string s = (txtAirfoil.Text ?? "").Trim();
            try
            {
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int code) && !s.Contains('.'))
                {
                    var af = Naca.Generate(code);
                    return af is null ? null : Paneling.Pangen(af.Xb, af.Yb, af.Name);
                }
                if (File.Exists(s))
                {
                    var r = AirfoilFile.Read(File.ReadAllText(s));
                    if (!r.Ok)
                        return null;
                    string name = string.IsNullOrWhiteSpace(r.Name) ? Path.GetFileNameWithoutExtension(s) : r.Name;
                    return Paneling.Pangen(r.X, r.Y, name);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void RunPolarNative(double re, double mach, double ncrit, double aMin, double aMax, double aStep)
        {
            var pan = BuildPanelAirfoilNative();
            if (pan is null)
            {
                AppMessageBox.Show("Could not build the airfoil - check the NACA code or the .dat file path.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblStatus.Text = "Status: running polar sweep (native)...";
            Application.DoEvents();

            var points = new List<XfoilPolarPoint>();
            double step = Math.Abs(aStep) * (aMax >= aMin ? 1.0 : -1.0);
            int steps = (int)Math.Round(Math.Abs((aMax - aMin) / aStep)) + 1;
            var inv = re > 0d ? null : new InviscidSolver(pan);
            for (int k = 0; k < steps; k++)
            {
                double a = aMin + k * step;
                try
                {
                    if (re > 0d)
                    {
                        var r = new ViscousSolver(pan, re, mach, ncrit).Solve(a, 80);
                        if (r.Converged)
                            points.Add(new XfoilPolarPoint() { Alpha = a, CL = r.Cl, CD = r.Cd, CDp = r.Cdp, CM = r.Cm, TopXtr = r.XtrTop, BotXtr = r.XtrBottom });
                    }
                    else
                    {
                        var pt = inv.SolveAlpha(a, mach);
                        points.Add(new XfoilPolarPoint() { Alpha = a, CL = pt.Cl, CD = 0d, CDp = pt.Cdp, CM = pt.Cm, TopXtr = 0d, BotXtr = 0d });
                    }
                }
                catch
                {
                    // skip a point that fails to build/converge, matching xfoil.exe's behavior
                }
            }

            if (points.Count == 0)
            {
                lblStatus.Text = "Status: no converged points";
                AppMessageBox.Show("No polar points converged for this airfoil / conditions.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var run = new XfoilPolarRun()
            {
                Label = $"{txtAirfoil.Text.Trim()}  Re={FormatRe(re)}  M={mach:0.00}",
                Color = _runColorPalette[_polarRuns.Count % _runColorPalette.Length],
                Points = points,
                Visible = true
            };
            _polarRuns.Add(run);
            lstPolarRuns.Items.Add(run.Label, true);

            RenderPolarPlot();
            lblStatus.Text = $"Status: polar sweep complete (native) - {points.Count} point(s)";
            tc.SelectedIndex = 0;
        }

        private void RunPointNative(double re, double mach, double ncrit, double alpha)
        {
            var pan = BuildPanelAirfoilNative();
            if (pan is null)
            {
                AppMessageBox.Show("Could not build the airfoil - check the NACA code or the .dat file path.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblStatus.Text = "Status: running point analysis (native)...";
            Application.DoEvents();

            _airfoilCoords = new List<XfoilGeomPoint>();
            for (int i = 0; i < pan.N; i++)
                _airfoilCoords.Add(new XfoilGeomPoint() { X = pan.X[i], Y = pan.Y[i] });

            _cpPoints = new List<XfoilCpPoint>();
            _blTop.Clear();
            _blBottom.Clear();

            if (re > 0d)
            {
                var vs = new ViscousSolver(pan, re, mach, ncrit);
                var r = vs.Solve(alpha, 80);
                if (!r.Converged)
                    AppToast.Show("Viscous solve did not fully converge - showing best estimate");

                foreach (var c in vs.SurfaceCp())
                    _cpPoints.Add(new XfoilCpPoint() { X = c.x, Cp = c.cpv, CpInv = c.cpi });

                // Split top/bottom by y-sign (including the wake, which runs to ~2 chords),
                // exactly like ParseBlFile does for the external xfoil.exe dump.
                foreach (var b in vs.BoundaryLayer())
                {
                    var p = new XfoilBLPoint() { X = b.X, Y = b.Y, Ue = b.Ue, Dstar = b.Dstar, Theta = b.Theta, Cf = b.Cf, H = b.H, N = b.N, Ctau = b.Ctau, Cd = b.Cd };
                    if (b.Y >= 0d)
                        _blTop.Add(p);
                    else
                        _blBottom.Add(p);
                }

                _lastPointCL = r.Cl;
                _lastPointCM = r.Cm;
                _lastPointCD = r.Cd;
                _lastTopXtr = r.XtrTop;
                _lastBotXtr = r.XtrBottom;
                _lastPointRe = re;
                _lastPointNcrit = ncrit;
            }
            else
            {
                var pt = new InviscidSolver(pan).SolveAlpha(alpha, mach);
                for (int i = 0; i < pan.N; i++)
                    _cpPoints.Add(new XfoilCpPoint() { X = pan.X[i], Cp = pt.Cp[i] });

                _lastPointCL = pt.Cl;
                _lastPointCM = pt.Cm;
                _lastPointCD = 0d;
                _lastTopXtr = default;
                _lastBotXtr = default;
                _lastPointRe = default;
                _lastPointNcrit = default;
            }

            _lastPointAlpha = alpha;
            _lastPointMach = mach;

            lblStatus.Text = $"Status: point analysis complete (native) at alpha = {alpha}";
            tc.SelectedIndex = 1;
            RenderCpPlot();
            RenderBlPlot();
            RenderGeometryPlot();
        }

        #endregion

        #region Run point analysis (Cp + boundary layer)

        private async void btnRunPoint_Click(object sender, EventArgs e)
        {
            double re;
            double mach;
            double alpha;
            double ncrit;
            if (!double.TryParse(txtRe.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out re))
                re = 0d;
            if (!double.TryParse(txtMach.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out mach))
                mach = 0d;
            if (!double.TryParse(txtNcrit.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out ncrit))
                ncrit = 9d;
            if (!double.TryParse(txtSingleAlpha.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out alpha))
            {
                AppMessageBox.Show("Enter a valid numeric single alpha value.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtAirfoil.Text))
            {
                AppMessageBox.Show("Enter a NACA code (e.g. 0012) or browse to an airfoil .dat file.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Native XFOIL engine: compute Cp + boundary layer in-process with Xfoil.Core.
            if (My.MyProject.Forms.frmMain.UseNativeXfoil)
            {
                RunPointNative(re, mach, ncrit, alpha);
                return;
            }

            btnRunPolar.Enabled = false;
            btnRunPoint.Enabled = false;
            btnLoadAirfoil.Enabled = false;
            try
            {
                lblStatus.Text = "Status: loading airfoil...";
                if (!await EnsureAirfoilLoadedAsync())
                    return;

                // EnsureAirfoilLoadedAsync always closes its own process once the geometry file
                // is safely flushed to disk (see its comment) - start a fresh one here for the
                // actual analysis run, re-sending the airfoil load since it's a brand new session.
                if (!await EnsureXfoilReadyAsync())
                    return;
                LoadAirfoilCommands();

                _cpFile = NewTempFile("cp");
                _blFile = NewTempFile("bl");

                lblStatus.Text = "Status: running point analysis...";

                WriteCmd("oper");
                if (re > 0d)
                {
                    WriteCmd("visc " + re.ToString("F0", CultureInfo.InvariantCulture));
                    WriteCmd("iter 200");
                    WriteCmd("vpar");
                    WriteCmd("n " + ncrit.ToString("0.###", CultureInfo.InvariantCulture));
                    WriteCmd("");
                }
                WriteCmd("mach " + mach.ToString("0.###", CultureInfo.InvariantCulture));
                WriteCmd($"alfa {alpha.ToString("0.###", CultureInfo.InvariantCulture)}");
                WriteCmd($"cpwr {Path.GetFileName(_cpFile)}");
                if (re > 0d)
                    WriteCmd($"dump {Path.GetFileName(_blFile)}");
                FlushCmd();
                CloseXfoilInput();

                bool ok = await WaitForFileAsync(_cpFile, 10000, path => ParseCpFile(path).Count > 0);
                if (!ok)
                {
                    lblStatus.Text = "Status: point analysis did not produce Cp output (check log below)";
                    AppMessageBox.Show("XFOIL did not produce a Cp output file in time. Check the log for errors.", "XFOIL Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _cpPoints = ParseCpFile(_cpFile);

                if (re > 0d)
                {
                    bool okBl = await WaitForFileAsync(_blFile, 10000, path =>
                        {
                            var top = new List<XfoilBLPoint>();
                            var bot = new List<XfoilBLPoint>();
                            ParseBlFile(path, top, bot);
                            return top.Count > 0 || bot.Count > 0;
                        });
                    if (okBl)
                    {
                        ParseBlFile(_blFile, _blTop, _blBottom);
                    }
                    else
                    {
                        _blTop.Clear();
                        _blBottom.Clear();
                    }
                }
                else
                {
                    _blTop.Clear();
                    _blBottom.Clear();
                }

                ParseTransitionFromLog();
                _lastPointAlpha = alpha;
                _lastPointMach = mach;
                if (re > 0d)
                {
                    _lastPointRe = re;
                    _lastPointNcrit = ncrit;
                }
                else
                {
                    _lastPointRe = default;
                    _lastPointNcrit = default;
                }

                lblStatus.Text = $"Status: point analysis complete at alpha = {alpha}";
                tc.SelectedIndex = 1;
                RenderCpPlot();
                RenderBlPlot();
            }
            finally
            {
                btnRunPolar.Enabled = true;
                btnRunPoint.Enabled = true;
                btnLoadAirfoil.Enabled = true;
            }
        }

        /// <summary>
    /// XFOIL's console echo for a converged point prints "Side 1 free transition at
    /// x/c = ..." / "Side 2 free transition at x/c = ..." once per Newton iteration
    /// (Side 1 = upper/top surface, Side 2 = lower/bottom - confirmed against a real
    /// run), and separately "a =  5.000      CL =  0.5571" / "Cm =  0.0019     CD =
    /// 0.00848   =>   CDf =  0.00543    CDp =  0.00304" for the converged forces. Take
    /// the LAST match of each from the accumulated log text (the final converged value)
    /// to mark transition and show the parameter block on the Cp plot, matching XFOIL's
    /// own CPX view.
    /// </summary>
        private void ParseTransitionFromLog()
        {
            _lastTopXtr = default;
            _lastBotXtr = default;
            _lastPointCL = default;
            _lastPointCM = default;
            _lastPointCD = default;
            string text = txtLog.Text;
            if (string.IsNullOrEmpty(text))
                return;

            _lastTopXtr = LastRegexDouble(text, @"Side\s+1\s+\S+\s+transition at x/c\s*=\s*([\d.]+)");
            _lastBotXtr = LastRegexDouble(text, @"Side\s+2\s+\S+\s+transition at x/c\s*=\s*([\d.]+)");
            _lastPointCL = LastRegexDouble(text, @"CL\s*=\s*(-?[\d.]+)");
            _lastPointCM = LastRegexDouble(text, @"Cm\s*=\s*(-?[\d.]+)");
            // "CD" only (not "CDp"/"CDf") - the trailing letter in those means \s*= never
            // matches right after "CD", so this is safely just the total drag coefficient.
            _lastPointCD = LastRegexDouble(text, @"CD\s*=\s*(-?[\d.]+)");
        }

        private double? LastRegexDouble(string text, string pattern)
        {
            var matches = Regex.Matches(text, pattern);
            if (matches.Count == 0)
                return default;
            double v;
            if (double.TryParse(matches[matches.Count - 1].Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
            {
                return v;
            }
            return default;
        }

        #endregion

        #region Parsing

        /// <summary>
    /// XFOIL's PACC polar file: a fixed header block ending in a line naming the
    /// columns ("alpha  CL  CD  CDp  CM  Top_Xtr  Bot_Xtr") followed by a dashed
    /// separator, then one fixed-width numeric row per alpha.
    /// </summary>
        private List<XfoilPolarPoint> ParsePolarFile(string path)
        {
            var result = new List<XfoilPolarPoint>();
            string[] lines = File.ReadAllLines(path);
            int headerIdx = -1;
            for (int i = 0, loopTo = lines.Length - 1; i <= loopTo; i++)
            {
                if (lines[i].IndexOf("alpha", StringComparison.OrdinalIgnoreCase) >= 0 && lines[i].IndexOf("CL", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    headerIdx = i;
                    break;
                }
            }
            if (headerIdx < 0)
                return result;

            for (int i = headerIdx + 2, loopTo1 = lines.Length - 1; i <= loopTo1; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                string[] toks = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (toks.Length < 7)
                    continue;

                var vals = new double[7];
                bool allOk = true;
                for (int c = 0; c <= 6; c++)
                {
                    if (!double.TryParse(toks[c], NumberStyles.Float, CultureInfo.InvariantCulture, out vals[c]))
                    {
                        allOk = false;
                        break;
                    }
                }
                if (!allOk)
                    continue;

                result.Add(new XfoilPolarPoint()
                {
                    Alpha = vals[0],
                    CL = vals[1],
                    CD = vals[2],
                    CDp = vals[3],
                    CM = vals[4],
                    TopXtr = vals[5],
                    BotXtr = vals[6]
                });
            }
            return result;
        }

        /// <summary>
    /// XFOIL's CPWR output (verified against the real xfoil.exe): a title line, an
    /// "Alfa = ... Re = ..." line, a "# x y Cp" column header, then rows of
    /// "x  y  Cp" (3 columns). Take the first token as x and the last as Cp so this
    /// still degrades gracefully on older/inviscid-only XFOIL builds that write just
    /// "x  Cp" (2 columns). Preserve file order (traces the airfoil surface as a
    /// loop: TE -> upper -> LE -> lower -> TE) rather than sorting by x.
    /// </summary>
        private List<XfoilCpPoint> ParseCpFile(string path)
        {
            var result = new List<XfoilCpPoint>();
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                string[] toks = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (toks.Length < 2)
                    continue;
                double x;
                double cp;
                if (double.TryParse(toks[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) && double.TryParse(toks[toks.Length - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out cp))
                {
                    result.Add(new XfoilCpPoint() { X = x, Cp = cp });
                }
            }
            return result;
        }

        /// <summary>
    /// XFOIL's SAVE command (verified against the real xfoil.exe): a title line (the
    /// airfoil name) followed by plain "x y" coordinate rows tracing the airfoil
    /// surface as a loop (TE -> upper -> LE -> lower -> TE). Same lenient "any line
    /// with 2+ numeric tokens" parse as ParseCpFile, preserving file order.
    /// </summary>
        private List<XfoilGeomPoint> ParseGeomFile(string path)
        {
            var result = new List<XfoilGeomPoint>();
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                string[] toks = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (toks.Length < 2)
                    continue;
                double x;
                double y;
                if (double.TryParse(toks[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) && double.TryParse(toks[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                {
                    result.Add(new XfoilGeomPoint() { X = x, Y = y });
                }
            }
            return result;
        }

        /// <summary>
    /// XFOIL's boundary-layer "dump" file (verified against the real xfoil.exe): a
    /// "#" header row followed by exactly 8 columns per row - s, x, y, Ue/Vinf, Dstar,
    /// Theta, Cf, H (no amplification/Ctau column in this XFOIL 6.99 build; some other
    /// versions reportedly add a 9th/10th column, hence still bounds-checking rather
    /// than assuming exactly 8). Rows are split into upper vs lower surface by the sign
    /// of y - a safe heuristic since airfoil coordinates are referenced to the chord
    /// line (y >= 0 is the upper surface) - rather than relying on the exact blank-line
    /// block boundaries XFOIL uses between the two sides.
    /// </summary>
        private void ParseBlFile(string path, List<XfoilBLPoint> top, List<XfoilBLPoint> bottom)
        {
            top.Clear();
            bottom.Clear();
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                string[] toks = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (toks.Length < 8)
                    continue;

                var vals = new double[8];
                bool allOk = true;
                for (int c = 0; c <= 7; c++)
                {
                    if (!double.TryParse(toks[c], NumberStyles.Float, CultureInfo.InvariantCulture, out vals[c]))
                    {
                        allOk = false;
                        break;
                    }
                }
                if (!allOk)
                    continue;

                var pt = new XfoilBLPoint() { X = vals[1], Y = vals[2], Ue = vals[3], Dstar = vals[4], Theta = vals[5], Cf = vals[6], H = vals[7] };
                if (vals[2] >= 0d)
                {
                    top.Add(pt);
                }
                else
                {
                    bottom.Add(pt);
                }
            }
        }

        #endregion

        #region Rendering

        private Font MakeTickFont()
        {
            return new Font("Segoe UI", 8.0f);
        }

        private Font MakeAxisFont()
        {
            return new Font("Segoe UI", 9.0f, FontStyle.Bold);
        }

        /// <summary>
    /// Draws the airfoil-name + Re/alpha/CL/CM/CD/L-over-D/Ncrit parameter block shared
    /// by the Cp and BL plots, matching XFOIL's own CPX view exactly (including the
    /// "×10⁶" Re exponent and the C_L/C_M/C_D/N_cr subscripts). Values come from the
    /// last completed single-point run (_lastPoint... fields); any not yet available
    /// show "--".
    /// </summary>
        private void DrawParamBlock(SvgGraphics g, Font titleFont, Font paramFont, Font subFont, SolidBrush brush, float paramX, float paramY)
        {
            string airfoilName = txtAirfoil.Text.Trim();
            if (Regex.IsMatch(airfoilName, @"^\d{4,5}$"))
                airfoilName = "NACA " + airfoilName;
            g.DrawString(airfoilName.ToUpperInvariant(), titleFont, brush, new PointF(paramX, paramY));

            float lineY = paramY + 26f;
            void DrawValue(string value)
            {
                g.DrawString("=", paramFont, brush, new PointF(paramX + 50f, lineY));
                g.DrawString(value, paramFont, brush, new PointF(paramX + 70f, lineY));
                lineY += 19f;
            };
            void DrawSub(string mainText, string subText)
            {
                g.DrawString(mainText, paramFont, brush, new PointF(paramX, lineY));
                float tw = g.MeasureString(mainText, paramFont).Width;
                g.DrawString(subText, subFont, brush, new PointF(paramX + tw - 1f, lineY + 6f));
            };

            // Re, with a superscript "×10⁶" exponent.
            g.DrawString("Re", paramFont, brush, new PointF(paramX, lineY));
            if (_lastPointRe.HasValue && _lastPointRe.Value > 0d)
            {
                g.DrawString("=", paramFont, brush, new PointF(paramX + 50f, lineY));
                string reMain = (_lastPointRe.Value / 1000000.0d).ToString("0.000", CultureInfo.InvariantCulture) + "×10";
                g.DrawString(reMain, paramFont, brush, new PointF(paramX + 70f, lineY));
                float reW = g.MeasureString(reMain, paramFont).Width;
                g.DrawString("6", subFont, brush, new PointF(paramX + 70f + reW - 1f, lineY - 3f));
                lineY += 19f;
            }
            else
            {
                DrawValue("inviscid");
            }

            // alpha
            g.DrawString("α", paramFont, brush, new PointF(paramX, lineY));
            DrawValue(_lastPointAlpha.HasValue ? _lastPointAlpha.Value.ToString("0.0000", CultureInfo.InvariantCulture) + "°" : "--");

            // CL
            DrawSub("C", "L");
            DrawValue(_lastPointCL.HasValue ? _lastPointCL.Value.ToString("0.0000", CultureInfo.InvariantCulture) : "--");

            // CM
            DrawSub("C", "M");
            DrawValue(_lastPointCM.HasValue ? _lastPointCM.Value.ToString("0.0000", CultureInfo.InvariantCulture) : "--");

            // CD
            DrawSub("C", "D");
            DrawValue(_lastPointCD.HasValue ? _lastPointCD.Value.ToString("0.00000", CultureInfo.InvariantCulture) : "--");

            // L/D
            g.DrawString("L/D", paramFont, brush, new PointF(paramX, lineY));
            string ldText = "--";
            if (_lastPointCL.HasValue && _lastPointCD.HasValue && _lastPointCD.Value != 0d)
            {
                ldText = (_lastPointCL.Value / _lastPointCD.Value).ToString("0.00", CultureInfo.InvariantCulture);
            }
            DrawValue(ldText);

            // Ncrit (only meaningful for a viscous run)
            DrawSub("N", "cr");
            DrawValue(_lastPointNcrit.HasValue ? _lastPointNcrit.Value.ToString("0.00", CultureInfo.InvariantCulture) : "--");
        }

        /// <summary>
    /// Index of the point with the smallest X (the leading edge) in a list that traces
    /// the airfoil surface as a loop (TE -> upper -> LE -> lower -> TE) - splitting a
    /// polyline at this index separates the upper-surface half from the lower-surface
    /// half so each can be colored differently.
    /// </summary>
        private int IndexOfMinX(List<XfoilCpPoint> points)
        {
            int idx = 0;
            double minX = double.MaxValue;
            for (int i = 0, loopTo = points.Count - 1; i <= loopTo; i++)
            {
                if (points[i].X < minX)
                {
                    minX = points[i].X;
                    idx = i;
                }
            }
            return idx;
        }

        private int IndexOfMinX(List<XfoilGeomPoint> points)
        {
            int idx = 0;
            double minX = double.MaxValue;
            for (int i = 0, loopTo = points.Count - 1; i <= loopTo; i++)
            {
                if (points[i].X < minX)
                {
                    minX = points[i].X;
                    idx = i;
                }
            }
            return idx;
        }

        private void RenderPolarPlot(bool captureVectors = false)
        {
            if (pPolar is null || pPolar.Width <= 0 || pPolar.Height <= 0)
                return;
            int w = pPolar.Width;
            int h = pPolar.Height;
            var bmp = new Bitmap(w, h);
            var tickFont = MakeTickFont();
            var axisFont = MakeAxisFont();
            string svgOut = "";
            string pdfOut = "";

            bool hasVisibleData = false;
            foreach (var r in _polarRuns)
            {
                if (r.Visible && r.Points.Count > 0)
                {
                    hasVisibleData = true;
                    break;
                }
            }

            using (var g = new SvgGraphics(w, h, Graphics.FromImage(bmp), captureVectors))
            {
                g.FontScale = CurrentPlotFontScale;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(ThemeBackColor);
                ApplyPlotZoomTransform(g, pPolar, captureVectors);

                if (!hasVisibleData)
                {
                    g.DrawString("Set airfoil + alpha range and click \"Run Polar Sweep\".", tickFont, Brushes.Gray, new PointF(10f, 10f));
                }
                else
                {
                    float marginX = 55f;
                    float marginY = 35f;
                    float cellW = (w - marginX * 3f) / 2f;
                    float cellH = (h - marginY * 3f) / 2f;

                    DrawSubplot(g, tickFont, axisFont, marginX, marginY, cellW, cellH, "CL vs alpha", _polarRuns, pt => pt.Alpha, pt => pt.CL, showLegend: true);
                    DrawSubplot(g, tickFont, axisFont, marginX * 2f + cellW, marginY, cellW, cellH, "CD vs alpha", _polarRuns, pt => pt.Alpha, pt => pt.CD);
                    DrawSubplot(g, tickFont, axisFont, marginX, marginY * 2f + cellH, cellW, cellH, "Cm vs alpha", _polarRuns, pt => pt.Alpha, pt => pt.CM);
                    DrawSubplot(g, tickFont, axisFont, marginX * 2f + cellW, marginY * 2f + cellH, cellW, cellH, "CL vs CD (drag polar)", _polarRuns, pt => pt.CD, pt => pt.CL);
                }

                if (captureVectors)
                {
                    svgOut = g.GetSvgContent();
                    pdfOut = g.GetPdfContentStream();
                }
            }

            SetPlotBitmap(pPolar, bmp);
            if (captureVectors)
            {
                _polarSvg = svgOut;
                _polarPdf = pdfOut;
            }
        }

        /// <summary>
    /// Draws one subplot for every visible run in the shared axis range so multiple
    /// polar sweeps can be overlaid/compared, each in its own color (XfoilPolarRun.Color),
    /// with an optional small color-swatch legend when more than one run is visible.
    /// </summary>
        private void DrawSubplot(SvgGraphics g, Font tickFont, Font axisFont, float x, float y, float w, float h, string title, List<XfoilPolarRun> runs, Func<XfoilPolarPoint, double> xOf, Func<XfoilPolarPoint, double> yOf, bool showLegend = false)
        {
            var visibleRuns = new List<XfoilPolarRun>();
            foreach (var r in runs)
            {
                if (r.Visible && r.Points.Count > 0)
                    visibleRuns.Add(r);
            }
            if (visibleRuns.Count == 0)
                return;

            double xMin = double.MaxValue;
            double xMax = double.MinValue;
            double yMin = double.MaxValue;
            double yMax = double.MinValue;
            foreach (var r in visibleRuns)
            {
                foreach (var pt in r.Points)
                {
                    double xv = xOf(pt);
                    double yv = yOf(pt);
                    xMin = Math.Min(xMin, xv);
                    xMax = Math.Max(xMax, xv);
                    yMin = Math.Min(yMin, yv);
                    yMax = Math.Max(yMax, yv);
                }
            }
            if (xMin == xMax)
            {
                xMin -= 1d;
                xMax += 1d;
            }
            if (yMin == yMax)
            {
                yMin -= 1d;
                yMax += 1d;
            }
            double yPad = (yMax - yMin) * 0.1d;
            yMin -= yPad;
            yMax += yPad;

            var axisPen = new Pen(ThemeForeColor, 1f);
            var gridPen = new Pen(ThemeGridColor, 1f) { DashStyle = DashStyle.Dash };
            var fgBrush = new SolidBrush(ThemeForeColor);

            g.DrawRectangle(axisPen, x, y, w, h);
            g.DrawString(title, axisFont, fgBrush, new PointF(x, y - axisFont.Height - 2f));

            if (yMin < 0d && yMax > 0d)
            {
                float zeroY = (float)((double)(y + h) - (0d - yMin) / (yMax - yMin) * (double)h);
                g.DrawLine(gridPen, x, zeroY, x + w, zeroY);
            }

            foreach (var r in visibleRuns)
            {
                var ordered = new List<XfoilPolarPoint>(r.Points);
                ordered.Sort((a, b) => a.Alpha.CompareTo(b.Alpha));

                var pen = new Pen(r.Color, 1.5f);
                var markerBrush = new SolidBrush(r.Color);
                var pts = new List<PointF>();
                foreach (var pt in ordered)
                {
                    float px = (float)((double)x + (xOf(pt) - xMin) / (xMax - xMin) * (double)w);
                    float py = (float)((double)(y + h) - (yOf(pt) - yMin) / (yMax - yMin) * (double)h);
                    pts.Add(new PointF(px, py));
                }
                if (pts.Count >= 2)
                    g.DrawLines(pen, pts.ToArray());
                foreach (var pt in pts)
                    g.FillEllipse(markerBrush, pt.X - 2f, pt.Y - 2f, 4f, 4f);
            }

            g.DrawString(xMin.ToString("0.00"), tickFont, fgBrush, new PointF(x, y + h + 2f));
            string maxLabel = xMax.ToString("0.00");
            var maxLabelSize = g.MeasureString(maxLabel, tickFont);
            g.DrawString(maxLabel, tickFont, fgBrush, new PointF(x + w - maxLabelSize.Width, y + h + 2f));

            string yMaxLabel = yMax.ToString("0.0000");
            var yMaxLabelSize = g.MeasureString(yMaxLabel, tickFont);
            g.DrawString(yMaxLabel, tickFont, fgBrush, new PointF(x - yMaxLabelSize.Width - 2f, y));
            string yMinLabel = yMin.ToString("0.0000");
            var yMinLabelSize = g.MeasureString(yMinLabel, tickFont);
            g.DrawString(yMinLabel, tickFont, fgBrush, new PointF(x - yMinLabelSize.Width - 2f, y + h - yMinLabelSize.Height));

            if (showLegend && visibleRuns.Count > 1)
            {
                float ly = y + 4f;
                foreach (var r in visibleRuns)
                {
                    g.FillRectangle(new SolidBrush(r.Color), x + 4f, ly, 10f, 10f);
                    g.DrawString(r.Label, tickFont, fgBrush, new PointF(x + 18f, ly - 1f));
                    ly += 14f;
                }
            }
        }

        /// <summary>
    /// Matches XFOIL's own native CPX plot exactly (confirmed against a screenshot of the
    /// real thing): black background, white content, no border rectangle - just a single
    /// Y axis with ticks every 0.5 and a horizontal Cp=0 line with small unlabeled ticks
    /// (XFOIL doesn't label the x-axis at all). The Cp axis is a fixed -2.0..1.0 range,
    /// widened outward in 0.5 steps only if the data actually exceeds it, rather than
    /// auto-fitting tightly to the data the way the other plots here do. "XFOIL" is
    /// tagged top-left and a parameter block (airfoil name, alpha, CL, CM, CDp) sits
    /// top-right, same as the reference. The airfoil shape is drawn in its own band
    /// below the Cp curve (not overlaid on the same axis) with a matching x mapping so
    /// leading/trailing edge line up vertically with the curve above it.
    /// </summary>
        private void RenderCpPlot(bool captureVectors = false)
        {
            int w = _ovrPlotW > 0 ? _ovrPlotW : (pCp?.Width ?? 0);
            int h = _ovrPlotH > 0 ? _ovrPlotH : (pCp?.Height ?? 0);
            if (w <= 0 || h <= 0)
                return;
            var bmp = new Bitmap(w, h);
            var tickFont = MakeTickFont();
            var tagFont = new Font("Consolas", 8.0f);
            var paramFont = new Font("Consolas", 9.5f);
            var titleFont = new Font("Consolas", 11.0f, FontStyle.Bold);
            var cpLabelFont = new Font("Consolas", 15.0f);
            var cpLabelSubFont = new Font("Consolas", 10.0f);
            string svgOut = "";
            string pdfOut = "";
            var whiteBrush = new SolidBrush(ThemeForeColor);
            var whitePen = new Pen(ThemeForeColor, 1f);
            var upperPen = new Pen(ThemeUpperColor, 1.5f);
            var lowerPen = new Pen(ThemeLowerColor, 1.5f);

            using (var g = new SvgGraphics(w, h, Graphics.FromImage(bmp), captureVectors))
            {
                g.FontScale = CurrentPlotFontScale;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(ThemeBackColor);
                ApplyPlotZoomTransform(g, pCp, captureVectors);

                if (_cpPoints.Count == 0)
                {
                    g.DrawString("Set a single alpha and click \"Run Point Analysis\" to compute the Cp distribution.", tickFont, whiteBrush, new PointF(10f, 10f));
                }
                else
                {
                    float marginX = 65f;
                    float topY = 40f;
                    float airfoilBandH = 90f;
                    float plotW = w - marginX - 20f;
                    float plotBottomY = h - airfoilBandH;

                    double xMin = double.MaxValue;
                    double xMax = double.MinValue;
                    double cpMin = double.MaxValue;
                    double cpMax = double.MinValue;
                    foreach (var pt in _cpPoints)
                    {
                        xMin = Math.Min(xMin, pt.X);
                        xMax = Math.Max(xMax, pt.X);
                        cpMin = Math.Min(cpMin, pt.Cp);
                        cpMax = Math.Max(cpMax, pt.Cp);
                        // The dashed inviscid reference can peak past the viscous curve; keep it
                        // inside the axis so it isn't clipped.
                        if (pt.CpInv.HasValue)
                        {
                            cpMin = Math.Min(cpMin, pt.CpInv.Value);
                            cpMax = Math.Max(cpMax, pt.CpInv.Value);
                        }
                    }
                    if (xMin == xMax)
                    {
                        xMin -= 1d;
                        xMax += 1d;
                    }

                    // XFOIL's default Cp axis is a fixed -2.0..1.0 range, only widened (in 0.5
                    // steps) if the data doesn't fit - not auto-fit tightly like the other plots.
                    double cpAxisMin = -2.0d;
                    double cpAxisMax = 1.0d;
                    while (cpMin < cpAxisMin)
                        cpAxisMin -= 0.5d;
                    while (cpMax > cpAxisMax)
                        cpAxisMax += 0.5d;

                    float mapX(double xv) => (float)((double)marginX + (xv - xMin) / (xMax - xMin) * (double)plotW);
                    float mapY(double cpv) => (float)((double)topY + (cpv - cpAxisMin) / (cpAxisMax - cpAxisMin) * (double)(plotBottomY - topY));

                    // Y axis: single vertical line with ticks/labels every 0.5, no border box.
                    g.DrawLine(whitePen, marginX, topY, marginX, plotBottomY);
                    double cpTick = cpAxisMin;
                    while (cpTick <= cpAxisMax + 0.001d)
                    {
                        float ty = mapY(cpTick);
                        g.DrawLine(whitePen, marginX - 4f, ty, marginX, ty);
                        // Guard against "-0.0" from floating-point drift landing just below zero.
                        double tickValue = Math.Abs(cpTick) < 0.001d ? 0.0d : cpTick;
                        string lbl = tickValue.ToString("0.0", CultureInfo.InvariantCulture);
                        var lblSize = g.MeasureString(lbl, tickFont);
                        g.DrawString(lbl, tickFont, whiteBrush, new PointF(marginX - 8f - lblSize.Width, ty - lblSize.Height / 2f));
                        cpTick += 0.5d;
                    }

                    // Horizontal Cp=0 line spans the full plot width with small unlabeled ticks
                    // every 0.1 x/c - XFOIL doesn't label the x-axis at all.
                    float zeroY = mapY(0.0d);
                    g.DrawLine(whitePen, marginX, zeroY, marginX + plotW, zeroY);
                    for (int i = 0; i <= 10; i++)
                    {
                        float tx = mapX(xMin + (xMax - xMin) * i / 10.0d);
                        g.DrawLine(whitePen, tx, zeroY - 3f, tx, zeroY + 3f);
                    }

                    // "Cp" axis label, left of the axis, vertically centered - "C" full size
                    // with a smaller subscript "p" offset down-right, same as the reference.
                    // Positioned in its own column further left than the tick labels so the
                    // two never collide regardless of how wide the widest tick label is.
                    float cpLabelY = (topY + plotBottomY) / 2.0f - 10f;
                    g.DrawString("C", cpLabelFont, whiteBrush, new PointF(6f, cpLabelY));
                    g.DrawString("p", cpLabelSubFont, whiteBrush, new PointF(6f + g.MeasureString("C", cpLabelFont).Width - 3f, cpLabelY + 15f));

                    // "XFOIL"/version tag, top-left corner of the axis.
                    g.DrawString("XFOIL", tagFont, whiteBrush, new PointF(marginX + 2f, topY - 32f));
                    g.DrawString("V 6.99", tagFont, whiteBrush, new PointF(marginX + 2f, topY - 20f));

                    // Parameter block, top-right: airfoil name + Re/alpha/CL/CM/CD/L/D/Ncrit,
                    // matching XFOIL's own CPX view.
                    DrawParamBlock(g, titleFont, paramFont, tagFont, whiteBrush, w - 190, 12f);

                    // Transition markers (from the last point-analysis run's console echo).
                    if (_lastTopXtr.HasValue || _lastBotXtr.HasValue)
                    {
                        var trPen = new Pen(Color.Gray, 1f) { DashStyle = DashStyle.Dot };
                        if (_lastTopXtr.HasValue)
                        {
                            float tx = mapX(_lastTopXtr.Value);
                            g.DrawLine(trPen, tx, topY, tx, plotBottomY);
                        }
                        if (_lastBotXtr.HasValue)
                        {
                            float bx = mapX(_lastBotXtr.Value);
                            g.DrawLine(trPen, bx, topY, bx, plotBottomY);
                        }
                    }

                    // Cp curve, inverted: cpAxisMax (most positive) maps to the BOTTOM. Points
                    // trace the airfoil surface as a loop (TE -> upper -> LE -> lower -> TE);
                    // split at the index of minimum x (the leading edge) so the upper and lower
                    // surface halves can be colored separately.
                    int cpSplitIdx = IndexOfMinX(_cpPoints);
                    var cpUpperPts = new List<PointF>();
                    for (int i = 0, loopTo = cpSplitIdx; i <= loopTo; i++)
                        cpUpperPts.Add(new PointF(mapX(_cpPoints[i].X), mapY(_cpPoints[i].Cp)));
                    if (cpUpperPts.Count >= 2)
                        g.DrawLines(upperPen, cpUpperPts.ToArray());
                    var cpLowerPts = new List<PointF>();
                    for (int i = cpSplitIdx, loopTo1 = _cpPoints.Count - 1; i <= loopTo1; i++)
                        cpLowerPts.Add(new PointF(mapX(_cpPoints[i].X), mapY(_cpPoints[i].Cp)));
                    if (cpLowerPts.Count >= 2)
                        g.DrawLines(lowerPen, cpLowerPts.ToArray());

                    // Inviscid Cp reference, dashed, overlaid on the viscous curves exactly as
                    // xfoil.exe's CPX view does for a viscous solution. Only present when CpInv
                    // was computed (viscous run); split upper/lower the same way.
                    if (_cpPoints.Count > 0 && _cpPoints[0].CpInv.HasValue)
                    {
                        using var invPen = new Pen(ThemeForeColor, 1f) { DashStyle = DashStyle.Dash };
                        var invUpperPts = new List<PointF>();
                        for (int i = 0; i <= cpSplitIdx; i++)
                            invUpperPts.Add(new PointF(mapX(_cpPoints[i].X), mapY(_cpPoints[i].CpInv.Value)));
                        if (invUpperPts.Count >= 2)
                            g.DrawLines(invPen, invUpperPts.ToArray());
                        var invLowerPts = new List<PointF>();
                        for (int i = cpSplitIdx, loopTo4 = _cpPoints.Count - 1; i <= loopTo4; i++)
                            invLowerPts.Add(new PointF(mapX(_cpPoints[i].X), mapY(_cpPoints[i].CpInv.Value)));
                        if (invLowerPts.Count >= 2)
                            g.DrawLines(invPen, invLowerPts.ToArray());
                    }

                    g.DrawString("upper", tickFont, new SolidBrush(ThemeUpperColor), new PointF(w - 90, topY - 32f));
                    g.DrawString("lower", tickFont, new SolidBrush(ThemeLowerColor), new PointF(w - 45, topY - 32f));

                    // Legend for the dashed inviscid reference (viscous run only). A short dashed
                    // swatch + label so the second curve is self-explanatory.
                    if (_cpPoints.Count > 0 && _cpPoints[0].CpInv.HasValue)
                    {
                        using var legPen = new Pen(ThemeForeColor, 1f) { DashStyle = DashStyle.Dash };
                        float legY = topY - 14f;
                        g.DrawLine(legPen, w - 90, legY + 6f, w - 66, legY + 6f);
                        g.DrawString("inviscid", tickFont, whiteBrush, new PointF(w - 62, legY));
                    }

                    // Airfoil shape, drawn in its own band below the Cp curve (not overlaid on
                    // it) with the same x mapping so LE/TE line up with the curve above, split
                    // upper/lower the same way as the Cp curve.
                    if (_airfoilCoords.Count >= 3)
                    {
                        double gXMin = double.MaxValue;
                        double gXMax = double.MinValue;
                        double gYMin = double.MaxValue;
                        double gYMax = double.MinValue;
                        foreach (var pt in _airfoilCoords)
                        {
                            gXMin = Math.Min(gXMin, pt.X);
                            gXMax = Math.Max(gXMax, pt.X);
                            gYMin = Math.Min(gYMin, pt.Y);
                            gYMax = Math.Max(gYMax, pt.Y);
                        }
                        if (gYMin == gYMax)
                        {
                            gYMin -= 0.05d;
                            gYMax += 0.05d;
                        }
                        float bandTop = plotBottomY + 15f;
                        float bandH = airfoilBandH - 25f;
                        double bandScale = Math.Min((double)plotW / (gXMax - gXMin), (double)bandH / (gYMax - gYMin)) * 0.95d;
                        float shapeH = (float)((gYMax - gYMin) * bandScale);
                        float bandOriginY = bandTop + (bandH - shapeH) / 2.0f;

                        PointF mapGeom(XfoilGeomPoint pt)
                        {
                            float gx = (float)((double)mapX(gXMin) + (pt.X - gXMin) * bandScale);
                            float gy = (float)((double)(bandOriginY + shapeH) - (pt.Y - gYMin) * bandScale);
                            return new PointF(gx, gy);
                        };

                        int geomSplitIdx = IndexOfMinX(_airfoilCoords);
                        var geomUpperPts = new List<PointF>();
                        for (int i = 0, loopTo2 = geomSplitIdx; i <= loopTo2; i++)
                            geomUpperPts.Add(mapGeom(_airfoilCoords[i]));
                        if (geomUpperPts.Count >= 2)
                            g.DrawLines(upperPen, geomUpperPts.ToArray());
                        var geomLowerPts = new List<PointF>();
                        for (int i = geomSplitIdx, loopTo3 = _airfoilCoords.Count - 1; i <= loopTo3; i++)
                            geomLowerPts.Add(mapGeom(_airfoilCoords[i]));
                        // Close the loop back to the first (TE) point.
                        if (geomUpperPts.Count > 0)
                            geomLowerPts.Add(geomUpperPts[0]);
                        if (geomLowerPts.Count >= 2)
                            g.DrawLines(lowerPen, geomLowerPts.ToArray());

                        // Boundary-layer / wake edge, drawn as a dashed line standing off the surface
                        // by the local displacement thickness (Dstar) - the same overlay the real
                        // xfoil.exe's own CPX view shows on its airfoil band. Only present for a
                        // VISCOUS solution (inviscid ALFA has no boundary layer, so nothing to draw
                        // here - matching xfoil.exe's .OPERi vs .OPERv behavior). Drawn a touch bolder
                        // so the displacement/wake line reads clearly against the solid surface.
                        if (_blTop.Count >= 2)
                        {
                            var edgePts = ComputeBlEdge(_blTop).ConvertAll(pt => mapGeom(pt));
                            g.DrawLines(new Pen(ThemeUpperColor, 1.6f) { DashStyle = DashStyle.Dash }, edgePts.ToArray());
                        }
                        if (_blBottom.Count >= 2)
                        {
                            var edgePts = ComputeBlEdge(_blBottom).ConvertAll(pt => mapGeom(pt));
                            g.DrawLines(new Pen(ThemeLowerColor, 1.6f) { DashStyle = DashStyle.Dash }, edgePts.ToArray());
                        }
                    }
                }

                if (captureVectors)
                {
                    svgOut = g.GetSvgContent();
                    pdfOut = g.GetPdfContentStream();
                }
            }

            if (_grabPlotMode)
                _grabbedPlot = bmp;
            else
                SetPlotBitmap(pCp, bmp);
            if (captureVectors)
            {
                _cpSvg = svgOut;
                _cpPdf = pdfOut;
            }
        }

        /// <summary>
    /// Offsets each boundary-layer point outward from the airfoil surface, along the
    /// local surface normal, by its displacement thickness (Dstar) - producing the
    /// "boundary-layer edge" curve XFOIL overlays on its own airfoil-band view. Points
    /// are re-sorted by x/c first since the BL dump's row order isn't guaranteed to be
    /// monotonic in x for either surface.
    /// </summary>
        private List<XfoilGeomPoint> ComputeBlEdge(List<XfoilBLPoint> points)
        {
            var ordered = new List<XfoilBLPoint>(points);
            ordered.Sort((a, b) => a.X.CompareTo(b.X));

            var result = new List<XfoilGeomPoint>();
            for (int i = 0, loopTo = ordered.Count - 1; i <= loopTo; i++)
            {
                double tX;
                double tY;
                if (ordered.Count == 1)
                {
                    tX = 1d;
                    tY = 0d;
                }
                else if (i == 0)
                {
                    tX = ordered[1].X - ordered[0].X;
                    tY = ordered[1].Y - ordered[0].Y;
                }
                else if (i == ordered.Count - 1)
                {
                    tX = ordered[i].X - ordered[i - 1].X;
                    tY = ordered[i].Y - ordered[i - 1].Y;
                }
                else
                {
                    tX = ordered[i + 1].X - ordered[i - 1].X;
                    tY = ordered[i + 1].Y - ordered[i - 1].Y;
                }
                double tLen = Math.Sqrt(tX * tX + tY * tY);
                if (tLen < 0.0000001d)
                    tLen = 1d;
                double nx = -tY / tLen;
                double ny = tX / tLen;
                // Orient the normal outward from the chord line: up for the upper surface,
                // down for the lower.
                if (ordered[i].Y >= 0d && ny < 0d || ordered[i].Y < 0d && ny > 0d)
                {
                    nx = -nx;
                    ny = -ny;
                }
                result.Add(new XfoilGeomPoint()
                {
                    X = ordered[i].X + nx * ordered[i].Dstar,
                    Y = ordered[i].Y + ny * ordered[i].Dstar
                });
            }
            return result;
        }

        /// <summary>
    /// Rounds a data range up to a "nice" axis tick step (1/2/5 x 10^n) targeting
    /// roughly targetTicks gridlines - the standard nice-numbers approach used by most
    /// charting libraries, so axis labels land on round values like 0.2/0.5/1.0 rather
    /// than whatever the raw data range happens to be.
    /// </summary>
        private double NiceStep(double range, int targetTicks)
        {
            if (range <= 0d)
                return 1d;
            double rawStep = range / targetTicks;
            double mag = Math.Pow(10d, Math.Floor(Math.Log10(rawStep)));
            double norm = rawStep / mag;
            double niceNorm;
            if (norm < 1.5d)
            {
                niceNorm = 1d;
            }
            else if (norm < 3d)
            {
                niceNorm = 2d;
            }
            else if (norm < 7d)
            {
                niceNorm = 5d;
            }
            else
            {
                niceNorm = 10d;
            }
            return niceNorm * mag;
        }

        /// <summary>
    /// Draws the two-row parameter block spanning the full plot width at the top of the
    /// BL plot, matching XFOIL's own VPLO output exactly: Ma/alpha/CL/top-transition on
    /// row 1, Re/Ncrit/CD/bottom-transition on row 2, with the top-surface transition
    /// tagged "T:" in yellow and the bottom-surface one tagged "B:" in cyan (XFOIL's own
    /// convention for distinguishing the two sides).
    /// </summary>
        private void DrawBlParamBlock(SvgGraphics g, Font titleFont, Font paramFont, Font subFont, SolidBrush brush, int w)
        {
            string airfoilName = txtAirfoil.Text.Trim();
            if (Regex.IsMatch(airfoilName, @"^\d{4,5}$"))
                airfoilName = "NACA " + airfoilName;
            g.DrawString(airfoilName.ToUpperInvariant(), titleFont, brush, new PointF(60f, 6f));

            float col1 = 60f;
            float col2 = w * 0.30f;
            float col3 = w * 0.50f;
            float col4 = w * 0.70f;
            float row1Y = 36f;
            float row2Y = 62f;

            void DrawSub2(string mainText, string subText, float x, float y, SolidBrush br)
            {
                g.DrawString(mainText, paramFont, br, new PointF(x, y));
                float tw = g.MeasureString(mainText, paramFont).Width;
                g.DrawString(subText, subFont, br, new PointF(x + tw - 1f, y + 6f));
            };
            void DrawRe(float x, float y)
            {
                g.DrawString("Re", paramFont, brush, new PointF(x, y));
                g.DrawString("=", paramFont, brush, new PointF(x + 45f, y));
                if (_lastPointRe.HasValue && _lastPointRe.Value > 0d)
                {
                    string reMain = (_lastPointRe.Value / 1000000.0d).ToString("0.000", CultureInfo.InvariantCulture) + "×10";
                    g.DrawString(reMain, paramFont, brush, new PointF(x + 65f, y));
                    float reW = g.MeasureString(reMain, paramFont).Width;
                    g.DrawString("6", subFont, brush, new PointF(x + 65f + reW - 1f, y - 3f));
                }
                else
                {
                    g.DrawString("inviscid", paramFont, brush, new PointF(x + 65f, y));
                }
            };
            void DrawXtr(string tag, double? value, float x, float y, Color color)
            {
                var br = new SolidBrush(color);
                g.DrawString(tag, paramFont, br, new PointF(x, y));
                g.DrawString("x", paramFont, br, new PointF(x + 24f, y));
                g.DrawString("tr", subFont, br, new PointF(x + 24f + g.MeasureString("x", paramFont).Width - 1f, y + 6f));
                g.DrawString("/c =", paramFont, br, new PointF(x + 47f, y));
                g.DrawString(value.HasValue ? value.Value.ToString("0.0000", CultureInfo.InvariantCulture) : "--", paramFont, br, new PointF(x + 98f, y));
            };

            // Row 1: Ma, alpha, CL, top-surface transition (yellow).
            g.DrawString("Ma", paramFont, brush, new PointF(col1, row1Y));
            g.DrawString("=", paramFont, brush, new PointF(col1 + 45f, row1Y));
            g.DrawString(_lastPointMach.HasValue ? _lastPointMach.Value.ToString("0.0000", CultureInfo.InvariantCulture) : "--", paramFont, brush, new PointF(col1 + 65f, row1Y));

            g.DrawString("α", paramFont, brush, new PointF(col2, row1Y));
            g.DrawString("=", paramFont, brush, new PointF(col2 + 40f, row1Y));
            g.DrawString(_lastPointAlpha.HasValue ? _lastPointAlpha.Value.ToString("0.0000", CultureInfo.InvariantCulture) + "°" : "--", paramFont, brush, new PointF(col2 + 60f, row1Y));

            DrawSub2("C", "L", col3, row1Y, brush);
            g.DrawString("=", paramFont, brush, new PointF(col3 + 30f, row1Y));
            g.DrawString(_lastPointCL.HasValue ? _lastPointCL.Value.ToString("0.0000", CultureInfo.InvariantCulture) : "--", paramFont, brush, new PointF(col3 + 50f, row1Y));

            DrawXtr("T:", _lastTopXtr, col4, row1Y, Color.Yellow);

            // Row 2: Re, Ncrit, CD, bottom-surface transition (cyan).
            DrawRe(col1, row2Y);

            DrawSub2("N", "cr", col2, row2Y, brush);
            g.DrawString("=", paramFont, brush, new PointF(col2 + 40f, row2Y));
            g.DrawString(_lastPointNcrit.HasValue ? _lastPointNcrit.Value.ToString("0.00", CultureInfo.InvariantCulture) : "--", paramFont, brush, new PointF(col2 + 60f, row2Y));

            DrawSub2("C", "D", col3, row2Y, brush);
            g.DrawString("=", paramFont, brush, new PointF(col3 + 30f, row2Y));
            g.DrawString(_lastPointCD.HasValue ? _lastPointCD.Value.ToString("0.00000", CultureInfo.InvariantCulture) : "--", paramFont, brush, new PointF(col3 + 50f, row2Y));

            DrawXtr("B:", _lastBotXtr, col4, row2Y, Color.Cyan);
        }

        /// <summary>
    /// Returns the requested single-quantity BL value for one point; index is aligned
    /// with _blQuantityNames / _blQuantityAxisMain / _blQuantityAxisSub and with
    /// cmbBlQuantity.SelectedIndex. Not valid for the DT/DB dual-quantity indices
    /// (BlQuantityIndexDT/DB) - RenderBlPlot handles those separately.
    /// Re_theta (RT/RTL) is computed rather than read directly: XFOIL's dump file has no
    /// Re_theta column, but Re_theta = Re_chord * (Ue/Vinf) * (Theta/c) - the same
    /// relation XFOIL itself uses internally - and all three factors on the right ARE in
    /// the dump, so this reconstructs it exactly rather than approximating it.
    /// </summary>
        private double BlQuantityValue(int index, XfoilBLPoint pt)
        {
            switch (index)
            {
                case 0:
                    {
                        return pt.H;
                    }
                case 1:
                    {
                        return pt.Ue;
                    }
                case 2:
                    {
                        return pt.Cf;
                    }
                case 5:
                    {
                        return (_lastPointRe.HasValue ? _lastPointRe.Value : 0.0d) * pt.Ue * pt.Theta;
                    }
                case 6:
                    {
                        double reTheta = (_lastPointRe.HasValue ? _lastPointRe.Value : 0.0d) * pt.Ue * pt.Theta;
                        return reTheta > 0d ? Math.Log10(reTheta) : 0.0d;
                    }
                case 7:
                    {
                        return pt.N; // amplification factor n (native only)
                    }
                case 8:
                    {
                        return pt.Ctau; // sqrt shear-stress coefficient (native only)
                    }
                case 9:
                    {
                        return pt.Cd; // dissipation coefficient (native only)
                    }

                default:
                    {
                        return 0.0d;
                    }
            }
        }

        /// <summary>
    /// Renders one boundary-layer quantity at a time (whichever is selected in
    /// cmbBlQuantity), full-size, styled to match the real xfoil.exe's own VPLO output
    /// exactly: a bordered grid with dashed gridlines and labeled ticks on both axes (the
    /// x-axis runs past x/c = 1 into the wake, since the BL dump includes wake stations),
    /// a two-row Ma/alpha/CL/Re/Ncrit/CD/transition parameter block spanning the full
    /// width, and colored upper/lower series.
    /// </summary>
        private void RenderBlPlot(bool captureVectors = false)
        {
            int w = _ovrPlotW > 0 ? _ovrPlotW : (pBl?.Width ?? 0);
            int h = _ovrPlotH > 0 ? _ovrPlotH : (pBl?.Height ?? 0);
            if (w <= 0 || h <= 0)
                return;
            var bmp = new Bitmap(w, h);
            var tickFont = MakeTickFont();
            var tagFont = new Font("Consolas", 8.0f);
            var paramFont = new Font("Consolas", 9.5f);
            var titleFont = new Font("Consolas", 11.0f, FontStyle.Bold);
            var yLabelFont = new Font("Consolas", 15.0f);
            var yLabelSubFont = new Font("Consolas", 10.0f);
            string svgOut = "";
            string pdfOut = "";
            var whiteBrush = new SolidBrush(ThemeForeColor);
            var whitePen = new Pen(ThemeForeColor, 1f);
            var gridPen = new Pen(ThemeGridColor, 1f) { DashStyle = DashStyle.Dash };

            int qIndex = cmbBlQuantity is not null ? cmbBlQuantity.SelectedIndex : 0;
            if (qIndex < 0)
                qIndex = 0;

            // DT/DB are XFOIL's own combined Dstar+Theta-vs-x plot for a SINGLE surface -
            // everything else here is a single quantity plotted across both surfaces.
            bool isDual = qIndex == BlQuantityIndexDT || qIndex == BlQuantityIndexDB;
            var dualPts = qIndex == BlQuantityIndexDT ? _blTop : _blBottom;

            using (var g = new SvgGraphics(w, h, Graphics.FromImage(bmp), captureVectors))
            {
                g.FontScale = CurrentPlotFontScale;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(ThemeBackColor);
                ApplyPlotZoomTransform(g, pBl, captureVectors);

                var allPts = new List<XfoilBLPoint>();
                if (isDual)
                {
                    allPts.AddRange(dualPts);
                }
                else
                {
                    allPts.AddRange(_blTop);
                    allPts.AddRange(_blBottom);
                }

                if (allPts.Count == 0)
                {
                    g.DrawString("Run a viscous (Re > 0) point analysis to compute boundary-layer data.", tickFont, whiteBrush, new PointF(10f, 10f));
                }
                else
                {
                    float marginX = 55f;
                    float topY = 92f;
                    float bottomMargin = 40f;
                    float plotW = w - marginX - 15f;
                    float plotBottomY = h - bottomMargin;

                    double xDataMin = double.MaxValue;
                    double xDataMax = double.MinValue;
                    double yDataMin = double.MaxValue;
                    double yDataMax = double.MinValue;
                    foreach (var pt in allPts)
                    {
                        xDataMin = Math.Min(xDataMin, pt.X);
                        xDataMax = Math.Max(xDataMax, pt.X);
                        if (isDual)
                        {
                            yDataMin = Math.Min(yDataMin, Math.Min(pt.Dstar, pt.Theta));
                            yDataMax = Math.Max(yDataMax, Math.Max(pt.Dstar, pt.Theta));
                        }
                        else
                        {
                            double yv = BlQuantityValue(qIndex, pt);
                            yDataMin = Math.Min(yDataMin, yv);
                            yDataMax = Math.Max(yDataMax, yv);
                        }
                    }
                    if (xDataMin == xDataMax)
                        xDataMax += 1d;
                    if (yDataMin == yDataMax)
                        yDataMax += 1d;

                    // Nice round axis bounds (always including zero as a baseline), so ticks
                    // land on values like 0.2/1.0 instead of the raw data range.
                    double xStep = NiceStep(xDataMax - Math.Min(0.0d, xDataMin), 7);
                    double xAxisMin = Math.Floor(Math.Min(0.0d, xDataMin) / xStep) * xStep;
                    double xAxisMax = Math.Ceiling(xDataMax / xStep) * xStep;
                    double yStep = NiceStep(yDataMax - Math.Min(0.0d, yDataMin), 6);
                    double yAxisMin = Math.Floor(Math.Min(0.0d, yDataMin) / yStep) * yStep;
                    double yAxisMax = Math.Ceiling(yDataMax / yStep) * yStep;

                    float mapX(double xv) => (float)((double)marginX + (xv - xAxisMin) / (xAxisMax - xAxisMin) * (double)plotW);
                    float mapY(double yv) => (float)((double)topY + (yAxisMax - yv) / (yAxisMax - yAxisMin) * (double)(plotBottomY - topY));

                    // Dashed gridlines + labeled ticks on both axes, inside a solid border box.
                    double xt = xAxisMin;
                    while (xt <= xAxisMax + xStep * 0.001d)
                    {
                        float tx = mapX(xt);
                        g.DrawLine(gridPen, tx, topY, tx, plotBottomY);
                        string lbl = xt.ToString("0.0", CultureInfo.InvariantCulture);
                        var lblSize = g.MeasureString(lbl, tickFont);
                        g.DrawString(lbl, tickFont, whiteBrush, new PointF(tx - lblSize.Width / 2f, plotBottomY + 4f));
                        xt += xStep;
                    }
                    double yt = yAxisMin;
                    while (yt <= yAxisMax + yStep * 0.001d)
                    {
                        float ty = mapY(yt);
                        g.DrawLine(gridPen, marginX, ty, marginX + plotW, ty);
                        string lbl = yt.ToString("0.0", CultureInfo.InvariantCulture);
                        var lblSize = g.MeasureString(lbl, tickFont);
                        g.DrawString(lbl, tickFont, whiteBrush, new PointF(marginX - 8f - lblSize.Width, ty - lblSize.Height / 2f));
                        yt += yStep;
                    }
                    g.DrawRectangle(whitePen, marginX, topY, plotW, plotBottomY - topY);

                    // "X" axis label, bottom-right corner under the last tick.
                    g.DrawString("X", tickFont, whiteBrush, new PointF(marginX + plotW - 6f, plotBottomY + 20f));

                    if (isDual)
                    {
                        // No single axis letter fits "Dstar and Theta together" - draw a small
                        // two-color legend instead, top-left inside the axis box.
                        g.DrawString("d*", MakeAxisFont(), new SolidBrush(Color.Orange), new PointF(marginX + 6f, topY + 4f));
                        g.DrawString("θ", MakeAxisFont(), new SolidBrush(Color.MediumOrchid), new PointF(marginX + 36f, topY + 4f));
                    }
                    else
                    {
                        // Quantity axis label, left of the axis, vertically centered - main
                        // character full size with a smaller subscript, same style as the Cp
                        // plot's "Cp" label (e.g. "H" + subscript "k" for the shape parameter).
                        float yLabelY = (topY + plotBottomY) / 2.0f - 10f;
                        g.DrawString(_blQuantityAxisMain[qIndex], yLabelFont, whiteBrush, new PointF(6f, yLabelY));
                        if (!string.IsNullOrEmpty(_blQuantityAxisSub[qIndex]))
                        {
                            g.DrawString(_blQuantityAxisSub[qIndex], yLabelSubFont, whiteBrush, new PointF(6f + g.MeasureString(_blQuantityAxisMain[qIndex], yLabelFont).Width - 3f, yLabelY + 15f));
                        }
                    }

                    // Two-row parameter block spanning the full width, matching XFOIL's own
                    // VPLO output (airfoil name, Ma/alpha/CL/top-transition, Re/Ncrit/CD/
                    // bottom-transition).
                    DrawBlParamBlock(g, titleFont, paramFont, tagFont, whiteBrush, w);

                    if (isDual)
                    {
                        DrawBlSeries(g, marginX, topY, plotW, plotBottomY - topY, xAxisMin, xAxisMax, yAxisMin, yAxisMax, dualPts, pt => pt.Dstar, Color.Orange);
                        DrawBlSeries(g, marginX, topY, plotW, plotBottomY - topY, xAxisMin, xAxisMax, yAxisMin, yAxisMax, dualPts, pt => pt.Theta, Color.MediumOrchid);
                    }
                    else
                    {
                        // Top/bottom curves use the same yellow/cyan convention as the T:/B:
                        // transition labels above (matching the real xfoil.exe's own VPLO colors).
                        DrawBlSeries(g, marginX, topY, plotW, plotBottomY - topY, xAxisMin, xAxisMax, yAxisMin, yAxisMax, _blTop, pt => BlQuantityValue(qIndex, pt), Color.Yellow);
                        DrawBlSeries(g, marginX, topY, plotW, plotBottomY - topY, xAxisMin, xAxisMax, yAxisMin, yAxisMax, _blBottom, pt => BlQuantityValue(qIndex, pt), Color.Cyan);
                    }
                }

                if (captureVectors)
                {
                    svgOut = g.GetSvgContent();
                    pdfOut = g.GetPdfContentStream();
                }
            }

            if (_grabPlotMode)
                _grabbedPlot = bmp;
            else
                SetPlotBitmap(pBl, bmp);
            if (captureVectors)
            {
                _blSvg = svgOut;
                _blPdf = pdfOut;
            }
        }

        private void DrawBlSeries(SvgGraphics g, float x, float y, float w, float h, double xMin, double xMax, double yMin, double yMax, List<XfoilBLPoint> points, Func<XfoilBLPoint, double> yOf, Color color)
        {
            if (points.Count < 2)
                return;
            var ordered = new List<XfoilBLPoint>(points);
            ordered.Sort((a, b) => a.X.CompareTo(b.X));

            var pen = new Pen(color, 1.5f);
            var pts = new List<PointF>();
            foreach (var pt in ordered)
            {
                float px = (float)((double)x + (pt.X - xMin) / (xMax - xMin) * (double)w);
                float py = (float)((double)(y + h) - (yOf(pt) - yMin) / (yMax - yMin) * (double)h);
                pts.Add(new PointF(px, py));
            }
            g.DrawLines(pen, pts.ToArray());
        }

        /// <summary>
    /// Standalone airfoil-shape view: the same _airfoilCoords used for the Cp overlay,
    /// drawn full-size with an aspect-ratio-correct (equal x/y scale) mapping so the
    /// airfoil isn't visually stretched.
    /// </summary>
        private void RenderGeometryPlot(bool captureVectors = false)
        {
            if (pGeom is null || pGeom.Width <= 0 || pGeom.Height <= 0)
                return;
            int w = pGeom.Width;
            int h = pGeom.Height;
            var bmp = new Bitmap(w, h);
            var tickFont = MakeTickFont();
            var axisFont = MakeAxisFont();
            string svgOut = "";
            string pdfOut = "";

            using (var g = new SvgGraphics(w, h, Graphics.FromImage(bmp), captureVectors))
            {
                g.FontScale = CurrentPlotFontScale;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(ThemeBackColor);
                ApplyPlotZoomTransform(g, pGeom, captureVectors);

                if (_airfoilCoords.Count < 3)
                {
                    g.DrawString("Enter an airfoil and click \"Load Airfoil\" (or run an analysis) to view its shape.", tickFont, Brushes.Gray, new PointF(10f, 10f));
                }
                else
                {
                    float marginX = 20f;
                    float marginY = 35f;
                    float plotW = w - marginX * 2f;
                    float plotH = h - marginY * 2f;

                    double xMin = double.MaxValue;
                    double xMax = double.MinValue;
                    double yMin = double.MaxValue;
                    double yMax = double.MinValue;
                    foreach (var pt in _airfoilCoords)
                    {
                        xMin = Math.Min(xMin, pt.X);
                        xMax = Math.Max(xMax, pt.X);
                        yMin = Math.Min(yMin, pt.Y);
                        yMax = Math.Max(yMax, pt.Y);
                    }
                    if (xMin == xMax)
                    {
                        xMin -= 1d;
                        xMax += 1d;
                    }
                    if (yMin == yMax)
                    {
                        yMin -= 1d;
                        yMax += 1d;
                    }

                    // Equal scale on both axes (aspect-ratio-correct), fit to the smaller dimension,
                    // then center the shape in the remaining space.
                    double scale = Math.Min((double)plotW / (xMax - xMin), (double)plotH / (yMax - yMin)) * 0.9d;
                    float shapeW = (float)((xMax - xMin) * scale);
                    float shapeH = (float)((yMax - yMin) * scale);
                    float originX = marginX + (plotW - shapeW) / 2.0f;
                    float originY = marginY + (plotH - shapeH) / 2.0f;

                    g.DrawString($"Airfoil geometry ({_airfoilCoords.Count} points)", axisFont, new SolidBrush(ThemeForeColor), new PointF(marginX, 8f));
                    g.DrawString("upper", tickFont, new SolidBrush(ThemeUpperColor), new PointF(w - 90, 10f));
                    g.DrawString("lower", tickFont, new SolidBrush(ThemeLowerColor), new PointF(w - 45, 10f));

                    PointF mapGeom(XfoilGeomPoint pt)
                    {
                        float px = (float)((double)originX + (pt.X - xMin) * scale);
                        // Flip Y: airfoil y increases upward, screen y increases downward.
                        float py = (float)((double)(originY + shapeH) - (pt.Y - yMin) * scale);
                        return new PointF(px, py);
                    };

                    // Split upper/lower the same way as the Cp plot's airfoil overlay.
                    int splitIdx = IndexOfMinX(_airfoilCoords);
                    var upperPts = new List<PointF>();
                    for (int i = 0, loopTo = splitIdx; i <= loopTo; i++)
                        upperPts.Add(mapGeom(_airfoilCoords[i]));
                    if (upperPts.Count >= 2)
                        g.DrawLines(new Pen(ThemeUpperColor, 1.5f), upperPts.ToArray());
                    var lowerPts = new List<PointF>();
                    for (int i = splitIdx, loopTo1 = _airfoilCoords.Count - 1; i <= loopTo1; i++)
                        lowerPts.Add(mapGeom(_airfoilCoords[i]));
                    if (upperPts.Count > 0)
                        lowerPts.Add(upperPts[0]); // close the loop back to TE
                    if (lowerPts.Count >= 2)
                        g.DrawLines(new Pen(ThemeLowerColor, 1.5f), lowerPts.ToArray());
                }

                if (captureVectors)
                {
                    svgOut = g.GetSvgContent();
                    pdfOut = g.GetPdfContentStream();
                }
            }

            SetPlotBitmap(pGeom, bmp);
            if (captureVectors)
            {
                _geomSvg = svgOut;
                _geomPdf = pdfOut;
            }
        }

        #endregion

        private void frmXfoilAnalysis_FormClosing(object sender, FormClosingEventArgs e)
        {
            Leaving = true;
            _logFlushTimer.Stop();
            try
            {
                if (p is not null && !p.HasExited)
                    p.Kill();
                p?.Dispose();
            }
            catch
            {
            }
        }

    }

    public class XfoilPolarPoint
    {
        public double Alpha, CL, CD, CDp, CM, TopXtr, BotXtr;
    }

    public class XfoilCpPoint
    {
        public double X, Cp;
        // Inviscid Cp at this node, drawn as xfoil.exe's dashed reference curve over a
        // viscous run. Null for inviscid-only runs (nothing to overlay).
        public double? CpInv;
    }

    public class XfoilGeomPoint
    {
        public double X, Y;
    }

    public class XfoilBLPoint
    {
        public double X, Y, Cf, H, Theta, Dstar, Ue;
        // Extra quantities available from the native engine (VPLO's N / CT / CD plots):
        // amplification factor, sqrt shear-stress coefficient, and dissipation coefficient.
        // Left at 0 for external xfoil.exe (its dump file doesn't contain them).
        public double N, Ctau, Cd;
    }

    /// <summary>
/// One completed polar sweep, shown as its own colored/labeled series so multiple
/// sweeps (different Re/Mach/airfoil) can be overlaid and compared in the Polar tab.
/// </summary>
    public class XfoilPolarRun
    {
        public string Label;
        public Color Color;
        public List<XfoilPolarPoint> Points = new List<XfoilPolarPoint>();
        public bool Visible = true;
    }
}