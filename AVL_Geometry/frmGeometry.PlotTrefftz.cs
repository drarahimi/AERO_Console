using AeroPlot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AERO_Console
{
    // Detached-window support for the AVL Trefftz (spanwise-loading) plot. Unlike the 3D
    // geometry view this is a static 2D plot, so the pop-out gets render + PNG/SVG/PDF export
    // + resize, but no rotate/zoom/fit (the plot already auto-scales to its data).
    public partial class frmGeometry
    {
        private frmPlotWindow _plotTrefftzWindow;

        /// <summary>Opens (or focuses) the detached, resizable Trefftz plot window. Co-exists
        /// with the docked Trefftz tab; both render from the same _lastTrefftzSurfaces data, so
        /// the analysis must already have been run (docked "Run Trefftz Plot" or the console "T").</summary>
        public void OpenTrefftzWindow()
        {
            if (_plotTrefftzWindow is not null && !_plotTrefftzWindow.IsDisposed)
            {
                _plotTrefftzWindow.Focus();
                _plotTrefftzWindow.RequestRender();
                return;
            }

            _plotTrefftzWindow = new frmPlotWindow(new DelegatePlotSource(
                "AVL Trefftz Plane - Spanwise Loading",
                render: (w, h, view) => PopoutRender(() => BuildTrefftzBitmap(w, h, view, false, out _, out _)),
                export: ExportTrefftz,
                setTheme: SetPopoutTheme, initialDark: IsDarkTheme, setFontScale: SetPopoutFontScale))
            {
                Icon = this.Icon,
            };
            _plotTrefftzWindow.FormClosed += (_, __) => _plotTrefftzWindow = null;
            _plotTrefftzWindow.Show(this);
        }

        /// <summary>Export handler for the detached Trefftz window's "Export ▾" menu: renders the
        /// plot at the window size and saves as PNG, SVG, or PDF (same writers as frmGeometry's
        /// docked Export button).</summary>
        public void ExportTrefftz(string format, int width, int height)
        {
            Bitmap bmp = null;
            _popoutRenderActive = true; // render the export at the pop-out's chosen font size
            try
            {
                bmp = BuildTrefftzBitmap(width, height, AeroPlot.PlotView.Identity, true, out var svg, out var pdf);

                if (format != "PNG" && string.IsNullOrEmpty(svg))
                {
                    AppMessageBox.Show("The view has not finished rendering. Please try again in a moment.",
                        "Export View", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using var sfd = new SaveFileDialog { Title = $"Export Trefftz Plot as {format}" };
                string projName = (projectName ?? "").Trim();
                if (string.IsNullOrEmpty(projName) || projName.Contains("Enter AVL Project") || projName.Contains("Enter NACA"))
                {
                    sfd.FileName = "Trefftz_Loading";
                }
                else
                {
                    foreach (var c in Path.GetInvalidFileNameChars())
                        projName = projName.Replace(c, '_');
                    sfd.FileName = $"{projName}_Trefftz_Loading";
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
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Error exporting file: " + ex.Message, "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _popoutRenderActive = false;
                bmp?.Dispose();
            }
        }

        /// <summary>Native-AVL console "T" (OPER Trefftz plot): compute the Trefftz loading from
        /// the CURRENT solved state -- matching avl.exe's "T" semantics -- then pop the interactive
        /// window. Sends only "ft"/"fs" to the live session (no load/oper/x), so the user's console
        /// stays exactly where it was in the OPER menu.</summary>
        public async void ShowConsoleTrefftzPlot(string avlName, string avlText)
        {
            try
            {
                LoadConsoleGeometryInto(avlName, avlText);

                if (!My.MyProject.Forms.frmMain.EngineAlive)
                    return;

                var surfaces = await RunTrefftzFsLiveAsync();
                _lastTrefftzSurfaces = surfaces;
                _lastTrefftzCref = GetCrefFromAvlText(txt3.Text);
                _lastTrefftzTotals = ParseTotalForces(_lastTrefftzLog);
                RenderTrefftzPlot();

                if ((surfaces is null || surfaces.Count == 0)
                    && (_lastTrefftzLog ?? "").Contains("Execute flow calculation"))
                {
                    AppToast.Show("Run the case first: type 'x' in OPER, then 'T'.");
                }
            }
            catch
            {
            }
            OpenTrefftzWindow();
        }

        /// <summary>Writes the current run case's Trefftz strip forces to a temp file by sending
        /// "ft" (totals, for the plot header) then "fs" to the ALREADY-RUNNING OPER session, and
        /// parses it. Non-destructive: no load/oper/x and no trailing blank, so the session stays
        /// in OPER. Returns empty if the case hasn't been executed ("fs" needs a solved state).</summary>
        private async Task<List<TrefftzSurface>> RunTrefftzFsLiveAsync()
        {
            string outFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _lastTrefftzLog = "";
            if (!My.MyProject.Forms.frmMain.EngineAlive)
                return new List<TrefftzSurface>();

            int logLengthBefore = My.MyProject.Forms.frmMain.txtLog.Text.Length;
            try
            {
                var wb = My.MyProject.Forms.frmMain;
                // "ft" and "fs" each prompt "Enter filename, or <return> for screen output" -- so a
                // bare "ft" would otherwise swallow the next "fs" as its filename. Answer ft's prompt
                // with a blank line (print totals to the log for the plot header), then fs -> file.
                wb.EngineSendLine("ft");
                wb.EngineSendLine("");          // <return> => FT totals to screen/log
                wb.EngineSendLine("fs");        // strip forces...
                wb.EngineSendLine(outFile);     // ...to this file
                wb.EngineFlush();
            }
            catch
            {
                return new List<TrefftzSurface>();
            }

            var deadline = DateTime.Now.AddSeconds(10d);
            long lastLen = -1;
            do
            {
                await Task.Delay(100);
                try
                {
                    if (File.Exists(outFile))
                    {
                        long len = new FileInfo(outFile).Length;
                        if (len > 0L && len == lastLen)
                            break;
                        lastLen = len;
                    }
                }
                catch
                {
                }
            }
            while (DateTime.Now < deadline);

            await Task.Delay(150); // let frmMain's batched log-flush catch up
            try
            {
                string logText = My.MyProject.Forms.frmMain.txtLog.Text;
                if (logLengthBefore <= logText.Length)
                    _lastTrefftzLog = logText.Substring(logLengthBefore);
            }
            catch
            {
            }

            if (!File.Exists(outFile))
                return new List<TrefftzSurface>();

            try { _lastTrefftzRawFile = File.ReadAllText(outFile); } catch { }

            try
            {
                return ParseTrefftzStripFile(outFile);
            }
            catch
            {
                return new List<TrefftzSurface>();
            }
            finally
            {
                try { File.Delete(outFile); } catch { }
            }
        }
    }
}
